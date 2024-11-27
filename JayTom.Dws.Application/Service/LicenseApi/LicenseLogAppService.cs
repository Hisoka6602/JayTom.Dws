using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Service.LicenseApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public class LicenseLogAppService : ILicenseLogAppService {
        private readonly ILicenseLogService _licenseLogService;

        public LicenseLogAppService(ILicenseLogService licenseLogService) {
            _licenseLogService = licenseLogService;
        }

        public Task<KeyValuePair<bool, object>> GetLicenseAuthorizationLog(DateTime? startTime, DateTime? endTime, string? licenseCode, string? userCode) {
            return _licenseLogService.GetLicenseAuthorizationLog(startTime, endTime, licenseCode, userCode);
        }
    }
}