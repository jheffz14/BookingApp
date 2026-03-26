using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookingAppV2.Models
{
  public class Users
  {
    public string userID { get; set; }
    public string pass_word { get; set; }
    public string role { get; set; }

    public string DepartmentID { get; set; }
    public string DepartmentName { get; set; } // optional (for views)

  }
}
