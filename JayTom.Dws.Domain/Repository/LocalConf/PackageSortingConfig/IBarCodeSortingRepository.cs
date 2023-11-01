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

        /*//插入
        Task<bool> InsertDetailAsync(BarCodeSortingInfoModel entity, CancellationToken token = default);

        //批量插入
        Task<bool> InsertRangeDetailAsync(List<BarCodeSortingInfoModel> entities, CancellationToken token = default);

        //更新
        Task<bool> UpdateDetailAsync(BarCodeSortingInfoModel entity, CancellationToken token = default);*/
    }
}