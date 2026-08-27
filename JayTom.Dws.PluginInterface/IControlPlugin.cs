using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 控件接口
    /// </summary>
    public interface IControlPlugin : IViewIPlugin {

        /// <summary>
        /// 控件显示前事件
        /// </summary>
        event EventHandler<PluginWpfEventArgs> ControlShowing;

        /// <summary>
        /// 控件显示后事件
        /// </summary>
        event EventHandler<PluginWpfEventArgs> ControlShown;

        /// <summary>
        /// 控件交互事件
        /// </summary>
        event EventHandler<PluginWpfEventArgs> ControlInteraction;

        /// <summary>
        /// 显示控件方法
        /// </summary>
        void ShowControl();

        /// <summary>
        /// 设置父控件方法
        /// </summary>
        /// <param name="parent">父控件</param>
        /// <param name="token"></param>
        Task<JayTom.Dws.Abstractions.Results.OperationResult<string>> SetParentControl(
            UserControl parent,
            CancellationToken token = default);

        /// <summary>
        /// 收缩控件方法
        /// </summary>
        void CollapseControl();

        /// <summary>
        /// 展开控件方法
        /// </summary>
        void ExpandControl();

        /// <summary>
        /// 释放控件方法
        /// </summary>
        void ReleaseControl();
    }
}
