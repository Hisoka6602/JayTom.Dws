using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseClientBindingRepository :
    RepositoryBase<LicenseClientBindingInfo, LicenseApiContext>, ILicenseClientBindingRepository {

        public LicenseClientBindingRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}