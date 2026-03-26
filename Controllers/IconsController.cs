using BookingAppV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BookingAppV2.Controllers;

public class IconsController : BaseController
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
  public IActionResult Boxicons() => View();
}
