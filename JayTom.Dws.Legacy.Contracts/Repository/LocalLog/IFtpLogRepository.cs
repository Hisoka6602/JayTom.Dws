using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalLog {

    public interface IFtpLogRepository : ILogMaintenanceRepository<FtpLogInfoModel> {

        /// <summary>
        /// 删除N天前的数据
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        new Task<KeyValuePair<bool, string>> DeleteDataThanDays(int days, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除最早的一天的数据
        /// </summary>
        /// <returns></returns>
        new Task<KeyValuePair<bool, string>> DeleteEarliestData(CancellationToken cancellationToken = default);
    }
}
