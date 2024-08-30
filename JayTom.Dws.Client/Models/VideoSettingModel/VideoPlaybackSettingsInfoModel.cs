using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.VideoSettingModel {

    public class VideoPlaybackSettingsInfoModel : BindableBase {
        private int _videoLengthInSeconds = 30;
        private int _secondsToSubtract = 3;
        private bool _isWatermarkTimeMarked = true;

        /// <summary>
        /// 获取或设置视频长度，以秒为单位。
        /// </summary>
        public int VideoLengthInSeconds {
            get => _videoLengthInSeconds;
            set => SetProperty(ref _videoLengthInSeconds, value);
        }

        /// <summary>
        /// 获取或设置要从视频中减去的秒数。
        /// </summary>
        public int SecondsToSubtract {
            get => _secondsToSubtract;
            set => SetProperty(ref _secondsToSubtract, value);
        }

        /// <summary>
        /// 获取或设置一个值，指示是否标记水印时间。
        /// </summary>
        public bool IsWatermarkTimeMarked {
            get => _isWatermarkTimeMarked;
            set => SetProperty(ref _isWatermarkTimeMarked, value);
        }
    }
}