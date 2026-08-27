using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface ILogisticsCodeRecognitionRepository : ISortingDetailRepository<LogisticsCodeRecognitionInfoModel> {

        /// <summary>
        /// 查询(包含正则)
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<LogisticsCodeRecognitionInfoModel>> LogisticsCodes(Expression<Func<LogisticsCodeRecognitionInfoModel, bool>> @where, CancellationToken token = default);

        /// <summary>读取物流编码配置及其规则明细。</summary>
        async Task<List<LogisticsCodeRecognitionInfoModel>> ISortingDetailRepository<LogisticsCodeRecognitionInfoModel>.SelectDetails(
            Expression<Func<LogisticsCodeRecognitionInfoModel, bool>> predicate,
            CancellationToken cancellationToken) =>
            await LogisticsCodes(predicate, cancellationToken).ConfigureAwait(false);
    }
}
