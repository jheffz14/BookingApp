using BookingAppV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BookingAppV2.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;

  public override void OnActionExecuting(ActionExecutingContext filterContext)
  {
    base.OnActionExecuting(filterContext);
    if (filterContext.Result != null) return;

    if (!IsAdmin())
    {
      filterContext.Result = RedirectToAction("Index", "AccessDenied");
      return;
    }
  }

  public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
