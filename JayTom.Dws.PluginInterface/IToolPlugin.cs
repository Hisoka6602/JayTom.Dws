using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 工具接口
    /// </summary>
    public interface IToolPlugin : IPlugin {

        /// <summary>
        /// 页面内容
        /// </summary>
        UserControl Content { get; }

        /// <summary>
        /// 准备处理事件
        /// </summary>
        event EventHandler<object> Preparing;

        /// <summary>
        /// 处理完成事件
        /// </summary>
        event EventHandler<object> Completed;

        /// <summary>
        /// 菜单图标
        /// </summary>
        byte[] MenuIcon { get; }

        /// <summary>
        /// 菜单标题
        /// </summary>
        string MenuTitle { get; }

        /// <summary>
        /// 关闭弹窗
        /// </summary>
        void CloseDialog();

        /// <summary>
        /// 显示弹窗
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        void ShowDialog(object message, CancellationToken token = default);
    }
}