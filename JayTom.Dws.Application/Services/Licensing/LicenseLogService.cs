using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Repositories.License;

namespace JayTom.Dws.Application.Services.LicenseApi {

    public class LicenseLogService : ILicenseLogService {
        private readonly ILicenseAuthorizationLogRepository _licenseAuthorizationLogRepository;

        public LicenseLogService(ILicenseAuthorizationLogRepository licenseAuthorizationLogRepository) {
            _licenseAuthorizationLogRepository = licenseAuthorizationLogRepository;
        }

        public async Task<KeyValuePair<bool, object>> GetLicenseAuthorizationLog(DateTime? startTime,
            DateTime? endTime, string? licenseCode, string? userCode) {
            try {
                var orderByDescending = await _licenseAuthorizationLogRepository.SelectOrderByDescending(w =>
                        (startTime == null || w.OperationTime >= startTime) &&
                        (endTime == null || w.OperationTime <= endTime) &&
                        (string.IsNullOrEmpty(licenseCode) || w.LicenseCode.Equals(licenseCode)) &&
                        (string.IsNullOrEmpty(userCode) || w.UserCode.Equals(userCode)),
                    o => o.OperationTime);

                return new KeyValuePair<bool, object>(true, orderByDescending);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, e.Message);
            }
        }
    }
}