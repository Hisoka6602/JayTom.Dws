using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.Client.Attributes;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.ViewModels.Editors.Enums;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration
{
    public class NvrIpcDeviceEditorViewModel : BindableBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private string _identifier = string.Empty;
        private string _message = string.Empty;
        private bool _isOk;
        private SnackbarMessageQueue _nvrIpcDeviceEditorMessageQueue = new(TimeSpan.FromSeconds(1));
        private string _deviceName = string.Empty;
        private ObservableCollection<DeviceType> _deviceTypeItems = new(Enum.GetValues(typeof(DeviceType)).Cast<DeviceType>());
        private EditorOperationType _showType;
        private int _selectDeviceCount;
        private ObservableCollection<IpcNvrItemInfoModel> _selectDevices = new();
        private IpcNvrItemInfoModel _ipcNvrItemInfo = new();

        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public EditorOperationType ShowType
        {
            get => _showType;
            set => SetProperty(ref _showType, value);
        }

        public ObservableCollection<DeviceType> DeviceTypeItems
        {
            get => _deviceTypeItems;
            set => SetProperty(ref _deviceTypeItems, value);
        }

        public ObservableCollection<IpcNvrItemInfoModel> SelectDevices
        {
            get => _selectDevices;
            set => SetProperty(ref _selectDevices, value);
        }

        public SnackbarMessageQueue NvrIpcDeviceEditorMessageQueue
        {
            get => _nvrIpcDeviceEditorMessageQueue;
            set => SetProperty(ref _nvrIpcDeviceEditorMessageQueue, value);
        }

        public int SelectDeviceCount
        {
            get => _selectDeviceCount;
            set => SetProperty(ref _selectDeviceCount, value);
        }

        public IpcNvrItemInfoModel IpcNvrItemInfo
        {
            get => _ipcNvrItemInfo;
            set => SetProperty(ref _ipcNvrItemInfo, value);
        }

        public NvrIpcDeviceEditorViewModel(IDeviceService deviceService,
            IIpcNvrConfigRepository ipcNvrConfigRepository)
        {
            _deviceService = deviceService;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
        }

        /// <summary>
        /// 页面加载
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj)
        {
            if (ShowType != EditorOperationType.BatchChangePassword)
            {
                SelectDevices.Clear();
            }
            else
            {
                SelectDeviceCount = SelectDevices.Count;
            }
            if (ShowType != EditorOperationType.Edit)
            {
                IpcNvrItemInfo = new IpcNvrItemInfoModel();
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate()
        {
            if (ShowType == EditorOperationType.BatchChangePassword)
            {
                if (string.IsNullOrEmpty(IpcNvrItemInfo.Username) || string.IsNullOrEmpty(IpcNvrItemInfo.Password))
                {
                    NvrIpcDeviceEditorMessageQueue.Enqueue("账号和密码均不能为空");
                    return;
                }

                if (!SelectDevices.Any())
                {
                    Message = "未选中需要修改的项";
                }
                else
                {
                    foreach (var ipcNvrItemInfoModel in SelectDevices)
                    {
                        ipcNvrItemInfoModel.Username = IpcNvrItemInfo.Username;
                        ipcNvrItemInfoModel.Password = IpcNvrItemInfo.Password;
                    }
                }
            }
            else if (ShowType is EditorOperationType.Add or EditorOperationType.Edit)
            {
                if (string.IsNullOrEmpty(IpcNvrItemInfo.Username) ||
                    string.IsNullOrEmpty(IpcNvrItemInfo.Password) ||
                    string.IsNullOrEmpty(IpcNvrItemInfo.IpAddress) ||
                    IpcNvrItemInfo.Port <= 0)
                {
                    NvrIpcDeviceEditorMessageQueue.Enqueue("账号、密码、IP、端口均不能为空");
                    return;

                }

            }



            IsOk = true;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate()
        {
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand TestLogInCommand => new DelegateCommand(TestLogInDelegate);

        private void TestLogInDelegate()
        {
            //加载通道
            NvrIpcDeviceEditorMessageQueue.Enqueue("登录测试");
        }
    }
}