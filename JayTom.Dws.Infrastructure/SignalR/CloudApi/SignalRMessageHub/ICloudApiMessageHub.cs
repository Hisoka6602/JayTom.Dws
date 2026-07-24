using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace JayTom.Dws.Infrastructure.SignalR.CloudApi.SignalRMessageHub {

    public interface ICloudApiMessageHub {

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="excludedClients"></param>
        //[HubMethodName("Stop")]
        Task Stop(List<string> excludedClients);

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="excludedClients"></param>
        //[HubMethodName("Start")]
        Task Start(List<string> excludedClients);

        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="excludedClients"></param>
        //[HubMethodName("Exit")]
        Task Exit(List<string> excludedClients);

        /// <summary>
        /// 同步设置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="excludedClient"></param>
        /// <param name="settingsName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SyncSettingsInfo(string excludedClient, string settingsName, object message);

        /// <summary>
        /// 消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        //[HubMethodName("MessageAll")]
        Task MessageAll(string messageType, object message);

        /// <summary>
        /// 消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        //[HubMethodName("MessageToClient")]
        Task MessageToClient(string client, string messageType, object message);

        /// <summary>
        /// 消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clients"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        //[HubMethodName("MessageToClients")]
        Task MessageToClients(List<string> clients, string messageType, object message);

        /// <summary>
        /// 消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientGroup"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        //[HubMethodName("MessageToGroup")]
        Task SendMessageToGroup(string clientGroup, string messageType, object message);
    }

    public class SyncSettingsInfo {

        /// <summary>
        /// 配置名称
        /// </summary>
        public string SettingsName { get; set; } = string.Empty;

        /// <summary>
        /// 配置信息
        /// </summary>
        public object? SettingsInfo { get; set; }
    }
}
