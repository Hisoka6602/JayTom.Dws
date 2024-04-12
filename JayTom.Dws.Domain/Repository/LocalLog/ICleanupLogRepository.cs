using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Repository.LocalLog {

    public interface ICleanupLogRepository : IRepository<LogCleaningLogInfoModel> {

        /// <summary>
        /// 删除N天前的数据
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> DeleteDataThanDays(int days);

        /// <summary>
        /// 删除最早的一天的数据
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> DeleteEarliestData();
    }
}