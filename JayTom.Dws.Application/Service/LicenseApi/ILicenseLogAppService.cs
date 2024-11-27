using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public interface ILicenseLogAppService {

        /// <summary>
        /// 获取授权日志
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