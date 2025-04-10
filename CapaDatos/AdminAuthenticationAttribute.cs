using System.Web.Mvc;
using System.Web;

namespace REFOOD.Filters
{
    public class AdminAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Check if admin is logged in
            if (filterContext.HttpContext.Session["AdminEmail"] == null)
            {
                // Redirect to login page if not authenticated
                filterContext.Result = new RedirectResult("/Acceso/Index");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}