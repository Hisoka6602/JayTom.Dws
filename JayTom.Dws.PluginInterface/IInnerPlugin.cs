using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 内页接口
    /// </summary>
    public interface IInnerPlugin : IViewIPlugin {

        /// <summary>
        /// 菜单图标
        /// </summary>
        byte[] MenuIcon { get; }

        /// <summary>
        /// 菜单标题
        /// </summary>
        string MenuTitle { get; }

        /// <summary>
        /// 加载页面
        /// </summary>
        void LoadPage();

        /// <summary>
        /// 显示页面
        /// </summary>
        void ShowPage();

        /// <summary>
        /// 隐藏页面
        /// </summary>
        void HidePage();

        /// <summary>
        /// 释放页面
        /// </summary>
        void ReleasePage();
    }
}