using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Data.CloudApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.CloudApi;

namespace JayTom.Dws.Infrastructure.Repository.CloudApi {

    public class ExceptionTypeRepository : MemoryCacheRepositoryBase<ExceptionTypeInfoModel>, IExceptionTypeRepository {

        public ExceptionTypeRepository(IDbContextFactory<CloudApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}