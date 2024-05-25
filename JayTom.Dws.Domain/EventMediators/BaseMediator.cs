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
}