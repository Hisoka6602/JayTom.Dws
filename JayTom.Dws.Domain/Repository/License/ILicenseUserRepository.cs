using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Repository.License {

    public interface ILicenseUserRepository : IMemoryCacheRepository<LicenseUserInfo> {

        /// <summary>
        /// 详细信息
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DetailsInfo(string userCode, CancellationToken token);
    }
}