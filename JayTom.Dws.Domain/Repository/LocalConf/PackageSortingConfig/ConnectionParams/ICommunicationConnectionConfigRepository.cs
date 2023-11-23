using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams {

    public interface ICommunicationConnectionConfigRepository : IRepository<CommunicationConnectionConfigInfoModel> {

        Task<List<CommunicationConnectionConfigInfoModel>> CommunicationConnectionConfigItems(Expression<Func<CommunicationConnectionConfigInfoModel, bool>> @where, CancellationToken token = default);

        //插入
        Task<bool> InsertDetailAsync(CommunicationConnectionConfigInfoModel entity, CancellationToken token = default);

        //批量插入
        Task<bool> InsertRangeDetailAsync(List<CommunicationConnectionConfigInfoModel> entities, CancellationToken token = default);

        //更新
        Task<bool> UpdateDetailAsync(CommunicationConnectionConfigInfoModel entity, CancellationToken token = default);
    }
}