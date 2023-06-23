using JayTom.Dws.Data.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalLog;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog {

    internal class InstructionLogRepository : LocalRepositoryBase<InstructionLogInfoModel>, IInstructionLogRepository {

        public InstructionLogRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}