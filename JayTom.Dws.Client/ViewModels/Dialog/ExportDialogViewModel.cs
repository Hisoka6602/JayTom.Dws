using System;
using Prism.Mvvm;
using Prism.Commands;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class ExportDialogViewModel : BindableBase {
        private double _maxProgress = 100;
        private double _progress = 0;
        private string _progressText = $"0%";
        private string _identifier = string.Empty;
        private string _message = string.Empty;
        private string _filePath = string.Empty;

        public double MaxProgress {
            get => _maxProgress;
            set => SetProperty(ref _maxProgress, value);
        }

        public double Progress {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string ProgressText {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string FilePath {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public CancellationToken CancelToken { get; set; }
        public Stopwatch RunStopwatch { get; set; } = new Stopwatch();
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        /// <summary>
        /// 取消事件
        /// </summary>
        public EventHandler<EventArgs> CancelAfter = (sender, args) => {
        };

        /// <summary>
        /// 完成事件
        /// </summary>
        public EventHandler<object> Completed = (sender, o) => {
        };

        /// <summary>
        /// 加载事件
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
        }

        /// <summary>
        /// 取消
        /// </summary>

        public ICommand CancelCommand {
            get => new DelegateCommand<object>(CancelDelegate);
        }

        /// <summary>
        /// 取消事件
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CancelDelegate(object obj) {
        }
    }
}