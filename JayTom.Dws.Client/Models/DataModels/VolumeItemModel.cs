using System;
using Prism.Mvvm;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.DataModels
{

    public class VolumeItemModel : BindableBase
    {
        private SourceType _sourceType;
        private string _originalText = string.Empty;
        private decimal _formattedLength;
        private decimal _formattedWidth;
        private decimal _formattedHeight;
        private decimal _formattedVolume;
        private DateTime? _createTime;

        /// <summary>
        /// 来源类型
        /// </summary>
        public SourceType SourceType
        {
            get => _sourceType;
            set => SetProperty(ref _sourceType, value);
        }

        /// <summary>
        /// 源字符
        /// </summary>
        public string OriginalText
        {
            get => _originalText;
            set => SetProperty(ref _originalText, value);
        }

        /// <summary>
        /// 格式化后的长
        /// </summary>
        public decimal FormattedLength
        {
            get => _formattedLength;
            set => SetProperty(ref _formattedLength, value);
        }

        /// <summary>
        /// 格式化后的宽
        /// </summary>
        public decimal FormattedWidth
        {
            get => _formattedWidth;
            set => SetProperty(ref _formattedWidth, value);
        }

        /// <summary>
        /// 格式化后的高
        /// </summary>
        public decimal FormattedHeight
        {
            get => _formattedHeight;
            set => SetProperty(ref _formattedHeight, value);
        }

        /// <summary>
        /// 格式化的体积
        /// </summary>
        public decimal FormattedVolume
        {
            get => _formattedVolume;
            set => SetProperty(ref _formattedVolume, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }
    }
}