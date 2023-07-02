using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 主页单独工具接口
    /// </summary>
    public interface IHomeToolPlugin : IViewIPlugin {

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
        /// <param name="token"></param>
        void ShowDialog(CancellationToken token = default);
    }
}