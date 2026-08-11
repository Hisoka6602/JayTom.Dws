using System;
using NPOI.HPSF;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using Newtonsoft.Json;
using Microsoft.Win32;
using TouchSocket.Core;
using JayTom.Dws.License;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.AppSettingModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings
{

    public class LicensePageViewModel : BindableBase
    {
        private readonly IClientLicenseApi _clientLicenseApi;
        private SnackbarMessageQueue _licenseMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isRequestingAuthorization;
        private string _customerName = string.Empty;
        private bool _licenseStatus;
        private string _licenseCode = string.Empty;
        private string _machineCode = string.Empty;
        private string _failureReason = "未检测到授权文件";
        private bool _isLoaded;
        private string _remarks = string.Empty;

        public LicensePageViewModel(IClientLicenseApi clientLicenseApi)
        {
            _clientLicenseApi = clientLicenseApi;
        }

        public SnackbarMessageQueue LicenseMessageQueue
        {
            get => _licenseMessageQueue;
            set => SetProperty(ref _licenseMessageQueue, value);
        }

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode
        {
            get => _machineCode;
            set => SetProperty(ref _machineCode, value);
        }

        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseCode
        {
            get => _licenseCode;
            set => SetProperty(ref _licenseCode, value);
        }

        /// <summary>
        /// 授权状态
        /// </summary>
        public bool LicenseStatus
        {
            get => _licenseStatus;
            set => SetProperty(ref _licenseStatus, value);
        }

        /// <summary>
        /// 客户名
        /// </summary>
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        /// <summary>
        /// 是否正在申请授权
        /// </summary>
        public bool IsRequestingAuthorization
        {
            get => _isRequestingAuthorization;
            set => SetProperty(ref _isRequestingAuthorization, value);
        }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailureReason
        {
            get => _failureReason;
            set => SetProperty(ref _failureReason, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var loadingDialog = new LoadingDialog();
                    if (loadingDialog.DataContext is LoadingDialogViewModel model)
                    {
                        model.Identifier = "LicenseDialog";
                        DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                        await Task.Delay(500);
                        Task.Run(async () =>
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                            {
                                try
                                {
                                    MachineCode = LicenseManager.GenerateMachineCode();
                                    var licenseDirectory = Path.Combine(AppContext.BaseDirectory, "License");
                                    var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                                    if (firstOrDefault is not null)
                                    {
                                        //解密授权
                                        var (key, value) =
                                            LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);
                                        if (data is not null)
                                        {
                                            LicenseCode = data.LicenseCode;
                                            CustomerName = data.UserName;
                                            FailureReason = value;
                                            Remarks = data.Remarks;
                                        }

                                        LicenseStatus = key;
                                    }
                                    else
                                    {
                                        FailureReason = "未检测到授权文件";
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                                }
                                finally
                                {
                                    if (DialogHost.IsDialogOpen(model.Identifier))
                                    {
                                        DialogHost.Close(model.Identifier);
                                    }
                                }
                            }, DispatcherPriority.Background);
                        });
                    }
                });
            }
        }

        public ICommand RemoteAuthorizeCommand => new DelegateCommand<object>(RemoteAuthorizeDelegate);

        private void RemoteAuthorizeDelegate(object obj)
        {
            Task.Run(async () =>
            {
                if (!IsRequestingAuthorization)
                {
                    IsRequestingAuthorization = true;
                    await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                    {
                        try
                        {
                            LicenseStatus = false;
                            var (key, value) = await _clientLicenseApi.CreateAuthorization(LicenseCode, MachineCode, Remarks);
                            if (value is { } result)
                            {
                                //获取授权文件地址
                                if (result.IsSuccess && !string.IsNullOrEmpty(result.Data))
                                {
                                    var licenseDirectory = Path.Combine(AppContext.BaseDirectory, "License");
                                    if (!Directory.Exists(licenseDirectory))
                                    {
                                        Directory.CreateDirectory(licenseDirectory);
                                    }
                                    var files = Directory.GetFiles(licenseDirectory, "*.key");
                                    Parallel.ForEach(files, File.Delete);

                                    var fileAsync = await _clientLicenseApi.DownloadFileAsync(result.Data,
                                        $"{licenseDirectory}\\License.key");
                                    if (fileAsync.IsSuccess)
                                    {
                                        await Task.Delay(1000);
                                        var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                                        if (firstOrDefault is not null)
                                        {
                                            //解密授权
                                            var (b, s) = LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);
                                            if (data is not null)
                                            {
                                                LicenseCode = data.LicenseCode;
                                                CustomerName = data.UserName;
                                                FailureReason = s;
                                                Remarks = data.Remarks;
                                            }
                                            LicenseStatus = b;
                                        }
                                        else
                                        {
                                            FailureReason = "未检测到授权文件";
                                        }
                                    }
                                    else
                                    {
                                        FailureReason = "下载授权文件失败";
                                        LicenseMessageQueue.Enqueue("下载授权文件失败");
                                    }
                                }
                                else
                                {
                                    FailureReason = result.Message;
                                    LicenseMessageQueue.Enqueue(result.Message);
                                }
                            }
                            else
                            {
                                FailureReason = "授权失败";
                                LicenseMessageQueue.Enqueue("授权失败");
                            }
                        }
                        catch (Exception e)
                        {
                            LicenseMessageQueue.Enqueue(e.Message);
                        }
                        finally
                        {
                            IsRequestingAuthorization = false;
                        }
                    });
                }
            });
        }

        public ICommand ImportLicenseFileCommand => new DelegateCommand<object>(ImportLicenseFileDelegate);

        private async void ImportLicenseFileDelegate(object obj)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Title = "请选择需要打开的授权文件",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Filter =
                    $"授权文件 (*.key)|*.key",
                DefaultExt = ".key",
                RestoreDirectory = true,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var licenseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "License");
                    if (!Directory.Exists(licenseDirectory))
                    {
                        Directory.CreateDirectory(licenseDirectory);
                    }
                    var files = Directory.GetFiles(licenseDirectory, "*.key");
                    Parallel.ForEach(files, File.Delete);
                    File.Copy(openFileDialog.FileName, $"{licenseDirectory}\\{new FileInfo(openFileDialog.FileName).Name}");
                    var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                    if (firstOrDefault is not null)
                    {
                        //解密授权
                        var (b, s) = LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);
                        if (data is not null)
                        {
                            LicenseCode = data.LicenseCode;
                            CustomerName = data.UserName;

                            Remarks = data.Remarks;
                        }
                        FailureReason = s;
                        LicenseStatus = b;
                    }
                    else
                    {
                        FailureReason = "未检测到授权文件";
                    }
                });
            }
        }
    }
}
