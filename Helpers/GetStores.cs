using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetStores
  {
    public static List<SelectListItem> GetStoreList()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "61", Value = "61" },
                new SelectListItem { Text = "62", Value = "62" },
                new SelectListItem { Text = "63", Value = "63" },
                new SelectListItem { Text = "64", Value = "64" },
                new SelectListItem { Text = "65", Value = "65" },
                new SelectListItem { Text = "67", Value = "67" },
                new SelectListItem { Text = "68", Value = "68" },
                new SelectListItem { Text = "69", Value = "69" },
                new SelectListItem { Text = "73", Value = "73" },
                new SelectListItem { Text = "99", Value = "99" }
            };
    }
  }
}
