using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig {

    public interface IOcrSortingRepository : IRepository<OcrSortingInfoModel> {

        Task<List<OcrSortingInfoModel>> OcrSortingItems(Expression<Func<OcrSortingInfoModel, bool>> @where, CancellationToken token = default);

        /*//插入
        Task<bool> InsertDetailAsync(OcrSortingInfoModel entity, CancellationToken token = default);

        //批量插入
        Task<bool> InsertRangeDetailAsync(List<OcrSortingInfoModel> entities, CancellationToken token = default);

        //更新
        Task<bool> UpdateDetailAsync(OcrSortingInfoModel entity, CancellationToken token = default);*/
    }
}