using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel {

    public class UploadSettingsInfoModel : BindableBase {
        private int _retryCount;
        private int _sendDelay;
        private bool _isAutoUploadOnRestart;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount {
            get => _retryCount;
            set => SetProperty(ref _retryCount, value);
        }

        /// <summary>
        /// 发送延迟
        /// </summary>
        public int SendDelay {
            get => _sendDelay;
            set => SetProperty(ref _sendDelay, value);
        }

        /// <summary>
        /// 是否程序重启后自动上传未成功数据
        /// </summary>
        public bool IsAutoUploadOnRestart {
            get => _isAutoUploadOnRestart;
            set => SetProperty(ref _isAutoUploadOnRestart, value);
        }
    }
}