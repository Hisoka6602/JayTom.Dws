using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel
{

    public class MachineReplyInfoModel : BindableBase
    {
        private bool _isVerificationEnabled;
        private int _timeout;
        private int _maxRetryCount;

        /// <summary>
        /// 获取或设置一个值，指示是否启用下位机回复的验证功能。
        /// </summary>
        public bool IsVerificationEnabled
        {
            get => _isVerificationEnabled;
            set => SetProperty(ref _isVerificationEnabled, value);
        }

        /// <summary>
        /// 获取或设置验证超时时间（以毫秒为单位）。
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        /// <summary>
        /// 获取或设置最大重试次数。
        /// </summary>
        public int MaxRetryCount
        {
            get => _maxRetryCount;
            set => SetProperty(ref _maxRetryCount, value);
        }
    }
}