using BookingAppV2.Connection;
using BookingAppV2.Models;
using BookingAppV2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace BookingAppV2.Controllers
{
  public class UserBookingController : BaseController
  {
    private readonly dbAccess _dbAccess;

    public UserBookingController(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }
    public ActionResult Index()
    {
      string? role = HttpContext.Session.GetString("Role");
      string? deptID = HttpContext.Session.GetString("DepartmentID");

      if (string.IsNullOrEmpty(role))
        return RedirectToAction("Index", "Login");

      string query;
      List<OleDbParameter>? parameters = null;

      if (role == "Users")
      {
        // NORMAL USER → department-only
        if (string.IsNullOrEmpty(deptID))
          return RedirectToAction("Index", "Login");

        query = @"
            SELECT B.bookingID, I.item_name, D.departmentName, B.userID,
                   B.quantity, B.purpose, B.date_requested, B.date_returned,
                   B.status, B.remarks
            FROM ((BookingTrans AS B
            INNER JOIN Items AS I ON B.itemID = I.itemID)
            INNER JOIN Department AS D ON B.departmentID = D.departmentID)
            WHERE B.departmentID = ?
             and  B.status = 'Pending'
            ORDER BY B.bookingID DESC";

        parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", deptID)
        };
      }
      else
      {
        // ADMIN / 
        query = @"
            SELECT B.bookingID, I.item_name, D.departmentName, B.userID,
                   B.quantity, B.purpose, B.date_requested, B.date_returned,
                   B.status, B.remarks
            FROM ((BookingTrans AS B
            INNER JOIN Items AS I ON B.itemID = I.itemID)
            INNER JOIN Department AS D ON B.departmentID = D.departmentID)
            ORDER BY B.bookingID DESC";
      }

      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);
      return View(dt);
    }

    // GET: Users/Create
    // GET: UserBooking/Create
    // GET: UserBooking/Create
    public ActionResult Create()
    {
      // Populate Items and Booking Status dropdowns
      ViewBag.Items = GetItems();
      ViewBag.Statuses = GetBookingStatus();
      //ViewBag.Statuses = "Pending";

      string? role = HttpContext.Session.GetString("Role");

      string? sessionDeptId = HttpContext.Session.GetString("DepartmentID");
      string? sessionUserId = HttpContext.Session.GetString("UserID");

      if (role == "Superadmin" || role == "Admin")
      {
        ViewBag.Departments = GetDepartments();
        ViewBag.Users = GetUsers();
      }
      else
      {
        // NORMAL USER → single fixed values
        ViewBag.Departments = GetDepartments()
            .Where(d => d.Value == sessionDeptId)
            .ToList();

        ViewBag.Users = GetUsers()
            .Where(u => u.Value == sessionUserId)
            .ToList();
      }

      // Optionally: pre-select the logged-in user's department and ID
      string? userDeptID = sessionDeptId;
      string? userID = sessionUserId;

      if (!string.IsNullOrEmpty(userDeptID))
        ViewBag.SelectedDepartmentID = userDeptID;

      if (!string.IsNullOrEmpty(userID))
        ViewBag.SelectedUserID = userID;

      return View();
    }


    // Safe DB query helpers
    private string GetDepartmentName(string departmentID)
    {
      if (string.IsNullOrEmpty(departmentID)) return "Unknown";

      string query = "SELECT departmentName FROM Department WHERE departmentID = ?";
      var dt = _dbAccess.ExecuteQueryBooking(query, new List<OleDbParameter> { new OleDbParameter("?", departmentID) });
      return dt.Rows.Count > 0 ? (dt.Rows[0]["departmentName"]?.ToString() ?? "Unknown") : "Unknown";
    }

    private string GetUserName(string userID)
    {
      if (string.IsNullOrEmpty(userID)) return "Unknown";

      string query = "SELECT user_name FROM Users WHERE userID = ?";
      var dt = _dbAccess.ExecuteQueryBooking(query, new List<OleDbParameter> { new OleDbParameter("?", userID) });
      return dt.Rows.Count > 0 ? (dt.Rows[0]["user_name"]?.ToString() ?? "Unknown") : "Unknown";
    }


    // GET: UserBooking/Create
    public ActionResult BookingCreate()
    {
      // Populate Items
      string itemQuery = "SELECT itemID, item_name FROM Items";
      var itemTable = _dbAccess.ExecuteQueryBooking(itemQuery, null);
      ViewBag.Items = itemTable.AsEnumerable()
                               .Select(r => new SelectListItem
                               {
                                 Value = r["itemID"]?.ToString() ?? "",
                                 Text = r["item_name"]?.ToString() ?? ""
                               });

      // Populate Departments            
      string deptQuery = @"Select departmentID from Users where userID = ?";
      var deptTable = _dbAccess.ExecuteQueryBooking(deptQuery, null);
      ViewBag.Departments = deptTable.AsEnumerable()
                                     .Select(r => new SelectListItem
                                     {
                                       Value = r["departmentID"]?.ToString() ?? ""
                                     });

      // Populate Users (optional: could filter by department)
      string userQuery = "SELECT userID, user_name FROM Users";
      var userTable = _dbAccess.ExecuteQueryBooking(userQuery, null);
      ViewBag.Users = userTable.AsEnumerable()
                               .Select(r => new SelectListItem
                               {
                                 Value = r["userID"]?.ToString() ?? "",
                                 Text = r["user_name"]?.ToString() ?? ""
                               });

      // Optionally, you can prefill the current logged-in user
      ViewBag.CurrentUserID = HttpContext.Session.GetString("UserID");
      return View();
    }



    // POST: Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Booking model)
    {
      string? role = HttpContext.Session.GetString("Role");
      string? sessionUserID = HttpContext.Session.GetString("UserID");
      string? sessionDeptID = HttpContext.Session.GetString("DepartmentID");

      if (string.IsNullOrEmpty(sessionUserID) || string.IsNullOrEmpty(sessionDeptID))
        return RedirectToAction("Index", "Login");

      // 🔒 Force values for NORMAL USERS
      if (role == "Users")
      {
        model.userID = sessionUserID!;
        model.departmentID = sessionDeptID!;
        model.status = GetBookingStatus().First().Value; // "Pending"
      }

      // ===================== STOCK CHECK ADDED HERE =====================
      int available = GetAvailableStock(model.itemID);

      if (model.quantity > available)
      {
        TempData["Error"] =
            "No available units. All are currently borrowed. Contact IT Asset team.";
        return RedirectToAction("Index");
      }
      // ===================== END STOCK CHECK =====================


      string query = @"
        INSERT INTO BookingTrans
        (itemID, departmentID, userID, quantity, purpose, [date_requested], [status], remarks)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

      var parameters = new List<OleDbParameter>
    {
        new OleDbParameter("?", model.itemID), // string
        new OleDbParameter("?", model.departmentID), // string
        new OleDbParameter("?", model.userID), // string
        new OleDbParameter("?", model.quantity), // integer
        new OleDbParameter("?", string.IsNullOrEmpty(model.purpose) ? "" : model.purpose), // string
        new OleDbParameter("?", model.date_requested.HasValue ? (object)model.date_requested.Value : DBNull.Value), // Date
        new OleDbParameter("?", model.status ?? "Pending"), // string
        new OleDbParameter("?", string.IsNullOrEmpty(model.remarks) ? "" : model.remarks) // string
    };


      _dbAccess.ExecuteNonQueryBooking(query, parameters);
      return RedirectToAction("Index");
    }


    // Helper: fetch single booking


    // GET: Booking/Edit/5
    public ActionResult Edit(int id)
    {
      var booking = GetBookingById(id);
      if (booking == null) return NotFound();

      // ===================== ADDED =====================
      // Block USERS from accessing Edit via URL if status is final
      string? sessionRole = HttpContext.Session.GetString("Role");
      if (sessionRole == "Users" &&
          (booking.status == "Approved" ||
           booking.status == "Returned" ||
           booking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot edit this booking because it is already finalized.";
        return RedirectToAction("Index");
      }
      // =================== END ADDED ===================


      // Populate Items and Booking Status dropdowns
      ViewBag.Items = GetItems();
      ViewBag.Statuses = GetBookingStatus();

      string? role = HttpContext.Session.GetString("Role");
      string? sessionDeptId = HttpContext.Session.GetString("DepartmentID");
      string? sessionUserId = HttpContext.Session.GetString("UserID");

      if (role == "Superadmin" || role == "Admin")
      {
        // Admin sees all departments and users
        ViewBag.Departments = GetDepartments();
        ViewBag.Users = GetUsers();
      }
      else
      {
        // Normal user → restrict dropdowns to their own department & themselves
        ViewBag.Departments = GetDepartments()
            .Where(d => d.Value == sessionDeptId)
            .ToList();

        ViewBag.Users = GetUsers()
            .Where(u => u.Value == sessionUserId)
            .ToList();
      }

      // Pre-select values for the dropdowns
      ViewBag.SelectedDepartmentID = booking.departmentID;
      ViewBag.SelectedUserID = booking.userID;
      return View(booking);
    }

    // POST: Booking/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Booking model)
    {
      string? role = HttpContext.Session.GetString("Role");
      string? sessionUserID = HttpContext.Session.GetString("UserID");
      string? sessionDeptID = HttpContext.Session.GetString("DepartmentID");
      var existingBooking = GetBookingById(model.bookingid);

      // ===================== ADDED =====================
      // Final server-side protection (even if user bypasses UI or uses Postman)
      if (role == "Users" &&
          (existingBooking.status == "Approved" ||
           existingBooking.status == "Returned" ||
           existingBooking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot edit this booking because it is already finalized.";
        return RedirectToAction("Index");
      }
      // =================== END ADDED ===================


      // 🔒 Force values for NORMAL USERS
      if (role == "Users")
      {
        model.userID = sessionUserID!;
        model.departmentID = sessionDeptID!;
        model.status = GetBookingStatus().First().Value;
      }
      if (ModelState.IsValid)
      {
        var parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", model.itemID),
            new OleDbParameter("?", model.departmentID ?? ""),  // if short text
            new OleDbParameter("?", model.userID),
            new OleDbParameter("?", model.quantity),
            new OleDbParameter("?", string.IsNullOrEmpty(model.purpose) ? "" : model.purpose),
            new OleDbParameter("?", model.date_requested.HasValue ? (object)model.date_requested.Value : DBNull.Value),
            new OleDbParameter("?", model.date_returned.HasValue ? (object)model.date_returned.Value : DBNull.Value),
            //new OleDbParameter("?", model.status ?? ""),
            new OleDbParameter("?", model.bookingid)
        };

        string query = @"UPDATE BookingTrans 
                         SET itemID = ?, departmentID = ?, userID = ?, quantity = ?,purpose = ?, date_requested = ?, 
                              date_returned = ?
                         WHERE bookingID = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);

        return RedirectToAction("Index");
      }

      // Repopulate dropdowns in case of validation errors
      ViewBag.Items = GetItems();
      ViewBag.Statuses = GetBookingStatus();

      if (role == "Superadmin" || role == "Admin")
      {
        ViewBag.Departments = GetDepartments();
        ViewBag.Users = GetUsers();
      }
      else
      {
        ViewBag.Departments = GetDepartments()
            .Where(d => d.Value == sessionDeptID)
            .ToList();

        ViewBag.Users = GetUsers()
            .Where(u => u.Value == sessionUserID)
            .ToList();
      }

      return View(model);
    }

    // GET: Booking/Delete/5
    public ActionResult Delete(int id)
    {
      var booking = GetBookingById(id);
      if (booking == null) return NotFound();

      // ===================== ADDED =====================
      // Block USERS from accessing Delete via URL if status is final
      string? sessionRole = HttpContext.Session.GetString("Role");
      if (sessionRole == "Users" &&
          (booking.status == "Approved" ||
           booking.status == "Returned" ||
           booking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot delete this booking because it is already finalized.";
        return RedirectToAction("Index");
      }
      // =================== END ADDED =================

      return View(booking);
    }

    // POST: Booking/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)
    {

      // Users cannot delete approved bookings
      var existingBooking = GetBookingById(id);
      if (existingBooking == null)
      {
        TempData["Error"] = "Booking not found.";
        return RedirectToAction("Index");
      }

      // Users cannot delete approved bookings
      string? sessionRole = HttpContext.Session.GetString("Role");
      if (sessionRole == "Users" &&
         (existingBooking.status == "Approved" ||
          existingBooking.status == "Returned" ||
          existingBooking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot delete this booking if status is Approved, Returned, Disapproved.";
        return RedirectToAction("Index");
      }


      var parameters = new List<OleDbParameter> { new OleDbParameter("?", id) };
      string query = "DELETE FROM BookingTrans WHERE bookingID = ?";
      _dbAccess.ExecuteNonQueryBooking(query, parameters);
      return RedirectToAction("Index");
    }

    private Users? GetUserById(string id)
    {
      string query = @"
        SELECT U.*, D.DepartmentName
        FROM Users U
        LEFT JOIN Department D ON U.DepartmentID = D.DepartmentID
        WHERE U.userID = ?";

      var parameters = new List<OleDbParameter>
    {
        new OleDbParameter("?", id)
    };

      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);
      if (dt.Rows.Count == 0) return null;

      var row = dt.Rows[0];
      return new Users
      {
        userID = row["userID"]?.ToString() ?? "",
        pass_word = row["pass_word"]?.ToString() ?? "",
        role = row["role"]?.ToString() ?? "",
        DepartmentID = row["DepartmentID"]?.ToString() ?? "",
        DepartmentName = row["DepartmentName"]?.ToString() ?? ""
      };
    }



    // Helper: get roles for dropdown
    private List<SelectListItem> GetRoles()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "Superadmin", Value = "Superadmin" },
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "Users", Value = "Users" }
            };
    }


    private Booking? GetBookingById(int id)
    {
      string query = "SELECT * FROM BookingTrans WHERE bookingID = ?";
      var parameters = new List<OleDbParameter> { new OleDbParameter("?", id) };
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);

      if (dt.Rows.Count == 0) return null;

      var row = dt.Rows[0];
      return new Booking
      {
        bookingid = Convert.ToInt32(row["bookingID"]),
        itemID = row["itemID"]?.ToString() ?? "",          // <-- string now
        departmentID = row["departmentID"]?.ToString() ?? "", // <-- string now
        userID = row["userID"]?.ToString() ?? "",         // <-- string now
        quantity = Convert.ToInt32(row["quantity"]),
        purpose = row["purpose"]?.ToString() ?? "",
        date_requested = row["date_requested"] != DBNull.Value ? (DateTime?)row["date_requested"] : null,
        //date_borrowed = row["date_borrowed"] != DBNull.Value ? (DateTime?)row["date_borrowed"] : null,       
        status = row["status"]?.ToString() ?? ""
      };
    }

    // Helper: dropdown lists
    private List<SelectListItem> GetItems()
    {
      string query = "SELECT itemID, item_name FROM Items";
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, null);
      var list = new List<SelectListItem>();
      foreach (DataRow row in dt.Rows)
      {
        list.Add(new SelectListItem
        {
          Text = row["item_name"]?.ToString() ?? "",
          Value = row["itemID"]?.ToString() ?? ""
        });
      }
      return list;
    }

    private List<SelectListItem> GetDepartments()
    {
      string query = "SELECT departmentID, departmentName FROM Department ";
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, null);
      var list = new List<SelectListItem>();
      foreach (DataRow row in dt.Rows)
      {
        list.Add(new SelectListItem
        {
          Text = row["departmentName"]?.ToString() ?? "",
          Value = row["departmentID"]?.ToString() ?? ""
        });
      }
      return list;
    }

    private List<SelectListItem> GetUsers()
    {
      string query = "SELECT userID, userID FROM Users"; // Use userID or username
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, null);
      var list = new List<SelectListItem>();
      foreach (DataRow row in dt.Rows)
      {
        list.Add(new SelectListItem
        {
          Text = row["userID"]?.ToString() ?? "",
          Value = row["userID"]?.ToString() ?? ""
        });
      }
      return list;
    }

    private List<SelectListItem> GetBookingStatus()
    {
      return new List<SelectListItem>
    {
        new SelectListItem { Text = "Pending", Value = "Pending" }
    };
    }

    //for refresh rows of booking
    [HttpGet]
    public JsonResult GetLatestBookings()
    {
      string? role = HttpContext.Session.GetString("Role");
      string? deptID = HttpContext.Session.GetString("DepartmentID");

      if (string.IsNullOrEmpty(role))
        return Json(new List<object>());

      string query;
      List<OleDbParameter>? parameters = null;

      if (role == "Users")
      {
        // 🔒 NORMAL USER → department only
        query = @"
            SELECT 
                b.bookingId, 
                i.item_name, 
                d.departmentName, 
                u.userID AS username, 
                b.quantity, 
                b.purpose,
                b.date_requested, 
                b.date_returned, 
                b.status, 
                b.remarks
            FROM ((BookingTrans b
            INNER JOIN Items i ON b.itemID = i.itemID)
            INNER JOIN Department d ON b.departmentid = d.departmentid)
            INNER JOIN Users u ON b.userID = u.userID
            WHERE b.departmentID = ?
             and  B.status = 'Pending'
            ORDER BY b.bookingId DESC";

        parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", deptID)
        };
      }
      else
      {
        // 🔓 ADMIN / TREASURY → ALL
        query = @"
            SELECT 
                b.bookingId, 
                i.item_name, 
                d.departmentName, 
                u.userID AS username, 
                b.quantity, 
                b.purpose,
                b.date_requested, 
                b.date_returned, 
                b.status, 
                b.remarks
            FROM ((BookingTrans b
            INNER JOIN Items i ON b.itemID = i.itemID)
            INNER JOIN Department d ON b.departmentid = d.departmentid)
            INNER JOIN Users u ON b.userID = u.userID
            ORDER BY b.bookingId DESC";
      }

      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);

      var list = new List<object>();

      foreach (DataRow row in dt.Rows)
      {
        list.Add(new
        {
          bookingID = row["bookingID"],
          item = row["item_name"],
          department = row["departmentName"],
          user = row["username"],
          quantity = row["quantity"],
          purpose = row["purpose"],
          dateRequested = row["date_requested"] != DBNull.Value
                ? ((DateTime)row["date_requested"]).ToString("MM-dd-yyyy hh:mm tt")
                : "",
          dateReturned = row["date_returned"] != DBNull.Value
                ? ((DateTime)row["date_returned"]).ToString("MM-dd-yyyy")
                : "",
          status = row["status"],
          remarks = row["remarks"]
        });
      }

      return Json(list);
    }


    // ===================== NEW METHOD =====================
    // Calculates the remaining available stock for an item
    private int GetAvailableStock(string itemID)
    {
      // 1. Get total stock
      string stockQuery = "SELECT total_stock FROM Items WHERE itemID = ?";
      var stockDt = _dbAccess.ExecuteQueryBooking(stockQuery,
          new List<OleDbParameter> { new OleDbParameter("?", itemID) });

      if (stockDt.Rows.Count == 0)
        return 0;

      int totalStock = Convert.ToInt32(stockDt.Rows[0]["total_stock"]);

      // 2. Borrowed + Pending
      string borrowedQuery = @"
        SELECT SUM(quantity) AS borrowed
        FROM BookingTrans
        WHERE itemID = ?
        AND status IN ('Approved', 'Pending')";

      var borrowDt = _dbAccess.ExecuteQueryBooking(borrowedQuery,
          new List<OleDbParameter> { new OleDbParameter("?", itemID) });

      int borrowedQty = 0;
      if (borrowDt.Rows.Count > 0 && borrowDt.Rows[0]["borrowed"] != DBNull.Value)
        borrowedQty = Convert.ToInt32(borrowDt.Rows[0]["borrowed"]);

      return totalStock - borrowedQty;
    }

    // ================= END NEW METHOD =====================





    //protected override void OnActionExecuting(ActionExecutingContext filterContext)
    //{
    //    if (!IsAdmin())
    //    {
    //        filterContext.Result = new RedirectToRouteResult(
    //            new RouteValueDictionary(
    //                new { controller = "Login", action = "Index" }
    //            )
    //        );
    //        return;
    //    }

    //    base.OnActionExecuting(filterContext);
    //}

  }
}
