using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels {

    public class AppLogItemModel : BindableBase {
        private DateTime? _createTime;
        private LogType _type = LogType.Information;
        private string _message = string.Empty;
        private ICommand? _clickCommand;

        public DateTime? CreateTime {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        public LogType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand? ClickCommand {
            get => _clickCommand;
            set => SetProperty(ref _clickCommand, value);
        }
    }
}