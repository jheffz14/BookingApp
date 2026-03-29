using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http; // for ISession extension methods

public class BaseController : Controller
{
  public override void OnActionExecuting(ActionExecutingContext filterContext)
  {
    // 🔐 Not logged in
    if (HttpContext.Session.GetString("userID") == null)
    {
      filterContext.Result = new RedirectToRouteResult(
          new RouteValueDictionary(
              new { controller = "Login", action = "Index" }
          )
      );
      return;
    }

    base.OnActionExecuting(filterContext);
  }

  protected bool IsUsers()
  {
    var role = HttpContext.Session.GetString("role");
    //return role == "Superadmin" || role == "Admin";
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
