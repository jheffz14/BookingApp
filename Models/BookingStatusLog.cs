namespace BookingAppV2.Models
{
  public class BookingStatusLog
  {
    public int logID { get; set; }     // Auto-number (optional)
    public int bookingID { get; set; }
    public string itemID { get; set; }
    public string departmentID { get; set; }
    public string oldStatus { get; set; }
    public string newStatus { get; set; }
    public int quantity { get; set; }
    public string purpose { get; set; }
    public DateTime date_requested { get; set; }
    public DateTime date_returned { get; set; }
    public string changedBy { get; set; }
    public DateTime changedDate { get; set; }
    public string remarks { get; set; }
  }
}
