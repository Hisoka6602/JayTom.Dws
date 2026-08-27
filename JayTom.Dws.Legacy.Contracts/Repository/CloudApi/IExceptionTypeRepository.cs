using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;
using JayTom.Dws.Models.CloudApiData;

namespace JayTom.Dws.Legacy.Contracts.Repositories.CloudApi {

    public interface IExceptionTypeRepository : IMemoryCacheRepository<ExceptionTypeInfoModel> {
    }
}