using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.SignalRMessageHub;

namespace JayTom.Dws.Client.Service.SyncSettings {

    public class SyncSettingsService : ISyncSettingsService {
        private readonly ICloudApiClientMessageHub _cloudApiClientMessageHub;

        public SyncSettingsService(ICloudApiClientMessageHub cloudApiClientMessageHub) {
            _cloudApiClientMessageHub = cloudApiClientMessageHub;
            _cloudApiClientMessageHub.ReceiveMessage += info => {
                if (info.MethodName.Equals("SyncSettingsInfo")) {
                    try {
                        var syncSettingsInfo =
                            JsonConvert.DeserializeObject<SyncSettingsInfo>(info.MessageData.ToString() ??
                                                                            string.Empty);
                        if (syncSettingsInfo?.SettingsInfo != null) {
                            OnSyncContentReceived(syncSettingsInfo);
                        }
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                }
                return Task.CompletedTask;
            };
        }

        public bool IsConnected => _cloudApiClientMessageHub.IsConnected;

        public async Task<KeyValuePair<bool, string>> Connect(string url) {
            await _cloudApiClientMessageHub.StartAsync(url);
            return new KeyValuePair<bool, string>(_cloudApiClientMessageHub.IsConnected,
                $"连接{(_cloudApiClientMessageHub.IsConnected ? "成功" : "失败")}");
        }

        public async Task<KeyValuePair<bool, string>> SubmitSyncContent<T>(string settingsName, T message) {
            var syncSettingsInfo = await _cloudApiClientMessageHub.SyncSettingsInfo(settingsName, message);
            return new KeyValuePair<bool, string>(syncSettingsInfo, $"提交{(syncSettingsInfo ? "成功" : "失败")}");
        }

        public event EventHandler<SyncSettingsInfo>? SyncContentReceived;

        public Task Disconnect() {
            return _cloudApiClientMessageHub.StopAsync();
        }

        protected virtual void OnSyncContentReceived(SyncSettingsInfo e) {
            SyncContentReceived?.Invoke(this, e);
        }
    }
}
