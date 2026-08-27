using Prism.Mvvm;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.ImageSettingModels;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel
{
    public class DefaultApiModel : BindableBase
    {
        private ObservableCollection<ItemBaseTemplateModel> _dataTemplate = new();

        private bool _isUseJsonUpload;
        private string _url = string.Empty;
        private int _timeout = 1000;
        private ResponseValidationMode _validationMode = ResponseValidationMode.StringContains;
        private string _completeMatch = string.Empty;
        private string _stringContains = string.Empty;
        private string _regularExpression = string.Empty;
        private bool _isUseUploadImage;
        private bool _isUploadScanImage;
        private bool _isUploadPanoramaImage;

        /// <summary>
        /// 数据模板
        /// </summary>
        public ObservableCollection<ItemBaseTemplateModel> DataTemplate
        {
            get => _dataTemplate;
            set => SetProperty(ref _dataTemplate, value);
        }

        /// <summary>
        /// 是否使用Json上传
        /// </summary>
        public bool IsUseJsonUpload
        {
            get => _isUseJsonUpload;
            set => SetProperty(ref _isUseJsonUpload, value);
        }

        /// <summary>
        /// Url
        /// </summary>
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        /// <summary>
        /// 验证模式
        /// </summary>
        public ResponseValidationMode ValidationMode
        {
            get => _validationMode;
            set => SetProperty(ref _validationMode, value);
        }

        /// <summary>
        /// 完全匹配的内容
        /// </summary>
        public string CompleteMatch
        {
            get => _completeMatch;
            set => SetProperty(ref _completeMatch, value);
        }

        /// <summary>
        /// 包含字符串的内容
        /// </summary>
        public string StringContains
        {
            get => _stringContains;
            set => SetProperty(ref _stringContains, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression
        {
            get => _regularExpression;
            set => SetProperty(ref _regularExpression, value);
        }

        /// <summary>
        /// 是否上传图片
        /// </summary>
        public bool IsUseUploadImage
        {
            get => _isUseUploadImage;
            set => SetProperty(ref _isUseUploadImage, value);
        }

        /// <summary>
        /// 是否上传扫码图
        /// </summary>
        public bool IsUploadScanImage
        {
            get => _isUploadScanImage;
            set => SetProperty(ref _isUploadScanImage, value);
        }

        /// <summary>
        /// 是否上传全景图
        /// </summary>
        public bool IsUploadPanoramaImage
        {
            get => _isUploadPanoramaImage;
            set => SetProperty(ref _isUploadPanoramaImage, value);
        }
    }
}