using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Integrations.Contracts {

    public interface INetworkTime {

        /// <summary>
        /// 获取带时区偏移的本地网络时间
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<DateTimeOffset> GetLocalTimeAsync(CancellationToken cancellationToken = default);
    }
}
