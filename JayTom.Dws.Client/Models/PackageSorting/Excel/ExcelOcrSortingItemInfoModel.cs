using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;

namespace JayTom.Dws.Client.Models.PackageSorting.Excel
{

    public class ExcelOcrSortingItemInfoModel : BasePackageSortingItemInfoModel
    {
        private long? _exitId;
        private string? _exitName;
        private string _sortingName = string.Empty;
        private bool _isUseThreeSegmentCodeValidation;
        private bool _isUseRecipientAddressValidation;
        private string _threeSegmentCodeContainsChars = string.Empty;
        private string _recipientAddressContainsChars = string.Empty;
        private bool _isUseSenderAddressValidation;
        private string _senderAddressContainsChars = string.Empty;
        private bool _isUseSenderPhoneNumberValidation;
        private string _senderPhoneNumberEndsWith = string.Empty;

        /// <summary>
        /// 出口代码
        /// </summary>
        public long? ExitId
        {
            get => _exitId;
            set => SetProperty(ref _exitId, value);
        }

        /// <summary>
        /// 出口名称
        /// </summary>
        [DisplayName("格口名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string? ExitName
        {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 规则名称
        /// </summary>
        [DisplayName("规则名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string SortingName
        {
            get => _sortingName;
            set => SetProperty(ref _sortingName, value);
        }

        /// <summary>
        /// 是否使用三段码判断
        /// </summary>
        [DisplayName("是否使用三段码判断"), MemberNotNull, ExcelInfo(Width = 6000, IsBooleanToInt = true)]
        public bool IsUseThreeSegmentCodeValidation
        {
            get => _isUseThreeSegmentCodeValidation;
            set => SetProperty(ref _isUseThreeSegmentCodeValidation, value);
        }

        /// <summary>
        /// 三段码包含字符
        /// </summary>
        [DisplayName("三段码包含字符"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string ThreeSegmentCodeContainsChars
        {
            get => _threeSegmentCodeContainsChars;
            set => SetProperty(ref _threeSegmentCodeContainsChars, value);
        }

        /// <summary>
        /// 是否使用收件人地址判断
        /// </summary>
        [DisplayName("是否使用收件人地址判断"), MemberNotNull, ExcelInfo(Width = 7000, IsBooleanToInt = true)]
        public bool IsUseRecipientAddressValidation
        {
            get => _isUseRecipientAddressValidation;
            set => SetProperty(ref _isUseRecipientAddressValidation, value);
        }

        /// <summary>
        /// 收件人地址包含字符
        /// </summary>
        [DisplayName("收件人地址包含字符"), MemberNotNull, ExcelInfo(Width = 7000)]
        public string RecipientAddressContainsChars
        {
            get => _recipientAddressContainsChars;
            set => SetProperty(ref _recipientAddressContainsChars, value);
        }

        /// <summary>
        /// 是否使用发件人地址判断
        /// </summary>
        [DisplayName("是否使用发件人地址判断"), MemberNotNull, ExcelInfo(Width = 7000, IsBooleanToInt = true)]
        public bool IsUseSenderAddressValidation
        {
            get => _isUseSenderAddressValidation;
            set => SetProperty(ref _isUseSenderAddressValidation, value);
        }

        /// <summary>
        /// 发件人地址包含字符
        /// </summary>
        [DisplayName("发件人地址包含字符"), MemberNotNull, ExcelInfo(Width = 7000)]
        public string SenderAddressContainsChars
        {
            get => _senderAddressContainsChars;
            set => SetProperty(ref _senderAddressContainsChars, value);
        }

        /// <summary>
        /// 是否使用发件人手机尾号判断
        /// </summary>
        [DisplayName("是否使用发件人手机尾号判断"), MemberNotNull, ExcelInfo(Width = 8000, IsBooleanToInt = true)]
        public bool IsUseSenderPhoneNumberValidation
        {
            get => _isUseSenderPhoneNumberValidation;
            set => SetProperty(ref _isUseSenderPhoneNumberValidation, value);
        }

        /// <summary>
        /// 发件人手机尾号
        /// </summary>
        [DisplayName("发件人手机尾号"), MemberNotNull, ExcelInfo(Width = 7000)]
        public string SenderPhoneNumberEndsWith
        {
            get => _senderPhoneNumberEndsWith;
            set => SetProperty(ref _senderPhoneNumberEndsWith, value);
        }
    }
}