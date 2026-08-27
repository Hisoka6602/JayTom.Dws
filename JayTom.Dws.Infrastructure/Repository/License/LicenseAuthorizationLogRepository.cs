using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.License;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseAuthorizationLogRepository : RepositoryBase<LicenseAuthorizationLog, LicenseApiContext>, ILicenseAuthorizationLogRepository {

        public LicenseAuthorizationLogRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}