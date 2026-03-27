using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetUserBookingStatus
  {
    public List<SelectListItem> GetBookingStatus()
    {
      return new List<SelectListItem>
    {
        new SelectListItem { Text = "Pending", Value = "Pending" }
    };
    }
  }
}
