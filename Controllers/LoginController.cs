using BookingAppV2.Connection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace BookingAppV2.Controllers
{
  public class LoginController : Controller
  {
    private readonly dbAccess _dbAccess;

    public LoginController(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }

    public ActionResult Index()
    {
      // ✅ Already logged in — redirect to dashboard
      if (!string.IsNullOrEmpty(HttpContext.Session.GetString("userID")))
        return RedirectToAction("Index", "Dashboards");

      return View();
    }

    [HttpPost]
    public ActionResult Index(string userID, string pass_word)
    {
      string query = "SELECT * FROM Users WHERE userID = ? AND pass_word = ?";
      var parameters = new List<OleDbParameter>
            {
                new OleDbParameter("?", userID),
                new OleDbParameter("?", pass_word)
            };

      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);

      if (dt.Rows.Count == 1)
      {
        string role = dt.Rows[0]["role"]?.ToString() ?? "";
        string departmentID = dt.Rows[0]["Departmentid"]?.ToString() ?? "";

        // ✅ Safe check — column may not exist for old users
        bool isDefault = false;
        try
        {
          if (dt.Columns.Contains("is_default_password") &&
              dt.Rows[0]["is_default_password"] != DBNull.Value)
          {
            isDefault = Convert.ToBoolean(dt.Rows[0]["is_default_password"]);
          }
        }
        catch { isDefault = false; }

        // Store session
        HttpContext.Session.SetString("userID", userID);
        HttpContext.Session.SetString("role", role);
        HttpContext.Session.SetString("Departmentid", departmentID);
        HttpContext.Session.SetString("DepartmentID", departmentID);
        HttpContext.Session.SetString("is_default_password", isDefault ? "true" : "false");

        // ✅ Force change password if default
        if (isDefault)
          return RedirectToAction("Index", "ChangePass");

        // ✅ Role-based redirect
        if (role == "Admin")
          return RedirectToAction("Index", "UserBooking");

        if (role == "Superadmin")
          return RedirectToAction("Index", "Dashboards");

        if (role == "Users")
          return RedirectToAction("Index", "UserBooking");

        return RedirectToAction("Index", "Login");
      }

      ViewBag.Error = "Invalid username or password";
      return View();
    }

    public ActionResult Logout()
    {
      HttpContext.Session.Clear();
      HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
      return RedirectToAction("Index", "Login");
    }
  }
}
