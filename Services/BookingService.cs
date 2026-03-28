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
  public class BookingService
  {

    private readonly dbAccess _dbAccess;

    public BookingService(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }
    public Booking? GetBookingById(int id)
    {
      string query = "SELECT * FROM BookingTrans WHERE bookingID = ?";
      var parameters = new List<OleDbParameter> { new OleDbParameter("?", id) };
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);

      if (dt.Rows.Count == 0) return null;

      var row = dt.Rows[0];
      return new Booking
      {
        bookingid = Convert.ToInt32(row["bookingID"]),
        itemID = row["itemID"]?.ToString() ?? "",          // <-- string now
        departmentID = row["departmentID"]?.ToString() ?? "", // <-- string now
        userID = row["userID"]?.ToString() ?? "",         // <-- string now
        quantity = Convert.ToInt32(row["quantity"]),
        purpose = row["purpose"]?.ToString() ?? "",
        date_requested = row["date_requested"] != DBNull.Value ? (DateTime?)row["date_requested"] : null,
        //date_borrowed = row["date_borrowed"] != DBNull.Value ? (DateTime?)row["date_borrowed"] : null,       
        status = row["status"]?.ToString() ?? ""
      };
    }

    public void LogStatusChange(int bookingId, string itemID, string departmentID,
                             string oldStatus, string newStatus, int quantity,
                             string purpose, DateTime? date_requested,
                             DateTime? date_returned, string remarks,
                             string changedBy)
    {
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
        new OleDbParameter("?", OleDbType.VarChar) { Value = changedBy },
        new OleDbParameter("?", OleDbType.Date) { Value = DateTime.Now },
        new OleDbParameter("?", OleDbType.VarChar) { Value = remarks ?? "" }
    };

      _dbAccess.ExecuteNonQueryBooking(query, parameters);
    }


  }
}
