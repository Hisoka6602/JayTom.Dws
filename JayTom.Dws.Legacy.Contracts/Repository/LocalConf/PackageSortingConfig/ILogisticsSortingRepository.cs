using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface ILogisticsSortingRepository : ISortingDetailRepository<LogisticsSortingInfoModel> {

        Task<List<LogisticsSortingInfoModel>> LogisticsSortingItems(Expression<Func<LogisticsSortingInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取物流分拣配置及其规则明细。</summary>
        async Task<List<LogisticsSortingInfoModel>> ISortingDetailRepository<LogisticsSortingInfoModel>.SelectDetails(
            Expression<Func<LogisticsSortingInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await LogisticsSortingItems(predicate, cancellationToken).ConfigureAwait(false);

    }
}
