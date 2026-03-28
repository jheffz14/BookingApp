using BookingAppV2.Connection;
using BookingAppV2.Models;
using BookingAppV2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;

namespace BookingAppV2.Controllers
{
  public class StatusController : BaseController
  {
    private readonly dbAccess _dbAccess;
    private readonly BookingService _BookingService;
    private readonly ItemService _ItemService;
    private readonly DepartmentService _DepartmentService;
    private readonly UsersService _UsersService;

    public StatusController(dbAccess dbAccess,
                            BookingService bookingService,
                            ItemService itemService,
                            DepartmentService departmentService,
                            UsersService usersService)
    {
      _dbAccess = dbAccess;
      _BookingService = bookingService;
      _ItemService = itemService;
      _DepartmentService = departmentService;
      _UsersService = usersService;
    }

    public ActionResult Index(string status = "Approved",
                              DateTime? startDate = null,
                              DateTime? endDate = null)
    {
      string[] validStatuses = { "Approved", "Disapproved", "Returned" };
      if (!validStatuses.Contains(status))
        status = "Approved";

      if (!startDate.HasValue)
        startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
      if (!endDate.HasValue)
        endDate = startDate.Value.AddMonths(1).AddDays(-1);

      DataTable dt = GetBookingsByStatus(status, startDate.Value, endDate.Value);

      ViewBag.CurrentStatus = status;
      ViewBag.StatusList = validStatuses;
      ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
      ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

      return View(dt);
    }

    [HttpGet]
    public JsonResult GetBookings(string status = "Approved",
                                  DateTime? startDate = null,
                                  DateTime? endDate = null)
    {
      if (!startDate.HasValue)
        startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
      if (!endDate.HasValue)
        endDate = startDate.Value.AddMonths(1).AddDays(-1);

      DataTable dt = GetBookingsByStatus(status, startDate.Value, endDate.Value);
      var list = ConvertDataTableToJsonList(dt);

      return Json(list); // ✅ no JsonRequestBehavior in Core
    }

    private DataTable GetBookingsByStatus(string status,
                                           DateTime startDate,
                                           DateTime endDate)
    {
      // ✅ Core session syntax
      string? role = HttpContext.Session.GetString("role");
      string? deptID = HttpContext.Session.GetString("Departmentid");

      if (string.IsNullOrEmpty(role))
        return new DataTable();

      string query;
      List<OleDbParameter> parameters = new List<OleDbParameter>();
      string dateFilter = " AND B.date_requested BETWEEN ? AND ? ";

      if (role == "Users")
      {
        if (string.IsNullOrEmpty(deptID))
          return new DataTable();

        query = @"SELECT B.bookingID, I.item_name, D.departmentName, B.userID,
                               B.quantity, B.purpose, B.date_requested, B.date_returned,
                               B.status, B.remarks
                        FROM ((BookingTrans AS B
                        INNER JOIN Items AS I ON B.itemID = I.itemID)
                        INNER JOIN Department AS D ON B.departmentID = D.departmentID)
                        WHERE B.departmentID = ? AND B.status = ?"
                + dateFilter +
                " ORDER BY B.bookingID DESC";

        parameters.Add(new OleDbParameter("?", deptID));
        parameters.Add(new OleDbParameter("?", status));
        parameters.Add(new OleDbParameter("?", startDate.Date));
        parameters.Add(new OleDbParameter("?", endDate.Date.AddDays(1).AddSeconds(-1)));
      }
      else
      {
        query = @"SELECT B.bookingID, I.item_name, D.departmentName, B.userID,
                               B.quantity, B.purpose, B.date_requested, B.date_returned,
                               B.status, B.remarks
                        FROM ((BookingTrans AS B
                        INNER JOIN Items AS I ON B.itemID = I.itemID)
                        INNER JOIN Department AS D ON B.departmentID = D.departmentID)
                        WHERE B.status = ?"
                + dateFilter +
                " ORDER BY B.bookingID DESC";

        parameters.Add(new OleDbParameter("?", status));
        parameters.Add(new OleDbParameter("?", startDate.Date));
        parameters.Add(new OleDbParameter("?", endDate.Date.AddDays(1).AddSeconds(-1)));
      }

      return _dbAccess.ExecuteQueryBooking(query, parameters); // ✅ instance not static
    }

