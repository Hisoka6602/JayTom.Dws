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
    }
}