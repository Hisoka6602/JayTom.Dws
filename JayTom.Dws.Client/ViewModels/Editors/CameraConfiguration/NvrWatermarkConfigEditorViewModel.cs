using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Windows.Media;
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
        private int _duration;

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public IpcNvrItemInfoModel IpcNvrItemInfo {
            get => _ipcNvrItemInfo;
            set => SetProperty(ref _ipcNvrItemInfo, value);
        }

        public ObservableCollection<int> ChannelIdItems {
            get => _channelIdItems;
            set => SetProperty(ref _channelIdItems, value);
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
        /// 保存
        /// </summary>
        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate() {
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