using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingAppV2.Controllers
{
  public class SABaseController : Controller
  {
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      // 🔐 Not logged in
      if (HttpContext.Session.GetString("UserID") == null)
      {
        filterContext.Result = RedirectToAction("Index", "AccessDenied");
        return;
      }
      base.OnActionExecuting(filterContext);
    }

    protected bool IsSuperAdmin()
    {
      var role = HttpContext.Session.GetString("Role");
      return role == "Superadmin";
    }
  }
}
