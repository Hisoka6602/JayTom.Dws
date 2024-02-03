using JayTom.Dws.Data.License;
using JayTom.Dws.LicenseApi.Do;
using Microsoft.AspNetCore.Mvc;
using JayTom.Dws.LicenseApi.Vo;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.LicenseApi.Filter;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Application.Service.LicenseApi;

namespace JayTom.Dws.LicenseApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase {
        private readonly ILicenseApplicationAppService _licenseApplicationAppService;
        private readonly ILicenseCodeAppService _licenseCodeAppService;

        public LicenseController(ILicenseApplicationAppService licenseApplicationAppService,
            ILicenseCodeAppService licenseCodeAppService) {
            _licenseApplicationAppService = licenseApplicationAppService;
            _licenseCodeAppService = licenseCodeAppService;
        }

        /// <summary>
        /// 创建授权码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("CreateLicenseCode"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> CreateLicenseCode([FromBody] CreateLicenseCodeDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.CreateLicenseCode(param.TemplateInfoId,
                code ?? string.Empty, param.MaxClientCount, param.ExpirationDate,
                param.ClientName, cancellationToken);
            return key ? JsonResultVo.Success("创建成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 授权码数据列表
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("LicenseCodeData"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> LicenseCodeData(CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.LicenseCodeData(code ?? string.Empty, cancellationToken);

            return key ? JsonResultVo.Success("查询成功", value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 延期授权码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("ExtendLicenseCodeValidity"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> ExtendLicenseCodeValidity([FromBody] ExtendLicenseCodeValidityDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.ExtendLicenseCodeValidity(code ?? string.Empty, param.LicenseCode,
                param.ExpirationDate, cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 冻结授权码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("FreezeLicenseCode"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> FreezeLicenseCode([FromBody] FreezeLicenseCodeDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.FreezeLicenseCode(code ?? string.Empty, param.LicenseCode, param.IsFreeze,
                cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        [Produces("application/json")]
        [HttpPost("BulkExtendLicenseCodeValidity"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> BulkExtendLicenseCodeValidity([FromBody] BulkExtendLicenseCodeValidityDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.BulkExtendLicenseCodeValidity(code ?? string.Empty, param.LicenseCodes ?? new List<string>(),
                param.ExpirationDate, cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        [HttpPost("DownloadLicenseFile")]
        public async Task<IActionResult> DownloadLicenseFile([FromBody] DownloadLicenseFileDo param) {
            //下载授权文件/如果没激活则需要激活(绑定)

            return JsonResultVo.Fail("未实现该方法");
        }
    }
}