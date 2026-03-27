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
  public class UsersService
  {
    private readonly dbAccess _dbAccess;

    public UsersService(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }

    private Users? GetUserById(string id)
    {
      string query = @"
        SELECT U.*, D.DepartmentName
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


  }
}
