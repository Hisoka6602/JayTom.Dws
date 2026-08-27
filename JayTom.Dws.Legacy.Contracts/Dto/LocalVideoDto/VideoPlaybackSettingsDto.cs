using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.LocalVideoDto {

    public class VideoPlaybackSettingsDto {

        /// <summary>
        /// 获取或设置视频长度，以秒为单位。
        /// </summary>
        public int VideoLengthInSeconds { get; set; } = 30;

        /// <summary>
        /// 获取或设置要从视频中减去的秒数。
        /// </summary>
        public int SecondsToSubtract { get; set; } = 3;

        /// <summary>
        /// 获取或设置一个值，指示是否标记水印时间。
        /// </summary>
        public bool IsWatermarkTimeMarked { get; set; } = true;
    }
}