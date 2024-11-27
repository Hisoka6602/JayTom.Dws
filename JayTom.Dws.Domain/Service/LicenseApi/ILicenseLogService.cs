using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Service.LicenseApi {

    public interface ILicenseLogService {

        /// <summary>
        /// 授权日志
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="licenseCode"></param>
        /// <param name="userCode"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> GetLicenseAuthorizationLog(DateTime? startTime,
            DateTime? endTime, string? licenseCode, string? userCode);
    }
}