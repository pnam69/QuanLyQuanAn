using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuanLyQuanAn.Filters
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(
            ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            var user = httpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null);

                return;
            }

            var vaiTro = user.FindFirst("VaiTro")?.Value;

            if (string.IsNullOrEmpty(vaiTro))
            {
                vaiTro = httpContext.Session.GetString("VaiTro");
            }

            if (_roles.Length > 0 &&
                (vaiTro == null || !_roles.Contains(vaiTro)))
            {
                context.Result = new RedirectToActionResult(
                    "AccessDenied",
                    "Account",
                    null);
            }
        }
    }
}
