using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration {

    public class NvrBindingPreviewViewDialogViewModel : BindableBase, IDialogAware {
        private NvrPreviewViewItemInfo _nvrPreviewViewInfo = new();
        private BaseDaHuatech? _baseDaHuatech;

        public NvrPreviewViewItemInfo NvrPreviewViewInfo {
            get => _nvrPreviewViewInfo;
            set => SetProperty(ref _nvrPreviewViewInfo, value);
        }

        public NvrBindingPreviewViewDialogViewModel() {
            _baseDaHuatech ??= BaseDaHuatech.CreateInstance();
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog() {
            return true;
        }

        public async void OnDialogClosed() {
            //断开
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                if (_baseDaHuatech is not null) {
                    var (key, value) = await _baseDaHuatech.StopRealtimePreview(NvrPreviewViewInfo.SerialNumber, NvrPreviewViewInfo.ChannelId);

                    NvrPreviewViewInfo?.Dispose();
                }
            });
        }

        public async void OnDialogOpened(IDialogParameters parameters) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var info = parameters.GetValue<NvrBindingItemModel>("NvrBindingItem");
                if (info is not null) {
                    //打开
                    NvrPreviewViewInfo = new NvrPreviewViewItemInfo() {
                        ChannelId = info.Channel,
                        DisplayName = info.DeviceName,
                        SerialNumber = info.SerialNumber,
                    };
                    if (_baseDaHuatech is not null) {
                        _baseDaHuatech.RegisterRealtimePreviewCallback(NvrPreviewViewInfo.SerialNumber,
                            NvrPreviewViewInfo.ChannelId, NvrPreviewViewInfo.RealtimePreviewCallback);

                        var (key, value) = await _baseDaHuatech.StartRealTimePreview(NvrPreviewViewInfo.SerialNumber, NvrPreviewViewInfo.ChannelId);
                        if (key) {
                            NvrPreviewViewInfo.IsShow = true;
                        }
                    }
                }
            });
        }

        public string Title => "NVR预览";

        public event Action<IDialogResult>? RequestClose;
    }
}