using System;
using JayTom.Dws.Domain.Dto;
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
        //结果
    }
}