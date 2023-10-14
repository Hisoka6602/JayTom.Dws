using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig {
    public interface ILogisticsSortingRepository : IRepository<LogisticsSortingInfoModel> {

        Task<List<LogisticsSortingInfoModel>> LogisticsSortingItems(Expression<Func<LogisticsSortingInfoModel, bool>> @where, CancellationToken token = default);
    }
}