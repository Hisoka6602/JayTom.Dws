using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 弹窗接口
    /// </summary>
    public interface IDialogPlugin : IPlugin {

        /// <summary>
        /// 弹窗内容
        /// </summary>
        UserControl Content { get; }

        /// <summary>
        /// 弹窗前事件
        /// </summary>
        event EventHandler<object> DialogOpening;

        /// <summary>
        /// 弹窗后事件
        /// </summary>
        event EventHandler<object> DialogOpened;

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