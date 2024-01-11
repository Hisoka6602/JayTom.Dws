using JayTom.Dws.Data.License;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.LicenseApi.Filter {

    public class UserRoleAttribute : ActionFilterAttribute {
        public UserRole Role { get; set; }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next) {
            //获取Token

            var userCode = context.HttpContext.Response.HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(userCode)) {
                context.Result = await Task.FromResult(new JsonResult(new { Result = false, Msg = "Token错误!" }) { StatusCode = 200 });
                return;
            }

            var licenseUserRepository = context.HttpContext.RequestServices.GetService<ILicenseUserRepository>();
            if (licenseUserRepository is not null) {
                var licenseUserInfo = licenseUserRepository?.MemoryCacheData()?.
                    ConfigureAwait(false).GetAwaiter().GetResult()
                    ?.FirstOrDefault(f => f.UserCode.Equals(userCode));

                if (licenseUserInfo is null) {
                    context.Result = await Task.FromResult(new JsonResult(new { Result = false, Msg = "账号不存在!" }) { StatusCode = 200 });
                    return;
                }
                else {
                    if (licenseUserInfo.Role != Role) {
                        context.Result = await Task.FromResult(new JsonResult(new { Result = false, Msg = "您无权限访问!" }) { StatusCode = 200 });
                        return;
                    }
                }
            }
            else {
                context.Result = await Task.FromResult(new JsonResult(new { Result = false, Msg = "系统配置错误!" }) { StatusCode = 200 });
                return;
            }
            await base.OnActionExecutionAsync(context, next);
        }
    }
}