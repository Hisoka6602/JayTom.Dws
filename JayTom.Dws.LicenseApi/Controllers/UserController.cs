using JayTom.Dws.Data.License;
using Microsoft.AspNetCore.Mvc;
using JayTom.Dws.LicenseApi.Do;
using JayTom.Dws.LicenseApi.Vo;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.LicenseApi.Filter;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Application.Service.LicenseApi;

namespace JayTom.Dws.LicenseApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase {
        private readonly ILicenseUserAppService _licenseUserAppService;

        public UserController(ILicenseUserAppService licenseUserAppService) {
            _licenseUserAppService = licenseUserAppService;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Register")]
        public async Task<JsonResult> Register([FromBody] RegisterDo param,
            CancellationToken cancellationToken) {
            var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var keyValuePair = await _licenseUserAppService.Register(param.UserCode,
                param.UserName, param.PassWord,
                param.Phone, ipAddress ?? string.Empty, cancellationToken);

            return keyValuePair is { Key: true, Value: string msg } ? JsonResultVo.Success(msg) : JsonResultVo.Fail("注册失败!");
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Login"), UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> Login([FromBody] LoginDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _licenseUserAppService.
                Login(param.LoginCode, param.PassWord,
                    cancellationToken);
            return key ? JsonResultVo.Success("登录成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改资料
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("UpdateProfile"), Authorize, UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> UpdateProfile([FromBody] UpdateProfileDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseUserAppService.UpdateProfile(code,
                param.UserName,
                param.Phone,
                param.CompanyName,
                param.CompanyAddress,
                param.ContactEmail,
                param.Description,
                param.ContractFilePath,
                param.BusinessLicenseFilePath, cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("ChangePassword"), Authorize, UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> ChangePassword([FromBody] ChangePasswordDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseUserAppService.ChangePassword(code ?? string.Empty, param.NewPassWord,
                param.NewPassWord, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 个人信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("Info"), Authorize, UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> Info(CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseUserAppService.Info(code, cancellationToken);
            return key ? JsonResultVo.Success("查询成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 冻结用户
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("FreezeUser"), Authorize, UserRole(Role = UserRole.SuperAdmin)]
        public async Task<JsonResult> FreezeUser([FromBody] FreezeUserDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _licenseUserAppService.FreezeUser(param.UserCode,
                param.IsFreeze, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }
    }
}