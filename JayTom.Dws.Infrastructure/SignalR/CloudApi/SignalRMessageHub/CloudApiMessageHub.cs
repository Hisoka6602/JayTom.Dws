using System;
using System.Linq;
using System.Text;
using TouchSocket.Sockets;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub;

namespace JayTom.Dws.Infrastructure.SignalR.CloudApi.SignalRMessageHub {

    public class CloudApiMessageHub : Hub, ICloudApiMessageHub {
        private readonly IHubContext<CloudApiMessageHub> _hubContext;

        public CloudApiMessageHub(IHubContext<CloudApiMessageHub> hubContext) {
            _hubContext = hubContext;
        }

        public async Task Stop(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Stop",
             [
                "Stop"
             ]);
        }

        public async Task Start(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Start",
            [
                "Start"
            ]);
        }

        public async Task Exit(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Exit",
            [
                "Exit"
            ]);
        }

        public async Task SyncSettingsInfo(string excludedClient, string settingsName, object message) {
            await _hubContext.Clients.AllExcept(excludedClient).SendCoreAsync("SyncSettingsInfo",
            [
                new SyncSettingsInfo
                {
                    SettingsName = settingsName,
                    SettingsInfo = message
                }
            ]);
        }

        public async Task MessageAll(string messageType, object message) {
            await _hubContext.Clients.All.SendCoreAsync("Message",
            [
                messageType,
                message
            ]);
        }

        public async Task MessageToClient(string client, string messageType, object message) {
            await _hubContext.Clients.Clients(client).SendCoreAsync("Message",
             [
                messageType,
                message
             ]);
        }

        public async Task MessageToClients(List<string> clients, string messageType, object message) {
            await _hubContext.Clients.Clients(clients).SendCoreAsync("Message",
            [
                messageType,
                message
            ]);
        }

        public async Task SendMessageToGroup(string clientGroup, string messageType, object message) {
            await _hubContext.Clients.Group(clientGroup).SendCoreAsync("Message",
              [
                messageType,
                message
              ]);
        }
    }
}
