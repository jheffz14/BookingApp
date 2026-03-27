using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BookingAppV2.Helpers
{
  public class GetRolesUsers
  {
    public static List<SelectListItem> GetRoles()
    {
      return new List<SelectListItem>
            {
                new SelectListItem { Text = "SuperAdmin", Value = "SuperAdmin" },
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "User", Value = "User" }
            };
    }
  }
}
