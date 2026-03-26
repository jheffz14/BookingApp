using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetGender
  {
    public static List<SelectListItem> GetGenderList()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" }
            };
    }
  }
}