    private List<object> ConvertDataTableToJsonList(DataTable dt)
    {
      var list = new List<object>();
      foreach (DataRow row in dt.Rows)
      {
        list.Add(new
        {
          bookingID = row["bookingID"],
          item = row["item_name"],
          department = row["departmentName"],
          user = row["userID"],
          quantity = row["quantity"],
          purpose = row["purpose"],
          dateRequested = row["date_requested"] != DBNull.Value
                ? ((DateTime)row["date_requested"]).ToString("MM-dd-yyyy hh:mm tt") : "",
          dateReturned = row["date_returned"] != DBNull.Value
                ? ((DateTime)row["date_returned"]).ToString("MM-dd-yyyy hh:mm tt") : "",
          dateReturnedRaw = row["date_returned"] != DBNull.Value  // ✅ ADD THIS
          ? ((DateTime)row["date_returned"]).ToString("yyyy-MM-ddTHH:mm") : "",
          status = row["status"],
          remarks = row["remarks"]
        });
      }
      return list;
    }

    public ActionResult Edit(int id)
    {
      var booking = _BookingService.GetBookingById(id);
      if (booking == null) return NotFound(); // ✅ Core

      string? sessionRole = HttpContext.Session.GetString("role"); // ✅ Core
      if (sessionRole == "Users" &&
          (booking.status == "Approved" ||
           booking.status == "Returned" ||
           booking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot edit this booking because it is already finalized.";
        return RedirectToAction("Index");
      }

      string? role = HttpContext.Session.GetString("role"); // ✅ Core

      if (role == "Superadmin" || role == "Admin")
      {
        ViewBag.Departments = _DepartmentService.GetDepartments();
        ViewBag.Users = _UsersService.GetUsers();
        ViewBag.Items = _ItemService.GetItems();
        ViewBag.Statuses = GetBookingStatus();
      }
      else
      {
        string? deptID = HttpContext.Session.GetString("Departmentid");
        string? userID = HttpContext.Session.GetString("userID");

        ViewBag.Departments = _DepartmentService.GetDepartments()
            .Where(d => d.Value == deptID).ToList();
        ViewBag.Users = _UsersService.GetUsers()
            .Where(u => u.Value == userID).ToList();
      }

      ViewBag.SelectedDepartmentID = booking.departmentID;
      ViewBag.SelectedUserID = booking.userID;
      return RedirectToAction("Index"); // ✅ modal-based now
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Booking model)
    {
      string? role = HttpContext.Session.GetString("role"); // ✅ Core
      string? sessionUserID = HttpContext.Session.GetString("userID");
      string? sessionDeptID = HttpContext.Session.GetString("Departmentid");
      var existingBooking = _BookingService.GetBookingById(model.bookingid);

      if (existingBooking == null)
      {
        TempData["Error"] = "Booking not found.";
        return RedirectToAction("Index");
      }

      if (role == "Users" &&
          (existingBooking.status == "Approved" ||
           existingBooking.status == "Returned" ||
           existingBooking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot edit this booking because it is already finalized.";
        return RedirectToAction("Index");
      }

      if (role == "Users")
      {
        model.userID = sessionUserID!;
        model.departmentID = sessionDeptID!;
        model.status = "Pending";
      }

      DateTime? returnedDate = model.date_returned ?? existingBooking.date_returned;
      if (model.status == "Returned" && !returnedDate.HasValue)
      {
        returnedDate = DateTime.Now;
        model.date_returned = returnedDate;
      }

      bool isStatusChanged = existingBooking.status != model.status;
      bool isReturnedNow = model.status == "Returned";

      if (isStatusChanged || isReturnedNow)
      {
        string oldStatus = existingBooking.status;
        if (isReturnedNow && oldStatus == "Pending")
          oldStatus = "Pending (Direct Returned)";

        LogStatusChange(
            bookingId: model.bookingid,
            itemID: GetItemName(existingBooking.itemID),
            departmentID: existingBooking.departmentID,
            oldStatus: oldStatus,
            newStatus: model.status,
            quantity: existingBooking.quantity,
            purpose: model.purpose,
            date_requested: existingBooking.date_requested,
            date_returned: returnedDate,
            remarks: model.remarks
        );
      }

      var parameters = new List<OleDbParameter>
            {
                new OleDbParameter("?", model.date_returned.HasValue
                    ? (object)model.date_returned.Value : DBNull.Value),
                new OleDbParameter("?", model.status ?? ""),
                new OleDbParameter("?", string.IsNullOrEmpty(model.remarks) ? "" : model.remarks),
                new OleDbParameter("?", model.bookingid)
            };

      string query = @"UPDATE BookingTrans 
                             SET date_returned = ?, status = ?, remarks = ?
                             WHERE bookingID = ?";
      _dbAccess.ExecuteNonQueryBooking(query, parameters); // ✅ instance

      return RedirectToAction("Index");
    }

    private void LogStatusChange(int bookingId, string itemID, string departmentID,
                                  string oldStatus, string newStatus, int quantity,
                                  string purpose, DateTime? date_requested,
                                  DateTime? date_returned, string remarks)
    {
      string userId = HttpContext.Session.GetString("userID") ?? ""; // ✅ Core

      string query = @"INSERT INTO BookingStatusLog
                (bookingID, itemID, departmentID, oldStatus, newStatus, quantity,
                 purpose, date_requested, date_returned, changedBy, changedDate, remarks)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

      var parameters = new List<OleDbParameter>
            {
                new OleDbParameter("?", OleDbType.Integer) { Value = bookingId },
                new OleDbParameter("?", OleDbType.VarChar) { Value = itemID ?? "" },
                new OleDbParameter("?", OleDbType.VarChar) { Value = departmentID ?? "" },
                new OleDbParameter("?", OleDbType.VarChar) { Value = oldStatus ?? "" },
                new OleDbParameter("?", OleDbType.VarChar) { Value = newStatus ?? "" },
                new OleDbParameter("?", OleDbType.Integer) { Value = quantity },
                new OleDbParameter("?", OleDbType.VarChar) { Value = purpose ?? "" },
                new OleDbParameter("?", date_requested.HasValue
                    ? (object)date_requested.Value : DBNull.Value),
                new OleDbParameter("?", date_returned.HasValue
                    ? (object)date_returned.Value : DBNull.Value),
                new OleDbParameter("?", OleDbType.VarChar) { Value = userId },
                new OleDbParameter("?", OleDbType.Date) { Value = DateTime.Now },
                new OleDbParameter("?", OleDbType.VarChar) { Value = remarks ?? "" }
            };

      _dbAccess.ExecuteNonQueryBooking(query, parameters); // ✅ instance
    }

    public ActionResult Delete(int id)
    {
      var booking = _BookingService.GetBookingById(id);
      if (booking == null) return NotFound(); // ✅ Core

      string? sessionRole = HttpContext.Session.GetString("role"); // ✅ Core
      if (sessionRole == "Users" &&
          (booking.status == "Approved" ||
           booking.status == "Returned" ||
           booking.status == "Disapproved"))
      {
        TempData["Error"] = "You cannot delete this booking because it is already finalized.";
        return RedirectToAction("Index");
      }
      return RedirectToAction("Index"); // ✅ modal-based
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)
    {
      var parameters = new List<OleDbParameter> { new OleDbParameter("?", id) };
      string query = "DELETE FROM BookingTrans WHERE bookingID = ?";
      _dbAccess.ExecuteNonQueryBooking(query, parameters); // ✅ instance
      return RedirectToAction("Index");
    }

    private string GetItemName(string itemID)
    {
      if (string.IsNullOrEmpty(itemID)) return "(Unknown Item)";

      string query = "SELECT item_name FROM Items WHERE itemID = ?";
      var parameters = new List<OleDbParameter> { new OleDbParameter("?", itemID) };
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters); // ✅ instance

      return dt.Rows.Count > 0
          ? dt.Rows[0]["item_name"]?.ToString() ?? "(Unknown Item)"
          : "(Unknown Item)";
    }

    private List<SelectListItem> GetBookingStatus()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "Returned", Value = "Returned" }
            };
    }
  }
}
