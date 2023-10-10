using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig
{

    public interface ILogisticsCodeRecognitionRepository : IRepository<LogisticsCodeRecognitionInfoModel>
    {

        /// <summary>
        /// 查询(包含正则)
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<LogisticsCodeRecognitionInfoModel>> LogisticsCodes(Expression<Func<LogisticsCodeRecognitionInfoModel, bool>> @where, CancellationToken token = default);
    }
}