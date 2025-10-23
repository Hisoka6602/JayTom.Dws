using System;
using System.Threading;
using System.Threading.Tasks;

namespace JayTom.Dws.Infrastructure.MessageBus {

    /// <summary>
    /// 消息总线接口，用于发布和订阅领域事件
    /// Message bus interface for publishing and subscribing to domain events
    /// </summary>
    public interface IMessageBus {
        
        /// <summary>
        /// 发布异步消息
        /// Publish an async message
        /// </summary>
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
        
        /// <summary>
        /// 订阅消息
        /// Subscribe to messages
        /// </summary>
        IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class;
    }
}
