using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Client.Models.DataModels {

    public class VolumeItemModel : BindableBase {
        private SourceType _sourceType;
        private string _originalText = string.Empty;
        private double _formattedLength;
        private double _formattedWidth;
        private double _formattedHeight;
        private double _formattedVolume;
        private DateTime? _createTime;

        /// <summary>
        /// 来源类型
        /// </summary>
        public SourceType SourceType {
            get => _sourceType;
            set => SetProperty(ref _sourceType, value);
        }

        /// <summary>
        /// 源字符
        /// </summary>
        public string OriginalText {
            get => _originalText;
            set => SetProperty(ref _originalText, value);
        }

        /// <summary>
        /// 格式化后的长
        /// </summary>
        public double FormattedLength {
            get => _formattedLength;
            set => SetProperty(ref _formattedLength, value);
        }

        /// <summary>
        /// 格式化后的宽
        /// </summary>
        public double FormattedWidth {
            get => _formattedWidth;
            set => SetProperty(ref _formattedWidth, value);
        }

        /// <summary>
        /// 格式化后的高
        /// </summary>
        public double FormattedHeight {
            get => _formattedHeight;
            set => SetProperty(ref _formattedHeight, value);
        }

        /// <summary>
        /// 格式化的体积
        /// </summary>
        public double FormattedVolume {
            get => _formattedVolume;
            set => SetProperty(ref _formattedVolume, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }
    }
}