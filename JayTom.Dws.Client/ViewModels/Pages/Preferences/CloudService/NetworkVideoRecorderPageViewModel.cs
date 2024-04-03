using System;
using DryIoc;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using JayTom.Dws.Nvr;
using System.Windows;
using FFmpeg.AutoGen;
using Vlc.DotNet.Wpf;
using Newtonsoft.Json;
using System.Security;
using Vlc.DotNet.Core;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Nvr.Nvr;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Security.Policy;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Client.EventMediators;
using System.Windows.Forms.Integration;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.CloudSettingModel;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.ViewModels.Editors.CloudService;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService {

    public class NetworkVideoRecorderPageViewModel : SettingsPageTemplateViewModel {
        private readonly INvrManager _nvrManager;
        private NvrClientSettingsModel _vnrClientSettingsInfo = new();
        private ObservableCollection<int> _channelItems = new();
        private bool _isLogInProgress;
        private bool _isLoaded;
        private int _selectChannel;
        private MemoryStream _videoMemoryStream = new();
        private VlcControl? _vlcPlay;
        private bool _isPlaying;
        private SemaphoreSlim _playSlim = new(1);

        public NetworkVideoRecorderPageViewModel(IConfigRepository configRepository,
            INvrManager nvrManager) : base(configRepository) {
            _nvrManager = nvrManager;
            _nvrManager.RealTimePreviewCallback += async delegate (object? sender, RealTimePreviewEventArgs args) {
                /*if (args.Data is not null) {
                    args.Data.Position = 0;
                    args.Data?.CopyTo(VideoMemoryStream);
                }*/

                /*if (_vlcPlay?.SourceProvider.MediaPlayer.IsPlaying() != true) {
                    _vlcPlay?.SourceProvider.MediaPlayer.Play();
                }
                try {
                    await _playSlim.WaitAsync();
                    await Task.Delay(500);
                    if (_vlcPlay?.SourceProvider.MediaPlayer.IsPlaying() != true) {
                        _vlcPlay?.SourceProvider.MediaPlayer.Play();
                    }

                    /*if (!_isPlaying) {
                        await Task.Delay(2000);

                        _isPlaying = true;
                    }#1#
                }
                catch (Exception ex) {
                    Console.WriteLine($"播放视频时出现错误: {ex.Message}");
                }
                finally {
                    _playSlim.Release();
                }*/
            };
        }

        public MemoryStream VideoMemoryStream {
            get => _videoMemoryStream;
            set => SetProperty(ref _videoMemoryStream, value);
        }

        public NvrClientSettingsModel NvrClientSettingsInfo {
            get => _vnrClientSettingsInfo;
            set => SetProperty(ref _vnrClientSettingsInfo, value);
        }

        public ObservableCollection<int> ChannelItems {
            get => _channelItems;
            set => SetProperty(ref _channelItems, value);
        }

        public int SelectChannel {
            get => _selectChannel;
            set => SetProperty(ref _selectChannel, value);
        }

        /// <summary>
        /// 是否播放中
        /// </summary>
        public bool IsPlaying {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        /// <summary>
        /// 是否登录中
        /// </summary>
        public bool IsLogInProgress {
            get => _isLogInProgress;
            set => SetProperty(ref _isLogInProgress, value);
        }

        public override string Identifier => "NetworkVideoRecorderSettingsDialogHost";
        public override string SettingsName => "NetworkVideoRecorderSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new NvrClientSettingsDto() {
                    Ip = NvrClientSettingsInfo.Ip,
                    Port = NvrClientSettingsInfo.Port,
                    IsUseBarcodeWatermark = NvrClientSettingsInfo.IsUseBarcodeWatermark,
                    MaxWatermarkTime = NvrClientSettingsInfo.MaxWatermarkTime,
                    Password = NvrClientSettingsInfo.Password,
                    Username = NvrClientSettingsInfo.Username
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;

                try {
                    if (obj is Page page) {
                        var visualChild = PluginInterface.Utils.Utils.GetVisualChild<VlcControl>(page, f => f.Name.Equals("VlcPlayer"));
                        if (visualChild != null) {
                            _vlcPlay = visualChild;
                            _vlcPlay.SourceProvider.CreatePlayer(new DirectoryInfo($"{System.AppDomain.CurrentDomain.BaseDirectory}VideoLAN\\VLC"));
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    base.MessageQueue.Enqueue($"{e.Message}");
                }

                var vnrClientSettingsDto = await _configRepository.FirstOrDefaultEntity<NvrClientSettingsDto>(SettingsName);
                if (vnrClientSettingsDto is not null) {
                    NvrClientSettingsInfo = new NvrClientSettingsModel() {
                        Ip = vnrClientSettingsDto.Ip,
                        Port = vnrClientSettingsDto.Port,
                        IsUseBarcodeWatermark = vnrClientSettingsDto.IsUseBarcodeWatermark,
                        MaxWatermarkTime = vnrClientSettingsDto.MaxWatermarkTime,
                        Password = vnrClientSettingsDto.Password,
                        Username = vnrClientSettingsDto.Username
                    };
                }
                else {
                    base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}");
                }
            }
        }

        /// <summary>
        /// 通道选择
        /// </summary>
        public ICommand ChannelSelectionChangedCommand {
            get => new DelegateCommand<SelectionChangedEventArgs>(ChannelSelectionChangedDelegate);
        }

        private async void ChannelSelectionChangedDelegate(SelectionChangedEventArgs obj) {
            try {
                await _playSlim.WaitAsync();
                if (IsPlaying) {
                    _vlcPlay?.SourceProvider?.MediaPlayer?.Pause();
                    IsPlaying = false;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _playSlim.Release();
            }
        }

        public ICommand PlayCommand {
            get => new DelegateCommand<SelectionChangedEventArgs>(PlayDelegate);
        }

        private async void PlayDelegate(SelectionChangedEventArgs obj) {
            //播放
            try {
                await _playSlim.WaitAsync();
                if (IsPlaying) {
                    _vlcPlay?.SourceProvider?.MediaPlayer?.Pause();
                    IsPlaying = false;
                }
                else {
                    if (_vlcPlay is not null && ChannelItems?.Any() == true) {
                        // _vlcPlay.SourceProvider.MediaPlayer.SetMedia(new Uri("rtsp://admin:a12345678@192.168.31.166:554/cam/realmonitor?channel=4&subtype=0"));
                        _vlcPlay.SourceProvider.MediaPlayer.SetMedia(new Uri($"rtsp://{NvrClientSettingsInfo.Username}:{NvrClientSettingsInfo.Password}@{NvrClientSettingsInfo.Ip}:554/cam/realmonitor?channel={SelectChannel}&subtype=0"));
                        _vlcPlay.SourceProvider.MediaPlayer.Play();
                        IsPlaying = true;
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _playSlim.Release();
            }
        }

        /// <summary>
        /// 登录事件
        /// </summary>

        public ICommand LogInCommand {
            get => new DelegateCommand<object>(LogInDelegate);
        }

        private void LogInDelegate(object obj) {
            if (!IsLogInProgress) {
                IsLogInProgress = true;
                Task.Run(async () => {
                    var (key, value) = await _nvrManager.Login(NvrClientSettingsInfo.Ip,
                        NvrClientSettingsInfo.Port,
                        NvrClientSettingsInfo.Username,
                        NvrClientSettingsInfo.Password);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                        ChannelItems.Clear();

                        if (key) {
                            var (b, ints) = await _nvrManager.EnumerateChannels();
                            if (b) {
                                ChannelItems.AddRange(ints);
                                base.MessageQueue.Enqueue("枚举通道成功!");
                            }
                            else {
                                base.MessageQueue.Enqueue("枚举通道失败!");
                            }
                        }
                        else {
                            base.MessageQueue.Enqueue(value);
                        }

                        IsLogInProgress = false;
                    }, DispatcherPriority.Background);
                });
            }
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        public ICommand BindingCommand {
            get => new DelegateCommand<object>(BindingDelegate);
        }

        private async void BindingDelegate(object obj) {
            var nvrCameraBindingEditor = new NvrCameraBindingEditor();
            if (nvrCameraBindingEditor.DataContext is NvrCameraBindingEditorViewModel model) {
                model.Identifier = "NetworkVideoRecorderDialog";
                model.Channel = SelectChannel;
                model.IpAddress = NvrClientSettingsInfo.Ip;
                model.Port = NvrClientSettingsInfo.Port;
                model.Username = NvrClientSettingsInfo.Username;
                model.Password = NvrClientSettingsInfo.Password;
                await DialogHost.Show(nvrCameraBindingEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message)) {
                    base.MessageQueue.Enqueue(model.Message);
                }
            }

            //弹出绑定窗口
            //获取已绑定相机
            //如果没有则枚举一次
        }
    }
}