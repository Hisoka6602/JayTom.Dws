using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Client.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.PluginInterface.Utils;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IComputerInfoReporter _computerInfoReporter;
        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();
        private ObservableCollection<BarCodeItemModel> _barCodeItems = new();
        private DataGrid? _dataGrid = null;
        private int _totalDataCount;
        private int _uploadedDataCount;
        private int _abnormalDataCount;

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public ObservableCollection<BarCodeItemModel> BarCodeItems {
            get => _barCodeItems;
            set => SetProperty(ref _barCodeItems, value);
        }

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalDataCount {
            get => _totalDataCount;
            set => SetProperty(ref _totalDataCount, value);
        }

        /// <summary>
        /// 上传数量
        /// </summary>
        public int UploadedDataCount {
            get => _uploadedDataCount;
            set => SetProperty(ref _uploadedDataCount, value);
        }

        /// <summary>
        /// 异常数量
        /// </summary>
        public int AbnormalDataCount {
            get => _abnormalDataCount;
            set => SetProperty(ref _abnormalDataCount, value);
        }

        public HomeViewModel(IDialogService dialogService, IComputerInfoReporter computerInfoReporter) {
            _dialogService = dialogService;
            _computerInfoReporter = computerInfoReporter;
            CameraItems = new()
            {
                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.1",
                    Status = CameraStatus.Running,
                    Type = CameraType.IndustrialCamera,
                    ConnectionType = ConnectionType.Bluetooth,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.2",
                    Status = CameraStatus.Running,
                    Type = CameraType.PanoramicCamera,
                    ConnectionType = ConnectionType.Ethernet,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.2",
                    Status = CameraStatus.Running,
                    Type = CameraType.PanoramicCamera,
                    ConnectionType = ConnectionType.Ethernet,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                /*new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.3",
                    Status = CameraStatus.Failure,
                    Type = CameraType.SmartCamera,
                    ConnectionType = ConnectionType.SerialPort,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.1",
                    Status = CameraStatus.Paused,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Tcp,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                }*//*,
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.2",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Usb,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.3",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Bluetooth,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },

                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.3",
                    Status = CameraStatus.Failure,
                    Type = CameraType.SmartCamera,
                    ConnectionType = ConnectionType.SerialPort,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.1",
                    Status = CameraStatus.Paused,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Tcp,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.2",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Usb,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.3",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Bluetooth,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },*/
            };
            BarCodeItems = new()
            {
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = UploadStatus.Succeeded,
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)868.662,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = UploadStatus.Succeeded,
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                /*new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = "成功",
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },*/
            };
            _computerInfoReporter.ComputerInfoReceived += delegate (object? sender, ComputerInfoModel model) {
                AddNewRow(new BarCodeItemModel() {
                    Num = BarCodeItems.Count + 1,
                    Barcode = new Random().Next(100000000, 999999999).ToString()
                });
            };
        }

        /// <summary>
        /// 图像点击事件
        /// </summary>
        public ICommand ImageClickCommand {
            get => new DelegateCommand<CameraItemInfoModel>(ImageClickDelegate);
        }

        public ICommand UploadStatusCommand {
            get => new DelegateCommand<BarCodeItemModel>(UploadStatusDelegate);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<Page>(LoadedDelegate);
        }

        private async void LoadedDelegate(Page obj) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                _dataGrid = Utils.GetVisualChild<DataGrid>(obj, b => b.Name.Equals("BarCodeDataGrid"));
            });
        }

        private void UploadStatusDelegate(BarCodeItemModel obj) {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded) {
                _dialogService.ShowDialog("ApiAccessDialog", new DialogParameters { { "BarCodeItem", obj } }, null);
            }
        }

        private async void ImageClickDelegate(CameraItemInfoModel obj) {
            //放大图片(用另一个图像框显示、并重新绑定接收图像来源、过渡动画)
            await Application.Current.Dispatcher.InvokeAsync(() => {
                AddNewRow(new BarCodeItemModel() {
                    Barcode = new Random().Next(100000000, 999999999).ToString()
                });
            });
        }

        /// <summary>
        /// 状态点击事件
        /// </summary>
        public ICommand? StatusClickCommand {
            get => new DelegateCommand<CameraItemInfoModel>(StatusClickDelegate);
        }

        private async void StatusClickDelegate(CameraItemInfoModel obj) {
            //先加载进度条
            if (!obj.IsSwitchingState) {
                try {
                    obj.IsSwitchingState = true;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    obj.Status = obj.Status switch {
                        CameraStatus.Running => CameraStatus.Paused,
                        CameraStatus.Failure or CameraStatus.Paused or CameraStatus.Disconnected =>
                            CameraStatus.Running,
                        _ => obj.Status
                    };
                }
                catch (Exception e) {
                }
                finally {
                    obj.IsSwitchingState = false;
                }
            }

            Console.WriteLine(obj);
        }

        /// <summary>
        /// 添加一行
        /// </summary>
        private async void AddNewRow(BarCodeItemModel item) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                BarCodeItems.Insert(0, item);
                item.IsInserting = true;
                TotalDataCount += 1;
                if (item.RequestStatus == UploadStatus.Succeeded) {
                    UploadedDataCount += 1;
                }

                if (item.RequestStatus == UploadStatus.Failed) {
                    AbnormalDataCount += 1;
                }
            });
        }
    }
}