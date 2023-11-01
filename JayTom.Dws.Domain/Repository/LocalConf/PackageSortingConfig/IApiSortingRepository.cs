using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig {

    public interface IApiSortingRepository : IRepository<ApiSortingInfoModel> {

        Task<List<ApiSortingInfoModel>> ApiSortingItems(Expression<Func<ApiSortingInfoModel, bool>> @where, CancellationToken token = default);

        //插入
        Task<bool> InsertDetailAsync(ApiSortingInfoModel entity, CancellationToken token = default);

        //批量插入
        Task<bool> InsertRangeDetailAsync(List<ApiSortingInfoModel> entities, CancellationToken token = default);

        //更新
        Task<bool> UpdateDetailAsync(ApiSortingInfoModel entity, CancellationToken token = default);
    }
}