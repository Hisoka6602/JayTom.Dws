using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration {
    public class IpcPreviewViewModel : BindableBase {

        //private WriteableBitmap _videoFrame = new(561, 316, 96, 96, PixelFormats.Bgr24, null);
        private BaseDaHuatech? _baseDaHuatech;

        private ObservableCollection<PreviewViewChannelInfo> _channelItems = new();
        private ObservableCollection<NvrPreviewViewItemInfo> _nvrPreviewViewItems = new();

        public IpcPreviewViewModel() {
            _baseDaHuatech ??= BaseDaHuatech.CreateInstance();
        }

        public string Identifier { get; set; } = string.Empty;

        public IpcNvrItemInfoModel IpcNvrItemInfo { get; set; } = new();

        public ObservableCollection<PreviewViewChannelInfo> ChannelItems {
            get => _channelItems;
            set => SetProperty(ref _channelItems, value);
        }

        public ObservableCollection<NvrPreviewViewItemInfo> NvrPreviewViewItems {
            get => _nvrPreviewViewItems;
            set => SetProperty(ref _nvrPreviewViewItems, value);
        }

        /*public WriteableBitmap VideoFrame {
            get => _videoFrame;
            set => SetProperty(ref _videoFrame, value);
        }*/

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            if (!ChannelItems.Any(a => a.IsChecked)) {
                ChannelItems.Clear();
                foreach (var info in NvrPreviewViewItems) {
                    info.Dispose();
                }
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    ChannelItems.AddRange(Enumerable.Range(1, IpcNvrItemInfo.ChannelCount).Select((s, i) =>
                        new PreviewViewChannelInfo {
                            ChannelId = i,
                            DisplayName = $"通道{s}",
                            IsChecked = false
                        }).ToList());

                    if (_baseDaHuatech is not null) {
                        var (key, value) = await _baseDaHuatech.LogIn(IpcNvrItemInfo.SerialNumber, IpcNvrItemInfo.Username, IpcNvrItemInfo.Password);
                    }
                });
            }
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            //退出摄像头
            var itemInfos = NvrPreviewViewItems.Where(w => w.VideoFrame != null).ToList();
            foreach (var itemInfo in itemInfos) {
                _baseDaHuatech?.StopRealtimePreview(IpcNvrItemInfo.SerialNumber, itemInfo.ChannelId);
                itemInfo.Dispose();
            }
            NvrPreviewViewItems.Clear();
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand SelectChannelCommand => new DelegateCommand<PreviewViewChannelInfo>(SelectChannelDelegate);

        private async void SelectChannelDelegate(PreviewViewChannelInfo obj) {
            if (NvrPreviewViewItems.Count(c => c.IsShow) >= 6) {
                return;
            }
            if (_baseDaHuatech is not null) {
                if (obj.IsChecked) {
                    var previewViewItemInfo = new NvrPreviewViewItemInfo() {
                        DisplayName = obj.DisplayName,
                        ChannelId = obj.ChannelId,
                        IncreaseZoomCommand = new DelegateCommand<object>(sub => {
                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                            _baseDaHuatech?.AdjustZoomContinuouslyAsync(IpcNvrItemInfo.SerialNumber,
                                obj.ChannelId,
                                true, isStart);
                        }),
                        DecreaseZoomCommand = new DelegateCommand<object>(sub => {
                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                            _baseDaHuatech?.AdjustZoomContinuouslyAsync(IpcNvrItemInfo.SerialNumber,
                                obj.ChannelId,
                                false, isStart);
                        }),
                        IncreaseFocusCommand = new DelegateCommand<object>(sub => {
                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                            _baseDaHuatech?.AdjustPtzFocusContinuouslyAsync(IpcNvrItemInfo.SerialNumber,
                                obj.ChannelId,
                                true, isStart);
                        }),
                        DecreaseFocusCommand = new DelegateCommand<object>(sub => {
                            var isStart = sub.ToString()?.Equals("Stop", StringComparison.CurrentCultureIgnoreCase) == true;
                            _baseDaHuatech?.AdjustPtzFocusContinuouslyAsync(IpcNvrItemInfo.SerialNumber,
                                obj.ChannelId,
                                false, isStart);
                        }),
                        AutoFocusCommand = new DelegateCommand<object>(sub => {
                            _baseDaHuatech?.AutoFocusAsync(IpcNvrItemInfo.SerialNumber,
                                obj.ChannelId);
                        }),
                    };
                    if (previewViewItemInfo.VideoFrame is not null) {
                        _baseDaHuatech.RegisterRealtimePreviewCallback(IpcNvrItemInfo.SerialNumber, obj.ChannelId, previewViewItemInfo.RealtimePreviewCallback);

                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            NvrPreviewViewItems.Add(previewViewItemInfo);
                        });
                        var (b, s) = await _baseDaHuatech.StartRealTimePreview(IpcNvrItemInfo.SerialNumber, obj.ChannelId);
                        if (b) {
                            previewViewItemInfo.IsShow = true;
                        }
                    }
                }
                else {
                    var info = NvrPreviewViewItems.FirstOrDefault(f => f.ChannelId == obj.ChannelId);
                    if (info is not null) {
                        info.IsShow = false;
                        var (b, s) = await _baseDaHuatech.StopRealtimePreview(IpcNvrItemInfo.SerialNumber, obj.ChannelId);
                        NvrPreviewViewItems.Remove(info);
                        info.Dispose();
                    }
                }
            }
        }
    }

    public class PreviewViewChannelInfo : BindableBase {

        /// <summary>
        /// 显示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 选择
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        public int ChannelId { get; set; }
    }
}