using System;
using MediatR;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.EventMediators {

    public abstract class BaseMediator : INotificationHandler<GenericMessage> {
        private readonly IMediator _mediator;

        protected BaseMediator(IMediator mediator) {
            _mediator = mediator;
        }

        public abstract Task Handle(GenericMessage request, CancellationToken cancellationToken = default);

        public async Task PublishMessage(GenericMessage message, CancellationToken cancellationToken = default) {
            await _mediator.Publish(message, cancellationToken);
        }

        public string GetDescription(Enum value) {
            try {
                var field = value.GetType().GetField(value.ToString());
                if (field is not null) {
                    var attribute =
                        (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

                    return attribute == null ? value.ToString() : attribute.Description;
                }
            }
            catch (Exception e) {
                // ignored
            }

            return string.Empty;
        }
    }

    public class GenericMessage : INotification {

        [Description("内容")]
        public object? Content { get; set; }

        [Description("类型")]
        public GenericMessageType Type { get; set; }
    }

    public enum GenericMessageType {

        /// <summary>
        /// 包裹消息
        /// </summary>
        [Description("包裹消息")]
        Packaging,

        /// <summary>
        /// 通讯消息
        /// </summary>
        [Description("通讯消息")]
        Communication,

        /// <summary>
        /// 指令消息
        /// </summary>
        [Description("指令消息")]
        Command,

        /// <summary>
        /// Api消息
        /// </summary>
        [Description("Api消息")]
        Api,

        /// <summary>
        /// 系统消息
        /// </summary>
        [Description("系统消息")]
        System,

        /// <summary>
        /// 操作消息
        /// </summary>
        [Description("操作消息")]
        Operation,

        /// <summary>
        /// 远程消息
        /// </summary>
        [Description("远程消息")]
        Remote,

        /// <summary>
        /// 设置消息
        /// </summary>
        [Description("设置消息")]
        Setting,

        /// <summary>
        /// 设备消息
        /// </summary>
        [Description("设备消息")]
        Device,

        /// <summary>
        /// 插件消息
        /// </summary>
        [Description("插件消息")]
        Plugin,

        /// <summary>
        /// 数据消息
        /// </summary>
        [Description("数据消息")]
        Data
    }

    /// <summary>
    /// 表示不同操作模式的枚举。
    /// </summary>
    public enum RunMode {

        /// <summary>
        /// 标准模式/自定义模式
        /// </summary>
        [Description("标准模式/自定义模式")]
        StandardMode,

        /// <summary>
        /// 快手模式。
        /// </summary>
        [Description("快手模式")]
        QuickMode,

        /// <summary>
        /// 供包台模式。
        /// </summary>
        [Description("供包台模式")]
        SupplyMode,

        /// <summary>
        /// 窄带模式。
        /// </summary>
        [Description("窄带模式")]
        NarrowbandMode,

        /// <summary>
        /// 环线模式。
        /// </summary>
        [Description("环线模式")]
        LoopMode,

        /// <summary>
        /// 转向机模式。
        /// </summary>
        [Description("转向机模式")]
        SteeringMode,

        /// <summary>
        /// 视频追溯模式。
        /// </summary>
        [Description("视频追溯模式")]
        VideoTraceMode
    }

    /// <summary>
    /// 系统消息
    /// </summary>
    public class SystemMessageInfo {
        public SystemMessageType Type { get; set; }

        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime TriggerTime { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 信息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 参数
        /// </summary>
        public object? Parameters { get; set; } = string.Empty;
    }

    /// <summary>
    /// 系统消息类型枚举
    /// </summary>
    public enum SystemMessageType {

        /// <summary>
        /// 启动
        /// </summary>
        [Description("启动")]
        Start,

        /// <summary>
        /// 停止
        /// </summary>
        [Description("停止")]
        Stop,

        /// <summary>
        /// 最大化
        /// </summary>
        [Description("最大化")]
        Maximize,

        /// <summary>
        /// 最小化
        /// </summary>
        [Description("最小化")]
        Minimize,

        /// <summary>
        /// 加载程序
        /// </summary>
        [Description("加载程序")]
        LoadProgram,

        /// <summary>
        /// 退出程序
        /// </summary>
        [Description("退出程序")]
        ExitProgram,

        /// <summary>
        /// 点击关闭
        /// </summary>
        [Description("点击关闭")]
        CloseClicked,

        /// <summary>
        /// 授权成功
        /// </summary>
        [Description("授权成功")]
        AuthorizationSuccess,

        /// <summary>
        /// 授权失败
        /// </summary>
        [Description("授权失败")]
        AuthorizationFailure,

        /// <summary>
        /// 点击菜单
        /// </summary>
        [Description("点击菜单")]
        MenuClicked,

        /// <summary>
        /// 点击返回
        /// </summary>
        [Description("点击返回")]
        BackClicked,

        /// <summary>
        /// 进程退出
        /// </summary>
        [Description("进程退出")]
        ProcessExit,

        /// <summary>
        /// 多开限制触发
        /// </summary>
        [Description("多开限制触发")]
        MultiOpenLimitTriggered,

        /// <summary>
        /// 超时监控
        /// </summary>
        [Description("超时监控")]
        TimeoutMonitoring,

        /// <summary>
        /// 无响应触发
        /// </summary>
        [Description("无响应触发")]
        UnresponsiveTriggered,

        /// <summary>
        /// Cpu温度过高
        /// </summary>
        [Description("Cpu温度过高")]
        CpuTemperatureHigh,

        /// <summary>
        /// Cpu占用过高
        /// </summary>
        [Description("Cpu占用过高")]
        CpuUsageHigh,

        /// <summary>
        /// 内存占用过高
        /// </summary>
        [Description("内存占用过高")]
        MemoryUsageHigh,

        /// <summary>
        /// 磁盘占用过高
        /// </summary>
        [Description("磁盘占用过高")]
        DiskUsageHigh,

        /// <summary>
        /// 获取电脑设备信息超时
        /// </summary>
        [Description("获取电脑设备信息超时")]
        DeviceInfoTimeout,

        /// <summary>
        /// 网络连接断开
        /// </summary>
        [Description("网络连接断开")]
        NetworkDisconnected
    }

    /// <summary>
    /// 配置消息
    /// </summary>
    public class SettingMessageInfo {

        /// <summary>
        /// 配置名称
        /// </summary>
        public string SettingsName { get; set; } = string.Empty;

        /// <summary>
        /// 是否本地保存
        /// </summary>
        public bool IsLocallySaved { get; set; }
    }
}