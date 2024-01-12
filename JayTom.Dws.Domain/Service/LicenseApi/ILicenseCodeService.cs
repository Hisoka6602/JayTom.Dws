using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Service.LicenseApi {

    public interface ILicenseCodeService {

        /// <summary>
        /// 创建授权码
        /// </summary>
        /// <param name="templateInfoId"></param>
        /// <param name="userCode"></param>
        /// <param name="licenseCode"></param>
        /// <param name="maxClientCount"></param>
        /// <param name="expirationDate"></param>
        /// <param name="clientName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId,
            string userCode,
            string licenseCode,
            int maxClientCount,
            DateTime expirationDate,
            string clientName,
            CancellationToken token);

        /// <summary>
        /// 授权码数据
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> LicenseCodeData(string userCode, CancellationToken token);

        /// <summary>
        /// 设置到期时间
        /// </summary>
        /// <param name="expirationDate"></param>
        /// <param name="token"></param>
        /// <param name="userCode"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> ExtendLicenseCodeValidity(string userCode,
            string licenseCode, DateTime expirationDate, CancellationToken token);
    }
}