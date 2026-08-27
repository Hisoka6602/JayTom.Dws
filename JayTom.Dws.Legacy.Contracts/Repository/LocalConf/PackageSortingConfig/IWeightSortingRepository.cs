using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface IWeightSortingRepository : ISortingDetailRepository<WeightSortingInfoModel> {

        Task<List<WeightSortingInfoModel>> WeightSortingItems(Expression<Func<WeightSortingInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取重量分拣配置及其规则明细。</summary>
        async Task<List<WeightSortingInfoModel>> ISortingDetailRepository<WeightSortingInfoModel>.SelectDetails(
            Expression<Func<WeightSortingInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await WeightSortingItems(predicate, cancellationToken).ConfigureAwait(false);

    }
}
