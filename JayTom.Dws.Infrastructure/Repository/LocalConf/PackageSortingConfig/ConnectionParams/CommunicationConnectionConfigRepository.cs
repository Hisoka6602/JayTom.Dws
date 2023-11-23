using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams {

    public class CommunicationConnectionConfigRepository : LocalRepositoryBase<CommunicationConnectionConfigInfoModel>, ICommunicationConnectionConfigRepository {

        public CommunicationConnectionConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public Task<List<CommunicationConnectionConfigInfoModel>> CommunicationConnectionConfigItems(Expression<Func<CommunicationConnectionConfigInfoModel, bool>> where, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> InsertDetailAsync(CommunicationConnectionConfigInfoModel entity, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> InsertRangeDetailAsync(List<CommunicationConnectionConfigInfoModel> entities, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateDetailAsync(CommunicationConnectionConfigInfoModel entity, CancellationToken token = default) {
            throw new NotImplementedException();
        }
    }
}