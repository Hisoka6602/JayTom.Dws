using System;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.PluginInterface;
using System.Collections.Generic;

namespace JayTom.Dws.Client.EventMediators {

    public class EventAggregator {
        private static readonly Lazy<EventAggregator> _instance = new(() => new EventAggregator());

        public static EventAggregator Instance => _instance.Value;

        private readonly Dictionary<Type, List<Action<object>>> _eventSubscribers = new();

        private EventAggregator() {
        }

        public void Publish<TEventType>(TEventType eventData) {
            var eventType = typeof(TEventType);
            if (_eventSubscribers.TryGetValue(eventType, out var eventSubscriber)) {
                foreach (var subscriber in eventSubscriber) {
                    if (eventData != null) subscriber.Invoke(eventData);
                }
            }
        }

        public void Subscribe<TEventType>(Action<object> action) {
            var eventType = typeof(TEventType);
            if (!_eventSubscribers.ContainsKey(eventType)) {
                _eventSubscribers[eventType] = new List<Action<object>>();
            }
            _eventSubscribers[eventType].Add(action);
        }

        public void Unsubscribe<TEventType>(Action<object> action) {
            var eventType = typeof(TEventType);
            if (_eventSubscribers.TryGetValue(eventType, out var subscriber)) {
                subscriber.Remove(action);
            }
        }
    }

    public class SettingsChangedEvent {
        public string SettingsName { get; set; } = string.Empty;
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

    public class WindowsAction {
        public object? Windows { get; set; }
        public WindowsActionType Type { get; set; }
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
}