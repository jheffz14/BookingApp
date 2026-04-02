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
  public class ChangePassController : BaseController
  {
    private readonly dbAccess _dbAccess;
    public ChangePassController(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    
    }
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      base.OnActionExecuting(filterContext);
      if (filterContext.Result != null) return;
      if (!IsAdmin() && !IsSuperAdmin() && !IsUsers())
      {
        filterContext.Result = RedirectToAction("Index", "AccessDenied");
        return;
      }
    }

    public IActionResult Index()
    {
      return View();
    }



    // GET
    public ActionResult ChangePassword()
    {
      if (string.IsNullOrEmpty(HttpContext.Session.GetString("userID")))
        return RedirectToAction("Index", "Login");
      return View();
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ChangePassword(string newPassword, string confirmPassword)
    {
      string? userID = HttpContext.Session.GetString("userID");

      if (string.IsNullOrEmpty(userID))
        return RedirectToAction("Index", "Login");

      if (newPassword != confirmPassword)
      {
        TempData["Error"] = "Passwords do not match.";
        return RedirectToAction("Index");
      }

      if (newPassword == "kcckcc")
      {
        TempData["Error"] = "New password cannot be the default password.";
        return RedirectToAction("Index");
      }

      if (newPassword.Length < 6)
      {
        TempData["Error"] = "Password must be at least 6 characters.";
        return RedirectToAction("Index");
      }

      try
      {
        var parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", newPassword),
            new OleDbParameter("?", false),
            new OleDbParameter("?", userID)
        };
        string query = "UPDATE Users SET pass_word = ?, is_default_password = ? WHERE userID = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);

        HttpContext.Session.SetString("is_default_password", "false");
        TempData["Success"] = "Password changed successfully!";

        string? role = HttpContext.Session.GetString("role");
        if (role == "Superadmin" || role =="Admin")
          return RedirectToAction("Index", "Dashboards");
        return RedirectToAction("Index", "UserBooking");
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error: " + ex.Message;
        return RedirectToAction("Index");
      }
    }


  }
}
