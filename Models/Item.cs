using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;  // ← needed for Display

namespace BookingAppV2.Models
{
  public class Item
  {

    [Display(Name = "Item")]
    public string itemID { get; set; }
    public string item_name { get; set; }
    public int total_stock { get; set; }
  }
}
