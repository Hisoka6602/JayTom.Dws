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

namespace JayTom.Dws.VideoApiClient.ViewModels.Dialog {

    public class SettingDialogViewModel : BindableBase {
        private readonly IVideoApi _videoApi;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _webDomain = string.Empty;
        private int _videoLengthInSeconds;
        private int _secondsToSubtract;

        public SettingDialogViewModel(IVideoApi videoApi) {
            _videoApi = videoApi;
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

        public ICommand CancelCommand {
            get => new DelegateCommand(CancelDelegate);
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate() {
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
                // 将修改后的配置项保存回文件
                File.WriteAllText("appsettings.json", JsonConvert.SerializeObject(configObject, Formatting.Indented));
                IsOk = true;
                _videoApi.SetWebDomain(WebDomain);
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

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

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
            });
        }
    }
}