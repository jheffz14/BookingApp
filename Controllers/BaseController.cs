using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BookingAppV2.Controllers  // ✅ add namespace
{
  public class BaseController : Controller
  {
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      string? userID = HttpContext.Session.GetString("userID");
      string? isDefault = HttpContext.Session.GetString("is_default_password");
      string? controller = filterContext.RouteData.Values["controller"]?.ToString();
      string? action = filterContext.RouteData.Values["action"]?.ToString();

      // 🔐 Not logged in → go to Login
      if (string.IsNullOrEmpty(userID))
      {
        if (controller != "Login")
        {
          filterContext.Result = new RedirectToActionResult("Index", "Login", null);
          return;
        }
        base.OnActionExecuting(filterContext);
        return;
      }

      // 🔐 Default password → force change password
      if (isDefault == "true" &&
          controller != "ChangePass" &&  // ✅ allow ALL actions under ChangePass
          controller != "Login")
      {
        filterContext.Result = new RedirectToActionResult("Index", "ChangePass", null);
        return;
      }

      base.OnActionExecuting(filterContext);
    }

    protected bool IsUsers()
    {
      var role = HttpContext.Session.GetString("role");
      return role == "Users";
    }

    protected bool IsAdmin()
    {
      var role = HttpContext.Session.GetString("role");
      return role == "Admin";
    }

    protected bool IsSuperAdmin()
    {
      var role = HttpContext.Session.GetString("role");
      return role == "Superadmin";
    }
  }
}
