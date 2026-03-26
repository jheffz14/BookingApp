using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;

namespace BookingAppV2.ViewModels
{
  public class EPAReportViewModel
  {
    public string SelectedReport { get; set; }
    public DateTime DateStart { get; set; } = DateTime.Now;
    public DateTime DateEnd { get; set; } = DateTime.Now;
    public List<SelectListItem> ReportList { get; set; }
    public DataTable ReportData { get; set; }
  }
}
