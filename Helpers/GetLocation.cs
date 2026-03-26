using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetLocation
  {
    public static List<SelectListItem> GetReportList()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "Bonus Card", Value = "PerBarangay" },
                new SelectListItem { Text = "Kanegosyo", Value = "Kanegosyo" },
                new SelectListItem { Text = "Cotabato City", Value = "Cotabato" },
                new SelectListItem { Text = "Municipality", Value = "Municipality" },
                new SelectListItem { Text = "DOS", Value = "DOS" },
                new SelectListItem { Text = "Parang", Value = "Parang" },
                new SelectListItem { Text = "South Upi", Value = "South Upi" },
                new SelectListItem { Text = "North Upi", Value = "North Upi" },
                new SelectListItem { Text = "Sultan Kudarat", Value = "Sultan Kudarat" },
                new SelectListItem { Text = "Sultan Mastura", Value = "Sultan Mastura" },
                new SelectListItem { Text = "Talayan", Value = "Talayan" },
                new SelectListItem { Text = "Datu Anggal Midtimbang", Value = "Datu Anggal Midtimbang" },
                new SelectListItem { Text = "Guindolongan", Value = "Guindolongan" }
            };
    }
  }
}
