using System.Windows.Controls;

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace JayTom.Dws.Client.Views.Pages.Preferences
{

    /// <summary>
    /// APISettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class APISettingsPage : Page
    {
        /// <summary>
        /// 页签头部横向滚动容器。
        /// </summary>
        private ScrollViewer? _tabHeaderScrollViewer;

        /// <summary>
        /// 向左浏览页签按钮。
        /// </summary>
        private Button? _scrollLeftButton;

        /// <summary>
        /// 向右浏览页签按钮。
        /// </summary>
        private Button? _scrollRightButton;

        /// <summary>
        /// 打开全部页签菜单按钮。
        /// </summary>
        private Button? _overflowButton;

        /// <summary>
        /// 初始化 API 设置页面。
        /// </summary>
        public APISettingsPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            ApiTabs.SelectionChanged += OnApiTabsSelectionChanged;
        }

        /// <summary>
        /// 页面加载后连接页签导航控件。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">事件参数。</param>
        private void OnPageLoaded(object sender, RoutedEventArgs eventArgs)
        {
            ConnectTabHeaderNavigation();
            ScheduleNavigationRefresh(ensureSelectionVisible: true);
        }

        /// <summary>
        /// 页面卸载时断开页签导航控件，避免重复订阅模板事件。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">事件参数。</param>
        private void OnPageUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            DisconnectTabHeaderNavigation();
        }

        /// <summary>
        /// 从自定义模板中取得导航部件并连接交互事件。
        /// </summary>
        private void ConnectTabHeaderNavigation()
        {
            DisconnectTabHeaderNavigation();
            ApiTabs.ApplyTemplate();

            _tabHeaderScrollViewer =
                ApiTabs.Template.FindName("PART_HeaderScrollViewer", ApiTabs) as ScrollViewer;
            _scrollLeftButton =
                ApiTabs.Template.FindName("PART_ScrollLeftButton", ApiTabs) as Button;
            _scrollRightButton =
                ApiTabs.Template.FindName("PART_ScrollRightButton", ApiTabs) as Button;
            _overflowButton =
                ApiTabs.Template.FindName("PART_OverflowButton", ApiTabs) as Button;

            if (_tabHeaderScrollViewer is not null)
            {
                _tabHeaderScrollViewer.ScrollChanged += OnHeaderScrollChanged;
                _tabHeaderScrollViewer.PreviewMouseWheel += OnHeaderPreviewMouseWheel;
                _tabHeaderScrollViewer.SizeChanged += OnHeaderSizeChanged;
            }

            if (_scrollLeftButton is not null)
            {
                _scrollLeftButton.Click += OnScrollLeftClick;
            }

            if (_scrollRightButton is not null)
            {
                _scrollRightButton.Click += OnScrollRightClick;
            }

            if (_overflowButton is not null)
            {
                _overflowButton.Click += OnOverflowClick;
            }
        }

        /// <summary>
        /// 断开当前模板部件的事件订阅。
        /// </summary>
        private void DisconnectTabHeaderNavigation()
        {
            if (_tabHeaderScrollViewer is not null)
            {
                _tabHeaderScrollViewer.ScrollChanged -= OnHeaderScrollChanged;
                _tabHeaderScrollViewer.PreviewMouseWheel -= OnHeaderPreviewMouseWheel;
                _tabHeaderScrollViewer.SizeChanged -= OnHeaderSizeChanged;
            }

            if (_scrollLeftButton is not null)
            {
                _scrollLeftButton.Click -= OnScrollLeftClick;
            }

            if (_scrollRightButton is not null)
            {
                _scrollRightButton.Click -= OnScrollRightClick;
            }

            if (_overflowButton is not null)
            {
                _overflowButton.Click -= OnOverflowClick;
            }

            _tabHeaderScrollViewer = null;
            _scrollLeftButton = null;
            _scrollRightButton = null;
            _overflowButton = null;
        }

        /// <summary>
        /// 页签选择变化后自动展示选中的页签头。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">选择变化事件参数。</param>
        private void OnApiTabsSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            if (!ReferenceEquals(eventArgs.OriginalSource, ApiTabs))
            {
                return;
            }

            ScheduleNavigationRefresh(ensureSelectionVisible: true);
        }

        /// <summary>
        /// 页签头滚动后刷新左右按钮状态。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">滚动变化事件参数。</param>
        private void OnHeaderScrollChanged(object sender, ScrollChangedEventArgs eventArgs)
        {
            UpdateNavigationButtons();
        }

        /// <summary>
        /// 页签头可用宽度变化后重新判断是否存在溢出。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">尺寸变化事件参数。</param>
        private void OnHeaderSizeChanged(object sender, SizeChangedEventArgs eventArgs)
        {
            ScheduleNavigationRefresh(ensureSelectionVisible: true);
        }

        /// <summary>
        /// 将页签头区域的鼠标滚轮转换为横向浏览。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">滚轮事件参数。</param>
        private void OnHeaderPreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
        {
            if (_tabHeaderScrollViewer is null || _tabHeaderScrollViewer.ScrollableWidth <= 0)
            {
                return;
            }

            _tabHeaderScrollViewer.ScrollToHorizontalOffset(
                _tabHeaderScrollViewer.HorizontalOffset - eventArgs.Delta);
            eventArgs.Handled = true;
            UpdateNavigationButtons();
        }

        /// <summary>
        /// 向左翻动一页页签头。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">点击事件参数。</param>
        private void OnScrollLeftClick(object sender, RoutedEventArgs eventArgs)
        {
            ScrollHeaders(-1);
        }

        /// <summary>
        /// 向右翻动一页页签头。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">点击事件参数。</param>
        private void OnScrollRightClick(object sender, RoutedEventArgs eventArgs)
        {
            ScrollHeaders(1);
        }

        /// <summary>
        /// 打开包含全部 API 页签的快捷菜单。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="eventArgs">点击事件参数。</param>
        private void OnOverflowClick(object sender, RoutedEventArgs eventArgs)
        {
            if (_overflowButton is null || ApiTabs.Items.Count == 0)
            {
                return;
            }

            var menu = new ContextMenu
            {
                Placement = PlacementMode.Bottom,
                PlacementTarget = _overflowButton,
                MinWidth = 190,
                MaxHeight = 480,
                StaysOpen = false
            };

            for (var index = 0; index < ApiTabs.Items.Count; index++)
            {
                var capturedIndex = index;
                var tabItem = ApiTabs.Items[index] as TabItem ??
                    ApiTabs.ItemContainerGenerator.ContainerFromIndex(index) as TabItem;
                var header = GetTabHeaderText(tabItem?.Header, index);
                var menuItem = new MenuItem
                {
                    Header = header,
                    IsCheckable = true,
                    IsChecked = index == ApiTabs.SelectedIndex,
                    IsEnabled = tabItem?.IsEnabled ?? true
                };
                AutomationProperties.SetName(menuItem, $"切换到{header}");
                menuItem.Click += (_, _) => SelectTab(capturedIndex);
                menu.Items.Add(menuItem);
            }

            _overflowButton.ContextMenu = menu;
            menu.IsOpen = true;
        }

        /// <summary>
        /// 按指定方向翻动页签头视口。
        /// </summary>
        /// <param name="direction">负数向左，正数向右。</param>
        private void ScrollHeaders(int direction)
        {
            if (_tabHeaderScrollViewer is null)
            {
                return;
            }

            var pageSize = Math.Max(160, (_tabHeaderScrollViewer.ViewportWidth * 72) / 100);
            var targetOffset = _tabHeaderScrollViewer.HorizontalOffset + pageSize * direction;
            _tabHeaderScrollViewer.ScrollToHorizontalOffset(targetOffset);
            UpdateNavigationButtons();
        }

        /// <summary>
        /// 选择指定页签并确保其页签头可见。
        /// </summary>
        /// <param name="index">页签索引。</param>
        private void SelectTab(int index)
        {
            if (index < 0 || index >= ApiTabs.Items.Count)
            {
                return;
            }

            ApiTabs.SelectedIndex = index;
            ScheduleNavigationRefresh(ensureSelectionVisible: true);
        }

        /// <summary>
        /// 延迟到布局完成后刷新导航状态。
        /// </summary>
        /// <param name="ensureSelectionVisible">是否将当前选中页签滚入可视区。</param>
        private void ScheduleNavigationRefresh(bool ensureSelectionVisible)
        {
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (_tabHeaderScrollViewer is null)
                    {
                        return;
                    }

                    if (ensureSelectionVisible)
                    {
                        EnsureSelectedHeaderVisible();
                    }

                    UpdateNavigationButtons();
                }));
        }

        /// <summary>
        /// 将当前选中的页签头滚动到页签视口内。
        /// </summary>
        private void EnsureSelectedHeaderVisible()
        {
            if (_tabHeaderScrollViewer is null || ApiTabs.SelectedIndex < 0)
            {
                return;
            }

            ApiTabs.UpdateLayout();
            var selectedTab = ApiTabs.ItemContainerGenerator.ContainerFromIndex(ApiTabs.SelectedIndex)
                as FrameworkElement ?? ApiTabs.Items[ApiTabs.SelectedIndex] as FrameworkElement;
            if (selectedTab is null)
            {
                return;
            }

            try
            {
                var position = selectedTab.TransformToAncestor(_tabHeaderScrollViewer)
                    .Transform(new Point(0, 0));
                var targetOffset = _tabHeaderScrollViewer.HorizontalOffset;
                const int edgePadding = 8;

                if (position.X < edgePadding)
                {
                    targetOffset += position.X - edgePadding;
                }
                else if (position.X + selectedTab.ActualWidth >
                         _tabHeaderScrollViewer.ViewportWidth - edgePadding)
                {
                    targetOffset += position.X + selectedTab.ActualWidth -
                                    _tabHeaderScrollViewer.ViewportWidth + edgePadding;
                }

                _tabHeaderScrollViewer.ScrollToHorizontalOffset(
                    Math.Clamp(targetOffset, 0, _tabHeaderScrollViewer.ScrollableWidth));
            }
            catch (InvalidOperationException)
            {
                // 模板切换或页面卸载过程中，页签可能暂时不在滚动容器的可视树中。
            }
        }

        /// <summary>
        /// 根据当前滚动位置更新翻页按钮的显示和可用状态。
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (_tabHeaderScrollViewer is null)
            {
                return;
            }

            const int offsetTolerance = 1;
            var hasOverflow = _tabHeaderScrollViewer.ScrollableWidth > offsetTolerance;
            var navigationVisibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;

            if (_scrollLeftButton is not null)
            {
                _scrollLeftButton.Visibility = navigationVisibility;
                _scrollLeftButton.IsEnabled = hasOverflow &&
                                              _tabHeaderScrollViewer.HorizontalOffset > offsetTolerance;
            }

            if (_scrollRightButton is not null)
            {
                _scrollRightButton.Visibility = navigationVisibility;
                _scrollRightButton.IsEnabled = hasOverflow &&
                                               _tabHeaderScrollViewer.HorizontalOffset <
                                               _tabHeaderScrollViewer.ScrollableWidth - offsetTolerance;
            }

            if (_overflowButton is not null)
            {
                _overflowButton.Visibility = ApiTabs.Items.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 获取用于“更多”菜单展示的页签名称。
        /// </summary>
        /// <param name="header">页签头内容。</param>
        /// <param name="index">页签索引。</param>
        /// <returns>可读的页签名称。</returns>
        private static string GetTabHeaderText(object? header, int index)
        {
            return header switch
            {
                string text when !string.IsNullOrWhiteSpace(text) => text,
                TextBlock textBlock when !string.IsNullOrWhiteSpace(textBlock.Text) => textBlock.Text,
                not null => header.ToString() ?? $"接口 {index + 1}",
                _ => $"接口 {index + 1}"
            };
        }
    }
}
