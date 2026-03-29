using BookingAppV2.Connection;
using BookingAppV2.Models;
using BookingAppV2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace BookingAppV2.Controllers
{
  public class UsersController : BaseController
  {
    private readonly dbAccess _dbAccess;
    private readonly DepartmentService _DepartmentService;

    public UsersController(dbAccess dbAccess, DepartmentService departmentService)
    {
      _dbAccess = dbAccess;
      _DepartmentService = departmentService;
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
        query = @"SELECT U.userID, U.pass_word, U.role, D.DepartmentName
                          FROM Users U
                          LEFT JOIN Department D ON U.DepartmentID = D.DepartmentID
                          WHERE U.userID LIKE ? OR D.DepartmentName LIKE ?
                          ORDER BY U.userID ASC";
        parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", "%" + search + "%"),
                    new OleDbParameter("?", "%" + search + "%")
                };
      }
      else
      {
        query = @"SELECT U.userID, U.pass_word, U.role, D.DepartmentName
                          FROM Users U
                          LEFT JOIN Department D ON U.DepartmentID = D.DepartmentID
                          ORDER BY U.userID ASC";
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
      ViewBag.Departments = GetDepartments();

      return View(pagedData);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Users model)
    {
      try
      {
        // Check duplicate userID
        string checkQuery = "SELECT COUNT(*) FROM Users WHERE userID = ?";
        var checkParams = new List<OleDbParameter>
                {
                    new OleDbParameter("?", model.userID)
                };
        DataTable dtCheck = _dbAccess.ExecuteQueryBooking(checkQuery, checkParams);
        int existing = dtCheck.Rows.Count > 0
            ? Convert.ToInt32(dtCheck.Rows[0][0]) : 0;

        if (existing > 0)
        {
          TempData["Error"] = "User ID already exists.";
          return RedirectToAction("Index");
        }

        var parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", model.userID),
                    new OleDbParameter("?", model.pass_word),
                    new OleDbParameter("?", model.role),
                    new OleDbParameter("?", model.DepartmentID)
                };
        string query = "INSERT INTO Users (userID, pass_word, role, DepartmentID) VALUES (?, ?, ?, ?)";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "User added successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error adding user: " + ex.Message;
      }

      return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Users model)
    {
      try
      {
        var existingUser = GetUserById(model.userID);
        if (existingUser == null)
        {
          TempData["Error"] = "User not found.";
          return RedirectToAction("Index");
        }

        // Keep existing password if blank
        string passwordToSave = string.IsNullOrWhiteSpace(model.pass_word)
            ? existingUser.pass_word
            : model.pass_word;

        var parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", passwordToSave),
                    new OleDbParameter("?", model.role),
                    new OleDbParameter("?", model.DepartmentID),
                    new OleDbParameter("?", model.userID)
                };
        string query = "UPDATE Users SET pass_word = ?, role = ?, DepartmentID = ? WHERE userID = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "User updated successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error updating user: " + ex.Message;
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
        string query = "DELETE FROM Users WHERE userID = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "User deleted successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error deleting user: " + ex.Message;
      }

      return RedirectToAction("Index");
    }

    [HttpGet]
    public JsonResult GetUser(string id)
    {
      var user = GetUserById(id);
      if (user == null) return Json(null);

      return Json(new
      {
        userID = user.userID,
        role = user.role,
        departmentID = user.DepartmentID,
        departmentName = user.DepartmentName
      });
    }

    private Users? GetUserById(string id)
    {
      string query = @"SELECT U.*, D.DepartmentName
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

    private List<SelectListItem> GetDepartments()
    {
      string query = "SELECT DepartmentID, DepartmentName FROM Department";
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, null);
      var list = new List<SelectListItem>();
      foreach (DataRow row in dt.Rows)
      {
        list.Add(new SelectListItem
        {
          Value = row["DepartmentID"].ToString(),
          Text = row["DepartmentName"].ToString()
        });
      }
      return list;
    }
  }
}
