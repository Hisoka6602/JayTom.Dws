namespace JayTom.Dws.CrossCutting.SignalR {

    public interface IServiceAggregatorMessageHub : IBaseClientMessageHub {

        /// <summary>
        /// 系统信息推送
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PushSystemInfo(string message, CancellationToken cancellationToken);

        /// <summary>
        /// 系统警告推送
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PushSystemWarning(string message, CancellationToken cancellationToken);
    }
}