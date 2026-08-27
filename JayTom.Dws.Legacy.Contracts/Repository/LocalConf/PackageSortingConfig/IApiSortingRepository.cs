using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface IApiSortingRepository : ISortingDetailRepository<ApiSortingInfoModel> {

        Task<List<ApiSortingInfoModel>> ApiSortingItems(Expression<Func<ApiSortingInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取接口响应分拣配置及其规则明细。</summary>
        async Task<List<ApiSortingInfoModel>> ISortingDetailRepository<ApiSortingInfoModel>.SelectDetails(
            Expression<Func<ApiSortingInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await ApiSortingItems(predicate, cancellationToken).ConfigureAwait(false);

    }
}
