using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class TcpScanSettingsViewModel : BindableBase {
        private readonly IScanNodeConfigRepository _scanNodeConfigRepository;

        private ObservableCollection<ScanNodeItemInfoModel> _scanNodeItems = new()
        {
            new ScanNodeItemInfoModel()
            {
                ImagePath = "D:\\Workspace\\JayTom\\JayTom.Dws\\JayTom.Dws.Client\\bin\\Debug\\net7.0-windows",
                IpAddress = "127.0.0.1",
                NodeName = "节点1",
                NodeNum = 1,
                Num = 1,
                Port = 2000,
                Timeout = 5000,
                Status = NodeStatus.Connected
            }
        };

        public ObservableCollection<ScanNodeItemInfoModel> ScanNodeItems {
            get => _scanNodeItems;
            set => SetProperty(ref _scanNodeItems, value);
        }

        public TcpScanSettingsViewModel(IScanNodeConfigRepository scanNodeConfigRepository) {
            _scanNodeConfigRepository = scanNodeConfigRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            //从数据库加载数据
        }

        public ICommand ModifyCommand => new DelegateCommand<object>(ModifyDelegate);

        private void ModifyDelegate(object obj) {
            //打开添加编辑框
        }

        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private void DeleteDelegate(object obj) {
            //删除选中项
        }

        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        private void AddDelegate(object obj) {
            //打开添加编辑框
        }
    }
}