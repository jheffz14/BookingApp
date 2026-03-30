using BookingAppV2.Connection;
using BookingAppV2.Controllers;
using BookingAppV2.Helpers;
using BookingAppV2.Models;
using BookingAppV2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;

namespace BookingAppV2.Controllers
{
  public class UserBookingController : BaseController
  {
    private readonly dbAccess _dbAccess;
    private readonly UsersService _UsersService;
    private readonly BookingService _BookingService;
    private readonly StockService _StockService;
    private readonly ItemService _ItemService;
    private readonly DepartmentService _DepartmentService;
    private readonly GetRolesUsers _GetRolesUsers;
    private readonly GetUserBookingStatus _GetUserBookingStatus;


    public UserBookingController(dbAccess dbAccess,
                                 UsersService usersService,
                                 BookingService bookingService,
                                 StockService stockService,
                                 ItemService itemService,
                                 DepartmentService departmentService,
                                 GetRolesUsers getRoles,
                                 GetUserBookingStatus getUserBookingStatus)
    {
      _dbAccess = dbAccess;
      _UsersService = usersService;
      _BookingService = bookingService;
      _StockService = stockService;
      _ItemService = itemService;
      _DepartmentService = departmentService;
      _GetRolesUsers = getRoles;
      _GetUserBookingStatus = getUserBookingStatus;
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      base.OnActionExecuting(filterContext);
      if (filterContext.Result != null) return;

      if (!IsUsers() && !IsAdmin() && !IsSuperAdmin())
      {
        filterContext.Result = RedirectToAction("Index", "AccessDenied");
        return;
      }
    }
    public ActionResult Index()
    {
      string? role = HttpContext.Session.GetString("role");
      string? deptID = HttpContext.Session.GetString("Departmentid");

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
            WHERE B.status = 'Pending'
            ORDER BY B.bookingID DESC";
      }
      ViewBag.Items = _ItemService.GetItems(); // ✅ ADD HERE before return
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);
      return View(dt);
    }

    // GET: Users/Create
    // GET: UserBooking/Create
    // GET: UserBooking/Create
    public ActionResult Create()
    {
      // Populate Items and Booking Status dropdowns
      ViewBag.Items = _ItemService.GetItems();
      string? role = HttpContext.Session.GetString("role");
      ViewBag.Statuses = _GetUserBookingStatus.GetBookingStatusList(role ?? "");
      //ViewBag.Statuses = "Pending";
      string? sessionDeptId = HttpContext.Session.GetString("Departmentid");
      string? sessionUserId = HttpContext.Session.GetString("userID");

      if (role == "Superadmin" || role == "Admin")
      {
        ViewBag.Departments = _DepartmentService.GetDepartments();
        ViewBag.Users = _UsersService.GetUsers();
      }
      else
      {
        // NORMAL USER → single fixed values
        ViewBag.Departments = _DepartmentService.GetDepartments()
            .Where(d => d.Value == sessionDeptId)
            .ToList();

        ViewBag.Users = _UsersService.GetUsers()
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
      ViewBag.CurrentUserID = HttpContext.Session.GetString("userID");
      return View();
    }



    // POST: Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Booking model)
    {
      string? role = HttpContext.Session.GetString("role");
      string? sessionUserID = HttpContext.Session.GetString("userID");
      string? sessionDeptID = HttpContext.Session.GetString("Departmentid");

      if (string.IsNullOrEmpty(sessionUserID) || string.IsNullOrEmpty(sessionDeptID))
        return RedirectToAction("Index", "Login");

      // 🔒 Force values for NORMAL USERS
      if (role == "Users")
      {
        model.userID = sessionUserID!;
        model.departmentID = sessionDeptID!;
        model.status = "Pending";
      }

      // ===================== STOCK CHECK ADDED HERE =====================
      int available = _StockService.GetAvailableStock(model.itemID);

      //if (model.quantity > available)
      //{
      //  TempData["Error"] =
      //      "No available units. All are currently borrowed. Contact IT Asset team.";
      //  return RedirectToAction("Index");
      //}
      if (model.quantity > available)
      {
        TempData["Error"] = available == 0
            ? $"No units available. All are currently borrowed. Contact IT Asset team."
            : $"Only {available} unit(s) available. You requested {model.quantity}. Please reduce your quantity.";
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
     
      var booking = _BookingService.GetBookingById(id);
      if (booking == null) return NotFound();

      // ===================== ADDED =====================
      // Block USERS from accessing Edit via URL if status is final
      string? sessionRole = HttpContext.Session.GetString("role");
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
      ViewBag.Items = _ItemService.GetItems();
      string? role = HttpContext.Session.GetString("role");
      ViewBag.Statuses = _GetUserBookingStatus.GetBookingStatusList(role ?? "");
      string? sessionDeptId = HttpContext.Session.GetString("Departmentid");
      string? sessionUserId = HttpContext.Session.GetString("userID");

      if (role == "Superadmin" || role == "Admin")
      {
        // Admin sees all departments and users
        ViewBag.Departments = _DepartmentService.GetDepartments();
        ViewBag.Users = _UsersService.GetUsers();
      }
      else
      {
        // Normal user → restrict dropdowns to their own department & themselves
        ViewBag.Departments = _DepartmentService.GetDepartments()
            .Where(d => d.Value == sessionDeptId)
            .ToList();

        ViewBag.Users = _UsersService.GetUsers()
            .Where(u => u.Value == sessionUserId)
            .ToList();
      }

      // Pre-select values for the dropdowns
      ViewBag.SelectedDepartmentID = booking.departmentID;
      ViewBag.SelectedUserID = booking.userID;
      //return View("Index");
      return RedirectToAction("Index");

    }

    // POST: Booking/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Booking model)
    {
      string? role = HttpContext.Session.GetString("role");
      string? sessionUserID = HttpContext.Session.GetString("userID");
      string? sessionDeptID = HttpContext.Session.GetString("Departmentid");

      var existingBooking = _BookingService.GetBookingById(model.bookingid);
      if (existingBooking == null)
      {
        TempData["Error"] = "Booking not found.";
        return RedirectToAction("Index");
      }

      // Block Users from editing finalized bookings
      if (role == "Users" &&
          (existingBooking.status == "Approved" ||
           existingBooking.status == "Returned" ||
           existingBooking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot edit this booking because it is already finalized.";
        return RedirectToAction("Index");
      }

      // Force values for normal Users
      if (role == "Users")
      {
        model.userID = sessionUserID!;
        model.departmentID = sessionDeptID!;
        model.status = "Pending";
      }

      // ✅ ADMIN: handle date_returned and status change logging
      if (role == "Admin" || role == "Superadmin")
      {
        // Auto-set date_returned when status is Returned
        DateTime? returnedDate = existingBooking.date_returned;
        if (model.status == "Returned" && !returnedDate.HasValue)
        {
          returnedDate = DateTime.Now;
          model.date_returned = returnedDate;
        }

        // Log status change
        bool isStatusChanged = existingBooking.status != model.status;
        bool isReturnedNow = model.status == "Returned";

        if (isStatusChanged || isReturnedNow)
        {
          string oldStatus = existingBooking.status;
          if (isReturnedNow && oldStatus == "Pending")
            oldStatus = "Pending (Direct Returned)";

          _BookingService.LogStatusChange(
              bookingId: model.bookingid,
              itemID: existingBooking.itemID,
              departmentID: existingBooking.departmentID,
              oldStatus: oldStatus,
              newStatus: model.status,
              quantity: existingBooking.quantity,
              purpose: model.purpose ?? existingBooking.purpose,
              date_requested: existingBooking.date_requested,
              date_returned: returnedDate,
              remarks: model.remarks,
              changedBy: sessionUserID ?? ""
          );
        }
      }

      // Update DB — same query works for both roles
      string query = @"UPDATE BookingTrans 
                 SET date_requested = ?, date_returned = ?, status = ?, remarks = ?
                 WHERE bookingID = ?";

      var parameters = new List<OleDbParameter>
{
    new OleDbParameter("?", model.date_requested.HasValue
        ? (object)model.date_requested.Value : DBNull.Value),
    new OleDbParameter("?", model.date_returned.HasValue
        ? (object)model.date_returned.Value : DBNull.Value),
    new OleDbParameter("?", model.status ?? existingBooking.status),
    new OleDbParameter("?", model.remarks ?? ""),
    new OleDbParameter("?", model.bookingid)
};

      _dbAccess.ExecuteNonQueryBooking(query, parameters);
      return RedirectToAction("Index");
    }


    // GET: Booking/Delete/5
    public ActionResult Delete(int id)
    {
      var booking = _BookingService.GetBookingById(id);
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

      //return View("Index");
      return RedirectToAction("Index");
    }

    // POST: Booking/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)
    {

      // Users cannot delete approved bookings
      var existingBooking = _BookingService.GetBookingById(id);
      if (existingBooking == null)
      {
        TempData["Error"] = "Booking not found.";
        return RedirectToAction("Index");
      }

      // Users cannot delete approved bookings
      string? sessionRole = HttpContext.Session.GetString("role");
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



    //for refresh rows of booking
    [HttpGet]
    public JsonResult GetLatestBookings()
    {
      string? role = HttpContext.Session.GetString("role");
      string? deptID = HttpContext.Session.GetString("Departmentid");

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
            WHERE b.status = 'Pending'  
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
          dateRequestedRaw = row["date_requested"] != DBNull.Value  // ✅ ADD THIS
          ? ((DateTime)row["date_requested"]).ToString("yyyy-MM-ddTHH:mm")
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

    

  }
}
