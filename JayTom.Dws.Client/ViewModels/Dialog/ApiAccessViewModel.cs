using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using JayTom.Dws.Models.Package;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Client.Models.DataModels;

namespace JayTom.Dws.Client.ViewModels.Dialog
{

    public class ApiAccessViewModel : BindableBase, IDialogAware
    {
        private UploadStatus _requestStatus = UploadStatus.NotUploaded;
        private DateTime? _requestTime;
        private string _requestContent = string.Empty;
        private DateTime? _responseTime;
        private string _responseContent = string.Empty;
        private string _barcode = string.Empty;
        private decimal _duration;
        private string _url = string.Empty;

        /// <summary>
        /// 上传状态
        /// </summary>
        public UploadStatus RequestStatus
        {
            get => _requestStatus;
            set => SetProperty(ref _requestStatus, value);
        }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime? RequestTime
        {
            get => _requestTime;
            set => SetProperty(ref _requestTime, value);
        }

        /// <summary>
        /// 上传内容
        /// </summary>
        public string RequestContent
        {
            get => _requestContent;
            set => SetProperty(ref _requestContent, value);
        }

        /// <summary>
        /// 接口响应时间
        /// </summary>
        public DateTime? ResponseTime
        {
            get => _responseTime;
            set => SetProperty(ref _responseTime, value);
        }

        /// <summary>
        /// 接口响应内容
        /// </summary>
        public string ResponseContent
        {
            get => _responseContent;
            set => SetProperty(ref _responseContent, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 耗时
        /// </summary>
        public decimal Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        /// <summary>
        /// Url
        /// </summary>
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public ICommand CloseWinCommand => new DelegateCommand<object>(CloseWinDelegate);

        private void CloseWinDelegate(object obj)
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            Barcode = string.Empty;
            RequestStatus = UploadStatus.NotUploaded;
            RequestTime = null;
            RequestContent = string.Empty;
            ResponseTime = null;
            ResponseContent = string.Empty;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.Name.Equals("ApiAccessWindows"))
                {
                    window.Close();
                }
            }

            var itemModel = parameters.GetValue<PackageItemModel>("PackageItem");
            if (itemModel is not null)
            {
                Barcode = itemModel.Barcode;
                RequestStatus = itemModel.RequestStatus;
                Duration = (decimal)itemModel.UploadInfo.DurationInSeconds * 1000;
                RequestTime = itemModel.UploadInfo.RequestTime;
                RequestContent = itemModel.UploadInfo.RequestContent;
                ResponseTime = itemModel.UploadInfo.ResponseTime;
                ResponseContent = itemModel.UploadInfo.ResponseContent;
                Url = itemModel.UploadInfo.RequestUrl;
            }
        }

        public string Title { get; } = "Api访问内容";

        public event Action<IDialogResult>? RequestClose;

        public ICommand LoadedCommand => new DelegateCommand<UserControl>(LoadedDelegate);

        private void LoadedDelegate(UserControl obj)
        {
            var dialogWindow = System.Windows.Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (dialogWindow is not null)
            {
                dialogWindow.Owner = null;
                dialogWindow.Name = "ApiAccessWindows";
            }
        }
    }
}