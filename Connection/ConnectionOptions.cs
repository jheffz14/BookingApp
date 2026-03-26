namespace BookingAppV2.Connection
{
  // Simple POCO to hold the connection string from configuration
  public class ConnectionOptions
  {
    // bound from "ConnectionStrings:BookingApp"
    public string BookingApp { get; set; } = null!;
  }
}
