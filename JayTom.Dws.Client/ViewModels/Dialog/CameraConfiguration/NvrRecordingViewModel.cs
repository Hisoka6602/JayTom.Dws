using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration {
    public class NvrRecordingViewModel : BindableBase {
        private ObservableCollection<VideoPlayerModel> _videoPlayerItems = new();
        private string _identifier = string.Empty;
        private DateTime _startTime = DateTime.Now;
        private DateTime _endTime = DateTime.Now.AddHours(1);
        private DateTime _currentTime = DateTime.Now;
        private DateTime _selectionStartTime = DateTime.Now.AddSeconds(10);
        private DateTime _selectionEndTime = DateTime.Now.AddSeconds(400);

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public ObservableCollection<VideoPlayerModel> VideoPlayerItems {
            get => _videoPlayerItems;
            set => SetProperty(ref _videoPlayerItems, value);
        }

        public DateTime StartTime {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public DateTime CurrentTime {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public DateTime SelectionStartTime {
            get => _selectionStartTime;
            set => SetProperty(ref _selectionStartTime, value);
        }

        public DateTime SelectionEndTime {
            get => _selectionEndTime;
            set => SetProperty(ref _selectionEndTime, value);
        }
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {


        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            //退出播放
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}