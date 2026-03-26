using Microsoft.AspNetCore.Mvc;

namespace BookingAppV2.Controllers
{
  public class DashboardsController : BaseController
  {
    public IActionResult Index()
    {
      return View();
    }
  }
}
