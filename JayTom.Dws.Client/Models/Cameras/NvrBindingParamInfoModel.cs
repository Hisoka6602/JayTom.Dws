using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras
{

    public class NvrBindingParamInfoModel : BindableBase
    {
        private string _serialNumber = string.Empty;
        private string _displayIdentifier = string.Empty;
        private SourceType _bindingSource = SourceType.None;
        private string _remarks = string.Empty;

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string SerialNumber
        {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        /// <summary>
        /// 显示标识
        /// </summary>

        public string DisplayIdentifier
        {
            get => _displayIdentifier;
            set => SetProperty(ref _displayIdentifier, value);
        }

        /// <summary>
        /// 绑定源
        /// </summary>
        public SourceType BindingSource
        {
            get => _bindingSource;
            set => SetProperty(ref _bindingSource, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }
    }
}