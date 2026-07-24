using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using NPOI.SS.Formula.Functions;
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
            Expression<Func<PackageInfoModel, bool>> @where,
            Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default);

        Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackage<TOrder>(
            Expression<Func<PackageInfoModel, bool>> @where,
            Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default);

        /// <summary>
        /// 查询一条数据
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, PackageInfoModel>> FirstOrDefaultInfo(Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token = default);

        new Task<int> Total(Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>
        /// 获取缓存包裹数据
        /// </summary>
        /// <param name="packageTimestamped"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<PackageInfoModel?> GetMemoryCachePackageInfo(long packageTimestamped, CancellationToken token = default);

        /// <summary>
        /// 更新缓存
        /// </summary>
        /// <param name="info"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void UpDateMemoryCachePackageInfo(PackageInfoModel info, CancellationToken token = default);
    }
}
