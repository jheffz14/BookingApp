using BookingAppV2.Connection;
using BookingAppV2.Helpers;
using BookingAppV2.Queries;
using BookingAppV2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Data;
using ClosedXML.Excel;


namespace BookingAppV2.Controllers
{
  public class EPAReportsController : BaseController
  {
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      base.OnActionExecuting(filterContext);
      if (filterContext.Result != null) return;

      if (!IsAdmin())
      {
        filterContext.Result = RedirectToAction("Index", "AccessDenied");
        return;
      }
    }

    public IActionResult Index()
    {
      var model = new EPAReportViewModel
      {
        DateStart = DateTime.Now,
        DateEnd = DateTime.Now,
        ReportList = GetLocation.GetReportList()
      };
      return View(model);
    }

    [HttpPost]
    public IActionResult Index(EPAReportViewModel model)
    {
      model.ReportList = GetLocation.GetReportList();

      string query = GetQuery(model.SelectedReport);

      if (!string.IsNullOrEmpty(query))
      {
        var parameters = EPAQueries.DateParams(
            model.DateStart.ToString("yyyyMMdd"),
            model.DateEnd.ToString("yyyyMMdd")
        );
        //model.ReportData = DBAccess.ExecuteQueryTPR(query, parameters);
      }

      return View(model);
    }

    public IActionResult ExportToExcel(string report, DateTime start, DateTime end)
    {
      string query = GetQuery(report);

      if (string.IsNullOrEmpty(query))
        return RedirectToAction("Index");

      var parameters = EPAQueries.DateParams(
          start.ToString("yyyyMMdd"),
          end.ToString("yyyyMMdd")
      );

      //DataTable dt = DBAccess.ExecuteQueryTPR(query, parameters);

      using (var workbook = new ClosedXML.Excel.XLWorkbook())
      {
        //workbook.Worksheets.Add(dt, "EPA Report");

        using (var stream = new System.IO.MemoryStream())
        {
          workbook.SaveAs(stream);
          stream.Position = 0;

          string fileName = $"EPA_Report_{report}({start:MMMM d, yyyy}, {end:MMMM d, yyyy}).xlsx";

          return File(
              stream.ToArray(),
              "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
              fileName
          );
        }
      }
    }

    // centralized to avoid repetition
    private string GetQuery(string selectedReport)
    {
      return selectedReport switch
      {
        "PerBarangay" => EPAQueries.PerBarangayQuery(),
        "Kanegosyo" => EPAQueries.KanegosyoQuery(),
        "Cotabato" => EPAQueries.CotCityQuery(),
        "Municipality" => EPAQueries.MunicipalityQuery(),
        "DOS" => EPAQueries.DOSQuery(),
        "Parang" => EPAQueries.ParangQuery(),
        "South Upi" => EPAQueries.SouthUpiQuery(),
        "North Upi" => EPAQueries.NorthUpiQuery(),
        "Sultan Kudarat" => EPAQueries.SultanKudaratQuery(),
        "Sultan Mastura" => EPAQueries.SultanMasturaQuery(),
        "Talayan" => EPAQueries.TalayanQuery(),
        "Datu Anggal Midtimbang" => EPAQueries.DAMQuery(),
        "Guindolongan" => EPAQueries.GUINDULUNGANQuery(),
        _ => string.Empty
      };
    }



  }
}
