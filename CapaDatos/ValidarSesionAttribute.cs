using System.Web.Mvc;

public class ValidarSesionAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var session = filterContext.HttpContext.Session;
        if (session["ClienteId"] == null)
        {
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary {
                    { "controller", "Acceso" },
                    { "action", "Index" }
                });
        }
        base.OnActionExecuting(filterContext);
    }
}