using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.VideoApiClient.Api;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.ClientMessageHub;

namespace JayTom.Dws.VideoApiClient.ViewModels.Dialog {

    public class SettingDialogViewModel : BindableBase {
        private readonly IVideoApi _videoApi;
        private readonly IClientMessageHub _clientMessageHub;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _webDomain = string.Empty;
        private int _videoLengthInSeconds;
        private int _secondsToSubtract;
        private string _nvrIpAddress = string.Empty;

        public SettingDialogViewModel(IVideoApi videoApi,
            IClientMessageHub clientMessageHub) {
            _videoApi = videoApi;
            _clientMessageHub = clientMessageHub;
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public string WebDomain {
            get => _webDomain;
            set => SetProperty(ref _webDomain, value);
        }

        public int VideoLengthInSeconds {
            get => _videoLengthInSeconds;
            set => SetProperty(ref _videoLengthInSeconds, value);
        }

        public int SecondsToSubtract {
            get => _secondsToSubtract;
            set => SetProperty(ref _secondsToSubtract, value);
        }

        public string NvrIpAddress {
            get => _nvrIpAddress;
            set => SetProperty(ref _nvrIpAddress, value);
        }

        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private async void SaveDelegate() {
            try {
                string json = File.ReadAllText("appsettings.json");
                // 解析 JSON，将配置文件内容转换为 JObject 对象
                var configObject = JsonConvert.DeserializeObject<JObject>(json);

                // 修改配置项
                if (configObject?["AppSettings"]?["VideoLengthInSeconds"] is not null) {
                    configObject["AppSettings"]["VideoLengthInSeconds"] = VideoLengthInSeconds;
                }
                if (configObject?["AppSettings"]?["WebDomain"] is not null) {
                    configObject["AppSettings"]["WebDomain"] = WebDomain;
                }
                if (configObject?["AppSettings"]?["SecondsToSubtract"] is not null) {
                    configObject["AppSettings"]["SecondsToSubtract"] = SecondsToSubtract;
                }
                if (configObject?["AppSettings"]?["NvrIpAddress"] is not null) {
                    configObject["AppSettings"]["NvrIpAddress"] = SecondsToSubtract;
                }
                // 将修改后的配置项保存回文件
                File.WriteAllText("appsettings.json", JsonConvert.SerializeObject(configObject, Formatting.Indented));
                IsOk = true;
                _videoApi.SetWebDomain(WebDomain);

                if (_clientMessageHub.IsConnected) {
                    await _clientMessageHub.StopAsync();
                    await _clientMessageHub.StartAsync($"http://{WebDomain}/Message");
                }
            }
            catch (Exception e) {
                IsOk = false;
            }

            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        private void CancelDelegate() {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json")
                 .Build();
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                WebDomain = configuration.GetSection("AppSettings:WebDomain").Value ?? string.Empty;
                var videoLengthInSeconds = configuration.GetSection("AppSettings:VideoLengthInSeconds").Value ?? string.Empty;
                int.TryParse(videoLengthInSeconds, out var lengthInSeconds);
                VideoLengthInSeconds = lengthInSeconds;
                var secondsToSubtracts = configuration.GetSection("AppSettings:SecondsToSubtract").Value ?? string.Empty;
                int.TryParse(secondsToSubtracts, out var secondsToSubtract);
                SecondsToSubtract = secondsToSubtract;
                NvrIpAddress = configuration.GetSection("AppSettings:NvrIpAddress").Value ?? string.Empty;
            });
        }
    }
}