using System;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.DataModels
{
    public class OcrItemInfo : BindableBase
    {
        private string _originalContent = string.Empty;
        private string _ocrInterfaceName = string.Empty;
        private string _parsedContent = string.Empty;
        private DateTime? _createTime;
        private bool _isUseOcr;

        /// <summary>
        /// 原始内容
        /// </summary>
        public string OriginalContent
        {
            get => _originalContent;
            set => SetProperty(ref _originalContent, value);
        }

        /// <summary>
        /// 接口名称
        /// </summary>
        public string OcrInterfaceName
        {
            get => _ocrInterfaceName;
            set => SetProperty(ref _ocrInterfaceName, value);
        }

        /// <summary>
        /// 解析后名称
        /// </summary>
        public string ParsedContent
        {
            get => _parsedContent;
            set => SetProperty(ref _parsedContent, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 是否使用Ocr
        /// </summary>
        public bool IsUseOcr
        {
            get => _isUseOcr;
            set => SetProperty(ref _isUseOcr, value);
        }
    }
}