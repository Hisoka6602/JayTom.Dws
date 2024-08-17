using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using ApplicationStatus = JayTom.Dws.Domain.EventMediators.ApplicationStatus;
using ApplicationStatusChanged = JayTom.Dws.Client.EventMediators.ApplicationStatusChanged;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels {

    public class NvrPreviewHomeViewModel : BindableBase {
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private ObservableCollection<NvrPreviewViewItemInfo> _nvrPreviewViewItems = new();
        private BaseDaHuatech? _baseDaHuatech;
        private static SemaphoreSlim _runningSemaphoreSlim = new(1, 1);

        public ObservableCollection<NvrPreviewViewItemInfo> NvrPreviewViewItems {
            get => _nvrPreviewViewItems;
            set => SetProperty(ref _nvrPreviewViewItems, value);
        }

        public NvrPreviewHomeViewModel(INvrCameraBindingRepository nvrCameraBindingRepository,
            IConfigRepository configRepository,
            IIpcNvrConfigRepository ipcNvrConfigRepository) {
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _configRepository = configRepository;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _baseDaHuatech ??= BaseDaHuatech.CreateInstance();

            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(async item => {
                if (item is { } info) {
                    if (info.Status == EventMediators.ApplicationStatus.Start) {
                        try {
                            await _runningSemaphoreSlim.WaitAsync();
                            var ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
                            await BaseDaHuatech.EnumDevices();
                            var settingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>("ContentInputSettings") ?? new ContentInputSettingsDto();

                            var bindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
                            var infoModels = bindingInfoModels.Where(w => w.SerialNumber.Equals(settingsDto.KeyboardDevice.DevicePath))
                                .ToList();
                            foreach (var model in infoModels) {
                                await Application.Current.Dispatcher.InvokeAsync(async () => {
                                    var previewViewItemInfo = new NvrPreviewViewItemInfo() {
                                        ChannelId = model.Channel,
                                    };
                                    if (previewViewItemInfo.VideoFrame is not null) {
                                        var ipcNvrConfigInfoModel = ipcNvrConfigInfoModels.FirstOrDefault(f => f.Username.Equals(model.Username) &&
                                            f.IpAddress.Equals(model.IpAddress) &&
                                            f.Password.Equals(model.Password));
                                        if (ipcNvrConfigInfoModel is not null) {
                                            var (key, value) = await _baseDaHuatech.LogIn(ipcNvrConfigInfoModel.SerialNumber, model.Username, model.Password);
                                            if (key) {
                                                _baseDaHuatech.RegisterRealtimePreviewCallback(ipcNvrConfigInfoModel.SerialNumber, model.Channel, previewViewItemInfo.RealtimePreviewCallback);

                                                var (b, s) = await _baseDaHuatech.StartRealTimePreview(ipcNvrConfigInfoModel.SerialNumber, model.Channel);
                                                if (b) {
                                                    previewViewItemInfo.IsShow = true;
                                                }
                                            }

                                            NvrPreviewViewItems.Add(previewViewItemInfo);
                                        }
                                    }
                                });
                            }
                        }
                        finally {
                            _runningSemaphoreSlim.Release();
                        }
                    }
                    else if (info.Status == EventMediators.ApplicationStatus.Stop) {
                        //停止
                        try {
                            await _runningSemaphoreSlim.WaitAsync();
                            await Task.Delay(300);
                            await Application.Current.Dispatcher.InvokeAsync(() => {
                                var itemInfos = NvrPreviewViewItems.Where(w => w.VideoFrame != null).ToList();
                                foreach (var itemInfo in itemInfos) {
                                    _baseDaHuatech?.StopRealtimePreview(itemInfo.SerialNumber, itemInfo.ChannelId);
                                    itemInfo.Dispose();
                                }

                                NvrPreviewViewItems.Clear();
                            });
                        }
                        finally {
                            _runningSemaphoreSlim.Release();
                        }
                    }
                }
            });
        }
    }
}