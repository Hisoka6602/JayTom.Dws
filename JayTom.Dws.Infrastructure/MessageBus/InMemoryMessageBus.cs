using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace JayTom.Dws.Infrastructure.MessageBus {

    /// <summary>
    /// 内存消息总线实现，支持异步发布和订阅
    /// In-memory message bus implementation with async publish and subscribe support
    /// </summary>
    public class InMemoryMessageBus : IMessageBus {
        
        private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 异步发布消息到所有订阅者
        /// Asynchronously publish message to all subscribers
        /// </summary>
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
            where T : class {
            
            if (message == null) {
                return;
            }

            var messageType = typeof(T);
            
            if (!_handlers.TryGetValue(messageType, out var handlers) || handlers.Count == 0) {
                _logger.Debug($"No handlers registered for message type: {messageType.Name}");
                return;
            }

            var tasks = new List<Task>();
            
            foreach (var handler in handlers.ToList()) {
                if (handler is Func<T, CancellationToken, Task> asyncHandler) {
                    tasks.Add(InvokeHandlerAsync(asyncHandler, message, cancellationToken));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 订阅消息类型
        /// Subscribe to message type
        /// </summary>
        public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler) 
            where T : class {
            
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var messageType = typeof(T);
            
            _handlers.AddOrUpdate(
                messageType,
                _ => new List<object> { handler },
                (_, list) => {
                    lock (list) {
                        list.Add(handler);
                        return list;
                    }
                });

            _logger.Debug($"Handler subscribed for message type: {messageType.Name}");
            
            return new Subscription(() => RemoveHandler(messageType, handler));
        }

        private async Task InvokeHandlerAsync<T>(
            Func<T, CancellationToken, Task> handler, 
            T message, 
            CancellationToken cancellationToken) {
            
            try {
                await handler(message, cancellationToken);
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error handling message of type: {typeof(T).Name}");
            }
        }

        private void RemoveHandler(Type messageType, object handler) {
            if (_handlers.TryGetValue(messageType, out var handlers)) {
                lock (handlers) {
                    handlers.Remove(handler);
                    _logger.Debug($"Handler unsubscribed from message type: {messageType.Name}");
                }
            }
        }

        private class Subscription : IDisposable {
            private readonly Action _dispose;
            private bool _disposed;

            public Subscription(Action dispose) {
                _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
            }

            public void Dispose() {
                if (!_disposed) {
                    _dispose();
                    _disposed = true;
                }
            }
        }
    }
}
