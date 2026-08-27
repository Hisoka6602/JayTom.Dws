using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.CloudApiData;

namespace JayTom.Dws.Legacy.Contracts.Repositories.CloudApi {

    public interface IExceptionMatchRepository : IMemoryCacheRepository<ExceptionMatchInfoModel> {
    }
}