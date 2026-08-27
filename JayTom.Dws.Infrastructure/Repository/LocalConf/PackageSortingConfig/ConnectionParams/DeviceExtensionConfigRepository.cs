using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams {

    public class DeviceExtensionConfigRepository : LocalRepositoryBase<DeviceExtensionConfigInfoModel, SqliteConfContext>, IDeviceExtensionConfigRepository {

        public DeviceExtensionConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}