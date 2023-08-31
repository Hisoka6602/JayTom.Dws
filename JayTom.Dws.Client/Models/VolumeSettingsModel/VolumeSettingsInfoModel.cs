using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.SettingsCommomModels;

namespace JayTom.Dws.Client.Models.VolumeSettingsModel {

    public class VolumeSettingsInfoModel : BindableBase {
        private ObservableCollection<ItemBaseTemplateModel> _dataTemplate = new();
        private bool _isUseTcpInput;
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private bool _triggerVolumeRequest;
        private VolumeInformationRequesterInfoModel _volumeInformationRequesterInfo = new();
        private string _separator = string.Empty;
        private VolumeUnit _unit = VolumeUnit.Millimeter;

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
        /// 是否使用Tcp输入
        /// </summary>
        public bool IsUseTcpInput {
            get => _isUseTcpInput;
            set => SetProperty(ref _isUseTcpInput, value);
        }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfoModel TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }

        /// <summary>
        /// 是否主动触发体积获取
        /// </summary>
        public bool IsTriggerVolumeRequest {
            get => _triggerVolumeRequest;
            set => SetProperty(ref _triggerVolumeRequest, value);
        }

        /// <summary>
        /// 发送参数
        /// </summary>
        public VolumeInformationRequesterInfoModel VolumeInformationRequesterInfo {
            get => _volumeInformationRequesterInfo;
            set => SetProperty(ref _volumeInformationRequesterInfo, value);
        }
    }
}