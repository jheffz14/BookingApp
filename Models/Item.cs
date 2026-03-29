using System.ComponentModel.DataAnnotations;

namespace BookingAppV2.Models
{
  public class Item
  {
    [Display(Name = "Item ID")]
    public int itemID { get; set; }

    [Display(Name = "Item Name")]
    public string item_name { get; set; }

    [Display(Name = "Total Stock")]
    public int total_stock { get; set; }
  }
}
