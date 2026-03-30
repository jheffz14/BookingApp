using BookingAppV2.Connection;
using BookingAppV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace BookingAppV2.Controllers
{
  public class DashboardsController : BaseController
  {
    private readonly dbAccess _dbAccess;

    public DashboardsController(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
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

      // ✅ Item availability — all roles see this
      string itemQuery = @"
    SELECT 
        i.itemID,
        i.item_name,
        i.total_stock,
        IIF(SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0)) IS NULL, 0, 
            SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0))) AS borrowed,
        i.total_stock - IIF(SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0)) IS NULL, 0, 
            SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0))) AS available
    FROM Items i
    LEFT JOIN BookingTrans b ON i.itemID = b.itemID
    GROUP BY i.itemID, i.item_name, i.total_stock
    ORDER BY i.item_name ASC";

      DataTable itemAvailability = _dbAccess.ExecuteQueryBooking(itemQuery, null);
      ViewBag.ItemAvailability = itemAvailability;

      // ✅ Booking schedule — upcoming/active bookings
      string scheduleQuery;
      List<OleDbParameter>? scheduleParams = null;

      if (role == "Users" && !string.IsNullOrEmpty(deptID))
      {
        // Users see only their department's bookings
        scheduleQuery = @"
                    SELECT 
                        b.bookingID,
                        i.item_name,
                        d.departmentName,
                        b.userID,
                        b.quantity,
                        b.purpose,
                        b.date_requested,
                        b.date_returned,
                        b.status
                    FROM ((BookingTrans b
                    INNER JOIN Items i ON b.itemID = i.itemID)
                    INNER JOIN Department d ON b.departmentID = d.departmentID)
                    WHERE b.status IN ('Pending','Approved')
                    AND b.departmentID = ?
                    ORDER BY b.date_requested ASC";

        scheduleParams = new List<OleDbParameter>
                {
                    new OleDbParameter("?", deptID)
                };
      }
      else
      {
        // Admin sees all
        scheduleQuery = @"
                    SELECT 
                        b.bookingID,
                        i.item_name,
                        d.departmentName,
                        b.userID,
                        b.quantity,
                        b.purpose,
                        b.date_requested,
                        b.date_returned,
                        b.status
                    FROM ((BookingTrans b
                    INNER JOIN Items i ON b.itemID = i.itemID)
                    INNER JOIN Department d ON b.departmentID = d.departmentID)
                    WHERE b.status IN ('Pending','Approved')
                    ORDER BY b.date_requested ASC";
      }

      DataTable bookingSchedule = _dbAccess.ExecuteQueryBooking(scheduleQuery, scheduleParams);
      ViewBag.BookingSchedule = bookingSchedule;

      // ✅ Summary counts — Admin only
      if (role == "Admin" || role == "Superadmin")
      {
        string summaryQuery = @"
                    SELECT
                        SUM(IIF(status='Pending', 1, 0)) AS pending,
                        SUM(IIF(status='Approved', 1, 0)) AS approved,
                        SUM(IIF(status='Returned', 1, 0)) AS returned,
                        SUM(IIF(status='Disapproved', 1, 0)) AS disapproved
                    FROM BookingTrans";

        DataTable summary = _dbAccess.ExecuteQueryBooking(summaryQuery, null);
        if (summary.Rows.Count > 0)
        {
          ViewBag.PendingCount = summary.Rows[0]["pending"];
          ViewBag.ApprovedCount = summary.Rows[0]["approved"];
          ViewBag.ReturnedCount = summary.Rows[0]["returned"];
          ViewBag.DisapprovedCount = summary.Rows[0]["disapproved"];
        }
      }

      ViewBag.Role = role;
      return View();
    }

    // ✅ AJAX endpoint for item availability check in booking
    [HttpGet]
    public JsonResult GetItemAvailability(string itemID)
    {
      string query = @"
    SELECT 
        i.item_name,
        i.total_stock,
        IIF(SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0)) IS NULL, 0,
            SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0))) AS borrowed
    FROM Items i
    LEFT JOIN BookingTrans b ON i.itemID = b.itemID
    WHERE i.itemID = ?
    GROUP BY i.itemID, i.item_name, i.total_stock";

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

      //// Validate role is allowed
      //if (role != "Admin" && role != "Superadmin")
      //{
      //  return Json(new { error = "Access denied" },
      //             System.Text.Json.JsonSerializerOptions.Web);
      //}

      // Item availability
      string itemQuery = @"
        SELECT 
            i.itemID,
            i.item_name,
            i.total_stock,
            IIF(SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0)) IS NULL, 0,
                SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0))) AS borrowed,
            i.total_stock - IIF(SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0)) IS NULL, 0,
                SUM(IIF(b.status = 'Approved' OR b.status = 'Pending', b.quantity, 0))) AS available
        FROM Items i
        LEFT JOIN BookingTrans b ON i.itemID = b.itemID
        GROUP BY i.itemID, i.item_name, i.total_stock
        ORDER BY i.item_name ASC";

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
        scheduleQuery = @"
            SELECT i.item_name,
                   u.userID,
                   d.departmentName,
                   b.quantity,
                   b.date_requested,
                   b.status
          FROM (((BookingTrans b 
                 INNER JOIN Items i ON b.itemID = i.itemID)
                 INNER JOIN Department d ON b.departmentID = d.departmentID)
                 INNER JOIN Users u ON b.userID = u.userID)
            WHERE b.status IN ('Pending','Approved')
            AND b.departmentID = ?
            ORDER BY b.date_requested ASC";
        scheduleParams = new List<OleDbParameter>
        {
            new OleDbParameter(" ? ", deptID)
        };
      }
      else
      {
        scheduleQuery = @"
          SELECT i.item_name,
       u.userID,
       d.departmentName,
       b.quantity,
       b.date_requested,
       b.status
FROM (((BookingTrans b 
       INNER JOIN Items i ON b.itemID = i.itemID)
       INNER JOIN Department d ON b.departmentID = d.departmentID)
       INNER JOIN Users u ON b.userID = u.userID)
WHERE b.status IN ('Pending','Approved')
ORDER BY b.date_requested ASC";
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
        int pending = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(
          "SELECT COUNT(*) AS cnt FROM BookingTrans WHERE status = 'Pending'", null).Rows[0]["cnt"]);
        int approved = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(
            "SELECT COUNT(*) AS cnt FROM BookingTrans WHERE status = 'Approved'", null).Rows[0]["cnt"]);
        int returned = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(
            "SELECT COUNT(*) AS cnt FROM BookingTrans WHERE status = 'Returned'", null).Rows[0]["cnt"]);
        int disapproved = Convert.ToInt32(_dbAccess.ExecuteQueryBooking(
            "SELECT COUNT(*) AS cnt FROM BookingTrans WHERE status = 'Disapproved'", null).Rows[0]["cnt"]);

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
