using Prism.Mvvm;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.LocalLog;

namespace JayTom.Dws.Client.Models.WeightSettingsModel
{

    public class StaticWeightParamsModel : BindableBase
    {
        private int _dataInterval = 50;
        private bool _isReversed;
        private WeightAccessMode _accessMode = WeightAccessMode.Readonly;
        private int _balanceCount = 10;
        private decimal _balanceQty = 0.002m;
        private string _identifier = "=";
        private int _characterLength = 8;
        private int _identifierPosition = 0;
        private int _integerStartPosition;
        private int _integerEndPosition;
        private int _decimalStartPosition;
        private int _decimalEndPosition;
        private string _sendingContent = string.Empty;
        private DataFormatType _sendingFormat = DataFormatType.Ascii;

        /// <summary>
        /// 每条数据间隔时间(采样频率)
        /// </summary>
        public int DataInterval
        {
            get => _dataInterval;
            set => SetProperty(ref _dataInterval, value);
        }

        /// <summary>
        /// 是否反转
        /// </summary>
        public bool IsReversed
        {
            get => _isReversed;
            set => SetProperty(ref _isReversed, value);
        }

        /// <summary>
        /// 获取方式
        /// </summary>
        public WeightAccessMode AccessMode
        {
            get => _accessMode;
            set => SetProperty(ref _accessMode, value);
        }

        /// <summary>
        /// 稳定个数
        /// </summary>
        public int BalanceCount
        {
            get => _balanceCount;
            set => SetProperty(ref _balanceCount, value);
        }

        /// <summary>
        /// 稳定精度(误差范围)
        /// </summary>
        public decimal BalanceQty
        {
            get => _balanceQty;
            set => SetProperty(ref _balanceQty, value);
        }

        /// <summary>
        /// 标识符
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 字符长度
        /// </summary>
        public int CharacterLength
        {
            get => _characterLength;
            set => SetProperty(ref _characterLength, value);
        }

        /// <summary>
        /// 标识符位置
        /// </summary>
        public int IdentifierPosition
        {
            get => _identifierPosition;
            set => SetProperty(ref _identifierPosition, value);
        }

        /// <summary>
        /// 整数起始位置
        /// </summary>
        public int IntegerStartPosition
        {
            get => _integerStartPosition;
            set => SetProperty(ref _integerStartPosition, value);
        }

        /// <summary>
        /// 整数结束位置
        /// </summary>
        public int IntegerEndPosition
        {
            get => _integerEndPosition;
            set => SetProperty(ref _integerEndPosition, value);
        }

        /// <summary>
        /// 小数起始位置
        /// </summary>
        public int DecimalStartPosition
        {
            get => _decimalStartPosition;
            set => SetProperty(ref _decimalStartPosition, value);
        }

        /// <summary>
        /// 小数结束位置
        /// </summary>
        public int DecimalEndPosition
        {
            get => _decimalEndPosition;
            set => SetProperty(ref _decimalEndPosition, value);
        }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string SendingContent
        {
            get => _sendingContent;
            set => SetProperty(ref _sendingContent, value);
        }

        /// <summary>
        /// 发送格式
        /// </summary>
        public DataFormatType SendingFormat
        {
            get => _sendingFormat;
            set => SetProperty(ref _sendingFormat, value);
        }
    }
}
