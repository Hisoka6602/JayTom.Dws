using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting {
    public class LogisticsCodeRecognitionItemInfoModel : BasePackageSortingItemInfoModel {
        private string _logisticsCode = string.Empty;
        private string _logisticsName = string.Empty;
        private byte[]? _soundBytes;
        private ImageSource? _icon;
        private string _exitName = string.Empty;
        private string _regexPattern = string.Empty;

        /// <summary>
        /// 物流代码
        /// </summary>
        public string LogisticsCode {
            get => _logisticsCode;
            set => SetProperty(ref _logisticsCode, value);
        }

        /// <summary>
        /// 物流名称
        /// </summary>
        public string LogisticsName {
            get => _logisticsName;
            set => SetProperty(ref _logisticsName, value);
        }

        /// <summary>
        /// 声音
        /// </summary>
        public byte[]? SoundBytes {
            get => _soundBytes;
            set => SetProperty(ref _soundBytes, value);
        }

        /// <summary>
        /// 图标
        /// </summary>
        public ImageSource? Icon {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// 绑定格口
        /// </summary>
        public string ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }
    }
}