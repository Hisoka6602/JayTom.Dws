using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface IVolumeSortingRepository : ISortingDetailRepository<VolumeSortingInfoModel> {

        Task<List<VolumeSortingInfoModel>> VolumeSortingItems(Expression<Func<VolumeSortingInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取体积分拣配置及其规则明细。</summary>
        async Task<List<VolumeSortingInfoModel>> ISortingDetailRepository<VolumeSortingInfoModel>.SelectDetails(
            Expression<Func<VolumeSortingInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await VolumeSortingItems(predicate, cancellationToken).ConfigureAwait(false);

    }
}
