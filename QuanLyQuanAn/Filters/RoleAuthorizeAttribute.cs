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

            var maNV = httpContext.Session.GetInt32("MaNV");
            var vaiTro = httpContext.Session.GetString("VaiTro");

            // Chưa đăng nhập
            if (maNV == null)
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null);

                return;
            }

            // Không có quyền
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