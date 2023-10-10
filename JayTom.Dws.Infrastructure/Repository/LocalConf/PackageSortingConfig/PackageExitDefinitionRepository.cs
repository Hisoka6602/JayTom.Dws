using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class PackageExitDefinitionRepository : LocalRepositoryBase<PackageExitDefinitionInfoModel>, IPackageExitDefinitionRepository {

        public PackageExitDefinitionRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}