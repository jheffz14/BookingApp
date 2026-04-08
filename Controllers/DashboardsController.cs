using BookingAppV2.Connection;
using BookingAppV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using BookingAppV2.Services;

namespace BookingAppV2.Controllers
{
  public class DashboardsController : BaseController
  {
    private readonly dbAccess _dbAccess;
    private readonly ItemService _itemService;
    private readonly BookingService _bookingService;

  

    public DashboardsController(dbAccess dbAccess,
                                ItemService itemService,
                                BookingService bookingService)
    {
      _dbAccess = dbAccess;
      _itemService = itemService;
      _bookingService = bookingService;
    }

    private (int pending, int approved, int returned, int disapproved) StatusCount()
    {
      string countPending = _bookingService.GetPendingCountQuery();
      string countApproved = _bookingService.GetApprovedCountQuery();
      string countReturned = _bookingService.GetReturnedCountQuery();
      string countDisapproved = _bookingService.GetDisapprovedCountQuery();

      int pending = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(countPending, null).Rows[0]["cnt"]);
      int approved = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(countApproved, null).Rows[0]["cnt"]);
      int returned = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(countReturned, null).Rows[0]["cnt"]);
      int disapproved = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(countDisapproved, null).Rows[0]["cnt"]);

      // ✅ Set ViewBag for Index view
      ViewBag.PendingCount = pending;
      ViewBag.ApprovedCount = approved;
      ViewBag.ReturnedCount = returned;
      ViewBag.DisapprovedCount = disapproved;

      // ✅ Return for JSON use in GetDashboardData
      return (pending, approved, returned, disapproved);
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
    public IActionResult Index()
    {
      string? role = HttpContext.Session.GetString("role");
      string? deptID = HttpContext.Session.GetString("Departmentid");

      string itemQuery = _itemService.GetItemQuery();

      DataTable itemAvailability = _dbAccess.ExecuteQueryBooking(itemQuery, null);
      ViewBag.ItemAvailability = itemAvailability;

      // ✅ Booking schedule — upcoming/active bookings
      string scheduleQuery;
      List<OleDbParameter>? scheduleParams = null;

      if (role == "Users" && !string.IsNullOrEmpty(deptID))
      {
        // Users see only their department's bookings
        scheduleQuery = _bookingService.GetScheduleUserQuery();

        scheduleParams = new List<OleDbParameter>
                {
                    new OleDbParameter("?", deptID)
                };
      }
      else
      {
        // Admin sees all
        scheduleQuery = _bookingService.GetScheduleAdminQuery();
      }

      DataTable bookingSchedule = _dbAccess.ExecuteQueryBooking(scheduleQuery, scheduleParams);
      ViewBag.BookingSchedule = bookingSchedule;

      // ✅ Summary counts — Admin only
      if (role == "Admin" || role == "Superadmin")
      {
        string summaryQuery = _bookingService.GetSummaryQuery();

        DataTable summary = _dbAccess.ExecuteQueryBooking(summaryQuery, null);
        if (summary.Rows.Count > 0)
        {
         
          StatusCount();
        }
      }

      ViewBag.Role = role;
      return View();
    }

    // ✅ AJAX endpoint for item availability check in booking
    [HttpGet]
    public JsonResult GetItemAvailability(string itemID)
    {
      string query = _itemService.GetItemAvailabilityQuery();

      var parameters = new List<OleDbParameter>
            {
                new OleDbParameter("?", itemID)
            };

      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);

      if (dt.Rows.Count == 0)
        return Json(new { available = 0, total = 0, itemName = "" });

      var row = dt.Rows[0];
      int total = Convert.ToInt32(row["total_stock"]);
      int borrowed = Convert.ToInt32(row["borrowed"]);
      int available = total - borrowed;

      return Json(new
      {
        itemName = row["item_name"].ToString(),
        total = total,
        borrowed = borrowed,
        available = available
      });
    }

    // ✅ AJAX endpoint for dashboard refresh
    [HttpGet]
    public JsonResult GetDashboardData()
    {
      // Check if user is logged in
      string? userID = HttpContext.Session.GetString("userID");
      if (string.IsNullOrEmpty(userID))
      {
        return Json(new { error = "Unauthorized" },
                   System.Text.Json.JsonSerializerOptions.Web);
      }
      string? role = HttpContext.Session.GetString("role");
      string? deptID = HttpContext.Session.GetString("Departmentid");

     
      // Item availability
      string itemQuery = _itemService.GetItemQuery();

      DataTable itemDt = _dbAccess.ExecuteQueryBooking(itemQuery, null);

      var items = new List<object>();
      foreach (DataRow row in itemDt.Rows)
      {
        items.Add(new
        {
          itemName = row["item_name"].ToString(),
          total = Convert.ToInt32(row["total_stock"]),
          borrowed = Convert.ToInt32(row["borrowed"]),
          available = Convert.ToInt32(row["available"])
        });
      }

      // Booking schedule
      string scheduleQuery;
      List<OleDbParameter>? scheduleParams = null;

      if (role == "Users" && !string.IsNullOrEmpty(deptID))
      {
        scheduleQuery = _bookingService.GetDBSchedUserQuery();

        scheduleParams = new List<OleDbParameter>
        {
            new OleDbParameter(" ? ", deptID)
        };
      }
      else
      {
        scheduleQuery = _bookingService.GetDBSchedAdminQuery();
      }

      DataTable scheduleDt = _dbAccess.ExecuteQueryBooking(scheduleQuery, scheduleParams);

      var schedule = new List<object>();
      foreach (DataRow row in scheduleDt.Rows)
      {
        schedule.Add(new
        {
          itemName = row["item_name"].ToString(),
          userID = row["userID"].ToString(),
          department = row["departmentName"].ToString(),
          quantity = Convert.ToInt32(row["quantity"]),
          dateRequested = row["date_requested"] != DBNull.Value
                ? ((DateTime)row["date_requested"]).ToString("MM-dd-yyyy hh:mm tt") : "",
          status = row["status"].ToString()
        });
      }

      // Summary counts (Admin only)
      object? summary = null;
      if (role == "Admin" || role == "Superadmin")
      {
        var (pending, approved, returned, disapproved) = StatusCount();

        // ✅ Build the summary object for JSON
        summary = new
        {
          pending,
          approved,
          returned,
          disapproved
        };
      }

      return Json(new { items, schedule, summary });
    }



  }
}
