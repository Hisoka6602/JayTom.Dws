using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class SoundRepository : LocalRepositoryBase<SoundInfoModel, SqliteContext>, ISoundRepository {

        public SoundRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}