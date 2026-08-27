using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Legacy.Contracts.Repositories.License {

    public interface ILicenseUserRepository : IMemoryCacheRepository<LicenseUserInfo> {

        /// <summary>
        /// 详细信息
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DetailsInfo(string userCode, CancellationToken token);

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="order"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        new Task<KeyValuePair<bool, object>> SelectOrderByDescending<TOrder>(Expression<Func<LicenseUserInfo, bool>> @where,
            Expression<Func<LicenseUserInfo, TOrder>> order, CancellationToken token);

        /// <summary>
        /// 修改用户授权上限数量
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="licensePermissionTemplateInfoId"></param>
        /// <param name="maxLicenseCodeCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> UpdateTenantLicenseMaxCount(string userCode, long licensePermissionTemplateInfoId, int maxLicenseCodeCount,
            CancellationToken cancellationToken);
    }
}
