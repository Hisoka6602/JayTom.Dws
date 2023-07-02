using System;
using System.Windows.Controls;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 主页接口
    /// </summary>
    public interface IHomePlugin : IViewIPlugin {

        /// <summary>
        /// 打开内页后事件
        /// </summary>
        event EventHandler<EventArgs> InnerPageOpened;

        /// <summary>
        /// 关闭内页后事件
        /// </summary>
        event EventHandler<EventArgs> InnerPageClosed;

        /// <summary>
        /// 程序退出事件
        /// </summary>
        event EventHandler<EventArgs> ApplicationExitRequested;

        /// <summary>
        /// 主页图标
        /// </summary>
        byte[] Icon { get; }

        /// <summary>
        /// 程序标题
        /// </summary>
        string ProgramTitle { get; }

        /// <summary>
        /// 程序名称
        /// </summary>
        string ProgramName { get; }

        /// <summary>
        /// 加载插件
        /// </summary>
        void Load();

        /// <summary>
        /// 打开内页
        /// </summary>
        void OpenInnerPage();

        /// <summary>
        /// 关闭内页
        /// </summary>
        void CloseInnerPage();
    }
}