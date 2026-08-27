using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig {

    public class WeightRuleRepository : LocalRepositoryBase<WeightRuleInfoModel, SqliteConfContext>, IWeightRuleRepository {

        public WeightRuleRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}