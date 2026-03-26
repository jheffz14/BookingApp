using Microsoft.AspNetCore.Mvc;

namespace BookingAppV2.Controllers
{
  public class AccessDeniedController : Controller
  {
    public IActionResult Index()
    {
      Response.StatusCode = 403;
      return View();
    }
  }
}
