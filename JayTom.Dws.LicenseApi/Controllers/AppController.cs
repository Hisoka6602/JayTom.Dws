using JayTom.Dws.Data.License;
using JayTom.Dws.LicenseApi.Do;
using JayTom.Dws.LicenseApi.Vo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.LicenseApi.Filter;
using JayTom.Dws.Domain.Dto.LicenseApi;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Application.Service.LicenseApi;

namespace JayTom.Dws.LicenseApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase {
        private readonly ILicenseApplicationAppService _licenseApplicationAppService;

        public AppController(ILicenseApplicationAppService licenseApplicationAppService) {
            _licenseApplicationAppService = licenseApplicationAppService;
        }

        /// <summary>
        /// 创建应用
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("CreateApplication"),
         UserStatus(Status = UserStatus.Active),
        UserRole(Role = (int)UserRole.SuperAdmin),
         Authorize]
        public async Task<JsonResult> CreateApplication([FromBody] CreateApplicationDo param,
            CancellationToken cancellationToken) {
            var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var (key, value) = await _licenseApplicationAppService.CreateApplication(
                param.ApplicationName,
                param.Description,
                ipAddress ?? string.Empty,
                (param.FeatureInfos ?? new List<FeatureDo>()).Select(s => new LicenseFeatureDto() {
                    Description = s.Description,
                    FeatureGuid = s.FeatureGuid,
                    FeatureName = s.FeatureName,
                    IsActive = s.IsActive
                }).ToList(),
                cancellationToken
            );
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 创建应用模板
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("CreateApplicationTemplate"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> CreateApplicationTemplate([FromBody] CreateApplicationTemplateDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseApplicationAppService.CreateApplicationTemplate(param.LicenseApplicationInfoId,
                param.TemplateName, code ?? string.Empty, cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 设置模板权限
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("SetTemplatePermissions"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> SetTemplatePermissions([FromBody] SetTemplatePermissionsDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseApplicationAppService.SetTemplatePermissions(code ?? string.Empty, param.TemplateId, param.FeatureInfos?.Select(
                    s => new LicenseFeatureDto() {
                        Description = s.Description,
                        FeatureGuid = s.FeatureGuid,
                        FeatureName = s.FeatureName,
                        IsActive = s.IsActive
                    })?.ToList() ?? new List<LicenseFeatureDto>(),
                cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 获取应用列表
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("ApplicationData"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> ApplicationData(CancellationToken cancellationToken) {
            var (key, value) = await _licenseApplicationAppService.ApplicationData(cancellationToken);
            return key ? JsonResultVo.Success("查询成功", value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 获取模板列表
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("TemplateData"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> TemplateData(CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseApplicationAppService.TemplateData(code ?? string.Empty, cancellationToken);
            return key ? JsonResultVo.Success("查询成功", value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 删除应用
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("DeleteApplication"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)UserRole.SuperAdmin),
         Authorize]
        public async Task<JsonResult> DeleteApplication([FromBody] DeleteApplicationDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _licenseApplicationAppService.DeleteApplication(
                param.DeleteApplicationId,
                cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("DeleteTemplate"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> DeleteTemplate([FromBody] DeleteTemplateDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseApplicationAppService.DeleteTemplate(
                code ?? string.Empty,
                param.DeleteTemplateId,
                cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }
    }
}