using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto {

    public class OcrRuleJsonDto {

        /// <summary>
        /// 是否使用三段码判断
        /// </summary>
        public bool IsUseThreeSegmentCodeValidation { get; set; }

        /// <summary>
        /// 三段码包含字符
        /// </summary>
        public string ThreeSegmentCodeContainsChars { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用收件人地址判断
        /// </summary>
        public bool IsUseRecipientAddressValidation { get; set; }

        /// <summary>
        /// 收件人地址包含字符
        /// </summary>
        public string RecipientAddressContainsChars { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用发件人地址判断
        /// </summary>
        public bool IsUseSenderAddressValidation { get; set; }

        /// <summary>
        /// 发件人地址包含字符
        /// </summary>
        public string SenderAddressContainsChars { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用发件人手机尾号判断
        /// </summary>
        public bool IsUseSenderPhoneNumberValidation { get; set; }

        /// <summary>
        /// 发件人手机尾号
        /// </summary>
        public string SenderPhoneNumberEndsWith { get; set; } = string.Empty;
    }
}