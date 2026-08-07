using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.AppSettingModel
{

    public class OtherSettingsModel : BindableBase
    {
        private bool _isAutoMaximize;
        private bool _isAutoStart;
        private string _programTitle = string.Empty;
        private string _programLogoPath = string.Empty;
        private bool _isAutoRunEnabled;

        /// <summary>
        /// 是否自动最大化
        /// </summary>
        public bool IsAutoMaximize
        {
            get => _isAutoMaximize;
            set => SetProperty(ref _isAutoMaximize, value);
        }

        /// <summary>
        /// 是否自动启动
        /// </summary>
        public bool IsAutoStart
        {
            get => _isAutoStart;
            set => SetProperty(ref _isAutoStart, value);
        }

        /// <summary>
        /// 是否开机自动运行
        /// </summary>
        public bool IsAutoRunEnabled
        {
            get => _isAutoRunEnabled;
            set => SetProperty(ref _isAutoRunEnabled, value);
        }

        /// <summary>
        /// 程序标题
        /// </summary>
        public string ProgramTitle
        {
            get => _programTitle;
            set => SetProperty(ref _programTitle, value);
        }

        /// <summary>
        /// 程序Logo路径
        /// </summary>
        public string ProgramLogoPath
        {
            get => _programLogoPath;
            set => SetProperty(ref _programLogoPath, value);
        }
    }
}