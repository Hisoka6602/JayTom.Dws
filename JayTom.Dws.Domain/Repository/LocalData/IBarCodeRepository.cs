using System.Linq.Expressions;
using JayTom.Dws.Data.LocalData;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.LocalData {

    public interface IBarCodeRepository : IRepository<BarCodeInfoModel> {

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
        Task<KeyValuePair<bool, List<BarCodeInfoModel>>> SelectBarCodeOrderByDescending<TOrder>(
           [NotNull] Expression<Func<BarCodeInfoModel, bool>> @where,
           [NotNull] Expression<Func<BarCodeInfoModel, TOrder>> order, int pageIndex, int pageSize,
           CancellationToken token = default);
    }
}