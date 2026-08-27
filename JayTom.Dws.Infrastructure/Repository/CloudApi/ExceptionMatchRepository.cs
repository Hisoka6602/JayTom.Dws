using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.CloudApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.CloudApi;

namespace JayTom.Dws.Infrastructure.Repository.CloudApi {

    public class ExceptionMatchRepository : MemoryCacheRepositoryBase<ExceptionMatchInfoModel, CloudApiContext>, IExceptionMatchRepository {

        public ExceptionMatchRepository(IDbContextFactory<CloudApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}