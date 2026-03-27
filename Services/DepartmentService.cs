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


namespace BookingAppV2.Services
{
  public class DepartmentService
  {
    private readonly dbAccess _dbAccess;

    public DepartmentService(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }
    public List<SelectListItem> GetDepartments()
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


  }
}
