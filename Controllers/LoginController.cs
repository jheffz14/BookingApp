using BookingAppV2.Connection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // added
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;


namespace BookingApp.Controllers
{
  public class LoginController : Controller
  {
    private readonly dbAccess _dbAccess;
    public LoginController(dbAccess dbAccess)
    {
        _dbAccess = dbAccess;
    }
    // GET: Login
    public ActionResult Index()
    {
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
        string role = dt.Rows[0]["role"].ToString();
        string departmentID = dt.Rows[0]["Departmentid"].ToString();

        // store session (ASP.NET Core)
        HttpContext.Session.SetString("UserID", userID);
        HttpContext.Session.SetString("Role", role);
        HttpContext.Session.SetString("Departmentid", departmentID);

        string deptID = HttpContext.Session.GetString("Departmentid");
        string roles = HttpContext.Session.GetString("Role");

        // role-based redirect
        if (role == "Admin")
          return RedirectToAction("Index", "Dashboards");

        if (role == "Superadmin")
          return RedirectToAction("Index", "Dashboards");

        if (role == "Users")
          return RedirectToAction("Index", "Dashboards");
        // fallback
        return RedirectToAction("Index", "Home");
      }

      ViewBag.Error = "Invalid username or password";
      return View();
    }

    public ActionResult Logout()
    {
      HttpContext.Session.Clear(); // use Clear in Core
      HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
      return RedirectToAction("Index", "Login");
    }
  }
}
