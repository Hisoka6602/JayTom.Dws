using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface {

    public interface INetworkTime {

        /// <summary>
        /// 获取时间
        /// </summary>
        /// <returns></returns>
        public Task<DateTime> GetTime();
    }
}