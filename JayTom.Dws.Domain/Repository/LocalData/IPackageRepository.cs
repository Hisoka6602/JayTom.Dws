using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.LocalData {

    public interface IPackageRepository : IRepository<PackageInfoModel> {

        /// <summary>
        /// 查询条码数据(联表)
        /// </summary>
        /// <typeparam name="TOrder"></typeparam>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackageOrderByDescending<TOrder>(
            [NotNull] Expression<Func<PackageInfoModel, bool>> @where,
            [NotNull] Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default);

        Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackage<TOrder>(
            [NotNull] Expression<Func<PackageInfoModel, bool>> @where,
            [NotNull] Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default);

        /// <summary>
        /// 查询一条数据
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, PackageInfoModel>> FirstOrDefaultInfo([NotNull] Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token = default);

        new Task<int> Total([NotNull] Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>
        /// 查询缓存数据
        /// </summary>
        /// <param name="packageTimestamped"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<PackageInfoModel?> GetCachedPackage(long packageTimestamped, CancellationToken token = default);

        /// <summary>
        /// 查询前后N条数据
        /// </summary>
        /// <param name="packageTimestamped"></param>
        /// <param name="amount"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, List<PackageInfoModel>>> GetPackagesAround(
            long packageTimestamped,
            int amount,
            CancellationToken token = default);

        /// <summary>
        /// 查询详细信息
        /// </summary>
        /// <param name="packageTimestamped"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<PackageInfoModel?> GetPackageDetails(long packageTimestamped, CancellationToken token = default);

        /// <summary>
        /// 设置缓存时间
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        bool SetCacheDuration(TimeSpan duration);

        /// <summary>
        /// 填充信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packageTimestamped"></param>
        /// <param name="property"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        Task<bool> FillNavigationPropertyAsync<T>(long packageTimestamped, T property, int retryCount = 5)
            where T : class;
    }
}