using JayTom.Dws.Domain.Dto;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel
{

    public class AudioOutputSettingsInfoModel : BindableBase
    {
        private string? _successAudio;
        private string? _failureAudio;
        private TriggerPositionEnum _triggerPosition;
        private ResultEnum _result;

        /// <summary>
        /// 成功音频
        /// </summary>
        public string? SuccessAudio
        {
            get => _successAudio;
            set => SetProperty(ref _successAudio, value);
        }

        /// <summary>
        /// 失败音频
        /// </summary>
        public string? FailureAudio
        {
            get => _failureAudio;
            set => SetProperty(ref _failureAudio, value);
        }

        /// <summary>
        /// 触发位置
        /// </summary>
        public TriggerPositionEnum TriggerPosition
        {
            get => _triggerPosition;
            set => SetProperty(ref _triggerPosition, value);
        }

        /// <summary>
        /// 结果判断
        /// </summary>
        public ResultEnum Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }
    }
}