using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.LocalData {

    public interface ISortingRepository : IRepository<SortingInfoModel> {

        new Task<SortingInfoModel?> FirstOrDefault([NotNull] Expression<Func<SortingInfoModel, bool>> @where,
             CancellationToken token = default);
    }
}