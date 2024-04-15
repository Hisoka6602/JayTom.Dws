using Prism.Mvvm;
using JayTom.Dws.Domain.Converters;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.SettingsCommomModels;

namespace JayTom.Dws.Client.Models.VolumeSettingsModel {

    public class VolumeSettingsInfoModel : BindableBase {
        private ObservableCollection<ItemBaseTemplateModel> _dataTemplate = new();
        private bool _isUseExternalVolumeInput;
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private bool _triggerVolumeRequest;
        private VolumeInformationRequesterInfoModel _volumeInformationRequesterInfo = new();
        private string _separator = string.Empty;
        private VolumeUnit _unit = VolumeUnit.Millimeter;
        private bool _isUseFusionTimeout;
        private int _fusionTimeout;
        private int _triggerDelayMilliseconds;

        /// <summary>
        /// 体积单位
        /// </summary>
        public VolumeUnit Unit {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 数据模板
        /// </summary>
        public ObservableCollection<ItemBaseTemplateModel> DataTemplate {
            get => _dataTemplate;
            set => SetProperty(ref _dataTemplate, value);
        }

        /// <summary>
        /// 分隔符
        /// </summary>
        public string Separator {
            get => _separator;
            set => SetProperty(ref _separator, value);
        }

        /// <summary>
        /// 是否使用外部体积输入
        /// </summary>
        public bool IsUseExternalVolumeInput {
            get => _isUseExternalVolumeInput;
            set => SetProperty(ref _isUseExternalVolumeInput, value);
        }

        /// <summary>
        /// 是否主动触发体积获取
        /// </summary>
        public bool IsTriggerVolumeRequest {
            get => _triggerVolumeRequest;
            set => SetProperty(ref _triggerVolumeRequest, value);
        }

        /// <summary>
        /// 是否使用融合超时
        /// </summary>
        public bool IsUseFusionTimeout {
            get => _isUseFusionTimeout;
            set => SetProperty(ref _isUseFusionTimeout, value);
        }

        /// <summary>
        /// 融合超时时间
        /// </summary>
        public int FusionTimeout {
            get => _fusionTimeout;
            set => SetProperty(ref _fusionTimeout, value);
        }

        /// <summary>
        /// 发送参数
        /// </summary>
        public VolumeInformationRequesterInfoModel VolumeInformationRequesterInfo {
            get => _volumeInformationRequesterInfo;
            set => SetProperty(ref _volumeInformationRequesterInfo, value);
        }

        /// <summary>
        /// 触发延迟(毫秒)
        /// </summary>
        public int TriggerDelayMilliseconds {
            get => _triggerDelayMilliseconds;
            set => SetProperty(ref _triggerDelayMilliseconds, value);
        }
    }
}