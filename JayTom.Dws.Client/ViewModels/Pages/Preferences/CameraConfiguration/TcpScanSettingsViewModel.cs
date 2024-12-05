using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.ScanNode;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Client.ViewModels.Editors.Enums;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class TcpScanSettingsViewModel : BindableBase {
        private readonly IScanNodeConfigRepository _scanNodeConfigRepository;
        private readonly IDeviceService _deviceService;
        private readonly INodeCommunicationService _nodeCommunicationService;

        private ObservableCollection<ScanNodeItemInfoModel> _scanNodeItems = new();

        private SnackbarMessageQueue _tcpScanSettingsMessageQueue = new(TimeSpan.FromSeconds(2));

        public ObservableCollection<ScanNodeItemInfoModel> ScanNodeItems {
            get => _scanNodeItems;
            set => SetProperty(ref _scanNodeItems, value);
        }

        public SnackbarMessageQueue TcpScanSettingsMessageQueue {
            get => _tcpScanSettingsMessageQueue;
            set => SetProperty(ref _tcpScanSettingsMessageQueue, value);
        }

        public TcpScanSettingsViewModel(IScanNodeConfigRepository scanNodeConfigRepository,
            IDeviceService deviceService,
            INodeCommunicationService nodeCommunicationService) {
            _scanNodeConfigRepository = scanNodeConfigRepository;
            _deviceService = deviceService;
            _nodeCommunicationService = nodeCommunicationService;

            _nodeCommunicationService.NodeConnected += async (sender, info) => {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var nodeItemInfoModel = ScanNodeItems.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress) &&
                        f.Port.Equals(info.Port));
                    if (nodeItemInfoModel is not null) {
                        nodeItemInfoModel.Status = NodeStatus.Connected;
                    }
                });
            };
            _nodeCommunicationService.NodeDisconnected += async (sender, info) => {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var nodeItemInfoModel = ScanNodeItems.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress) &&
                                                                              f.Port.Equals(info.Port));
                    if (nodeItemInfoModel is not null) {
                        nodeItemInfoModel.Status = NodeStatus.Disconnected;
                    }
                });
            };
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {
            LoadData();
        }

        public ICommand ModifyCommand => new DelegateCommand<object>(ModifyDelegate);

        private async void ModifyDelegate(object obj) {
            //打开添加编辑框
            if (_deviceService.RunningStatus) {
                TcpScanSettingsMessageQueue.Enqueue("设备工作中,无法设置");
                return;
            }
            if (obj is ScanNodeItemInfoModel item) {
                var scanNodeConfigEditor = new ScanNodeConfigEditor();
                if (scanNodeConfigEditor.DataContext is ScanNodeConfigEditorViewModel model) {
                    model.Identifier = "CameraSettingDialog";
                    model.ScanNodeItemInfo = item;
                    await DialogHost.Show(scanNodeConfigEditor, model.Identifier);
                    if (model.IsOk) {
                        if (string.IsNullOrEmpty(model.ScanNodeItemInfo.ImagePath)) {
                            TcpScanSettingsMessageQueue.Enqueue("存图路径不能为空!");
                            return;
                        }
                        if (string.IsNullOrEmpty(model.ScanNodeItemInfo.IpAddress)) {
                            TcpScanSettingsMessageQueue.Enqueue("IP不能为空!");
                            return;
                        }
                        if (string.IsNullOrEmpty(model.ScanNodeItemInfo.NodeName)) {
                            TcpScanSettingsMessageQueue.Enqueue("节点名称不能为空!");
                            return;
                        }
                        if (model.ScanNodeItemInfo.NodeNum <= 0) {
                            TcpScanSettingsMessageQueue.Enqueue("节点序号不能小于等于0!");
                            return;
                        }

                        var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                        if (scanNodeConfigInfoModels.Any(a => a.NodeNum == model.ScanNodeItemInfo.NodeNum &&
                                                              a.Id != model.ScanNodeItemInfo.Id)) {
                            TcpScanSettingsMessageQueue.Enqueue("节点序号已存在!");
                            return;
                        }

                        //添加到数据
                        var update = await _scanNodeConfigRepository.Update(new ScanNodeConfigInfoModel() {
                            ImagePath = model.ScanNodeItemInfo.ImagePath,
                            IpAddress = model.ScanNodeItemInfo.IpAddress,
                            Port = model.ScanNodeItemInfo.Port,
                            NodeName = model.ScanNodeItemInfo.NodeName,
                            NodeNum = model.ScanNodeItemInfo.NodeNum,
                            Timeout = model.ScanNodeItemInfo.Timeout,
                            Id = model.ScanNodeItemInfo.Id
                        });
                        if (!update) {
                            //提示异常
                            TcpScanSettingsMessageQueue.Enqueue("更新失败!");
                        }
                        else {
                            LoadData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = "TcpScanSettings",
                                IsLocallySaved = true
                            });
                        }
                    }
                }
            }
        }

        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private void DeleteDelegate(object obj) {
            //删除选中项
            if (_deviceService.RunningStatus) {
                TcpScanSettingsMessageQueue.Enqueue("设备工作中,无法设置");
                return;
            }
            if (obj is ScanNodeItemInfoModel item) {
                Task.Run(async () => {
                    var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                    var model = scanNodeConfigInfoModels.FirstOrDefault(f => f.Id.Equals(item.Id));
                    if (model is not null) {
                        var delete = await _scanNodeConfigRepository.Delete(model);
                        if (!delete) {
                            //提示异常
                            TcpScanSettingsMessageQueue.Enqueue("删除失败!");
                        }
                        else {
                            LoadData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = "TcpScanSettings",
                                IsLocallySaved = true
                            });
                        }
                    }
                });
            }
        }

        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        private async void AddDelegate(object obj) {
            //打开添加编辑框
            if (_deviceService.RunningStatus) {
                TcpScanSettingsMessageQueue.Enqueue("设备工作中,无法设置");
                return;
            }
            var scanNodeConfigEditor = new ScanNodeConfigEditor();
            if (scanNodeConfigEditor.DataContext is ScanNodeConfigEditorViewModel model) {
                model.Identifier = "CameraSettingDialog";
                await DialogHost.Show(scanNodeConfigEditor, model.Identifier);
                if (model.IsOk) {
                    if (string.IsNullOrEmpty(model.ScanNodeItemInfo.ImagePath)) {
                        TcpScanSettingsMessageQueue.Enqueue("存图路径不能为空!");
                        return;
                    }
                    if (string.IsNullOrEmpty(model.ScanNodeItemInfo.IpAddress)) {
                        TcpScanSettingsMessageQueue.Enqueue("IP不能为空!");
                        return;
                    }
                    if (string.IsNullOrEmpty(model.ScanNodeItemInfo.NodeName)) {
                        TcpScanSettingsMessageQueue.Enqueue("节点名称不能为空!");
                        return;
                    }
                    if (model.ScanNodeItemInfo.NodeNum <= 0) {
                        TcpScanSettingsMessageQueue.Enqueue("节点序号不能小于等于0!");
                        return;
                    }

                    var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                    if (scanNodeConfigInfoModels.Any(a => a.NodeNum == model.ScanNodeItemInfo.NodeNum)) {
                        TcpScanSettingsMessageQueue.Enqueue("节点序号已存在!");
                        return;
                    }
                    //添加到数据
                    var insert = await _scanNodeConfigRepository.Insert(new ScanNodeConfigInfoModel() {
                        ImagePath = model.ScanNodeItemInfo.ImagePath,
                        IpAddress = model.ScanNodeItemInfo.IpAddress,
                        Port = model.ScanNodeItemInfo.Port,
                        NodeName = model.ScanNodeItemInfo.NodeName,
                        NodeNum = model.ScanNodeItemInfo.NodeNum,
                        Timeout = model.ScanNodeItemInfo.Timeout
                    });
                    if (!insert) {
                        //提示异常
                        TcpScanSettingsMessageQueue.Enqueue("添加失败!");
                    }
                    else {
                        LoadData();
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "TcpScanSettings",
                            IsLocallySaved = true
                        });
                    }
                }
            }
        }

        private void LoadData() {
            Task.Run(async () => {
                var nodeCommunicationInfos = _nodeCommunicationService.GetAllListeningNodes();
                var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    ScanNodeItems.Clear();
                    var scanNodeItemInfoModels = scanNodeConfigInfoModels.OrderBy(o => o.NodeNum)
                        .Select((s, i) => new ScanNodeItemInfoModel {
                            Id = s.Id,
                            IpAddress = s.IpAddress,
                            ImagePath = s.ImagePath,
                            NodeName = s.NodeName,
                            NodeNum = s.NodeNum,
                            Num = i + 1,
                            Port = s.Port,
                            Timeout = s.Timeout,
                            Status = nodeCommunicationInfos.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress) &&
                                                                              f.Port.Equals(s.Port))?.IsOnline == true ? NodeStatus.Connected :
                                NodeStatus.Disconnected,
                        }).ToList();
                    ScanNodeItems.AddRange(scanNodeItemInfoModels);
                });
            });
        }
    }
}