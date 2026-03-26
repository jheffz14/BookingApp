using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingAppV2.Controllers
{
  public class SABaseController : Controller
  {
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      // 🔐 Not logged in
      if (HttpContext.Session.GetString("user_name") == null)
      {
        filterContext.Result = RedirectToAction("Index", "Login");
        return;
      }
      base.OnActionExecuting(filterContext);
    }

    protected bool IsSuperAdmin()
    {
      var role = HttpContext.Session.GetString("user_type");
      return role == "SuperAdmin";
    }
  }
}
