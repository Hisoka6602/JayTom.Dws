using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class ApiAccessViewModel : BindableBase, IDialogAware {
        private string _requestStatus = "NotUploaded";
        private DateTime? _requestTime;
        private string _requestContent = string.Empty;
        private DateTime? _responseTime;
        private string _responseContent = string.Empty;
        private string _barcode = string.Empty;

        /// <summary>
        /// 上传状态
        /// </summary>
        public string RequestStatus {
            get => _requestStatus;
            set => SetProperty(ref _requestStatus, value);
        }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime? RequestTime {
            get => _requestTime;
            set => SetProperty(ref _requestTime, value);
        }

        /// <summary>
        /// 上传内容
        /// </summary>
        public string RequestContent {
            get => _requestContent;
            set => SetProperty(ref _requestContent, value);
        }

        /// <summary>
        /// 接口响应时间
        /// </summary>
        public DateTime? ResponseTime {
            get => _responseTime;
            set => SetProperty(ref _responseTime, value);
        }

        /// <summary>
        /// 接口响应内容
        /// </summary>
        public string ResponseContent {
            get => _responseContent;
            set => SetProperty(ref _responseContent, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
            Barcode = string.Empty;
            RequestStatus = string.Empty;
            RequestTime = null;
            RequestContent = string.Empty;
            ResponseTime = null;
            ResponseContent = string.Empty;
        }

        public async void OnDialogOpened(IDialogParameters parameters) {
            var itemModel = parameters.GetValue<BarCodeItemModel>("BarCodeItem");
            if (itemModel is not null) {
                Barcode = itemModel.Barcode;
                RequestStatus = itemModel.RequestStatus;
                RequestTime = itemModel.RequestTime;
                RequestContent = itemModel.RequestContent;
                ResponseTime = itemModel.ResponseTime;
                ResponseContent = itemModel.ResponseContent;
            }
        }

        public string Title { get; } = "ApiAccessDialog";

        public event Action<IDialogResult>? RequestClose;
    }
}