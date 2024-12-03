using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.ViewModels.Editors.Enums;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class NvrWatermarkConfigEditorViewModel : BindableBase {
        private IpcNvrItemInfoModel _ipcNvrItemInfo = new();
        private string _identifier = string.Empty;
        private string _message = string.Empty;
        private Color _watermarkColor;
        private ObservableCollection<int> _channelIdItems = new();
        private int _duration = 2000;
        private int _selectChannelId;
        private bool _isOverlay = true;
        private bool _isAllChannel;
        private SnackbarMessageQueue _nvrWatermarkConfigEditorMessageQueue = new(TimeSpan.FromSeconds(1));
        private bool _isUseWatermark;

        public NvrWatermarkConfigEditorViewModel() {
        }

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public bool IsUseWatermark {
            get => _isUseWatermark;
            set => SetProperty(ref _isUseWatermark, value);
        }

        public IpcNvrItemInfoModel IpcNvrItemInfo {
            get => _ipcNvrItemInfo;
            set => SetProperty(ref _ipcNvrItemInfo, value);
        }

        public ObservableCollection<int> ChannelIdItems {
            get => _channelIdItems;
            set => SetProperty(ref _channelIdItems, value);
        }

        public SnackbarMessageQueue NvrWatermarkConfigEditorMessageQueue {
            get => _nvrWatermarkConfigEditorMessageQueue;
            set => SetProperty(ref _nvrWatermarkConfigEditorMessageQueue, value);
        }

        /// <summary>
        /// 是否叠加
        /// </summary>
        public bool IsOverlay {
            get => _isOverlay;
            set => SetProperty(ref _isOverlay, value);
        }

        /// <summary>
        /// 是否全部通道
        /// </summary>
        public bool IsAllChannel {
            get => _isAllChannel;
            set => SetProperty(ref _isAllChannel, value);
        }

        /// <summary>
        /// 选择的通道
        /// </summary>
        public int SelectChannelId {
            get => _selectChannelId;
            set => SetProperty(ref _selectChannelId, value);
        }

        /// <summary>
        /// 水印颜色
        /// </summary>
        public System.Windows.Media.Color WatermarkColor {
            get => _watermarkColor;
            set => SetProperty(ref _watermarkColor, value);
        }

        /// <summary>
        /// 持续时间
        /// </summary>
        public int Duration {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        /// <summary>
        /// 页面加载
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            /*await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                ChannelIdItems = new ObservableCollection<int>(
                    Enumerable.Range(1, IpcNvrItemInfo.ChannelCount)
                );
                Message = $"设备序列号:{IpcNvrItemInfo.SerialNumber},通道数:{IpcNvrItemInfo.ChannelCount}";

                //暂时不应用分别取段道概念,只读写第一个
                var ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
                var model = ipcNvrConfigInfoModels.FirstOrDefault(f => f.SerialNumber.Equals(IpcNvrItemInfo.SerialNumber));
                if (model != null) {
                    var infoModel = await _nvrWatermarkConfigRepository.FirstOrDefault(f =>
                        f.IpcNvrConfigId.Equals(model.Id));

                    if (infoModel != null) {
                        Duration = infoModel.Duration;
                        IsOverlay = infoModel.DisplayMode == 0;
                        WatermarkColor = (Color)ColorConverter.ConvertFromString(infoModel.BackgroundColorHex);
                        IsUseWatermark = true;
                    }
                    else {
                        IsUseWatermark = false;
                    }
                }
            });*/
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private async void SaveDelegate() {
            /*var isSuccess = false;
            var ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
            var model = ipcNvrConfigInfoModels.FirstOrDefault(f => f.SerialNumber.Equals(IpcNvrItemInfo.SerialNumber));
            if (IsUseWatermark) {
                if (model is not null) {
                    var infoModel = await _nvrWatermarkConfigRepository.FirstOrDefault(f =>
                        f.IpcNvrConfigId.Equals(model.Id));
                    if (infoModel != null) {
                        infoModel.DisplayMode = IsOverlay ? 0 : 1;
                        infoModel.Duration = Duration;
                        infoModel.BackgroundColorHex = WatermarkColor.ToString();
                        isSuccess = await _nvrWatermarkConfigRepository.Update(infoModel);
                    }
                    else {
                        isSuccess = await _nvrWatermarkConfigRepository.Insert(new NvrWatermarkConfigInfoModel() {
                            IpcNvrConfigId = model.Id,
                            DisplayMode = IsOverlay ? 0 : 1,
                            Duration = Duration,
                            BackgroundColorHex = WatermarkColor.ToString(),
                        });
                    }
                    NvrWatermarkConfigEditorMessageQueue.Enqueue($"保存{(isSuccess ? "成功" : "失败")}");
                }
                else {
                    NvrWatermarkConfigEditorMessageQueue.Enqueue($"保存失败,NVR未初始化或未登录");
                }
            }
            else {
                var models = await _nvrWatermarkConfigRepository.MemoryCacheData();
                isSuccess = await _nvrWatermarkConfigRepository.DeleteRange(models.Where(w => w.IpcNvrConfigId.Equals(model?.Id)).ToList());
                NvrWatermarkConfigEditorMessageQueue.Enqueue($"保存{(isSuccess ? "成功" : "失败")}");
            }*/
        }

        /// <summary>
        /// 取消/关闭
        /// </summary>
        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate() {
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}