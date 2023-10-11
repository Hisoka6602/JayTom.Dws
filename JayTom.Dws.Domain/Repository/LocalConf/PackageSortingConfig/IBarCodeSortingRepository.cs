using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig {
    public interface IBarCodeSortingRepository : IRepository<BarCodeSortingInfoModel> {

        Task<List<BarCodeSortingInfoModel>> BarCodeSortingItems(Expression<Func<BarCodeSortingInfoModel, bool>> @where, CancellationToken token = default);
    }
}