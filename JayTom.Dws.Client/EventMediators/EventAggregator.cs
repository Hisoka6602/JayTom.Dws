using System;
using Prism.Events;
using System.Diagnostics;
using System.ComponentModel;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;

namespace JayTom.Dws.Client.EventMediators {

    public class EventAggregator1 {
        private static readonly Lazy<EventAggregator> _instance = new(() => new EventAggregator());

        public static EventAggregator Instance => _instance.Value;

        private readonly ConcurrentDictionary<Type, List<Action<object>>> _eventSubscribers = new();
        private readonly IEventAggregator _eventAggregator = new Prism.Events.EventAggregator();
        private readonly IEventAggregator _packageEventAggregator = new Prism.Events.EventAggregator();

        public void Publish<TEventType>(TEventType eventData) {
            var eventType = typeof(TEventType);
            if (_eventSubscribers.TryGetValue(eventType, out var eventSubscriber)) {
                foreach (var subscriber in eventSubscriber) {
                    if (eventData != null) subscriber.Invoke(eventData);
                }
            }
            /*var eventType = typeof(TEventType);
            if (_eventSubscribers.TryGetValue(eventType, out var eventSubscriber)) {
                var stopwatch = Stopwatch.StartNew();

                foreach (var subscriber in eventSubscriber.ToList()) {
                    stopwatch.Restart();
                    await Task.Run(() => {
                        if (eventData != null) subscriber.Invoke(eventData);
                    });
                    stopwatch.Stop();

                    Debug.WriteLine($"Subscriber invoked in {stopwatch.ElapsedMilliseconds} ms");
                }
            }*/
            //_eventAggregator.GetEvent<PubSubEvent<TEventType>>().Publish(eventData);
        }

        public void Subscribe<TEventType>(Action<TEventType> action) {
            var eventType = typeof(TEventType);
            if (!_eventSubscribers.ContainsKey(eventType)) {
                _eventSubscribers[eventType] = new List<Action<object>>();
            }
            _eventSubscribers[eventType].Add(obj => action((TEventType)obj));
        }

        public void PublishPackage<TEventType>(TEventType eventData) {
            _packageEventAggregator.GetEvent<PubSubEvent<TEventType>>().Publish(eventData);
        }

        public void SubscribePackage<TEventType>(Action<TEventType> action) {
            _packageEventAggregator.GetEvent<PubSubEvent<TEventType>>().Subscribe(action, ThreadOption.PublisherThread, false);
        }

        public void Unsubscribe<TEventType>(Action<TEventType> action) {
            _eventAggregator.GetEvent<PubSubEvent<TEventType>>().Unsubscribe(action);
        }
    }

    public class SettingsChangedEvent {

        /// <summary>
        /// 配置名称
        /// </summary>
        public string SettingsName { get; set; } = string.Empty;

        /// <summary>
        /// 是否本地保存
        /// </summary>
        public bool IsLocallySaved { get; set; }
    }

    public class TriggerPositionEvent {

