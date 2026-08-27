using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface IBarCodeSortingRepository : ISortingDetailRepository<BarCodeSortingInfoModel> {

        Task<List<BarCodeSortingInfoModel>> BarCodeSortingItems(Expression<Func<BarCodeSortingInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取条码分拣配置及其规则明细。</summary>
        async Task<List<BarCodeSortingInfoModel>> ISortingDetailRepository<BarCodeSortingInfoModel>.SelectDetails(
            Expression<Func<BarCodeSortingInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await BarCodeSortingItems(predicate, cancellationToken).ConfigureAwait(false);

    }
}
