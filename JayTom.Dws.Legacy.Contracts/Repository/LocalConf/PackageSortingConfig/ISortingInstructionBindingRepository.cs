using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig {

    public interface ISortingInstructionBindingRepository : IRepository<SortingInstructionBindingInfoModel> {

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<SortingInstructionBindingInfoModel>> InstructionBindings(Expression<Func<SortingInstructionBindingInfoModel, bool>> @where, CancellationToken token = default);

        //插入
        Task<bool> InsertDetailAsync(SortingInstructionBindingInfoModel entity, CancellationToken token = default);

        //批量插入
        Task<bool> InsertRangeDetailAsync(List<SortingInstructionBindingInfoModel> entities, CancellationToken token = default);

        //更新
        Task<bool> UpdateDetailAsync(SortingInstructionBindingInfoModel entity, CancellationToken token = default);
    }
}