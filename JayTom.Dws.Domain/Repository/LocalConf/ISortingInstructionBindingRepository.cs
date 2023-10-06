using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf {

    public interface ISortingInstructionBindingRepository : IRepository<SortingInstructionBindingInfoModel> {

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<SortingInstructionBindingInfoModel>> InstructionBindings(Expression<Func<SortingInstructionBindingInfoModel, bool>> @where, CancellationToken token = default);
    }
}