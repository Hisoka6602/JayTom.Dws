using JayTom.Dws.Client.Models;
using JayTom.Dws.PluginInterface;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class PluginMarketplaceViewModel : BindableBase {
        private ObservableCollection<PluginItemInfoModel> _pluginItems;
        private ObservableCollection<PluginTypeItemInfoModel> _pluginTypeItems;
        private PluginItemInfoModel _selectPluginItem = new();

        public PluginMarketplaceViewModel() {
            _pluginTypeItems = new ObservableCollection<PluginTypeItemInfoModel>()
            {
                new()
                {
                    ToolTip = "集成插件",
                    FontIcon = "\xe638"
                },
                new()
                {
                    ToolTip = "主页插件",
                    FontIcon = "\xe8a1"
                },
                new()
                {
                    ToolTip = "内页插件",
                    FontIcon = "\xe731"
                },
                new()
                {
                    ToolTip = "弹窗插件",
                    FontIcon = "\xe61d"
                },
                new()
                {
                    ToolTip = "控件插件",
                    FontIcon = "\xe645"
                },
                new()
                {
                    ToolTip = "工具插件",
                    FontIcon = "\xe797"
                },
                new()
                {
                    ToolTip = "Api上传插件",
                    FontIcon = "\xe664",
                },
                new()
                {
                    ToolTip = "过滤逻辑插件",
                    FontIcon = "\xe675"
                },
                new()
                {
                    ToolTip = "处理逻辑插件",
                    FontIcon = "\xe6e0"
                },
                new()
                {
                    ToolTip = "初始化插件",
                    FontIcon = "\xe8b1"
                },
                new()
                {
                    ToolTip = "后台处理插件",
                    FontIcon = "\xe603"
                },
                new()
                {
                    ToolTip = "设备插件",
                    FontIcon = "\xeb01"
                },
                new()
                {
                    ToolTip = "弹窗工具插件",
                    FontIcon = "\xe61f"
                }
            };
            _pluginItems = new ObservableCollection<PluginItemInfoModel>()
            {
                new()
                {
                    Type = PluginType.Api,
                    Status = PluginStatus.Upgradeable,
                    Name = "IInnerPlugin",
                    Author = "Hisoka",
                    Version = new Version("1.0.0.0"),
                    Description = "DWS（Data Warehouse System）是数据仓库系统的缩写，它是一种用于管理和组织大量数据的技术。DWS通过集成、清洗和转换多个异构数据源，将数据存储在一个中央存储库中，为用户提供方便的数据访问和分析功能。它采用了并行计算和高性能硬件架构，支持复杂查询、数据挖掘和报表生成等操作，帮助企业快速从海量数据中提取有价值的信息和洞察。DWS还具备数据安全性和数据质量控制的能力，可以提供决策支持和业务智能分析。",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Type = PluginType.Tool,
                    Status = PluginStatus.BugFound,
                    Name = "IInnerPlugin",
                    Author = "Hisoka",
                    Version = new Version("1.0.0.0"),
                    Description = "DWS（Data Warehouse System）是数据仓库系统的缩写，它是一种用于管理和组织大量数据的技术。DWS通过集成、清洗和转换多个异构数据源，将数据存储在一个中央存储库中，为用户提供方便的数据访问和分析功能。它采用了并行计算和高性能硬件架构，支持复杂查询、数据挖掘和报表生成等操作，帮助企业快速从海量数据中提取有价值的信息和洞察。DWS还具备数据安全性和数据质量控制的能力，可以提供决策支持和业务智能分析。",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Type = PluginType.Device,
                    Status = PluginStatus.Invalid,
                    Name = "IInnerPlugin",
                    Author = "Hisoka",
                    Version = new Version("1.0.0.0"),
                    Description = "DWS（Data Warehouse System）是数据仓库系统的缩写，它是一种用于管理和组织大量数据的技术。DWS通过集成、清洗和转换多个异构数据源，将数据存储在一个中央存储库中，为用户提供方便的数据访问和分析功能。它采用了并行计算和高性能硬件架构，支持复杂查询、数据挖掘和报表生成等操作，帮助企业快速从海量数据中提取有价值的信息和洞察。DWS还具备数据安全性和数据质量控制的能力，可以提供决策支持和业务智能分析。",
                    ClickCommand = ClickCommand
                },
            };
        }

        public ObservableCollection<PluginItemInfoModel> PluginItems {
            get => _pluginItems;
            set => SetProperty(ref _pluginItems, value);
        }

        public ObservableCollection<PluginTypeItemInfoModel> PluginTypeItems {
            get => _pluginTypeItems;
            set => SetProperty(ref _pluginTypeItems, value);
        }

        public PluginItemInfoModel SelectPluginItem {
            get => _selectPluginItem;
            set => SetProperty(ref _selectPluginItem, value);
        }

        public ICommand ClickCommand {
            get => new DelegateCommand<PluginItemInfoModel>(ClickDelegate);
        }

        private async void ClickDelegate(PluginItemInfoModel obj) {
            this.SelectPluginItem = obj;
        }
    }
}