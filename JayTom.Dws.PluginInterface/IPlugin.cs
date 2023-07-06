using System;
using System.Windows;

namespace JayTom.Dws.PluginInterface {
    public interface IPlugin {

        //Guid
        public Guid Id { get; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 版本
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// 插件路径(全路径)
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// 简述
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// 发行日期
        /// </summary>
        public DateTime ReleaseDate { get; }

        /// <summary>
        /// 客户端依赖版本
        /// </summary>
        public Version ClientDependencyVersion { get; }

        /// <summary>
        /// 类型
        /// </summary>
        public PluginType Type { get; }

        /// <summary>
        /// 插件消息
        /// </summary>
        public event EventHandler<PluginMessageEventArgs> PluginMessageReceived;

        /// <summary>
        /// 插件加载事件
        /// </summary>
        public event EventHandler<IPlugin> PluginLoaded;

        /// <summary>
        /// 插件退出事件
        /// </summary>
        public event EventHandler<IPlugin> PluginExited;

        /// <summary>
        /// 语言切换事件
        /// </summary>
        public event EventHandler<ResourceDictionary> LanguageChanged;

        /// <summary>
        /// 插件异常事件
        /// </summary>
        public event EventHandler<Exception> PluginExceptionOccurred;
    }

    public enum PluginType {

        /// <summary>
        /// 拓展包
        /// </summary>
        ExtensionPackage,

        /// <summary>
        /// 主页
        /// </summary>
        Home,

        /// <summary>
        /// 内页
        /// </summary>
        Inner,

        /// <summary>
        /// 弹窗
        /// </summary>
        Dialog,

        /// <summary>
        /// 控件
        /// </summary>
        Control,

        /// <summary>
        /// 工具
        /// </summary>
        Tool,

        /// <summary>
        /// Api上传接口
        /// </summary>
        Api,

        /// <summary>
        /// 过滤逻辑
        /// </summary>
        Filter,

        /// <summary>
        /// 处理逻辑
        /// </summary>
        Process,

        /// <summary>
        /// 初始化插件
        /// </summary>
        Initialize,

        /// <summary>
        /// 后台处理
        /// </summary>
        Background,

        /// <summary>
        /// 设备
        /// </summary>
        Device,

        /// <summary>
        /// 主页工具
        /// </summary>
        HomeTool,
    }

    public class PluginMessageEventArgs : EventArgs {

        /// <summary>
        /// Id
        /// </summary>
        public Guid PluginGuid { get; set; }

        /// <summary>
        /// 插件类型
        /// </summary>
        public PluginType PluginType { get; set; }

        /// <summary>
        /// 插件名称
        /// </summary>
        public string PluginName { get; set; } = string.Empty;

        /// <summary>
        /// 目标类型
        /// </summary>
        public PluginType TargetType { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public object MessageContent { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SentTime { get; set; }

        /// <summary>
        /// 消息描述
        /// </summary>
        public string MessageDescription { get; set; } = string.Empty;

        /// <summary>
        /// 执行类型
        /// </summary>
        public ActionType ExecutionType { get; set; }
    }

    public enum ActionType {

        /// <summary>
        /// 发送
        /// </summary>
        Send,

        /// <summary>
        /// 取消
        /// </summary>
        Cancel,

        /// <summary>
        /// 跳转
        /// </summary>
        Redirect,

        /// <summary>
        /// 上传
        /// </summary>
        Upload,

        /// <summary>
        /// 关闭
        /// </summary>
        Close,

        /// <summary>
        /// 加载
        /// </summary>
        Load,

        /// <summary>
        /// 删除
        /// </summary>
        Delete,

        /// <summary>
        /// 展示
        /// </summary>
        Show
    }

    public enum PluginStatus {
        NotInstalled, // 未安装
        Installed, // 已安装
        Upgradeable, // 可以升级
        Invalid, // 已失效
        BugFound, // 发现Bug
    }
}