using BookingAppV2.Connection;
using BookingAppV2.Controllers;
using BookingAppV2.Helpers;
using BookingAppV2.Models;
using BookingAppV2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;


namespace BookingAppV2.Controllers
{
  public class ItemController : BaseController
  {
    private readonly dbAccess _dbAccess;
    private readonly ItemService _ItemService;

    public ItemController(dbAccess dbAccess, ItemService itemService)
    {
      _dbAccess = dbAccess;
      _ItemService = itemService;
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

      // ✅ Search filter
      string query;
      List<OleDbParameter>? parameters = null;

      if (!string.IsNullOrEmpty(search))
      {
        query = "SELECT * FROM Items WHERE item_name LIKE ? ORDER BY itemID ASC";
        parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", "%" + search + "%")
        };
      }
      else
      {
        query = "SELECT * FROM Items ORDER BY itemID ASC";
      }

      DataTable allData = _dbAccess.ExecuteQueryBooking(query, parameters);

      int totalRows = allData.Rows.Count;
      int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

      DataTable pagedData = allData.Clone();
      int start = (page - 1) * pageSize;
      int end = Math.Min(start + pageSize, totalRows);

      for (int i = start; i < end; i++)
      {
        pagedData.ImportRow(allData.Rows[i]);
      }

      ViewBag.CurrentPage = page;
      ViewBag.TotalPages = totalPages;
      ViewBag.Search = search; // ✅ keep search value in box

      return View(pagedData);
    }

    // POST: Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Item model)
    {
      try
      {
        var parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", model.item_name),
            new OleDbParameter("?", model.total_stock)
        };

        // ✅ No itemID - Access auto-generates it
        string query = "INSERT INTO Items (item_name, total_stock) VALUES (?, ?)";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "Item added successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error saving item: " + ex.Message;
      }

      return RedirectToAction("Index");
    }

    // POST: Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Item model)
    {
      try
      {
        var parameters = new List<OleDbParameter>
                {
                    new OleDbParameter("?", model.item_name),
                    new OleDbParameter("?", model.total_stock),
                    new OleDbParameter("?", model.itemID)
                };
        string query = "UPDATE Items SET item_name = ?, total_stock = ? WHERE itemID = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "Item updated successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error updating item: " + ex.Message;
      }

      return RedirectToAction("Index");
    }

    // POST: Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)  // ✅ int not string
    {
      try
      {
        var parameters = new List<OleDbParameter>
        {
            new OleDbParameter("?", id)
        };
        string query = "DELETE FROM Items WHERE itemID = ?";
        _dbAccess.ExecuteNonQueryBooking(query, parameters);
        TempData["Success"] = "Item deleted successfully.";
      }
      catch (OleDbException ex)
      {
        TempData["Error"] = ex.Message.Contains("related records")
            ? "Cannot delete — item has related booking transactions."
            : "Error deleting item: " + ex.Message;
      }
      return RedirectToAction("Index");
    }

    // GET: GetItemById for modal
    [HttpGet]
    public JsonResult GetItem(int id)  // ✅ int not string
    {
      string query = "SELECT * FROM Items WHERE itemID = ?";
      var parameters = new List<OleDbParameter>
    {
        new OleDbParameter("?", id)
    };
      DataTable dt = _dbAccess.ExecuteQueryBooking(query, parameters);

      if (dt.Rows.Count == 0) return Json(null);

      var row = dt.Rows[0];
      return Json(new
      {
        itemID = Convert.ToInt32(row["itemID"]),
        item_name = row["item_name"].ToString(),
        total_stock = Convert.ToInt32(row["total_stock"])
      });
    }


  }
}
