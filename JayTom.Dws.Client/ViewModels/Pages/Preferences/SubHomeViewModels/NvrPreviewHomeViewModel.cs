using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;
using ApplicationStatus = JayTom.Dws.Domain.EventMediators.ApplicationStatus;
using ApplicationStatusChanged = JayTom.Dws.Domain.EventMediators.ApplicationStatusChanged;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels
{

    public class NvrPreviewHomeViewModel : BindableBase
    {
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly ISettingsStore _settingsStore;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private ObservableCollection<NvrPreviewViewItemInfo> _nvrPreviewViewItems = new();
        private BaseDaHuatech? _baseDaHuatech;
        private static SemaphoreSlim _runningSemaphoreSlim = new(1, 1);

        public ObservableCollection<NvrPreviewViewItemInfo> NvrPreviewViewItems
        {
            get => _nvrPreviewViewItems;
            set => SetProperty(ref _nvrPreviewViewItems, value);
        }

        public NvrPreviewHomeViewModel(INvrCameraBindingRepository nvrCameraBindingRepository,
            ISettingsStore settingsStore,
            IIpcNvrConfigRepository ipcNvrConfigRepository)
        {
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _settingsStore = settingsStore;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _baseDaHuatech ??= BaseDaHuatech.CreateInstance();

            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(async item =>
            {
                if (item is { } info)
                {
                    if (info.Status == JayTom.Dws.Domain.EventMediators.ApplicationStatus.Start)
                    {
                        try
                        {
                            await _runningSemaphoreSlim.WaitAsync();
                            var ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
                            await BaseDaHuatech.InitializeDeviceDiscoveryAsync();
                            var settingsDto = await _settingsStore.GetAsync<ContentInputSettingsDto>("ContentInputSettings") ?? new ContentInputSettingsDto();

                            var bindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
                            var infoModels = bindingInfoModels.Where(w => w.SerialNumber.Equals(settingsDto.KeyboardDevice.DevicePath))
                                .ToList();
                            foreach (var model in infoModels)
                            {
                                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                                {
                                    var serialNumber = ipcNvrConfigInfoModels.FirstOrDefault(f => f.Username.Equals(model.Username) &&
                                            f.IpAddress.Equals(model.IpAddress) &&
                                            f.Password.Equals(model.Password))
                                        ?.SerialNumber ?? string.Empty;
                                    var previewViewItemInfo = new NvrPreviewViewItemInfo()
                                    {
                                        SerialNumber = serialNumber,
                                        ChannelNumber = model.Channel,
                                        IncreaseZoomCommand = new DelegateCommand<object>(sub =>
                                        {
                                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                                            _baseDaHuatech?.AdjustZoomContinuouslyAsync(serialNumber,
                                                model.Channel,
                                                true, isStart);
                                        }),
                                        DecreaseZoomCommand = new DelegateCommand<object>(sub =>
                                        {
                                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                                            _baseDaHuatech?.AdjustZoomContinuouslyAsync(serialNumber,
                                                model.Channel,
                                                false, isStart);
                                        }),
                                        IncreaseFocusCommand = new DelegateCommand<object>(sub =>
                                        {
                                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                                            _baseDaHuatech?.AdjustPtzFocusContinuouslyAsync(serialNumber,
                                                model.Channel,
                                                true, isStart);
                                        }),
                                        DecreaseFocusCommand = new DelegateCommand<object>(sub =>
                                        {
                                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                                            _baseDaHuatech?.AdjustPtzFocusContinuouslyAsync(serialNumber,
                                                model.Channel,
                                                false, isStart);
                                        }),
                                        AutoFocusCommand = new DelegateCommand<object>(sub =>
                                        {
                                            _baseDaHuatech?.AutoFocusAsync(serialNumber,
                                                model.Channel);
                                        }),
                                        ToggleImageSizeCommand = ToggleImageSizeCommand
                                    };
                                    if (previewViewItemInfo.VideoFrame is not null)
                                    {
                                        var ipcNvrConfigInfoModel = ipcNvrConfigInfoModels.FirstOrDefault(f => f.Username.Equals(model.Username) &&
                                            f.IpAddress.Equals(model.IpAddress) &&
                                            f.Password.Equals(model.Password));
                                        if (ipcNvrConfigInfoModel is not null)
                                        {
                                            var (key, value) = await _baseDaHuatech.LogIn(ipcNvrConfigInfoModel.SerialNumber, model.Username, model.Password);
                                            if (key)
                                            {
                                                _baseDaHuatech.RegisterRealtimePreviewCallback(ipcNvrConfigInfoModel.SerialNumber, model.Channel, previewViewItemInfo.RealtimePreviewCallback);

                                                var (b, s) = await _baseDaHuatech.StartRealTimePreview(ipcNvrConfigInfoModel.SerialNumber, model.Channel);
                                                if (b)
                                                {
                                                    previewViewItemInfo.IsShow = true;
                                                }
                                            }

                                            NvrPreviewViewItems.Add(previewViewItemInfo);
                                        }
                                    }
                                });
                            }
                        }
                        finally
                        {
                            _runningSemaphoreSlim.Release();
                        }
                    }
                    else if (info.Status == JayTom.Dws.Domain.EventMediators.ApplicationStatus.Stop)
                    {
                        //停止
                        try
                        {
                            await _runningSemaphoreSlim.WaitAsync();
                            await Task.Delay(300);
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                var itemInfos = NvrPreviewViewItems.Where(w => w.VideoFrame != null).ToList();
                                foreach (var itemInfo in itemInfos)
                                {
                                    _baseDaHuatech?.StopRealtimePreview(itemInfo.SerialNumber, itemInfo.ChannelNumber);
                                    itemInfo.Dispose();
                                }

                                NvrPreviewViewItems.Clear();
                            });
                        }
                        finally
                        {
                            _runningSemaphoreSlim.Release();
                        }
                    }
                }
            });
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<NvrPreviewViewItemInfo>(ToggleImageSizeDelegate);

        private void ToggleImageSizeDelegate(NvrPreviewViewItemInfo obj)
        {
            if (obj.ScreenState == ScreenState.Normal)
            {
                foreach (var videoPlayerModel in NvrPreviewViewItems)
                {
                    videoPlayerModel.ScreenState = !videoPlayerModel.Equals(obj) ? ScreenState.Hidden : ScreenState.Maximized;
                }
            }
            else
            {
                foreach (var videoPlayerModel in NvrPreviewViewItems)
                {
                    videoPlayerModel.ScreenState = ScreenState.Normal;
                }
            }
        }
    }
}
