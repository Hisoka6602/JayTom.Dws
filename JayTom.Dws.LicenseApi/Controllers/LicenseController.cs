using JayTom.Dws.Data.License;
using JayTom.Dws.LicenseApi.Do;
using Microsoft.AspNetCore.Mvc;
using JayTom.Dws.LicenseApi.Vo;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.LicenseApi.Dto;
using NPOI.SS.Formula.Functions;
using JayTom.Dws.LicenseApi.Filter;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Application.Service.LicenseApi;

namespace JayTom.Dws.LicenseApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase {
        private readonly ILicenseApplicationAppService _licenseApplicationAppService;
        private readonly ILicenseCodeAppService _licenseCodeAppService;
        private readonly ILicenseUserAppService _licenseUserAppService;
        private readonly ILogger<LicenseController> _logger;

        public LicenseController(ILicenseApplicationAppService licenseApplicationAppService,
            ILicenseCodeAppService licenseCodeAppService, ILicenseUserAppService licenseUserAppService, ILogger<LicenseController> logger) {
            _licenseApplicationAppService = licenseApplicationAppService;
            _licenseCodeAppService = licenseCodeAppService;
            _licenseUserAppService = licenseUserAppService;
            _logger = logger;
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
            var isSuperAdminCreated = false;
            var code = HttpContext.Response.HttpContext.User.Identity?.Name ?? string.Empty;
            var (b, o) = await _licenseUserAppService.Info(code, cancellationToken);
            if (b && o is LicenseUserInfo { Role: UserRole.SuperAdmin } && !string.IsNullOrEmpty(param.UserCode)) {
                code = param.UserCode;
                isSuperAdminCreated = true;
            }
            var (key, value) = await _licenseCodeAppService.CreateLicenseCode(param.TemplateInfoId,
                code, param.MaxClientCount, param.ExpirationDate,
                param.ClientName, isSuperAdminCreated, cancellationToken);
            return key ? JsonResultVo.Success("创建成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 批量创建授权码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("BulkCreateLicenseCode"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> BulkCreateLicenseCode([FromBody] BulkCreateLicenseCodeDo param,
            CancellationToken cancellationToken) {
            var isSuperAdminCreated = false;
            var code = HttpContext.Response.HttpContext.User.Identity?.Name ?? string.Empty;
            var (b, o) = await _licenseUserAppService.Info(code, cancellationToken);
            if (b && o is LicenseUserInfo { Role: UserRole.SuperAdmin } && !string.IsNullOrEmpty(param.UserCode)) {
                code = param.UserCode;
                isSuperAdminCreated = true;
            }

            var (key, value) = await _licenseCodeAppService.BulkCreateLicenseCode(param.TemplateInfoId,
                code, param.ExpirationDate, param.ClientName,
                param.LicenseCodeCount, isSuperAdminCreated, cancellationToken);
            return key ? JsonResultVo.Success("创建成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改授权码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("UpdateLicenseCode"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> UpdateLicenseCode([FromBody] UpdateLicenseCodeDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;

            param.UserCode = !string.IsNullOrEmpty(param.UserCode)
                ? await _licenseUserAppService.Info(code ?? string.Empty, cancellationToken) switch {
                    (_, LicenseUserInfo { Role: UserRole.SuperAdmin }) => param.UserCode,
                    _ => code ?? string.Empty
                }
                : code ?? string.Empty;

            var (key, value) = await _licenseCodeAppService.UpdateLicenseCode(param.TemplateInfoId,
                param.UserCode ?? string.Empty, param.LicenseCode, param.MaxClientCount, param.ExpirationDate,
                param.ClientName, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
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
            if (key && value is List<LicenseCodeInfo> infos) {
                var licenseInfoDtos = infos.Select(s => new LicenseInfoDto {
                    Id = s.Id,
                    TemplateName = s.LicensePermissionTemplateInfo?.TemplateName ?? string.Empty,
                    LicenseCode = s.LicenseCode,
                    MaxClientCount = s.MaxClientCount,
                    ActivatedClientCount = s.ActivatedClientCount,
                    ExpirationDate = s.ExpirationDate,
                    ClientName = s.ClientName,
                    IsAvailable = s.IsAvailable,
                    UserName = s.UserInfo?.UserName ?? string.Empty,
                    UserCode = s.UserInfo?.UserCode ?? string.Empty,
                    CreateTime = s.CreateTime,
                    GroupName = s.LicenseGroupInfo?.GroupName ?? string.Empty,
                    MachineCodeItem = s.LicenseClientBindingInfo?.Select(s1 => new LicenseClientBindingDto {
                        FirstActivatedDate = s1.FirstActivatedDate,
                        LastVerifiedDate = s1.LastVerifiedDate,
                        MachineCode = s1.MachineCode,
                        Remarks = s1.Remarks
                    })?.ToList() ?? new List<LicenseClientBindingDto>(),
                })?.ToList() ?? new List<LicenseInfoDto>();
                return JsonResultVo.Success("查询成功", data: licenseInfoDtos);
            }
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

        /// <summary>
        /// 批量延期授权
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 下载授权文件
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("DownloadLicenseFile"), UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<IActionResult> DownloadLicenseFile([FromBody] DownloadLicenseFileDo param, CancellationToken cancellationToken) {
            //下载授权文件/如果没激活则需要激活(绑定)

            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.GetLicenseFileUrl(code, param.LicenseCode, param.MachineCode, param.Remarks, cancellationToken);
            if (!key) return JsonResultVo.Fail(value.ToString() ?? string.Empty);
            var filePath = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/Scr/LicenseFile/{value.ToString()}";

            //激活
            await _licenseCodeAppService.ActivateAuthorization(param.LicenseCode, param.MachineCode, param.Remarks, cancellationToken);
            return JsonResultVo.Success("生成成功", filePath);
        }

        /// <summary>
        /// 解绑机器码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("UnbindMachineCode"), UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<IActionResult> UnbindMachineCode([FromBody] DownloadLicenseFileDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseCodeAppService.UnbindMachineCode(code, param.LicenseCode,
                param.MachineCode, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 创建远程创建授权
        /// </summary>
        /// <returns></returns>
        [HttpPost("CreateAuthorization")]
        public async Task<IActionResult> CreateAuthorization([FromBody] DownloadLicenseFileDo param, CancellationToken cancellationToken) {
            var (key, value) = await _licenseCodeAppService.GetUserCode(param.LicenseCode, cancellationToken);
            if (key && value is LicenseCodeInfo { UserInfo: not null } info) {
                var (b, o) = await _licenseCodeAppService.GetLicenseFileUrl(info.UserInfo.UserCode, param.LicenseCode, param.MachineCode, param.Remarks, cancellationToken);
                if (!b) return JsonResultVo.Fail(o.ToString() ?? string.Empty);
                var filePath = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/Scr/LicenseFile/{o.ToString()}";
                //激活
                await _licenseCodeAppService.ActivateAuthorization(param.LicenseCode, param.MachineCode, param.Remarks, cancellationToken);
                return JsonResultVo.Success("生成成功", filePath);
            }
            else {
                return JsonResultVo.Fail(value.ToString() ?? string.Empty);
            }
        }

        /// <summary>
        /// 激活授权
        /// </summary>
        /// <returns></returns>
        [HttpPost("ActivateAuthorization")]
        public async Task<IActionResult> ActivateAuthorization([FromBody] DownloadLicenseFileDo param, CancellationToken cancellationToken) {
            var (key, value) = await _licenseCodeAppService.ActivateAuthorization(param.LicenseCode, param.MachineCode, param.Remarks, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }
    }
}