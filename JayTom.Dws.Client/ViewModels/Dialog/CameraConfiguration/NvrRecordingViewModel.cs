using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Application.CameraConfigurations;
using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Plugin.Speech;
using System.Windows.Threading;
using NPOI.SS.Formula.Functions;
using JayTom.Dws.Models.LocalConf;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Legacy.Contracts.Dto.LocalVideoDto;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.VideoSettingModel;
using JayTom.Dws.Client.Attributes.WinClientAttributes;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;
using static JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR.DaHuatechNVR;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration
{

    public class NvrRecordingViewModel : BindableBase
    {
        private readonly ICameraConfigurationCatalog<NvrCameraBindingInfoModel> _nvrCameraBindingRepository;
        private readonly IPackageHistoryQueryService _packageHistory;
        private readonly ISettingsStore _settingsStore;
        private ObservableCollection<VideoPlayerModel> _videoPlayerItems = new();

        //private BaseDaHuatech? _baseDaHuatech;
        private DaHuatechNVR? _daHuatechNvr;

        private string _identifier = string.Empty;
        private DateTime _startTime = DateTime.Now.AddMinutes(-10);
        private DateTime _endTime = DateTime.Now.AddMinutes(-5);
        private DateTime _currentTime;
        private DateTime? _selectionStartTime;
        private DateTime? _selectionEndTime;
        private ObservableCollection<PlaybackStream> _playbackStreamItems = new(Enum.GetValues(typeof(PlaybackStream)).Cast<PlaybackStream>());
        private PlaybackStream _selectPlaybackStream = PlaybackStream.MainStream;
        private string? _fastForwardSpeed;
        private string? _slowMotionSpeed;
        private PlaybackState _playbackState = PlaybackState.Ready;
        private DateTime _playDateTime;
        private int _loadedSize = 0;

        private readonly DaHuatechNVR.FastForwardSpeed[] _fastForwardSpeeds =
            [ DaHuatechNVR.FastForwardSpeed.X2,
                DaHuatechNVR.FastForwardSpeed.X4,
                DaHuatechNVR.FastForwardSpeed.X8,
                DaHuatechNVR.FastForwardSpeed.X16,
                DaHuatechNVR.FastForwardSpeed.Normal
            ];

        private int _currentSpeedIndex = 0;

        // 定义慢放速度数组
        private readonly DaHuatechNVR.SlowSpeed[] _slowMotionSpeeds = [
            DaHuatechNVR.SlowSpeed.X2,
            DaHuatechNVR.SlowSpeed.X4,
            DaHuatechNVR.SlowSpeed.X8,
            DaHuatechNVR.SlowSpeed.X16,
            DaHuatechNVR.SlowSpeed.Normal,
        ];

        // 当前慢放速度索引
        private int _currentSlowMotionSpeedIndex = 0;

        private decimal _speed = 1;
        private PackageItemModel _packageItemModel = new();
        private VideoPlaybackSettingsInfoModel _videoPlaybackSettingsInfo = new();

        public NvrRecordingViewModel(
            ICameraConfigurationCatalog<NvrCameraBindingInfoModel> nvrCameraBindingRepository,
            IPackageHistoryQueryService packageHistory,
            ISettingsStore settingsStore)
        {
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _packageHistory = packageHistory;
            _settingsStore = settingsStore;
        }

        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public VideoPlaybackSettingsInfoModel VideoPlaybackSettingsInfo
        {
            get => _videoPlaybackSettingsInfo;
            set => SetProperty(ref _videoPlaybackSettingsInfo, value);
        }

        public PackageItemModel PackageItemModel
        {
            get => _packageItemModel;
            set => SetProperty(ref _packageItemModel, value);
        }

        public bool ProgressRelease { get; private set; }

        public ObservableCollection<VideoPlayerModel> VideoPlayerItems
        {
            get => _videoPlayerItems;
            set => SetProperty(ref _videoPlayerItems, value);
        }

        public ObservableCollection<PlaybackStream> PlaybackStreamItems
        {
            get => _playbackStreamItems;
            set => SetProperty(ref _playbackStreamItems, value);
        }

        public PlaybackStream SelectPlaybackStream
        {
            get => _selectPlaybackStream;
            set => SetProperty(ref _selectPlaybackStream, value);
        }

        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public DateTime CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public DateTime PlayDateTime
        {
            get => _playDateTime;
            set => SetProperty(ref _playDateTime, value);
        }

        public DateTime? SelectionStartTime
        {
            get => _selectionStartTime;
            set => SetProperty(ref _selectionStartTime, value);
        }

        public DateTime? SelectionEndTime
        {
            get => _selectionEndTime;
            set => SetProperty(ref _selectionEndTime, value);
        }

        /// <summary>
        /// 快进倍数
        /// </summary>
        public string? FastForwardSpeed
        {
            get => _fastForwardSpeed;
            set => SetProperty(ref _fastForwardSpeed, value);
        }

        /// <summary>
        /// 慢放倍数
        /// </summary>
        public string? SlowMotionSpeed
        {
            get => _slowMotionSpeed;
            set => SetProperty(ref _slowMotionSpeed, value);
        }

        public decimal Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public PlaybackState PlaybackState
        {
            get => _playbackState;
            set => SetProperty(ref _playbackState, value);
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<VideoPlayerModel>(ToggleImageSizeDelegate);

        private async void ToggleImageSizeDelegate(VideoPlayerModel obj)
        {
            if (obj.ScreenState == ScreenState.Normal)
            {
                foreach (var videoPlayerModel in VideoPlayerItems)
                {
                    videoPlayerModel.ScreenState = !videoPlayerModel.Equals(obj) ? ScreenState.Hidden : ScreenState.Maximized;
                }
                if (_daHuatechNvr is not null)
                {
                    obj.VideoFrame = new WriteableBitmap((int)obj.MaxSize.Width,
                        (int)obj.MaxSize.Height, 96, 96, PixelFormats.Bgr24, null);
                    await _daHuatechNvr.SetResolution(obj.IpAddress, obj.Channel, (int)obj.MaxSize.Width,
                        (int)obj.MaxSize.Height);
                }
            }
            else
            {
                var size = GetVideoPlayerSize();

                foreach (var videoPlayerModel in VideoPlayerItems)
                {
                    videoPlayerModel.ScreenState = ScreenState.Normal;
                    if (_daHuatechNvr is not null)
                    {
                        obj.VideoFrame = new WriteableBitmap((int)size.Width,
                            (int)size.Height, 96, 96, PixelFormats.Bgr24, null);
                        await _daHuatechNvr.SetResolution(obj.IpAddress, obj.Channel, (int)size.Width,
                            (int)size.Height);
                    }
                }
            }
        }

        private Size GetVideoPlayerSize()
        {
            var size = new Size(768, 432);
            switch (VideoPlayerItems.Count)
            {
                case 1:
                    size = new Size(768, 432);
                    break;

                case > 1 and <= 4:
                    size = new Size(614, 346);
                    break;

                case > 4:
                    size = new Size(449, 253);
                    break;
            }
            return size;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj)
        {
            if (PackageItemModel.TimestampMilliseconds <= 0)
            {
                return;
            }

            var settingsDto = await _settingsStore.GetAsync<VideoPlaybackSettingsDto>("VideoPlaybackSettings") ?? new VideoPlaybackSettingsDto();
            VideoPlaybackSettingsInfo = new VideoPlaybackSettingsInfoModel()
            {
                IsWatermarkTimeMarked = settingsDto.IsWatermarkTimeMarked,
                SecondsToSubtract = settingsDto.SecondsToSubtract,
                VideoLengthInSeconds = settingsDto.VideoLengthInSeconds,
            };

            var packageInfoModel = await _packageHistory.FindByTimestampAsync(PackageItemModel.TimestampMilliseconds);

            var serialNumber = packageInfoModel?.BarCodeInfo?.SerialNumber;
            if (serialNumber is null)
            {
                return;
            }

            StartTime = packageInfoModel?.BarCodeInfo?.ScanTime.AddSeconds(0 - VideoPlaybackSettingsInfo.SecondsToSubtract) ?? DateTime.Now.AddSeconds(-60);
            EndTime = StartTime.AddSeconds(VideoPlaybackSettingsInfo.VideoLengthInSeconds);
            CurrentTime = StartTime;
            if (VideoPlaybackSettingsInfo.IsWatermarkTimeMarked)
            {
                SelectionStartTime = packageInfoModel?.BarCodeInfo?.ScanTime;
                SelectionEndTime = SelectionStartTime?.AddSeconds(10);
            }
            //获取Nvr
            var nvrBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
            var nvrCameraBindingInfoModels = nvrBindingInfoModels.Where(f => f.SerialNumber.Equals(serialNumber)).ToList();

            var videoPlayerModels = nvrCameraBindingInfoModels?.Select(s =>
                new VideoPlayerModel
                {
                    Channel = s.Channel,
                    IpAddress = s.IpAddress,
                    Password = s.Password,
                    Port = s.Port,
                    Username = s.Username,
                    VideoFrame = new(449, 253, 96, 96, PixelFormats.Bgr24, null),
                    VideoScreenShotCommand = CaptureScreenShotCommand,
                    DownloadCommand = DownloadVideoCommand,
                    ToggleImageSizeCommand = ToggleImageSizeCommand,
                })?.ToList();

            VideoPlayerItems.AddRange(videoPlayerModels);
            if (!VideoPlayerItems.Any())
            {
                return;
            }
            _daHuatechNvr ??= DaHuatechNVR.Instance;
            await BaseDaHuatech.InitializeDeviceDiscoveryAsync();
            //登录
            var firstPlayer = VideoPlayerItems[0];
            var any = _daHuatechNvr.GetDevLogInInfo(f =>
                string.Equals(f.IpAddress, firstPlayer.IpAddress, StringComparison.Ordinal))?.Any();

            if (any != true)
            {
                //登录
                var (key, value) = await _daHuatechNvr.LogIn(firstPlayer.IpAddress, firstPlayer.Port,
                    firstPlayer.Username, firstPlayer.Password);
                if (key)
                {
                    await PlaybackAsync(obj);
                }
            }
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private async void CloseDialogDelegate(object obj)
        {
            try
            {
                // 先等待所有回放关闭，再注销 NVR，避免注销与关闭请求互相抢占。
                if (_daHuatechNvr is not null)
                {
                    var players = VideoPlayerItems.ToList();
                    await Task.WhenAll(players.Select(async item =>
                    {
                        try
                        {
                            await _daHuatechNvr.ClosePlayBackVideo(item.IpAddress, item.Channel);
                        }
                        finally
                        {
                            item.Dispose();
                        }
                    }));

                    if (players.Count > 0)
                    {
                        _daHuatechNvr.LogOut(players[0].IpAddress)
                            .Forget("退出录像机连接");
                    }
                }
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "关闭NVR回放失败");
            }
            finally
            {
                if (DialogHost.IsDialogOpen(Identifier))
                {
                    DialogHost.Close(Identifier);
                }
            }
        }

        public ICommand PlaybackCommand => new DelegateCommand<object>(PlaybackDelegate);

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="obj"></param>
        private async void PlaybackDelegate(object? obj)
        {
            await PlaybackAsync(obj);
        }

        private async Task PlaybackAsync(object? obj)
        {
            if (_daHuatechNvr is not null)
            {
                _loadedSize = 0;
                Speed = 1;
                _currentSpeedIndex = _currentSlowMotionSpeedIndex = 0;
                FastForwardSpeed = SlowMotionSpeed = null;
                if (PlaybackState == PlaybackState.Ready)
                {
                    PlaybackState = PlaybackState.Playing;
                    PlayDateTime = CurrentTime;
                    await Task.WhenAll(VideoPlayerItems.Select(async item =>
                    {
                        item.IsBuffering = true;
                        await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                        {
                            var (key, value) = await _daHuatechNvr.QueryVideoFile(item.IpAddress,
                                item.Channel, CurrentTime, EndTime, (int)SelectPlaybackStream);
                            if (key)
                            {
                                //临时显示

                                var (b, o) = await _daHuatechNvr.PlayBackVideo(item.IpAddress, item.Channel,
                                    CurrentTime,
                                    EndTime, item.RealtimePreviewCallback
                                    , async info =>
                                    {
                                        if (info.LoadSize > _loadedSize)
                                        {
                                            await UiThread.Dispatcher.InvokeAsync(() =>
                                            {
                                                _loadedSize = Math.Max(_loadedSize, info.LoadSize);
                                                if (item.IsBuffering)
                                                {
                                                    item.IsBuffering = false;
                                                }

                                                var addSeconds = PlayDateTime.AddSeconds(Convert.ToDouble(info.LoadSize * Speed));
                                                if (CurrentTime.CompareTo(addSeconds) < 0 && !ProgressRelease)
                                                {
                                                    CurrentTime = addSeconds;
                                                }
                                            }, DispatcherPriority.Background);
                                        }
                                    });
                                if (b)
                                {
                                    if (item.ScreenState == ScreenState.Maximized)
                                    {
                                        item.VideoFrame = new WriteableBitmap((int)item.MaxSize.Width,
                                            (int)item.MaxSize.Height, 96, 96, PixelFormats.Bgr24, null);
                                        await _daHuatechNvr.SetResolution(item.IpAddress, item.Channel, (int)item.MaxSize.Width,
                                            (int)item.MaxSize.Height);
                                    }
                                    else
                                    {
                                        var size = GetVideoPlayerSize();
                                        item.VideoFrame = new WriteableBitmap((int)size.Width,
                                            (int)size.Height, 96, 96, PixelFormats.Bgr24, null);
                                        await _daHuatechNvr.SetResolution(item.IpAddress, item.Channel, (int)size.Width,
                                            (int)size.Height);
                                    }
                                    item.IsBuffering = false;
                                }

                                item.PlaybackError = PlaybackError.None;
                            }
                            else
                            {
                                item.VideoFrame = null;
                                if (value is string msg && msg.Contains("录像文件"))
                                {
                                    item.PlaybackError = PlaybackError.VideoFileNotFound;
                                }
                                else
                                {
                                    item.PlaybackError = PlaybackError.UnknownError;
                                }
                                item.IsBuffering = false;
                                PlaybackState = PlaybackState.Ready;
                            }
                        });
                    }));
                }
                else if (PlaybackState == PlaybackState.Paused)
                {
                    await Task.WhenAll(VideoPlayerItems.Select(item =>
                        _daHuatechNvr.ResumePlayback(item.IpAddress, item.Channel)));
                    PlaybackState = PlaybackState.Playing;
                }
                else
                {
                    await Task.WhenAll(VideoPlayerItems.Select(item =>
                        _daHuatechNvr.PausePlayback(item.IpAddress, item.Channel)));
                    PlaybackState = PlaybackState.Paused;
                }
            }
        }

        public ICommand FastForwardCommand => new DelegateCommand<object>(FastForwardDelegate);

        /// <summary>
        /// 快进
        /// </summary>
        /// <param name="obj"></param>
        private async void FastForwardDelegate(object obj)
        {
            if (_daHuatechNvr is not null)
            {
                if (PlaybackState is PlaybackState.Ready or PlaybackState.Paused)
                {
                    await PlaybackAsync(obj);
                }
                // 获取当前的快进倍速
                var currentSpeed = _fastForwardSpeeds[_currentSpeedIndex];
                Speed = currentSpeed switch
                {
                    DaHuatechNVR.FastForwardSpeed.X2 => 1,
                    DaHuatechNVR.FastForwardSpeed.X4 => 1,
                    DaHuatechNVR.FastForwardSpeed.X8 => 2.2m,
                    DaHuatechNVR.FastForwardSpeed.X16 => 5,
                    DaHuatechNVR.FastForwardSpeed.Normal => 1,
                    _ => 1
                };
                await Task.WhenAll(VideoPlayerItems.Select(item =>
                    _daHuatechNvr.FastForward(item.IpAddress, item.Channel, currentSpeed)));
                PlaybackState = PlaybackState.FastForwarding;
                // 更新索引，指向下一个倍速
                FastForwardSpeed = currentSpeed != DaHuatechNVR.FastForwardSpeed.Normal ? currentSpeed.ToString() : null;
                if (FastForwardSpeed == null)
                {
                    PlaybackState = PlaybackState.Playing;
                }
                _currentSpeedIndex = (_currentSpeedIndex + 1) % _fastForwardSpeeds.Length;
            }
        }

        public ICommand SlowMotionCommand => new DelegateCommand<object>(SlowMotionDelegate);

        /// <summary>
        /// 慢放
        /// </summary>
        /// <param name="obj"></param>
        private async void SlowMotionDelegate(object obj)
        {
            if (_daHuatechNvr is not null)
            {
                if (PlaybackState is PlaybackState.Ready or PlaybackState.Paused)
                {
                    await PlaybackAsync(obj);
                }
                var currentSlowSpeed = _slowMotionSpeeds[_currentSlowMotionSpeedIndex];
                await Task.WhenAll(VideoPlayerItems.Select(item =>
                    _daHuatechNvr.Slow(item.IpAddress, item.Channel, currentSlowSpeed)));
                PlaybackState = PlaybackState.SlowMotion;
                SlowMotionSpeed = currentSlowSpeed != DaHuatechNVR.SlowSpeed.Normal ? currentSlowSpeed.ToString() : null;
                // 更新索引，指向下一个慢放倍速
                if (SlowMotionSpeed == null)
                {
                    PlaybackState = PlaybackState.Playing;
                }
                _currentSlowMotionSpeedIndex = (_currentSlowMotionSpeedIndex + 1) % _slowMotionSpeeds.Length;
            }
        }

        public ICommand FastForwardBySecondsCommand => new DelegateCommand<object>(FastForwardBySecondsDelegate);

        /// <summary>
        /// 快进指定秒数
        /// </summary>
        /// <param name="obj"></param>
        private async void FastForwardBySecondsDelegate(object obj)
        {
            if (int.TryParse(obj as string, out var seconds))
            {
                ProgressRelease = true;
                if (_daHuatechNvr is not null)
                {
                    await Task.WhenAll(VideoPlayerItems.Select(item =>
                        _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel)));
                    PlaybackState = PlaybackState.Ready;
                }

                await Task.Delay(600);
                CurrentTime = CurrentTime.AddSeconds(seconds);
                await PlaybackAsync(obj);
                ProgressRelease = false;
            }
        }

        public ICommand RewindBySecondsCommand => new DelegateCommand<object>(RewindBySecondsDelegate);

        /// <summary>
        /// 快退指定秒数
        /// </summary>
        /// <param name="obj"></param>
        private async void RewindBySecondsDelegate(object obj)
        {
            if (int.TryParse(obj as string, out var seconds))
            {
                ProgressRelease = true;
                if (_daHuatechNvr is not null)
                {
                    await Task.WhenAll(VideoPlayerItems.Select(item =>
                        _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel)));
                    PlaybackState = PlaybackState.Ready;
                }

                await Task.Delay(600);
                CurrentTime = CurrentTime.AddSeconds(0 - seconds);
                await PlaybackAsync(obj);
                ProgressRelease = false;
            }
        }

        public ICommand ProgressChangedCommand => new DelegateCommand<MouseButtonEventArgs>(ProgressChangedDelegate);

        /// <summary>
        /// 改变进度后
        /// </summary>
        /// <param name="obj"></param>
        private async void ProgressChangedDelegate(MouseButtonEventArgs obj)
        {
            if (obj.ChangedButton == MouseButton.Left && ProgressRelease)
            {
                await PlaybackAsync(obj);
                ProgressRelease = false;
            }
        }

        public ICommand ProgressReleaseCommand => new DelegateCommand<MouseButtonEventArgs>(ProgressReleaseDelegate);

        private async void ProgressReleaseDelegate(MouseButtonEventArgs obj)
        {
            if (obj.ChangedButton == MouseButton.Left)
            {
                ProgressRelease = true;
                if (_daHuatechNvr is not null)
                {
                    await Task.WhenAll(VideoPlayerItems.Select(item =>
                        _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel)));
                    PlaybackState = PlaybackState.Ready;
                }
            }
        }

        /// <summary>
        /// 截图
        /// </summary>
        public ICommand CaptureScreenShotCommand => new DelegateCommand<VideoPlayerModel>(CaptureScreenShotDelegate);

        private async void CaptureScreenShotDelegate(VideoPlayerModel obj)
        {
            if (_daHuatechNvr is not null)
            {
                var saveFileDialog = new SaveFileDialog()
                {
                    DefaultExt = ".bmp",
                    Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop), // 初始路径
                    FileName = $"img_{PackageItemModel.Barcode}",
                    Title = "保存截图"
                };
                var showDialog = saveFileDialog.ShowDialog();
                if (showDialog == true)
                {
                    await _daHuatechNvr.CaptureAsync(obj.IpAddress, obj.Channel, saveFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// 视频下载
        /// </summary>
        public ICommand DownloadVideoCommand => new DelegateCommand<VideoPlayerModel>(DownloadVideoDelegate);

        private async void DownloadVideoDelegate(VideoPlayerModel obj)
        {
            if (_daHuatechNvr is not null && obj.DownloadState == DownloadState.Ready)
            {
                var saveFileDialog = new SaveFileDialog()
                {
                    DefaultExt = ".bmp",
                    Filter = "dav files (*.dav)|*.dav|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop), // 初始路径
                    FileName = $"vid_{PackageItemModel.Barcode}",
                    Title = "保存视频"
                };
                var showDialog = saveFileDialog.ShowDialog();
                if (showDialog == true)
                {
                    var (key, value) = await _daHuatechNvr.QueryVideoFile(obj.IpAddress,
                        obj.Channel, StartTime, EndTime, 0);
                    if (key)
                    {
                        obj.DownloadProgress = 0;
                        await _daHuatechNvr.DownloadRecording(obj.IpAddress,
                             obj.Channel,
                             StartTime.AddSeconds(-2), EndTime, (int)SelectPlaybackStream,
                             saveFileDialog.FileName, async info =>
                             {
                                 await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                                 {
                                     if (info.IsDownloadComplete)
                                     {
                                         obj.DownloadState = DownloadState.Transcoding;
                                         await _daHuatechNvr.ConvertDavToMp4(saveFileDialog.FileName,
                                              Path.ChangeExtension(saveFileDialog.FileName, ".mp4"),
                                              (i, i1) =>
                                              {
                                                  if (obj.DownloadState != DownloadState.Transcoding)
                                                  {
                                                      obj.DownloadState = DownloadState.Transcoding;
                                                      obj.DownloadProgress = 0;
                                                  }
                                                  var d = ((decimal)i / i1) * 100;
                                                  if (d - obj.DownloadProgress > 2)
                                                  {
                                                      obj.DownloadProgress = d;
                                                  }

                                                  if (i == i1)
                                                  {
                                                      obj.DownloadState = DownloadState.Ready;
                                                      obj.DownloadProgress = 100;
                                                  }
                                                  return true;
                                              });
                                         obj.DownloadState = DownloadState.Ready;
                                         obj.DownloadProgress = 100;
                                     }
                                     else if (info.IsDownloadError)
                                     {
                                         //下载错误
                                         Console.WriteLine("下载错误");
                                         obj.DownloadState = DownloadState.Ready;
                                     }
                                     else
                                     {
                                         if (obj.DownloadState != DownloadState.Downloading)
                                         {
                                             obj.DownloadState = DownloadState.Downloading;
                                         }
                                     }

                                     var infoTotalSize = ((decimal)info.LoadSize / info.TotalSize) * 100;
                                     if (infoTotalSize - obj.DownloadProgress > 2)
                                     {
                                         obj.DownloadProgress = infoTotalSize;
                                     }
                                 });
                             });
                    }
                }
            }
        }

        public ICommand ChangedTimeCommand => new DelegateCommand<TextBox>(ChangedTimeDelegate);

        private async void ChangedTimeDelegate(TextBox textBox)
        {
            ProgressRelease = true;
            if (_daHuatechNvr is not null)
            {
                await Task.WhenAll(VideoPlayerItems.Select(item =>
                    _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel)));
                PlaybackState = PlaybackState.Ready;
            }
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            if (binding is not null)
            {
                //重新播放
                CurrentTime = StartTime;
                if (ProgressRelease)
                {
                    await Task.Delay(300);
                    await PlaybackAsync(null);
                }
            }
            ProgressRelease = false;
        }

        public ICommand SaveVideoPlaybackSettingsCommand => new DelegateCommand<Popup>(SaveVideoPlaybackSettingsDelegate);

        private async void SaveVideoPlaybackSettingsDelegate(Popup obj)
        {
            var insertOrUpdate = await _settingsStore.SaveAsync("VideoPlaybackSettings",new VideoPlaybackSettingsDto
                {
                    IsWatermarkTimeMarked = VideoPlaybackSettingsInfo.IsWatermarkTimeMarked,
                    SecondsToSubtract = VideoPlaybackSettingsInfo.SecondsToSubtract,
                    VideoLengthInSeconds = VideoPlaybackSettingsInfo.VideoLengthInSeconds
                });
            if (insertOrUpdate)
            {
                obj.IsOpen = false;
            }
        }
    }

    public enum PlaybackStream
    {

        [Description("主码流"), FontIcon("\xea07")]
        MainStream,

        [Description("辅码流"), FontIcon("\xea09")]
        SubStream
    }

    public enum PlaybackState
    {

        [Description("准备就绪"), AuxiliaryDescription("播放"),
         FontIcon("\xe9e9"), BackgroundColor("#4169E1"),
        LabelColor("#FFFFFF")]
        Ready,

        [Description("播放中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#FFFFFF")]
        Playing,

        [Description("快进中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#FFA500")]
        FastForwarding,

        [Description("慢放中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#00FA9A")]
        SlowMotion,

        [Description("暂停中"), AuxiliaryDescription("播放"),
         FontIcon("\xe9e9"), BackgroundColor("#4169E1"),
         LabelColor("#FFFFFF")]
        Paused
    }
}
