using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.SignalRMessageHub;

namespace JayTom.Dws.Client.Service.SyncSettings {

    public interface ISyncSettingsService {

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Connect(string url);

        /// <summary>
        /// 提交同步的内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settingsName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SubmitSyncContent<T>(string settingsName, T message);

        /// <summary>
        /// 接收到同步内容事件
        /// </summary>
        event EventHandler<SyncSettingsInfo> SyncContentReceived;

        /// <summary>
        /// 断开
        /// </summary>
        /// <returns>断开任务。</returns>
        Task Disconnect();
    }
}
