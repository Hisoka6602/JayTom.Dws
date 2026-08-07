using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace JayTom.Dws.Client.Controls
{

    /// <summary>
    /// 在控件首次显示时才导航到目标页面，避免选项卡一次性创建全部页面。
    /// </summary>
    public sealed class DeferredFrame : Frame
    {

        /// <summary>
        /// 延迟导航地址依赖属性。
        /// </summary>
        public static readonly DependencyProperty DeferredSourceProperty =
            DependencyProperty.Register(
                nameof(DeferredSource),
                typeof(Uri),
                typeof(DeferredFrame),
                new PropertyMetadata(null));

        /// <summary>
        /// 标记目标页面是否已经加载。
        /// </summary>
        private bool _isSourceLoaded;

        /// <summary>
        /// 初始化延迟加载页面容器。
        /// </summary>
        public DeferredFrame()
        {
            Loaded += OnLoaded;
        }

        /// <summary>
        /// 获取或设置首次显示时需要导航的页面地址。
        /// </summary>
        public Uri? DeferredSource
        {
            get => (Uri?)GetValue(DeferredSourceProperty);
            set => SetValue(DeferredSourceProperty, value);
        }

        /// <summary>
        /// 首次显示控件时加载目标页面。
        /// </summary>
        /// <param name="sender">触发加载事件的控件。</param>
        /// <param name="eventArgs">加载事件参数。</param>
        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            if (_isSourceLoaded || DeferredSource is null)
            {
                return;
            }

            _isSourceLoaded = true;
            Source = DeferredSource.IsAbsoluteUri
                ? DeferredSource
                : new Uri(BaseUriHelper.GetBaseUri(this), DeferredSource);
        }
    }
}
