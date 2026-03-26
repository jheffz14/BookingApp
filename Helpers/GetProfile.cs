using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetProfile
  {
    public static List<SelectListItem> GetProfileList()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "Cashier", Value = "Cashier" },
                new SelectListItem { Text = "Clerk/Bagger", Value = "Clerk/Bagger" },
                new SelectListItem { Text = "Supervisor", Value = "Supervisor" },
                new SelectListItem { Text = "OrderTaker", Value = "OrderTaker" },
                new SelectListItem { Text = "Supervisor 10% limit", Value = "Supervisor 10% limit" },
                new SelectListItem { Text = "Supervisor 20% limit", Value = "Supervisor 20% limit" },
                new SelectListItem { Text = "Supervisor 30% limit", Value = "Supervisor 30% limit" },
                new SelectListItem { Text = "Supervisor 50% limit", Value = "Supervisor 50% limit" }
            };
    }
  }
}
