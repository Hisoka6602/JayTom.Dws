using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface IOcrSortingRepository : ISortingDetailRepository<OcrSortingInfoModel> {

        Task<List<OcrSortingInfoModel>> OcrSortingItems(Expression<Func<OcrSortingInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取 OCR 分拣配置及其规则明细。</summary>
        async Task<List<OcrSortingInfoModel>> ISortingDetailRepository<OcrSortingInfoModel>.SelectDetails(
            Expression<Func<OcrSortingInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await OcrSortingItems(predicate, cancellationToken).ConfigureAwait(false);

    }
}
