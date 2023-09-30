using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.IO.Ports;
using Mono.Unix.Native;
using System.Threading;
using System.Windows.Input;
using System.Windows.Forms;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Client.Models.ResultOutputSettingsModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class ResultOutputSettingsPageViewModel : BindableBase {
        private readonly ISoundRepository _soundRepository;
        private readonly IConfigRepository _configRepository;

        private ObservableCollection<ItemBaseTemplateModel> _outputItems = new()
        {
            new ItemBaseTemplateModel()
            {
                Id = 0,
                Content = "{BarCode}",
                Type = 1,
                ApplicationType = ItemApplicationType.ResultData
            },
            new ItemBaseTemplateModel()
            {
                Id = 1,
                Content = "{TimestampedGuid}",
                Type = 1,
                ApplicationType = ItemApplicationType.ResultData
            },
            new ItemBaseTemplateModel()
            {
                Id = 2,
                Content = "",
                Type = 0,
                ApplicationType = ItemApplicationType.ResultData
            },
        };

        private bool _isLoaded;
        private bool _isUseTcpOutput;
        private bool _isUseSerialOutput;
        private bool _isUseAudioOutput;
        private bool _isUseLocationOutput;
        private UploadSettingsInfoModel _uploadSettingsInfo = new();
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private LocationOutputSettingsInfoModel _locationOutputSettingsInfo = new();
        private ObservableCollection<string> _portItems = new();
        private string _portName = string.Empty;
        private ParityInfoModel _selectParity = new();

        private ObservableCollection<ParityInfoModel> _parityItems = new()
        {
            new ParityInfoModel()
            {
                Name = "None",
                Value = Parity.None
            },
            new ParityInfoModel()
            {
                Name = "Odd",
                Value = Parity.Odd
            },
            new ParityInfoModel()
            {
                Name = "Even",
                Value = Parity.Even
            },
            new ParityInfoModel()
            {
                Name = "Mark",
                Value = Parity.Mark
            },
            new ParityInfoModel()
            {
                Name = "Space",
                Value = Parity.Space
            },
        };

        private StopBitsInfoModel _selectedStopBits = new();

        private ObservableCollection<StopBitsInfoModel> _stopBitsItems = new()
        {
            new StopBitsInfoModel()
            {
                Name = "None",
                Value = 0,
            },
            new StopBitsInfoModel()
            {
                Name = "One",
                Value = StopBits.One,
            },
            new StopBitsInfoModel()
            {
                Name = "Two",
                Value = StopBits.Two,
            },
            new StopBitsInfoModel()
            {
                Name = "OnePointFive",
                Value = StopBits.OnePointFive,
            },
        };

        private int _selectBaudRate = 115200;

        private ObservableCollection<int> _baudRateItems = new()
        {
            4800,9600,14400,19200,38400,115200
        };

        private ObservableCollection<int> _dataBitsItems = new()
        {
            5,6,7,8,
        };

        private int _selectedDataBits = 8;

        private ObservableCollection<TriggerPositionModel> _triggerPositionItems = new()
        {
            new TriggerPositionModel()
            {
                TriggerPositionName = "Http输出后",
                TriggerPositionValue = TriggerPositionEnum.HttpOutput,
            },
            new TriggerPositionModel()
            {
                TriggerPositionName = "Tcp输出后",
                TriggerPositionValue = TriggerPositionEnum.TcpOutput,
            },
            new TriggerPositionModel()
            {
                TriggerPositionName = "串口输出后",
                TriggerPositionValue = TriggerPositionEnum.SerialPortOutput,
            },
            new TriggerPositionModel()
            {
                TriggerPositionName = "位置输出后",
                TriggerPositionValue = TriggerPositionEnum.LocationOutput,
            },
            new TriggerPositionModel()
            {
                TriggerPositionName = "包裹触发后",
                TriggerPositionValue = TriggerPositionEnum.PackageTrigger,
            },
        };

        private TriggerPositionModel _selectedTriggerPosition = new();

        private SerialPortSettingsInfoModel _serialPortSettingsInfo = new();

        private ObservableCollection<TriggerPositionResultModel> _triggerPositionResultItems = new()
        {
            new TriggerPositionResultModel()
            {
                ResultName = "Api接口响应",
                ResultValue = ResultEnum.ApiResponse,
            },
            new TriggerPositionResultModel()
            {
                ResultName = "http输出响应",
                ResultValue = ResultEnum.HttpOutputResponse,
            },
            new TriggerPositionResultModel()
            {
                ResultName = "包裹识别",
                ResultValue = ResultEnum.PackageRecognition,
            },
            new TriggerPositionResultModel()
            {
                ResultName = "无",
                ResultValue = ResultEnum.NotSet,
            },
        };

        private TriggerPositionResultModel _selectedTriggerPositionResult = new();
        private string _soundFilePath = string.Empty;
        private ObservableCollection<string> _sounds = new();
        private SnackbarMessageQueue _resultOutputSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private AudioOutputSettingsInfoModel _audioOutputSettingsInfo = new();
        private bool _isSavingInProgress;
        private SerialPortResultOutputModel _serialPortResultOutput = new();

        private ObservableCollection<DataFormatTypeInfoModel> _dataFormatTypeItems = new()
        {
            new DataFormatTypeInfoModel()
            {
                Name = "Ascii",
                Value = DataFormatType.Ascii
            },
            new DataFormatTypeInfoModel()
            {
                Name = "Hex",
                Value = DataFormatType.Hex
            },
        };

        private DataFormatTypeInfoModel _selectDataFormat = new();

        public ResultOutputSettingsPageViewModel(ISoundRepository soundRepository,
            IConfigRepository configRepository) {
            _soundRepository = soundRepository;
            _configRepository = configRepository;
        }

        public SnackbarMessageQueue ResultOutputSettingsMessageQueue {
            get => _resultOutputSettingsMessageQueue;
            set => SetProperty(ref _resultOutputSettingsMessageQueue, value);
        }

        /// <summary>
        /// 数据模板
        /// </summary>
        public ObservableCollection<ItemBaseTemplateModel> OutputItems {
            get => _outputItems;
            set => SetProperty(ref _outputItems, value);
        }

        /// <summary>
        /// 成功音频列表
        /// </summary>
        public ObservableCollection<string> Sounds {
            get => _sounds;
            set => SetProperty(ref _sounds, value);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<string> PortItems {
            get => _portItems;
            set => SetProperty(ref _portItems, value);
        }

        /// <summary>
        /// 选中串口名称
        /// </summary>
        public string SelectedPort {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }

        public ParityInfoModel SelectedParity {
            get => _selectParity;
            set => SetProperty(ref _selectParity, value);
        }

        public ObservableCollection<ParityInfoModel> ParityItems {
            get => _parityItems;
            set => SetProperty(ref _parityItems, value);
        }

        public StopBitsInfoModel SelectedStopBits {
            get => _selectedStopBits;
            set => SetProperty(ref _selectedStopBits, value);
        }

        public ObservableCollection<StopBitsInfoModel> StopBitsItems {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        public int SelectBaudRate {
            get => _selectBaudRate;
            set => SetProperty(ref _selectBaudRate, value);
        }

        public ObservableCollection<int> BaudRateItems {
            get => _baudRateItems;
            set => SetProperty(ref _baudRateItems, value);
        }

        public int SelectedDataBits {
            get => _selectedDataBits;
            set => SetProperty(ref _selectedDataBits, value);
        }

        public ObservableCollection<int> DataBitsItems {
            get => _dataBitsItems;
            set => SetProperty(ref _dataBitsItems, value);
        }

        public ObservableCollection<TriggerPositionModel> TriggerPositionItems {
            get => _triggerPositionItems;
            set => SetProperty(ref _triggerPositionItems, value);
        }

        public TriggerPositionModel SelectedTriggerPosition {
            get => _selectedTriggerPosition;
            set => SetProperty(ref _selectedTriggerPosition, value);
        }

        public ObservableCollection<TriggerPositionResultModel> TriggerPositionResultItems {
            get => _triggerPositionResultItems;
            set => SetProperty(ref _triggerPositionResultItems, value);
        }

        public TriggerPositionResultModel SelectedTriggerPositionResult {
            get => _selectedTriggerPositionResult;
            set => SetProperty(ref _selectedTriggerPositionResult, value);
        }
        public DataFormatTypeInfoModel SelectDataFormat {
            get => _selectDataFormat;
            set => SetProperty(ref _selectDataFormat, value);
        }
        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }
        /// <summary>
        /// 串口内容
        /// </summary>
        public SerialPortResultOutputModel SerialPortResultOutput {
            get => _serialPortResultOutput;
            set => SetProperty(ref _serialPortResultOutput, value);
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand {
            get => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);
        }

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.ResultData) {
                    OutputItems.Remove(model);
                    foreach (var item in OutputItems) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            OutputItems.LastOrDefault() != item) {
                            OutputItems.Remove(item);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 添加数据模板
        /// </summary>
        public ICommand AddOutputItemCommand {
            get => new DelegateCommand<string>(AddOutputItemDelegate);
        }

        private async void AddOutputItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                var count = OutputItems.Count;
                OutputItems.Insert(count - 1 < 0 ? 0 : count - 1, new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = count,
                    Type = 1,
                    ApplicationType = ItemApplicationType.ResultData
                });
                var model = OutputItems?.LastOrDefault();
                if (model?.Type != 0) {
                    OutputItems?.Add(new ItemBaseTemplateModel() {
                        Content = string.Empty,
                        Id = OutputItems.Count,
                        ApplicationType = ItemApplicationType.ResultData
                    });
                }
            });
        }

        /// <summary>
        /// 是否使用Tcp输出
        /// </summary>
        public bool IsUseTcpOutput {
            get => _isUseTcpOutput;
            set => SetProperty(ref _isUseTcpOutput, value);
        }

        /// <summary>
        /// 是否使用串口输出
        /// </summary>
        public bool IsUseSerialOutput {
            get => _isUseSerialOutput;
            set => SetProperty(ref _isUseSerialOutput, value);
        }

        /// <summary>
        /// 是否使用音频输出
        /// </summary>
        public bool IsUseAudioOutput {
            get => _isUseAudioOutput;
            set => SetProperty(ref _isUseAudioOutput, value);
        }

        /// <summary>
        /// 是否使用位置输出
        /// </summary>
        public bool IsUseLocationOutput {
            get => _isUseLocationOutput;
            set => SetProperty(ref _isUseLocationOutput, value);
        }

        /// <summary>
        /// 上传设置
        /// </summary>
        public UploadSettingsInfoModel UploadSettingsInfo {
            get => _uploadSettingsInfo;
            set => SetProperty(ref _uploadSettingsInfo, value);
        }

        /// <summary>
        /// Tcp输出设置
        /// </summary>
        public TcpSettingsInfoModel TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }

        /// <summary>
        /// 串口输出
        /// </summary>
        public SerialPortSettingsInfoModel SerialPortSettingsInfo {
            get => _serialPortSettingsInfo;
            set => SetProperty(ref _serialPortSettingsInfo, value);
        }

        /// <summary>
        /// 位置输出
        /// </summary>
        public LocationOutputSettingsInfoModel LocationOutputSettingsInfo {
            get => _locationOutputSettingsInfo;
            set => SetProperty(ref _locationOutputSettingsInfo, value);
        }

        /// <summary>
        /// 声音输出
        /// </summary>
        public AudioOutputSettingsInfoModel AudioOutputSettingsInfo {
            get => _audioOutputSettingsInfo;
            set => SetProperty(ref _audioOutputSettingsInfo, value);
        }

        /// <summary>
        /// 声音文件路径
        /// </summary>
        public string SoundFilePath {
            get => _soundFilePath;
            set => SetProperty(ref _soundFilePath, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public ICommand BarCodeKeyDownCommand {
            get => new DelegateCommand<System.Windows.Input.KeyEventArgs>(BarCodeKeyDownDelegate);
        }

        private async void BarCodeKeyDownDelegate(System.Windows.Input.KeyEventArgs obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                LocationOutputSettingsInfo.BarcodeOutputKey = obj.Key.ToString();
            });
        }

        public ICommand WeightKeyDownCommand {
            get => new DelegateCommand<System.Windows.Input.KeyEventArgs>(WeightKeyDownDelegate);
        }

        private async void WeightKeyDownDelegate(System.Windows.Input.KeyEventArgs obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                LocationOutputSettingsInfo.WeightOutputKey = obj.Key.ToString();
            });
        }

        public ICommand BrowseSoundFileCommand {
            get => new DelegateCommand<object>(BrowseSoundFileDelegate);
        }

        private async void BrowseSoundFileDelegate(object obj) {
            var openFileDialog = new OpenFileDialog() {
                Filter = $"{Languages.Language.ResourceManager.GetString("声音文件") ?? string.Empty}|*.wav;*.mp3",
                Title = Languages.Language.ResourceManager.GetString("请选择声音文件"),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    SoundFilePath = openFileDialog.FileName;
                });
            }
        }

        public ICommand AddSoundFileCommand {
            get => new DelegateCommand<object>(AddSoundFileDelegate);
        }

        private async void AddSoundFileDelegate(object obj) {
            if (!string.IsNullOrWhiteSpace(SoundFilePath) &&
                File.Exists(SoundFilePath)) {
                //加载遮罩,加载锁

                var update = await _soundRepository.InsertOrUpdate(new SoundInfoModel() {
                    SoundName = new FileInfo(SoundFilePath).Name,
                    SoundFile = await File.ReadAllBytesAsync(SoundFilePath)
                });
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    if (update) {
                        Sounds.Clear();
                        var soundInfoModels = await _soundRepository.Select(w => w.Id > 0, o => o.Id);
                        if (soundInfoModels?.Any() == true) {
                            Sounds.AddRange(soundInfoModels.Select(s => s.SoundName));
                        }
                        ResultOutputSettingsMessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("添加成功") ?? string.Empty);
                    }
                    else {
                        ResultOutputSettingsMessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("添加失败") ?? string.Empty);
                        //提示失败
                    }
                });
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "ResultOutputSettings",
                        Value = JsonConvert.SerializeObject(new ResultOutputSettingsDto {
                            DataTemplate = OutputItems.Select(s => new ItemTemplateInfo {
                                ApplicationType = s.ApplicationType,
                                Content = s.Content,
                                Type = s.Type,
                            }).ToList(),
                            UploadSettingsInfo = new UploadSettingsInfo() {
                                IsAutoUploadOnRestart = UploadSettingsInfo.IsAutoUploadOnRestart,
                                RetryCount = UploadSettingsInfo.RetryCount,
                                SendDelay = UploadSettingsInfo.SendDelay,
                            },
                            IsUseTcpOutput = IsUseTcpOutput,
                            TcpSettingsInfo = new TcpSettingsInfo() {
                                ClientConfig = new TcpInfo() {
                                    IpAddress = TcpSettingsInfo.ClientConfig.IpAddress,
                                    Port = TcpSettingsInfo.ClientConfig.Port
                                },
                                ServerConfig = new TcpInfo() {
                                    IpAddress = TcpSettingsInfo.ServerConfig.IpAddress,
                                    Port = TcpSettingsInfo.ServerConfig.Port
                                },
                                ConnectionMode = TcpSettingsInfo.ConnectionMode
                            },
                            IsUseSerialOutput = IsUseSerialOutput,
                            SerialPortSettingsInfo = new SerialPortSettingsInfo() {
                                BaudRate = SelectBaudRate,
                                Parity = SelectedParity.Value,
                                PortName = SelectedPort,
                                DataBits = SelectedDataBits,
                                StopBits = SelectedStopBits.Value,
                                DataFormat = SelectDataFormat.Value,
                            },
                            SerialPortResultOutputInfo = new SerialPortResultOutputInfo() {
                                CustomOutputContent = SerialPortResultOutput.CustomOutputContent,
                                IsUseCustomContentOutput = SerialPortResultOutput.IsUseCustomContentOutput,
                                IsUseDataTemplateOutput = SerialPortResultOutput.IsUseDataTemplateOutput
                            },
                            IsUseAudioOutput = IsUseAudioOutput,
                            AudioOutputSettingsInfo = new AudioOutputSettingsInfo() {
                                FailureAudio = AudioOutputSettingsInfo.FailureAudio,
                                SuccessAudio = AudioOutputSettingsInfo.SuccessAudio,
                                TriggerPosition = SelectedTriggerPosition.TriggerPositionValue,
                                Result = SelectedTriggerPositionResult.ResultValue,
                            },
                            IsUseLocationOutput = IsUseLocationOutput,
                            LocationOutputSettingsInfo = new LocationOutputSettingsInfo() {
                                BarcodeOutputKey = LocationOutputSettingsInfo.BarcodeOutputKey,
                                BarcodeOutputPosition = LocationOutputSettingsInfo.BarcodeOutputPosition,
                                IsOutputBarcode = LocationOutputSettingsInfo.IsOutputBarcode,
                                IsOutputWeight = LocationOutputSettingsInfo.IsOutputWeight,
                                IsOutputWeightFirst = LocationOutputSettingsInfo.IsOutputWeightFirst,
                                OperationDelay = LocationOutputSettingsInfo.OperationDelay,
                                WeightOutputKey = LocationOutputSettingsInfo.WeightOutputKey,
                                WeightOutputPosition = LocationOutputSettingsInfo.WeightOutputPosition
                            },
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "ResultOutputSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    ResultOutputSettingsMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
                });
            }

            //显示遮罩
            //保存设置到数据库
            //通知设置更改事件
            //隐藏遮罩
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    //加载音频列表
                    Sounds.Clear();
                    var soundInfoModels = await _soundRepository.Select(w => w.Id > 0, o => o.Id);
                    if (soundInfoModels?.Any() == true) {
                        Sounds.AddRange(soundInfoModels.Select(s => s.SoundName));
                    }
                    PortItems.Clear();
                    //加载串口列表
                    PortItems.AddRange(SerialPort.GetPortNames()?.ToList());

                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("ResultOutputSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var settingsDto = JsonConvert.DeserializeObject<ResultOutputSettingsDto>(configInfoModel.Value);
                            if (settingsDto is not null) {
                                //加载停止位的值
                                SelectedStopBits = StopBitsItems.FirstOrDefault(f =>
                                    f.Value.Equals(settingsDto.SerialPortSettingsInfo.StopBits)) ?? new StopBitsInfoModel();
                                //加载效验位的值
                                SelectedParity = ParityItems.FirstOrDefault(f =>
                                    f.Value.Equals(settingsDto.SerialPortSettingsInfo.Parity)) ?? new ParityInfoModel();
                                //加载触发位置的值
                                SelectedTriggerPosition = TriggerPositionItems.FirstOrDefault(f =>
                                    f.TriggerPositionValue.Equals(settingsDto.AudioOutputSettingsInfo.TriggerPosition)) ?? new TriggerPositionModel();
                                //加载结果判断值
                                SelectedTriggerPositionResult = TriggerPositionResultItems.FirstOrDefault(f =>
                                    f.ResultValue.Equals(settingsDto.AudioOutputSettingsInfo.Result)) ?? new TriggerPositionResultModel();
                                SelectDataFormat =
                                    DataFormatTypeItems?.FirstOrDefault(f =>
                                        f.Value.Equals(settingsDto.SerialPortSettingsInfo.DataFormat)) ??
                                    new DataFormatTypeInfoModel();
                                SelectedPort = PortItems.FirstOrDefault(f =>
                                    f.Equals(settingsDto.SerialPortSettingsInfo.PortName)) ?? string.Empty;
                                OutputItems.Clear();
                                var models = settingsDto.DataTemplate.Select((s, i) => new ItemBaseTemplateModel() {
                                    ApplicationType = s.ApplicationType,
                                    Content = s.Content,
                                    Type = s.Type,
                                    Id = i + 1
                                }).ToList();
                                OutputItems.AddRange(models);
                                UploadSettingsInfo = new UploadSettingsInfoModel() {
                                    IsAutoUploadOnRestart = settingsDto.UploadSettingsInfo.IsAutoUploadOnRestart,
                                    RetryCount = settingsDto.UploadSettingsInfo.RetryCount,
                                    SendDelay = settingsDto.UploadSettingsInfo.SendDelay,
                                };
                                IsUseTcpOutput = settingsDto.IsUseTcpOutput;
                                TcpSettingsInfo = new TcpSettingsInfoModel() {
                                    ClientConfig = new TcpInfoModel() {
                                        IpAddress = settingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                        Port = settingsDto.TcpSettingsInfo.ClientConfig.Port,
                                    },
                                    ServerConfig = new TcpInfoModel() {
                                        IpAddress = settingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                        Port = settingsDto.TcpSettingsInfo.ServerConfig.Port,
                                    },
                                    ConnectionMode = settingsDto.TcpSettingsInfo.ConnectionMode,
                                };
                                IsUseSerialOutput = settingsDto.IsUseSerialOutput;
                                SerialPortSettingsInfo = new SerialPortSettingsInfoModel() {
                                    BaudRate = settingsDto.SerialPortSettingsInfo.BaudRate,
                                    Parity = settingsDto.SerialPortSettingsInfo.Parity,
                                    DataBits = settingsDto.SerialPortSettingsInfo.DataBits,
                                    PortName = settingsDto.SerialPortSettingsInfo.PortName,
                                    StopBits = settingsDto.SerialPortSettingsInfo.StopBits,
                                    DataFormat = settingsDto.SerialPortSettingsInfo.DataFormat
                                };
                                SerialPortResultOutput = new SerialPortResultOutputModel() {
                                    CustomOutputContent = settingsDto.SerialPortResultOutputInfo.CustomOutputContent,
                                    IsUseCustomContentOutput =
                                        settingsDto.SerialPortResultOutputInfo.IsUseCustomContentOutput,
                                    IsUseDataTemplateOutput =
                                        settingsDto.SerialPortResultOutputInfo.IsUseDataTemplateOutput
                                };
                                IsUseAudioOutput = settingsDto.IsUseAudioOutput;
                                AudioOutputSettingsInfo = new AudioOutputSettingsInfoModel() {
                                    FailureAudio = settingsDto.AudioOutputSettingsInfo.FailureAudio,
                                    Result = settingsDto.AudioOutputSettingsInfo.Result,
                                    SuccessAudio = settingsDto.AudioOutputSettingsInfo.SuccessAudio,
                                    TriggerPosition = settingsDto.AudioOutputSettingsInfo.TriggerPosition,
                                };
                                IsUseLocationOutput = settingsDto.IsUseLocationOutput;
                                LocationOutputSettingsInfo = new LocationOutputSettingsInfoModel() {
                                    BarcodeOutputKey = settingsDto.LocationOutputSettingsInfo.BarcodeOutputKey,
                                    BarcodeOutputPosition =
                                        settingsDto.LocationOutputSettingsInfo.BarcodeOutputPosition,
                                    IsOutputBarcode = settingsDto.LocationOutputSettingsInfo.IsOutputBarcode,
                                    IsOutputWeight = settingsDto.LocationOutputSettingsInfo.IsOutputWeight,
                                    IsOutputWeightFirst = settingsDto.LocationOutputSettingsInfo.IsOutputWeightFirst,
                                    OperationDelay = settingsDto.LocationOutputSettingsInfo.OperationDelay,
                                    WeightOutputKey = settingsDto.LocationOutputSettingsInfo.WeightOutputKey,
                                    WeightOutputPosition = settingsDto.LocationOutputSettingsInfo.WeightOutputPosition,
                                };

                            }
                        }
                        catch (Exception e) {
                            ResultOutputSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}:{e.Message}");
                        }
                    }
                });
            }
        }
    }
}