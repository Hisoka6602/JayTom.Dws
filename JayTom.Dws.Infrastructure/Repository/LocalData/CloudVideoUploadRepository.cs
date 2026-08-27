using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Infrastructure.Repository.LocalData
{

    public class CloudVideoUploadRepository : LocalRepositoryBase<CloudVideoUploadInfoModel, SqliteContext>, ICloudVideoUploadRepository {

        public CloudVideoUploadRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}