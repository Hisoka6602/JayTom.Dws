using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.VideoApi {

    public interface IVideoPackageRepository : IRepository<PackageInfoModel> {

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

        /// <summary>
        /// 统计符合条件的数据总数
        /// </summary>
        /// <param name="where">筛选条件</param>
        /// <param name="token">取消标记</param>
        /// <returns>符合条件的数据总数</returns>
        new Task<int> Total(Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>
        /// 查询设备信息列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, List<string>>> SelectNodeInfos(CancellationToken token = default);

        /// <summary>
        /// 查询 Nvr 信息列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, List<NvrInfoModel>>> SelectNvrInfos(CancellationToken token = default);
    }
}
