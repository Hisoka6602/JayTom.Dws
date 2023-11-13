using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.Timer {

    public class TimerDto {

        /// <summary>
        /// 耗时时长
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 格式化耗时
        /// </summary>
        public string FormattedElapsed { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>

        public DateTime StartTime { get; set; }
    }
}