using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class OcrInfoItemModel : BindableBase {
        private string _recipientAddress = string.Empty;
        private string _recipientName = string.Empty;
        private string _recipientPhone = string.Empty;
        private string _senderName = string.Empty;
        private string _senderPhone = string.Empty;
        private string _threeSegmentCode = string.Empty;
        private long _elapsedTime;

        /// <summary>
        /// 收件人地址。
        /// </summary>
        public string RecipientAddress {
            get => _recipientAddress;
            set => SetProperty(ref _recipientAddress, value);
        }

        /// <summary>
        /// 收件人姓名。
        /// </summary>
        public string RecipientName {
            get => _recipientName;
            set => SetProperty(ref _recipientName, value);
        }

        /// <summary>
        /// 收件人电话。
        /// </summary>
        public string RecipientPhone {
            get => _recipientPhone;
            set => SetProperty(ref _recipientPhone, value);
        }

        /// <summary>
        /// 寄件人姓名。
        /// </summary>
        public string SenderName {
            get => _senderName;
            set => SetProperty(ref _senderName, value);
        }

        /// <summary>
        /// 寄件人电话。
        /// </summary>
        public string SenderPhone {
            get => _senderPhone;
            set => SetProperty(ref _senderPhone, value);
        }

        /// <summary>
        /// 三段码。
        /// </summary>
        public string ThreeSegmentCode {
            get => _threeSegmentCode;
            set => SetProperty(ref _threeSegmentCode, value);
        }

        /// <summary>
        /// 耗时(ms)
        /// </summary>
        public long ElapsedTime {
            get => _elapsedTime;
            set => SetProperty(ref _elapsedTime, value);
        }
    }
}