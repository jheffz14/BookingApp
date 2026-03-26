using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookingAppV2.Models
{
  public class Booking
  {
    public int bookingid { get; set; } // Auto-increment, keep as int
    public string itemID { get; set; }  // Changed from int to string
    public string departmentID { get; set; } // Changed from int to string
    public string userID { get; set; }  // Changed from int to string
    public int quantity { get; set; }
    public string purpose { get; set; }
    public DateTime? date_requested { get; set; }
    //public DateTime? date_borrowed { get; set; }
    public DateTime? date_returned { get; set; }
    public string status { get; set; }
    public string remarks { get; set; }
  }
}
