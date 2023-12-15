using Prism.Mvvm;
using Prism.Commands;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace JayTom.Dws.VideoApiClient.ViewModels.Editors {

    public class DataTimeEditorViewModel : BindableBase {
        private DateTime? _selectedDate = DateTime.Now;
        private DateTime? _selectedTime = DateTime.Now;
        private DateTime? _selectedDataTime = DateTime.Today;
        private string _identifier;
        private bool _isOk;

        /// <summary>
        /// 标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 选择日期
        /// </summary>
        public DateTime? SelectedDate {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        /// <summary>
        /// 选择时间
        /// </summary>
        public DateTime? SelectedTime {
            get => _selectedTime;
            set => SetProperty(ref _selectedTime, value);
        }

        /// <summary>
        /// 选择的结果
        /// </summary>
        public DateTime? SelectedDataTime {
            get => _selectedDataTime;
            set => SetProperty(ref _selectedDataTime, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 确定
        /// </summary>
        public ICommand OkCommand {
            get => new DelegateCommand<object>(OkDelegate);
        }

        public ICommand TodayCommand {
            get => new DelegateCommand<object>(TodayDelegate);
        }

        private void TodayDelegate(object obj) {
            SelectedDate = DateTime.Today;
            SelectedTime = DateTime.Today;
            SelectedDataTime = Convert.ToDateTime($"{SelectedDate.Value:yyyy-MM-dd} {SelectedTime:HH:mm:ss}");
            IsOk = true;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        private void OkDelegate(object obj) {
            if (SelectedDate is not null &&
                SelectedTime is not null) {
                SelectedDataTime = Convert.ToDateTime($"{SelectedDate.Value:yyyy-MM-dd} {SelectedTime:HH:mm:ss}");
                IsOk = true;
            }

            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand NowCommand {
            get => new DelegateCommand<object>(NowDelegate);
        }

        private void NowDelegate(object obj) {
            SelectedDate = DateTime.Now;
            SelectedTime = DateTime.Now;
            SelectedDataTime = Convert.ToDateTime($"{SelectedDate.Value:yyyy-MM-dd} {SelectedTime:HH:mm:ss}");
            IsOk = true;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}