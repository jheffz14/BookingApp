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
 
  public class StockService
  {
    private readonly dbAccess _dbAccess;
    public StockService(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }

    public int GetAvailableStock(string itemID)
    {
      // 1. Get total stock
      string stockQuery = "SELECT total_stock FROM Items WHERE itemID = ?";
      var stockDt = _dbAccess.ExecuteQueryBooking(stockQuery,
          new List<OleDbParameter> { new OleDbParameter("?", itemID) });

      if (stockDt.Rows.Count == 0)
        return 0;

      int totalStock = Convert.ToInt32(stockDt.Rows[0]["total_stock"]);

      // 2. Borrowed + Pending + Approved
      string borrowedQuery = @"
        SELECT SUM(quantity) AS borrowed
        FROM BookingTrans
        WHERE itemID = ?
        AND status IN ('Approved', 'Pending')";

      var borrowDt = _dbAccess.ExecuteQueryBooking(borrowedQuery,
          new List<OleDbParameter> { new OleDbParameter("?", itemID) });

      int borrowedQty = 0;
      if (borrowDt.Rows.Count > 0 && borrowDt.Rows[0]["borrowed"] != DBNull.Value)
        borrowedQty = Convert.ToInt32(borrowDt.Rows[0]["borrowed"]);

      return totalStock - borrowedQty;
    }

  }
}
