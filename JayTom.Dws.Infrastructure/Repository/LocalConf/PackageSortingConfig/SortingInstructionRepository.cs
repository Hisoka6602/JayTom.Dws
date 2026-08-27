using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class SortingInstructionRepository : LocalRepositoryBase<SortingInstructionInfoModel, SqliteConfContext>, ISortingInstructionRepository {

        public SortingInstructionRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}