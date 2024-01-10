using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.License;
using JayTom.Dws.Domain.Repository.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseApplicationRepository :
        RepositoryBase<LicenseApplicationInfo>, ILicenseApplicationRepository {

        public LicenseApplicationRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}