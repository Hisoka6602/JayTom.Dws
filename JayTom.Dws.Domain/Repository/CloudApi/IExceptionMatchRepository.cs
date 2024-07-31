using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.CloudApiData;

namespace JayTom.Dws.Domain.Repository.CloudApi {

    public interface IExceptionMatchRepository : IMemoryCacheRepository<ExceptionMatchInfoModel> {
    }
}