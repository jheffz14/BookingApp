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
  public class DepartmentController : BaseController
  {
    private readonly dbAccess _dbAccess;

    public DepartmentController(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      base.OnActionExecuting(filterContext);
      if (filterContext.Result != null) return;
      if (!IsAdmin() && !IsSuperAdmin())
      {
        filterContext.Result = RedirectToAction("Index", "AccessDenied");
        return;
      }
    }

    public ActionResult Index(int page = 1, string search = "")
    {
      int pageSize = 10;

      string query;
      List<OleDbParameter>? parameters = null;

      if (!string.IsNullOrEmpty(search))
      {
        query = @"SELECT departmentid, departmentName FROM Department
                          WHERE departmentid LIKE ? OR departmentName LIKE ?
                          ORDER BY departmentid ASC";
        parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", "%" + search + "%"),
                    new OleDbParameter("?", "%" + search + "%")
                };
      }
      else
      {
        query = "SELECT departmentid, departmentName FROM Department ORDER BY departmentid ASC";
      }

      DataTable allData = _dbAccess.ExecuteQueryBooking(query, parameters);

      int totalRows = allData.Rows.Count;
      int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

      DataTable pagedData = allData.Clone();
      int start = (page - 1) * pageSize;
      int end = Math.Min(start + pageSize, totalRows);
      for (int i = start; i < end; i++)
        pagedData.ImportRow(allData.Rows[i]);

      ViewBag.CurrentPage = page;
      ViewBag.TotalPages = totalPages;
      ViewBag.Search = search;

      return View(pagedData);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Department model)
    {
      try
      {
        // Check duplicate
        string checkQuery = "SELECT COUNT(*) FROM Department WHERE departmentid = ?";
        var checkParams = new List<OleDbParameter>
                {
                    new OleDbParameter("?", model.departmentid)
                };
        DataTable dtCheck = _dbAccess.ExecuteQueryBooking(checkQuery, checkParams);
        int existing = dtCheck.Rows.Count > 0
            ? Convert.ToInt32(dtCheck.Rows[0][0]) : 0;

        if (existing > 0)
        {
          TempData["Error"] = "Department ID already exists.";
          return RedirectToAction("Index");
        }

        var parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", model.departmentid),
                    new OleDbParameter("?", model.departmentName)
                };
        string query = "INSERT INTO Department (departmentid, departmentName) VALUES (?, ?)";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "Department added successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error adding department: " + ex.Message;
      }

      return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Department model)
    {
      try
      {
        var parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", model.departmentName),
                    new OleDbParameter("?", model.departmentid)
                };
        string query = "UPDATE Department SET departmentName = ? WHERE departmentid = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "Department updated successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error updating department: " + ex.Message;
      }

      return RedirectToAction("Index");
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(string id)
    {
      try
      {
        var parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", id)
                };
        string query = "DELETE FROM Department WHERE departmentid = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "Department deleted successfully.";
      }
      catch (OleDbException ex)
      {
        TempData["Error"] = ex.Message.Contains("related records")
            ? "Cannot delete — department has related users or bookings."
            : "Error deleting: " + ex.Message;
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Unexpected error: " + ex.Message;
      }

      return RedirectToAction("Index");
    }
  }
}