        /// <summary>
        /// 触发位置
        /// </summary>
        public TriggerPositionEnum TriggerPosition { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 包裹信息
        /// </summary>
        public PackageInfo? PackageInfo { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    public class BarcodeTypeProviderEvent {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 需要扣除的重量
        /// </summary>
        public float LengthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的宽度
        /// </summary>
        public float WidthToDeduct { get; set; }

        /// <summary>
        /// /需要扣除的重量
        /// </summary>
        public float WeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的高度
        /// </summary>
        public float HeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的体积
        /// </summary>
        public float VolumeToDeduct { get; set; }
    }

    public class PluginParamChangedEvent {
        public PluginType Type { get; set; }
        public string PluginName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
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

    public class WindowsAction {
        public object? Windows { get; set; }
        public WindowsActionType Type { get; set; }
    }

    /// <summary>
    /// 远程操作
    /// </summary>
    public class RemoteAction {

        /// <summary>
        /// 消息
        /// </summary>
        public object? Message { get; set; }

        /// <summary>
        /// 指令
        /// </summary>
        public RemoteCommand Command { get; set; }
    }

    public enum WindowsActionType {

        /// <summary>
        /// 最小化
        /// </summary>
        Minimize,

        /// <summary>
        /// 最大化
        /// </summary>
        Maximize,

        /// <summary>
        /// 还原
        /// </summary>
        Restore,

        /// <summary>
        /// 显示
        /// </summary>
        Show,

        /// <summary>
        /// 隐藏
        /// </summary>
        Hide,

        /// <summary>
        /// 关闭
        /// </summary>
        Close,

        /// <summary>
        /// 激活
        /// </summary>
        Activate
    }

    public class ApplicationStatusChanged {
        public ApplicationStatus Status { get; set; }
    }

    public enum ApplicationStatus {
        Start,
        Stop
    }

    /// <summary>
    /// 远程指令
    /// </summary>
    public enum RemoteCommand {
        None,

        /// <summary>
        /// 停止
        /// </summary>
        Stop,

        /// <summary>
        /// 启动
        /// </summary>
        Start,

        /// <summary>
        /// 退出
        /// </summary>
        Exit,

        /// <summary>
        /// 重启
        /// </summary>
        Restart
    }

    /// <summary>
    /// 格口更新事件
    /// </summary>
    public class PackageExitUpdateEvent {

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 包裹时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 格口名称
        /// </summary>
        public string ExitName { get; set; } = string.Empty;

        /// <summary>
        /// 格口Id
        /// </summary>

        public long ExitId { get; set; }

        /// <summary>
        /// 格口类型(物理/理论)
        /// </summary>
        public SortingExitType ExitType { get; set; }

        /// <summary>
        /// 包裹异常原因
        /// </summary>
        public PackageAbnormalSortingType PackageAbnormalSortingType { get; set; }

        /// <summary>
        /// 指令信息
        /// </summary>
        public List<InstructionInfoModel>? InstructionInfos { get; set; }

        /// <summary>
        /// 指令类型
        /// </summary>
        public InstructionType InstructionType { get; set; }

        /// <summary>
        /// 格口类型
        /// </summary>
        public ExitType Type { get; set; }
    }

    public enum PackageAbnormalSortingType {

        /// <summary>
        /// 无
        /// </summary>
        [Description("正常分拣")]
        None,

        /// <summary>
        /// 网络超时
        /// </summary>
        [Description("网络超时")]
        NetworkTimeout,

        /// <summary>
        /// Api异常访问
        /// </summary>
        [Description("Api异常访问")]
        ApiAccessError,

        /// <summary>
        /// 无条码
        /// </summary>
        [Description("无条码")]
        NoRead,

        /// <summary>
        /// 多条码识别
        /// </summary>
        [Description("多条码识别")]
        MultipleBarCode,

        /// <summary>
        /// 无分拣指令
        /// </summary>
        [Description("无分拣指令")]
        NoSortingInstruction,

        /// <summary>
        /// 无物理格口
        /// </summary>
        [Description("无物理格口")]
        NoPhysicalMailbox,

        /// <summary>
        /// 锁格
        /// </summary>
        [Description("锁格")]
        LockExit,

        /// <summary>
        /// 叠包
        /// </summary>
        [Description("叠包")]
        StackedPackage,

        /// <summary>
        /// 非本机构条码
        /// </summary>
        [Description("非本机构条码")]
        PostNonLocalBarcode,

        /// <summary>
        /// 查不到段道
        /// </summary>
        [Description("查不到段道")]
        PostSegmentNotFound,

        /// <summary>
        /// 未命中规则
        /// </summary>
        [Description("未命中规则")]
        UnmatchedRule,

        /// <summary>
        /// 距离过近
        /// </summary>
        [Description("距离过近")]
        DistanceTooClose,

        /// <summary>
        /// 车号不匹配
        /// </summary>
        [Description("车号不匹配")]
        VehicleNumberMismatch,

        /// <summary>
        /// 线速度未稳定放包
        /// </summary>
        [Description("线速度未稳定放包")]
        UnstableLineSpeed
    }

    public enum SortingExitType {

        /// <summary>
        /// 物理格口
        /// </summary>
        PhysicalExit,

        /// <summary>
        /// 理论格口
        /// </summary>
        TheoreticalExit
    }

    /// <summary>
    /// 推送包裹
    /// </summary>
    public class PushPackageInfo {

        /// <summary>
        /// 落格信息
        /// </summary>
        public PackageExitUpdateEvent PackageExitUpdateEvent { get; set; } = new();

        /// <summary>
        /// 包裹信息
        /// </summary>
        public PackageInfo PackageInfo { get; set; } = new();

        /// <summary>
        /// 落格信号时间
        /// </summary>
        public DateTime? SignalCallbackTime { get; set; }
    }

    /// <summary>
    /// 推送备用格口分拣
    /// </summary>
    public class PushAlternateExitSorterEvent {

        /// <summary>
        /// 包裹信息
        /// </summary>
        public PackageInfo PackageInfo { get; set; } = new();

        /// <summary>
        /// 原出口Id
        /// </summary>
        public long OriginalExitId { get; set; }

        /// <summary>
        /// 原出口名称
        /// </summary>
        public string OriginalExitName { get; set; } = string.Empty;

        /// <summary>
        /// 锁格时间
        /// </summary>
        public DateTime LockTime { get; set; }
    }
}