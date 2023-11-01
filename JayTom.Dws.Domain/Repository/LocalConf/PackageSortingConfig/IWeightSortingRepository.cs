using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig {

    public interface IWeightSortingRepository : IRepository<WeightSortingInfoModel> {

        Task<List<WeightSortingInfoModel>> WeightSortingItems(Expression<Func<WeightSortingInfoModel, bool>> @where, CancellationToken token = default);

        /*//插入
        Task<bool> InsertDetailAsync(WeightSortingInfoModel entity, CancellationToken token = default);

        //批量插入
        Task<bool> InsertRangeDetailAsync(List<WeightSortingInfoModel> entities, CancellationToken token = default);

        //更新
        Task<bool> UpdateDetailAsync(WeightSortingInfoModel entity, CancellationToken token = default);*/
    }
}