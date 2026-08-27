using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using System.Windows.Input;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Legacy.Contracts.Dto.AppDto;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using WindowsAction = JayTom.Dws.Client.Events.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.Events.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages
{

    public class SettingsViewModel : BindableBase
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly IRegionManager _regionManager;
        private readonly ISettingsStore _settingsStore;
        private Frame? _frame;
        private ObservableCollection<MenuItemInfoModel> _menuItems;
        private decimal _listBoxMaxHeight = 900;
        private PassWordSettingsDto? _passWordSettingsDto;

        /// <summary>
        /// 标记页面导航是否正在执行，避免连续点击重复创建页面。
        /// </summary>
        private bool _isNavigating;

        /// <summary>
        /// 记录当前设置页面，避免重复导航到同一页面。
        /// </summary>
        private string _currentPageClassName = "DataManagementPage";

        /// <summary>
        /// 加载动画至少显示的毫秒数，确保用户能够看清动画。
        /// </summary>
        private const int MinimumLoadingDurationMilliseconds = 700;

        /// <summary>
        /// 标记设置页加载遮罩是否可见。
        /// </summary>
        private bool _isLoading;

        public SettingsViewModel(IRegionManager regionManager, ISettingsStore settingsStore,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _regionManager = regionManager;
            _settingsStore = settingsStore;
            _menuItems = new()
            {
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe69D",
                        IconSize = 25,
                    },
                    Title = "插件信息",
                    Description = "灵活下载/组合插件插件",
                    ClickCommand = ClickCommand,
                    PageClassName = "PluginMarketplacePage"
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6da",
                        IconSize = 22
                    },
                    Title = Languages.Language.ResourceManager.GetString("DataManagement")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("数据信息管理")??string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "DataManagementPage",
                    IsSelected = true
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe63a",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("CameraConfiguration")??string.Empty,
                    Description =Languages.Language.ResourceManager.GetString("相机管理配置")??string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CameraConfigurationPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe664",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("ApiInterface")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("Api上传接口设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "APISettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6b1",
                        IconSize = 23
                    },
                    Title = Languages.Language.ResourceManager.GetString("ImageStorageSettings")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("存图相关设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "SaveImageSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6af",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("FilteringSettings")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("条码过滤相关设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "BarcodeFilterSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe71e",
                        IconSize = 26
                    },
                    Title = Languages.Language.ResourceManager.GetString("WeightSettings")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("磅秤和称重设置相关") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "WeightSettingPages"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe72D",
                        IconSize = 28
                    },
                    Title = Languages.Language.ResourceManager.GetString("VolumeSettings")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("体积设置相关") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "VolumeSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe641",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("OutputResults")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("结果输出相关设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "ResultOutputSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6b5",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("InputContent")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("内容输入相关设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "ContentInputSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe79D",
                        IconSize = 33
                    },
                    Title = "分拣设置"??string.Empty,
                    Description = "分拣相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "PackageSortingSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe91e",
                        IconSize = 25
                    },
                    Title = "组包设置"??string.Empty,
                    Description = "组包相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CreatePackageSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7a0",
                        IconSize = 25
                    },
                    Title = "Ocr设置"??string.Empty,
                    Description = "Ocr相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "OcrSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe944",
                        IconSize = 25
                    },
                    Title = "云端服务"??string.Empty,
                    Description = "云端服务相关" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CloudServicePage"
                },
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe63e",
                        IconSize = 30
                    },
                    Title = "工作流"??string.Empty,
                    Description = "工作流相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "WorkflowSettingsPage"
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe713",
                        IconSize = 30
                    },
                    Title = "程序设置"??string.Empty,
                    Description = "程序设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "AppSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xeaad",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("LogManagement")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("运行日志\\设备日志\\通讯日志管理") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "LogManagerPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe74a",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("CacheCearing")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("释放缓存/空间的方案") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CacheClearSettingsPage"
                },
            };
            _eventBus.SubscribeAsync<WindowsAction>(async item =>
            {
                await Task.Delay(100);
                if (item is WindowsAction info && _frame is not null)
                {
                    if (info.Type == WindowsActionType.Maximize)
                    {
                        ListBoxMaxHeight = Convert.ToDecimal(_frame.ActualHeight);
                    }
                    else
                    {
                        ListBoxMaxHeight = 900;
                    }
                }
            });
            _eventBus.SubscribeAsync<SettingsChangedEvent>(async settings =>
            {
                if (settings is SettingsChangedEvent { SettingsName: "PassWordSettings" })
                {
                    _passWordSettingsDto = await _settingsStore.GetAsync<PassWordSettingsDto>("PassWordSettings") ?? new PassWordSettingsDto();
                }
            });
        }

        //MenuItems
        public ObservableCollection<MenuItemInfoModel> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        public decimal ListBoxMaxHeight
        {
            get => _listBoxMaxHeight;
            set => SetProperty(ref _listBoxMaxHeight, value);
        }

        /// <summary>
        /// 获取加载动画使用的显示信息。
        /// </summary>
        public LoadingDialogViewModel LoadingDialog { get; } = new()
        {
            Description = "正在加载页面..."
        };

        /// <summary>
        /// 获取或设置设置页加载遮罩是否可见。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand ClickCommand => new DelegateCommand<MenuItemInfoModel>(MenuClickDelegate);

        /// <summary>
        /// 窗口加载完成
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<Frame>(LoadedDelegate);

        private async void LoadedDelegate(Frame obj)
        {
            _frame = obj;
            _passWordSettingsDto ??= await _settingsStore.GetAsync<PassWordSettingsDto>("PassWordSettings") ?? new PassWordSettingsDto();
            if (!_regionManager.Regions.ContainsRegionWithName("ContentRegion"))
            {
                // 创建区域，用于视觉树以外的控件。
                RegionManager.SetRegionName(obj, NavigationRegions.Content.Name);
                RegionManager.SetRegionManager(obj, _regionManager);
                await NavigateWithLoadingAsync(
                    NavigationRequest.To(NavigationRegions.Content, _currentPageClassName));
            }
        }

        private async void MenuClickDelegate(MenuItemInfoModel obj)
        {
            if (_isNavigating ||
                string.IsNullOrWhiteSpace(obj?.PageClassName) ||
                string.Equals(
                    _currentPageClassName,
                    obj.PageClassName,
                    StringComparison.Ordinal))
            {
                return;
            }

            _isNavigating = true;
            try
            {
                // 弹出密码框。
                if (_passWordSettingsDto?.IsUsePasswordProtection == true && AppContext.GetData("IsValidationPassed") is not true &&
                    _passWordSettingsDto?.PasswordProtectionModuleItems
                        ?.Any(a => a.IsProtected && a.PageClassName.Equals(obj.PageClassName)) == true
                    )
                {
                    var passwordValidationDialog = new PasswordValidationDialog();
                    if (passwordValidationDialog.DataContext is PasswordValidationDialogViewModel viewModel)
                    {
                        viewModel.Identifier = "SettingDialog";
                        await DialogHost.Show(passwordValidationDialog, viewModel.Identifier);

                        if (!viewModel.IsValidationPassed)
                        {
                            return;
                        }
                    }
                }

                foreach (var item in MenuItems)
                {
                    item.IsSelected = false;
                    item.RadiusRight = new CornerRadius(0, 0, 0, 0);
                }

                obj.IsSelected = true;
                MenuItemInfoModel? previousItem = null, nextItem = null;
                var selectedIndex = MenuItems.IndexOf(obj);
                if (selectedIndex > 0)
                {
                    previousItem = MenuItems[selectedIndex - 1];
                }

                if (selectedIndex < MenuItems.Count - 1)
                {
                    nextItem = MenuItems[selectedIndex + 1];
                }

                previousItem?.RadiusRight = new CornerRadius(0, 0, 10, 0);

                nextItem?.RadiusRight = new CornerRadius(0, 10, 0, 0);

                await NavigateWithLoadingAsync(
                    NavigationRequest.To(NavigationRegions.Content, obj.PageClassName));
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// 在显示加载动画后导航，并在目标页面完成首帧渲染后关闭动画。
        /// </summary>
        /// <param name="request">强类型导航请求。</param>
        private async Task NavigateWithLoadingAsync(NavigationRequest request)
        {
            IsLoading = true;
            var minimumAnimationTask = Task.Delay(MinimumLoadingDurationMilliseconds);
            try
            {
                // 先让加载遮罩和等待圈完成首帧渲染，再创建目标页面。
                await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                _regionManager.Navigate(request);
                _currentPageClassName = request.Destination.RegisteredName;

                // 等待目标页面完成首帧布局，同时让等待圈至少完整显示一段时间。
                await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                await minimumAnimationTask;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
