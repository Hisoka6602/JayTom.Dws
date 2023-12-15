using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.VideoApiClient.Models;
using JayTom.Dws.VideoApiClient.Views.Editors;
using JayTom.Dws.VideoApiClient.ViewModels.Dialog;
using JayTom.Dws.VideoApiClient.ViewModels.Editors;

namespace JayTom.Dws.VideoApiClient.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private double _uniformCornerRadius = 10;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "最大化";
        private int _yesterdayBarcodeCount;
        private int _todayBarcodeCount;
        private DateTime? _nodeStartTime;
        private DateTime? _nodeEndTime;
        private List<string> _nodeList = new();
        private string? _selectedNode;
        private string? _barcode;
        private string? _cameraName;
        private int _pageCount;
        private int _pageIndex = 1;
        private int _pageSize = 100;

        private ObservableCollection<BarCodeItemModel> _barCodeItems = new()
        {
            new BarCodeItemModel()
            {
                BarCode = "SF123456789",
                CameraCustomName = "自定义名称1",
                CameraSerialNumber = "DAO555777888",
                NodeName = "节点1",
                Num = 1,
                ScanImageUrl = "https://cn.bing.com/images/search?view=detailV2&ccid=bgRCTyf0&id=E5BE88AFC56697CDE348D5A2D23F4905C002D754&thid=OIP.bgRCTyf0gxs-65-GGGT_kQHaEK&mediaurl=https%3a%2f%2fbbsfiles.vivo.com.cn%2fvivobbs%2fattachment%2fforum%2f201403%2f07%2f124302g8ftlk59dlcfqtoi.jpg&exph=2160&expw=3840&q=%e5%a3%81%e7%ba%b8&simid=608037425498389439&FORM=IRPRST&ck=1DED562AFBE1F3E34ADD5B96AA9F15A5&selectedIndex=2&itb=0&qpvt=%e5%a3%81%e7%ba%b8&ajaxhist=0&ajaxserp=0",
                ScanImageVisible = true,
                ScanTime = DateTime.Now,
                PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>()
                {
                    new()
                    {
                        ImageUrl = "https://cn.bing.com/images/search?view=detailV2&ccid=fbmVvqcT&id=F6680576C50090DE72CE6EEAFBA9EB579B54E340&thid=OIP.fbmVvqcTmK2ByjtyUx03WgHaEo&mediaurl=https%3A%2F%2Fts1.cn.mm.bing.net%2Fth%2Fid%2FR-C.7db995bea71398ad81ca3b72531d375a%3Frik%3DQONUm1frqfvqbg%26riu%3Dhttp%253a%252f%252fpic.bizhi360.com%252fbbpic%252f77%252f3477.jpg%26ehk%3DMyA5tLeOYZ4D9PHlB7i5jM8EF8rod1J31k%252fjnocTSe4%253d%26risl%3D%26pid%3DImgRaw%26r%3D0&exph=1050&expw=1680&q=%e5%a3%81%e7%ba%b8&simid=608046818578400455&form=IRPRST&ck=C375D3B228659679CFE5A50798B69EEE&selectedindex=1&itb=0&qpvt=%e5%a3%81%e7%ba%b8&ajaxhist=0&ajaxserp=0&vt=0&sim=11",
                        ImageVisible = true
                    },
                    new()
                    {
                        ImageUrl = "https://cn.bing.com/images/search?view=detailV2&ccid=GsQBVXXa&id=1DA6011E2BE719575E0DD1F7389F205B2E244C49&thid=OIP.GsQBVXXaX59PFfxzxGAPCAHaEo&mediaurl=https%3A%2F%2Fts1.cn.mm.bing.net%2Fth%2Fid%2FR-C.1ac4015575da5f9f4f15fc73c4600f08%3Frik%3DSUwkLlsgnzj30Q%26riu%3Dhttp%253a%252f%252fit.people.com.cn%252fmediafile%252f200807%252f18%252fF200807181408392043013242.jpg%26ehk%3DY5Jms6733640Shi6R5OXcUKbJsxn%252bdpsqWF%252beacGhz4%253d%26risl%3D%26pid%3DImgRaw%26r%3D0&exph=1200&expw=1920&q=%e5%a3%81%e7%ba%b8&simid=608025876327380709&form=IRPRST&ck=04459EFEC5305B893D107C71C953B4CA&selectedindex=0&itb=0&qpvt=%e5%a3%81%e7%ba%b8&ajaxhist=0&ajaxserp=0&vt=0&sim=11",
                        ImageVisible = true
                    },
                }
            },
            new BarCodeItemModel()
            {
                BarCode = "SF123456789",
                CameraCustomName = "自定义名称1",
                CameraSerialNumber = "DAO555777888",
                NodeName = "节点1",
                Num = 1,
                ScanImageUrl = "https://cn.bing.com/images/search?view=detailV2&ccid=bgRCTyf0&id=E5BE88AFC56697CDE348D5A2D23F4905C002D754&thid=OIP.bgRCTyf0gxs-65-GGGT_kQHaEK&mediaurl=https%3a%2f%2fbbsfiles.vivo.com.cn%2fvivobbs%2fattachment%2fforum%2f201403%2f07%2f124302g8ftlk59dlcfqtoi.jpg&exph=2160&expw=3840&q=%e5%a3%81%e7%ba%b8&simid=608037425498389439&FORM=IRPRST&ck=1DED562AFBE1F3E34ADD5B96AA9F15A5&selectedIndex=2&itb=0&qpvt=%e5%a3%81%e7%ba%b8&ajaxhist=0&ajaxserp=0",
                ScanImageVisible = true,
                ScanTime = DateTime.Now,
                PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>()
                {
                    new()
                    {
                        ImageUrl = "https://cn.bing.com/images/search?view=detailV2&ccid=fbmVvqcT&id=F6680576C50090DE72CE6EEAFBA9EB579B54E340&thid=OIP.fbmVvqcTmK2ByjtyUx03WgHaEo&mediaurl=https%3A%2F%2Fts1.cn.mm.bing.net%2Fth%2Fid%2FR-C.7db995bea71398ad81ca3b72531d375a%3Frik%3DQONUm1frqfvqbg%26riu%3Dhttp%253a%252f%252fpic.bizhi360.com%252fbbpic%252f77%252f3477.jpg%26ehk%3DMyA5tLeOYZ4D9PHlB7i5jM8EF8rod1J31k%252fjnocTSe4%253d%26risl%3D%26pid%3DImgRaw%26r%3D0&exph=1050&expw=1680&q=%e5%a3%81%e7%ba%b8&simid=608046818578400455&form=IRPRST&ck=C375D3B228659679CFE5A50798B69EEE&selectedindex=1&itb=0&qpvt=%e5%a3%81%e7%ba%b8&ajaxhist=0&ajaxserp=0&vt=0&sim=11",
                        ImageVisible = true
                    },
                    new()
                    {
                        ImageUrl = "https://cn.bing.com/images/search?view=detailV2&ccid=GsQBVXXa&id=1DA6011E2BE719575E0DD1F7389F205B2E244C49&thid=OIP.GsQBVXXaX59PFfxzxGAPCAHaEo&mediaurl=https%3A%2F%2Fts1.cn.mm.bing.net%2Fth%2Fid%2FR-C.1ac4015575da5f9f4f15fc73c4600f08%3Frik%3DSUwkLlsgnzj30Q%26riu%3Dhttp%253a%252f%252fit.people.com.cn%252fmediafile%252f200807%252f18%252fF200807181408392043013242.jpg%26ehk%3DY5Jms6733640Shi6R5OXcUKbJsxn%252bdpsqWF%252beacGhz4%253d%26risl%3D%26pid%3DImgRaw%26r%3D0&exph=1200&expw=1920&q=%e5%a3%81%e7%ba%b8&simid=608025876327380709&form=IRPRST&ck=04459EFEC5305B893D107C71C953B4CA&selectedindex=0&itb=0&qpvt=%e5%a3%81%e7%ba%b8&ajaxhist=0&ajaxserp=0&vt=0&sim=11",
                        ImageVisible = true
                    },
                }
            },
        };

        public MainWindowViewModel(IDialogService dialogService) {
            _dialogService = dialogService;
        }

        public SnackbarMessageQueue MainMessageQueue {
            get => _mainMessageQueue;
            set => SetProperty(ref _mainMessageQueue, value);
        }

        public double UniformCornerRadius {
            get => _uniformCornerRadius;
            set => SetProperty(ref _uniformCornerRadius, value);
        }

        /// <summary>
        /// 最大化按钮图标
        /// </summary>
        public string MaxBtnIcon {
            get => _maxBtnIcon;
            set => SetProperty(ref _maxBtnIcon, value);
        }

        /// <summary>
        /// 最大化按钮提示内容
        /// </summary>
        public string MaxBtnToolTip {
            get => _maxBtnToolTip;
            set => SetProperty(ref _maxBtnToolTip, value);
        }

        /// <summary>
        /// 昨天条码数量
        /// </summary>
        public int YesterdayBarcodeCount {
            get => _yesterdayBarcodeCount;
            set => SetProperty(ref _yesterdayBarcodeCount, value);
        }

        /// <summary>
        /// 今天条码数量
        /// </summary>
        public int TodayBarcodeCount {
            get => _todayBarcodeCount;
            set => SetProperty(ref _todayBarcodeCount, value);
        }

        /// <summary>
        /// 节点开始时间
        /// </summary>
        public DateTime? NodeStartTime {
            get => _nodeStartTime;
            set => SetProperty(ref _nodeStartTime, value);
        }

        /// <summary>
        /// 节点结束时间
        /// </summary>
        public DateTime? NodeEndTime {
            get => _nodeEndTime;
            set => SetProperty(ref _nodeEndTime, value);
        }

        /// <summary>
        /// 节点列表
        /// </summary>
        public List<string> NodeList {
            get => _nodeList;
            set => SetProperty(ref _nodeList, value);
        }

        /// <summary>
        /// 节点选中项
        /// </summary>
        public string? SelectedNode {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string? Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 相机名称
        /// </summary>
        public string? CameraName {
            get => _cameraName;
            set => SetProperty(ref _cameraName, value);
        }

        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex {
            get => _pageIndex;
            set => SetProperty(ref _pageIndex, value);
        }

        public ObservableCollection<BarCodeItemModel> BarCodeItems {
            get => _barCodeItems;
            set => SetProperty(ref _barCodeItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
        }

        public ICommand SizeChangedCommand {
            get => new DelegateCommand<object>(SizeChangeDelegate);
        }

        private void SizeChangeDelegate(object obj) {
            if (obj is Window window) {
                window.MaxHeight = SystemParameters.WorkArea.Width;
                window.MaxHeight = SystemParameters.WorkArea.Height;
                if (window.WindowState == WindowState.Maximized ||
                    (window.Height >= SystemParameters.WorkArea.Width &&
                     window.Width >= SystemParameters.WorkArea.Height)) {
                    //直角
                    UniformCornerRadius = 0;
                }
                else {
                    UniformCornerRadius = 10;
                    //圆角
                }
                if (window.WindowState == WindowState.Maximized) {
                    MaxBtnIcon = "\xe72c";
                    MaxBtnToolTip = "还原";
                }
                else {
                    MaxBtnIcon = "\xe600";
                    MaxBtnToolTip = "最大化";
                }
            }
        }

        public ICommand MinWinCommand {
            get => new DelegateCommand<object>(MinWinDelegate);
        }

        private void MinWinDelegate(object obj) {
            if (obj is Window window) {
                window.WindowState = WindowState.Minimized;
            }
        }

        public ICommand MaxWinCommand {
            get => new DelegateCommand<object>(MaxWinDelegate);
        }

        private void MaxWinDelegate(object obj) {
            if (obj is Window window) {
                if (window.WindowState == WindowState.Maximized) {
                    window.WindowState = WindowState.Normal;
                    return;
                }
                window.WindowState = WindowState.Maximized;
            }
        }

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj) {
            System.Windows.Application.Current.Shutdown();//关闭
        }

        public ICommand OpenDateTimeDialogCommand {
            get => new DelegateCommand<object>(OpenDateTimeDialogDelegate);
        }

        private async void OpenDateTimeDialogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model) {
                    model.Identifier = "MainDialog";
                    if (obj?.ToString()?.Equals("StartTime") == true) {
                        model.SelectedDataTime = NodeStartTime ?? DateTime.Today;
                        model.SelectedDate = NodeStartTime ?? DateTime.Today;
                        model.SelectedTime = NodeStartTime ?? DateTime.Today;
                    }
                    else {
                        model.SelectedDataTime = NodeEndTime ?? DateTime.Now;
                        model.SelectedDate = NodeEndTime ?? DateTime.Now;
                        model.SelectedTime = NodeEndTime ?? DateTime.Now;
                    }

                    await DialogHost.Show(dataTimeEditor, model.Identifier);
                    if (model.IsOk) {
                        if (obj?.ToString()?.Equals("StartTime") == true) {
                            NodeStartTime = model.SelectedDataTime.Value;
                        }
                        else if (obj?.ToString()?.Equals("EndTime") == true) {
                            if (DateTime.Now.CompareTo(model.SelectedDataTime.Value) < 0) {
                                //DataListMessageQueue.Enqueue("截止时间不能超过当前时间!");
                                NodeEndTime = DateTime.Now;
                                return;
                            }
                            NodeEndTime = model.SelectedDataTime.Value;
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 清空搜素条件
        /// </summary>
        public ICommand ClearSearchCriteriaCommand {
            get => new DelegateCommand<object>(ClearSearchCriteriaDelegate);
        }

        private async void ClearSearchCriteriaDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                NodeStartTime = null;
                NodeEndTime = null;
                SelectedNode = null;
                Barcode = null;
                CameraName = null;
            });
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public ICommand SearchCommand {
            get => new DelegateCommand<object>(SearchDelegate);
        }

        private void SearchDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        public ICommand SettingCommand {
            get => new DelegateCommand<object>(SettingDelegate);
        }

        private async void SettingDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                // _dialogService.Show("VideoDialog", new DialogParameters { { "VideoItem", obj } }, null);

                var settingDialog = new SettingDialog();
                if (settingDialog.DataContext is SettingDialogViewModel model) {
                    model.Identifier = "MainDialog";

                    await DialogHost.Show(settingDialog, model.Identifier);
                }
            });
        }

        private async void LoadData(int pageIndex, DateTime? startTime, DateTime? endTime,
            string? nodeName, string? barCode, string? cameraName) {
            return;
        }
    }
}