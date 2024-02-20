using System;
using NPOI.HPSF;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using Newtonsoft.Json;
using TouchSocket.Core;
using JayTom.Dws.License;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.AppSettingModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings {

    public class LicensePageViewModel : BindableBase {
        private readonly IClientLicenseApi _clientLicenseApi;
        private SnackbarMessageQueue _licenseMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isRequestingAuthorization;
        private string _customerName = string.Empty;
        private bool _licenseStatus;
        private string _licenseCode = string.Empty;
        private string _machineCode = string.Empty;
        private string _failureReason = "未检测到授权文件";
        private bool _isLoaded;

        public LicensePageViewModel(IClientLicenseApi clientLicenseApi) {
            _clientLicenseApi = clientLicenseApi;
        }

        public SnackbarMessageQueue LicenseMessageQueue {
            get => _licenseMessageQueue;
            set => SetProperty(ref _licenseMessageQueue, value);
        }

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode {
            get => _machineCode;
            set => SetProperty(ref _machineCode, value);
        }

        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseCode {
            get => _licenseCode;
            set => SetProperty(ref _licenseCode, value);
        }

        /// <summary>
        /// 授权状态
        /// </summary>
        public bool LicenseStatus {
            get => _licenseStatus;
            set => SetProperty(ref _licenseStatus, value);
        }

        /// <summary>
        /// 客户名
        /// </summary>
        public string CustomerName {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        /// <summary>
        /// 是否正在申请授权
        /// </summary>
        public bool IsRequestingAuthorization {
            get => _isRequestingAuthorization;
            set => SetProperty(ref _isRequestingAuthorization, value);
        }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailureReason {
            get => _failureReason;
            set => SetProperty(ref _failureReason, value);
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    MachineCode = LicenseManager.GenerateMachineCode();

                    var licenseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "License");
                    var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                    if (firstOrDefault is not null) {
                        //解密授权
                        var (key, value) = LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);
                        if (data is not null) {
                            LicenseCode = data.LicenseCode;
                            CustomerName = data.UserName;
                            FailureReason = value;
                        }
                        LicenseStatus = key;
                    }
                    else {
                        FailureReason = "未检测到授权文件";
                    }
                });
            }
        }

        public ICommand RemoteAuthorizeCommand {
            get => new DelegateCommand<object>(RemoteAuthorizeDelegate);
        }

        private void RemoteAuthorizeDelegate(object obj) {
            Task.Run(async () => {
                if (!IsRequestingAuthorization) {
                    IsRequestingAuthorization = true;
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                        try {
                            LicenseStatus = false;
                            var (key, value) = await _clientLicenseApi.CreateAuthorization(LicenseCode, MachineCode);
                            if (value is ApiResult result &&
                                !string.IsNullOrEmpty(result.Data?.ToString() ?? string.Empty)) {
                                //获取授权文件地址
                                if (result.Result) {
                                    var licenseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "License");
                                    var files = Directory.GetFiles(licenseDirectory, "*.key");
                                    Parallel.ForEach(files, File.Delete);

                                    var fileAsync = await _clientLicenseApi.DownloadFileAsync(result.Data.ToString(),
                                        $"{licenseDirectory}\\License.key");
                                    if (fileAsync) {
                                        var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                                        if (firstOrDefault is not null) {
                                            //解密授权
                                            var (b, s) = LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);
                                            if (data is not null) {
                                                LicenseCode = data.LicenseCode;
                                                CustomerName = data.UserName;
                                                FailureReason = s;
                                            }
                                            LicenseStatus = b;
                                        }
                                        else {
                                            FailureReason = "未检测到授权文件";
                                        }
                                    }
                                    else {
                                        FailureReason = "下载授权文件失败";
                                        LicenseMessageQueue.Enqueue("下载授权文件失败");
                                    }
                                }
                                else {
                                    FailureReason = result.Msg;
                                    LicenseMessageQueue.Enqueue(result.Msg);
                                }
                            }
                            else {
                                FailureReason = "授权失败";
                                LicenseMessageQueue.Enqueue("授权失败");
                            }
                        }
                        catch (Exception e) {
                            LicenseMessageQueue.Enqueue(e.Message);
                        }
                        finally {
                            IsRequestingAuthorization = false;
                        }
                    });
                }
            });
        }
    }
}