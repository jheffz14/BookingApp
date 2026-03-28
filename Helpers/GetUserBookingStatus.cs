using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetUserBookingStatus  // ✅ class declaration missing!
  {
    public List<SelectListItem> GetBookingStatusList(string role)
    {
      if (role == "Admin" || role == "Superadmin")
      {
        return new List<SelectListItem>
                {
                    new SelectListItem { Text = "Approved", Value = "Approved" },
                    new SelectListItem { Text = "Disapproved", Value = "Disapproved" }
                };
      }

      // Users role
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "Pending", Value = "Pending" }
            };
    }
  }
}
