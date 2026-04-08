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
  public class ItemService
  {
    private readonly dbAccess _dbAccess;

    public ItemService(dbAccess dbAccess)
    {
      _dbAccess = dbAccess;
    }

    public List<SelectListItem> GetItems()
    {
      string query = "SELECT itemID, item_name FROM Items";
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, null);
      var list = new List<SelectListItem>();
      foreach (DataRow row in dt.Rows)
      {
        list.Add(new SelectListItem
        {
          Text = row["item_name"]?.ToString() ?? "",
          Value = row["itemID"]?.ToString() ?? ""
        });
      }
      return list;
    }

    // ✅ Item availability — all roles see this
    public string GetItemQuery()
      {
        string query = @"
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
        return query;
    }


  }
}
