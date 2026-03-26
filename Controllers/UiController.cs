using BookingAppV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BookingAppV2.Controllers;

public class UiController : BaseController
{
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


  public IActionResult Accordion() => View();
  public IActionResult Alerts() => View();
  public IActionResult Badges() => View();
  public IActionResult Buttons() => View();
  public IActionResult Carousel() => View();
  public IActionResult Collapse() => View();
  public IActionResult Dropdowns() => View();
  public IActionResult Footer() => View();
  public IActionResult ListGroups() => View();
  public IActionResult Modals() => View();
  public IActionResult Navbar() => View();
  public IActionResult Offcanvas() => View();
  public IActionResult PaginationBreadcrumbs() => View();
  public IActionResult Progress() => View();
  public IActionResult Spinners() => View();
  public IActionResult TabsPills() => View();
  public IActionResult Toasts() => View();
  public IActionResult TooltipsPopovers() => View();
  public IActionResult Typography() => View();
}
