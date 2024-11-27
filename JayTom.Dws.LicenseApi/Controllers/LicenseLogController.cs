using RTools_NTS.Util;
using JayTom.Dws.Data.License;
using JayTom.Dws.LicenseApi.Vo;
using Microsoft.AspNetCore.Mvc;
using JayTom.Dws.LicenseApi.Do;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.LicenseApi.Filter;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Domain.Repository.License;
using JayTom.Dws.Application.Service.LicenseApi;
using JayTom.Dws.Infrastructure.Repository.License;

namespace JayTom.Dws.LicenseApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class LicenseLogController : ControllerBase {
        private readonly ILicenseLogAppService _licenseLogAppService;
        private readonly ILicenseUserRepository _licenseUserRepository;

        public LicenseLogController(ILicenseLogAppService licenseLogAppService,
            ILicenseUserRepository licenseUserRepository) {
            _licenseLogAppService = licenseLogAppService;
            _licenseUserRepository = licenseUserRepository;
        }

        [HttpPost("LicenseAuthorizationLog"),
         UserStatus(Status = UserStatus.Active),
         UserRole(Role = (int)(UserRole.SuperAdmin | UserRole.Tenant)),
         Authorize]
        public async Task<JsonResult> LicenseAuthorizationLog([FromBody] LicenseAuthorizationLogDo param, CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var licenseUserInfos = await _licenseUserRepository.MemoryCacheData();

            var userInfo = licenseUserInfos.FirstOrDefault(f => f.UserCode.Equals(code));
            if (userInfo?.Role != UserRole.SuperAdmin) {
                param.UserCode = userInfo?.UserCode;
            }

            var (key, value) = await _licenseLogAppService.GetLicenseAuthorizationLog(param.StartTime, param.EndTime, param.LicenseCode, param.UserCode);

            return key ? JsonResultVo.Success("查询成功", value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }
    }
}