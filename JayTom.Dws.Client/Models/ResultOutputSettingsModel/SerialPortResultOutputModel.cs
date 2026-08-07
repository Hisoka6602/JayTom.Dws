using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel
{
    public class SerialPortResultOutputModel : BindableBase
    {
        private bool _isUseDataTemplateOutput;
        private bool _isUseCustomContentOutput;
        private string _customOutputContent = string.Empty;

        /// <summary>
        /// 是否使用数据模板输出
        /// </summary>
        public bool IsUseDataTemplateOutput
        {
            get => _isUseDataTemplateOutput;
            set => SetProperty(ref _isUseDataTemplateOutput, value);
        }

        /// <summary>
        /// 是否使用自定义内容输出
        /// </summary>
        public bool IsUseCustomContentOutput
        {
            get => _isUseCustomContentOutput;
            set => SetProperty(ref _isUseCustomContentOutput, value);
        }

        /// <summary>
        /// 自定义内容
        /// </summary>
        public string CustomOutputContent
        {
            get => _customOutputContent;
            set => SetProperty(ref _customOutputContent, value);
        }
    }
}
