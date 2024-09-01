using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TouchSocket.Sockets;
using System.Configuration;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using JayTom.Dws.Domain.Repository.CloudApi;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub;

namespace JayTom.Dws.Infrastructure.SignalR.CloudApi.SignalRMessageHub {

    public class CloudApiMessageHub : Hub, ICloudApiMessageHub {
        private readonly IHubContext<CloudApiMessageHub> _hubContext;
        private readonly ICloudConfigRepository _cloudConfigRepository;

        public CloudApiMessageHub(IHubContext<CloudApiMessageHub> hubContext,
            ICloudConfigRepository cloudConfigRepository) {
            _hubContext = hubContext;
            _cloudConfigRepository = cloudConfigRepository;
        }

        public async void Stop(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Stop", new object?[]
             {
                "Stop"
             });
        }

        public async void Start(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Start", new object?[]
            {
                "Start"
            });
        }

        public async void Exit(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Exit", new object?[]
            {
                "Exit"
            });
        }

        public async void SyncSettingsInfo(string excludedClient, string settingsName, object message) {
            await _hubContext.Clients.AllExcept(excludedClient).SendCoreAsync("SyncSettingsInfo", new object?[]
            {
                new SyncSettingsInfo
                {
                    SettingsName = settingsName,
                    SettingsInfo = message
                }
            });

            await SaveSettings(settingsName, message);
        }

        public async void MessageAll(string messageType, object message) {
            await _hubContext.Clients.All.SendCoreAsync("Message", new object?[]
            {
                messageType,
                message
            });
        }

        public async void MessageToClient(string client, string messageType, object message) {
            await _hubContext.Clients.Clients(client).SendCoreAsync("Message", new object?[]
             {
                messageType,
                message
             });
        }

        public async void MessageToClients(List<string> clients, string messageType, object message) {
            await _hubContext.Clients.Clients(clients).SendCoreAsync("Message", new object?[]
            {
                messageType,
                message
            });
        }

        public async void SendMessageToGroup(string clientGroup, string messageType, object message) {
            await _hubContext.Clients.Group(clientGroup).SendCoreAsync("Message", new object?[]
              {
                messageType,
                message
              });
        }

        private async Task<bool> SaveSettings(string settingsName, object settingsInfo, CancellationToken cancellationToken = default) {
            if (settingsName.Equals("BarcodeFilterSettings", StringComparison.CurrentCultureIgnoreCase)) {
                try {
                    var barcodeFilterSettingsDto = JsonConvert.DeserializeObject<BarcodeFilterSettingsDto>(settingsInfo.ToString() ?? string.Empty);
                    if (barcodeFilterSettingsDto is not null) {
                        return await _cloudConfigRepository.InsertOrUpdate(new ConfigInfoModel() {
                            ConfigName = "BarcodeFilterSettings",
                            Value = settingsInfo.ToString() ?? string.Empty,
                        }, cancellationToken);
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
            return false;
        }
    }
}