using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Scale;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.WeightSettingsModel;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using WeightAccessMode = JayTom.Dws.Domain.Dto.WeightAccessMode;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class WeightSettingViewModel : BindableBase {
        private readonly IDynamicScale _dynamicScale;
        private readonly IStaticScale _staticScale;
        private readonly IConfigRepository _configRepository;
        private WeightSettingsInfoModel _weightSettingsInfo = new();
        private ObservableCollection<string> _portItems = new();

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

        private ObservableCollection<int> _baudRateItems = new()
        {
            4800,9600,14400,19200,38400,115200
        };

        private ObservableCollection<int> _dataBitsItems = new()
        {
            5,6,7,8,
        };

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

        private ObservableCollection<WeightModeInfoModel> _weightModeItems = new()
        {
            new WeightModeInfoModel()
            {
                Name =Languages.Language.ResourceManager.GetString("StaticWeighing")??string.Empty,
                Value = WeightMode.Static,
            },
            new WeightModeInfoModel()
            {
                Name =Languages.Language.ResourceManager.GetString("DynamicWeighing")??string.Empty,
                Value = WeightMode.Dynamic,
            },
            new WeightModeInfoModel()
            {
                Name =Languages.Language.ResourceManager.GetString("NoWeighing")??string.Empty,
                Value = WeightMode.None,
            },
        };

        private ParityInfoModel _selectParity = new();
        private StopBitsInfoModel _selectStopBits = new();
        private DataFormatTypeInfoModel _selectDataFormat = new();
        private DataFormatTypeInfoModel _sendDataFormat = new();

        private ObservableCollection<WeightAccessInfoMode> _weightAccessItems = new()
        {
            new WeightAccessInfoMode()
            {
                Name =Languages.Language.ResourceManager.GetString("ReadOnly")??string.Empty,
                Value = WeightAccessMode.Readonly
            },
            new WeightAccessInfoMode()
            {
                Name =Languages.Language.ResourceManager.GetString("Request")??string.Empty,
                Value = WeightAccessMode.QuestionAnswer
            },
        };

        private WeightAccessInfoMode _selectedWeightAccess = new();
        private WeightModeInfoModel _selectWeightMode = new();
        private bool _isRealtimeDataEnabled;
        private float _realtimeWeight;
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _weightSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;
        private string _receivedData = string.Empty;
        private string _weightSourceContent;
        private float _parsedWeight;

        public WeightSettingViewModel(IDynamicScale dynamicScale,
            IStaticScale staticScale, IConfigRepository configRepository) {
            _dynamicScale = dynamicScale;
            _staticScale = staticScale;
            _configRepository = configRepository;
            _dynamicScale.StabledWeight += async delegate (object? sender, float f) {
                if (SelectWeightMode.Value == WeightMode.Dynamic) {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        RealtimeWeight = f;
                    });
                }
            };
            _dynamicScale.Received += async delegate (object? sender, string s) {
                if (SelectWeightMode.Value == WeightMode.Dynamic && IsRealtimeDataEnabled) {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        ReceivedData ??= string.Empty;
                        if (ReceivedData.Length >= 5000) {
                            ReceivedData = string.Empty;
                        }
                        ReceivedData += s;
                    }, DispatcherPriority.Background);
                }
            };
            _staticScale.CurrentWeight += async delegate (object? sender, float f) {
                if (SelectWeightMode.Value == WeightMode.Static) {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        RealtimeWeight = f;
                    });
                }
            };
            _staticScale.Received += async delegate (object? sender, string s) {
                if (SelectWeightMode.Value == WeightMode.Static && IsRealtimeDataEnabled) {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        ReceivedData ??= string.Empty;
                        if (ReceivedData.Length >= 5000) {
                            ReceivedData = string.Empty;
                        }
                        ReceivedData += s;
                    }, DispatcherPriority.Background);
                }
            };
            _dynamicScale.Excepted += async delegate (object? sender, Exception exception) {
                //异常的输出之后需要取消
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    WeightSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("动态称异常") ?? string.Empty}:{exception.Message}");
                });
            };
            _staticScale.Excepted += async delegate (object? sender, Exception exception) {
                //异常的输出之后需要取消
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    WeightSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("静态称异常")}:{exception.Message}");
                });
            };
            _dynamicScale.Connected += async delegate (object? sender, IScale scale) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    WeightSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("动态称连接成功")}");
                });
            };
            _staticScale.Connected += async delegate (object? sender, IScale scale) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    WeightSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("静态称连接成功")}");
                });
            };
        }

        public SnackbarMessageQueue WeightSettingsMessageQueue {
            get => _weightSettingsMessageQueue;
            set => SetProperty(ref _weightSettingsMessageQueue, value);
        }

        public WeightSettingsInfoModel WeightSettingsInfo {
            get => _weightSettingsInfo;
            set => SetProperty(ref _weightSettingsInfo, value);
        }

        public ObservableCollection<WeightModeInfoModel> WeightModeItems {
            get => _weightModeItems;
            set => SetProperty(ref _weightModeItems, value);
        }

        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }

        public ObservableCollection<WeightAccessInfoMode> WeightAccessItems {
            get => _weightAccessItems;
            set => SetProperty(ref _weightAccessItems, value);
        }

        public WeightModeInfoModel SelectWeightMode {
            get => _selectWeightMode;
            set => SetProperty(ref _selectWeightMode, value);
        }

        public WeightAccessInfoMode SelectedWeightAccess {
            get => _selectedWeightAccess;
            set => SetProperty(ref _selectedWeightAccess, value);
        }

        public DataFormatTypeInfoModel SelectDataFormat {
            get => _selectDataFormat;
            set => SetProperty(ref _selectDataFormat, value);
        }

        /// <summary>
        /// 发送格式
        /// </summary>
        public DataFormatTypeInfoModel SendDataFormat {
            get => _sendDataFormat;
            set => SetProperty(ref _sendDataFormat, value);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<string> PortItems {
            get => _portItems;
            set => SetProperty(ref _portItems, value);
        }

        /// <summary>
        /// 效验位下拉选项
        /// </summary>
        public ObservableCollection<ParityInfoModel> ParityItems {
            get => _parityItems;
            set => SetProperty(ref _parityItems, value);
        }

        /// <summary>
        /// 效验位
        /// </summary>
        public ParityInfoModel SelectParity {
            get => _selectParity;
            set => SetProperty(ref _selectParity, value);
        }

        /// <summary>
        /// 停止位下拉选项
        /// </summary>
        public ObservableCollection<StopBitsInfoModel> StopBitsItems {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBitsInfoModel SelectStopBits {
            get => _selectStopBits;
            set => SetProperty(ref _selectStopBits, value);
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public ObservableCollection<int> BaudRateItems {
            get => _baudRateItems;
            set => SetProperty(ref _baudRateItems, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public ObservableCollection<int> DataBitsItems {
            get => _dataBitsItems;
            set => SetProperty(ref _dataBitsItems, value);
        }

        /// <summary>
        /// 是否开启实时接收数据
        /// </summary>
        public bool IsRealtimeDataEnabled {
            get => _isRealtimeDataEnabled;
            set => SetProperty(ref _isRealtimeDataEnabled, value);
        }

        /// <summary>
        /// 实时字符串内容
        /// </summary>
        public string ReceivedData {
            get => _receivedData;
            set => SetProperty(ref _receivedData, value);
        }

        /// <summary>
        /// 实时重量
        /// </summary>
        public float RealtimeWeight {
            get => _realtimeWeight;
            set => SetProperty(ref _realtimeWeight, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 重量源内容
        /// </summary>
        public string WeightSourceContent {
            get => _weightSourceContent;
            set => SetProperty(ref _weightSourceContent, value);
        }

        /// <summary>
        /// 解析后的重量
        /// </summary>
        public float ParsedWeight {
            get => _parsedWeight;
            set => SetProperty(ref _parsedWeight, value);
        }

        /// <summary>
        /// 串口刷新
        /// </summary>
        public ICommand PortUpdateCommand {
            get => new DelegateCommand(PortUpdateDelegate);
        }

        private async void PortUpdateDelegate() {
            //重新枚举串口
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }

        /// <summary>
        /// 实时内容开关更改
        /// </summary>
        public ICommand IsRealtimeDataEnabledChangedCommand {
            get => new DelegateCommand(IsRealtimeDataEnabledChangedDelegate);
        }

        private void IsRealtimeDataEnabledChangedDelegate() {
            /*if (!IsRealtimeDataEnabled) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    ReceivedData = string.Empty;
                });
            }*/
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
                _staticScale.Dispose();
                _dynamicScale.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(1));
                var properties = new WeightAdditionalProperties() {
                    IsUseActualWeightConversionRate =
                        WeightSettingsInfo.AdditionalWeight.IsUseActualWeightConversionRate,
                    IsUseAppendedWeight = WeightSettingsInfo.AdditionalWeight.IsUseAppendedWeight,
                    IsUseFixedWeight = WeightSettingsInfo.AdditionalWeight.IsUseFixedWeight,
                    IsUseMergedWeightTimeout = WeightSettingsInfo.AdditionalWeight.IsUseMergedWeightTimeout,
                    WeightConversionRate = WeightSettingsInfo.AdditionalWeight.WeightConversionRate,
                    AppendedWeightValue = WeightSettingsInfo.AdditionalWeight.AppendedWeightValue,
                    FixedWeightValue = WeightSettingsInfo.AdditionalWeight.FixedWeightValue,
                    MergedWeightTimeout = WeightSettingsInfo.AdditionalWeight.MergedWeightTimeout
                };
                switch (SelectWeightMode.Value) {
                    //连接、并保存设置
                    case WeightMode.Static:
                        _staticScale.WeightFormat = (ScaleWeightFormat)SelectDataFormat.Value;
                        _staticScale.WeightAdditionalProperties = properties;
                        _staticScale.SetWeightCalculationParameters(new DefaultStaticScaleValueParameters() {
                            AccessMode = (Plugin.Scale.StaticScale.WeightAccessMode)SelectedWeightAccess.Value,
                            BalanceCount = WeightSettingsInfo.StaticWeight.BalanceCount,
                            BalanceQty = WeightSettingsInfo.StaticWeight.BalanceQty,
                            CharacterLength = WeightSettingsInfo.StaticWeight.CharacterLength,
                            DataInterval = TimeSpan.FromMilliseconds(WeightSettingsInfo.StaticWeight.DataInterval),
                            DecimalEndPosition = WeightSettingsInfo.StaticWeight.DecimalEndPosition,
                            DecimalStartPosition = WeightSettingsInfo.StaticWeight.DecimalStartPosition,
                            Identifier = WeightSettingsInfo.StaticWeight.Identifier,
                            IdentifierPosition = WeightSettingsInfo.StaticWeight.IdentifierPosition,
                            IntegerEndPosition = WeightSettingsInfo.StaticWeight.IntegerEndPosition,
                            IntegerStartPosition = WeightSettingsInfo.StaticWeight.IntegerStartPosition,
                            IsReversed = WeightSettingsInfo.StaticWeight.IsReversed,
                            SendingContent = WeightSettingsInfo.StaticWeight.SendingContent,
                            SendingFormat = (ScaleWeightFormat)SendDataFormat.Value,
                            MaxWeight = WeightSettingsInfo.CommonWeight.MaxWeight,
                            MinWeight = WeightSettingsInfo.CommonWeight.MinWeight
                        });
                        _staticScale.Connect(new BaseScaleConnectParam() {
                            PortName = WeightSettingsInfo.Connection.PortName,
                            BaudRate = WeightSettingsInfo.Connection.BaudRate,
                            DataBits = WeightSettingsInfo.Connection.DataBits,
                            Parity = SelectParity.Value,
                            StopBits = SelectStopBits.Value
                        });
                        //连接静态称
                        break;

                    case WeightMode.Dynamic:
                        //连接动态称
                        _dynamicScale.WeightFormat = (ScaleWeightFormat)SelectDataFormat.Value;
                        _dynamicScale.WeightAdditionalProperties = properties;
                        _dynamicScale.SetWeightCalculationParameters(new DefaultDynamicScaleValueParameters() {
                            DecimalPlaces = WeightSettingsInfo.DynamicWeight.DecimalPrecision
                        });
                        _dynamicScale.Connect(new BaseScaleConnectParam() {
                            PortName = WeightSettingsInfo.Connection.PortName,
                            BaudRate = WeightSettingsInfo.Connection.BaudRate,
                            DataBits = WeightSettingsInfo.Connection.DataBits,
                            Parity = SelectParity.Value,
                            StopBits = SelectStopBits.Value
                        });
                        break;
                }
                //保存到数据库
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new() {
                        ConfigName = "WeightSettings",
                        Value = JsonConvert.SerializeObject(new WeightSettingsDto {
                            Mode = SelectWeightMode.Value,
                            Connection = new SerialPortSettingsInfo {
                                BaudRate = WeightSettingsInfo.Connection.BaudRate,
                                DataBits = WeightSettingsInfo.Connection.DataBits,
                                DataFormat = SelectDataFormat.Value,
                                Parity = SelectParity.Value,
                                PortName = WeightSettingsInfo.Connection.PortName,
                                StopBits = SelectStopBits.Value
                            },
                            CommonWeight = new CommonWeightParams {
                                MaxWeight = WeightSettingsInfo.CommonWeight.MaxWeight,
                                MinWeight = WeightSettingsInfo.CommonWeight.MinWeight
                            },
                            StaticWeight = new StaticWeightParams {
                                AccessMode = SelectedWeightAccess.Value,
                                BalanceCount = WeightSettingsInfo.StaticWeight.BalanceCount,
                                BalanceQty = WeightSettingsInfo.StaticWeight.BalanceQty,
                                CharacterLength = WeightSettingsInfo.StaticWeight.CharacterLength,
                                DataInterval = TimeSpan.FromMilliseconds(WeightSettingsInfo.StaticWeight.DataInterval),
                                DecimalEndPosition = WeightSettingsInfo.StaticWeight.DecimalEndPosition,
                                DecimalStartPosition = WeightSettingsInfo.StaticWeight.DecimalStartPosition,
                                Identifier = WeightSettingsInfo.StaticWeight.Identifier,
                                IdentifierPosition = WeightSettingsInfo.StaticWeight.IdentifierPosition,
                                IntegerEndPosition = WeightSettingsInfo.StaticWeight.IntegerEndPosition,
                                IntegerStartPosition = WeightSettingsInfo.StaticWeight.IntegerStartPosition,
                                IsReversed = WeightSettingsInfo.StaticWeight.IsReversed,
                                SendingContent = WeightSettingsInfo.StaticWeight.SendingContent,
                                SendingFormat = SendDataFormat.Value
                            },
                            DynamicWeight = new DynamicWeightParams() {
                                DecimalPrecision = WeightSettingsInfo.DynamicWeight.DecimalPrecision,
                            },
                            AdditionalWeight = new AdditionalWeightProperties() {
                                IsUseActualWeightConversionRate = WeightSettingsInfo.AdditionalWeight.IsUseActualWeightConversionRate,
                                IsUseAppendedWeight = WeightSettingsInfo.AdditionalWeight.IsUseAppendedWeight,
                                IsUseFixedWeight = WeightSettingsInfo.AdditionalWeight.IsUseFixedWeight,
                                IsUseMergedWeightTimeout = WeightSettingsInfo.AdditionalWeight.IsUseMergedWeightTimeout,
                                AppendedWeightValue = WeightSettingsInfo.AdditionalWeight.AppendedWeightValue,
                                WeightConversionRate = WeightSettingsInfo.AdditionalWeight.WeightConversionRate,
                                FixedWeightValue = WeightSettingsInfo.AdditionalWeight.FixedWeightValue,
                                MergedWeightTimeout = WeightSettingsInfo.AdditionalWeight.MergedWeightTimeout
                            }
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "WeightSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    WeightSettingsMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
                });
            }
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
                    PortItems.Clear();
                    PortItems.AddRange(SerialPort.GetPortNames());
                    //加载

                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("WeightSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var settingsDto = JsonConvert.DeserializeObject<WeightSettingsDto>(configInfoModel.Value);
                            if (settingsDto is not null) {
                                SelectWeightMode = WeightModeItems.FirstOrDefault(f => f.Value == settingsDto.Mode) ?? new WeightModeInfoModel();
                                SelectDataFormat =
                                    DataFormatTypeItems.FirstOrDefault(
                                        f => f.Value == settingsDto.Connection.DataFormat) ??
                                    new DataFormatTypeInfoModel();
                                SelectParity =
                                    ParityItems.FirstOrDefault(f => f.Value == settingsDto.Connection.Parity) ??
                                    new ParityInfoModel();
                                SelectStopBits =
                                    StopBitsItems.FirstOrDefault(f => f.Value == settingsDto.Connection.StopBits) ??
                                    new StopBitsInfoModel();
                                SelectedWeightAccess =
                                    WeightAccessItems.FirstOrDefault(
                                        f => f.Value == settingsDto.StaticWeight.AccessMode) ??
                                    new WeightAccessInfoMode();
                                SendDataFormat =
                                    DataFormatTypeItems.FirstOrDefault(f =>
                                        f.Value == settingsDto.StaticWeight.SendingFormat) ??
                                    new DataFormatTypeInfoModel();
                                WeightSettingsInfo = new WeightSettingsInfoModel() {
                                    Mode = settingsDto.Mode,
                                    Connection = new SerialPortSettingsInfoModel() {
                                        BaudRate = settingsDto.Connection.BaudRate,
                                        DataBits = settingsDto.Connection.DataBits,
                                        DataFormat = settingsDto.Connection.DataFormat,
                                        Parity = settingsDto.Connection.Parity,
                                        PortName = settingsDto.Connection.PortName,
                                        StopBits = settingsDto.Connection.StopBits
                                    },
                                    CommonWeight = new CommonWeightParamsModel() {
                                        MaxWeight = settingsDto.CommonWeight.MaxWeight,
                                        MinWeight = settingsDto.CommonWeight.MinWeight,
                                    },
                                    StaticWeight = new StaticWeightParamsModel() {
                                        AccessMode = settingsDto.StaticWeight.AccessMode,
                                        BalanceCount = settingsDto.StaticWeight.BalanceCount,
                                        BalanceQty = settingsDto.StaticWeight.BalanceQty,
                                        CharacterLength = settingsDto.StaticWeight.CharacterLength,
                                        DataInterval = (int)settingsDto.StaticWeight.DataInterval.TotalMilliseconds,
                                        DecimalEndPosition = settingsDto.StaticWeight.DecimalEndPosition,
                                        DecimalStartPosition = settingsDto.StaticWeight.DecimalStartPosition,
                                        Identifier = settingsDto.StaticWeight.Identifier,
                                        IdentifierPosition = settingsDto.StaticWeight.IdentifierPosition,
                                        IntegerEndPosition = settingsDto.StaticWeight.IntegerEndPosition,
                                        IntegerStartPosition = settingsDto.StaticWeight.IntegerStartPosition,
                                        IsReversed = settingsDto.StaticWeight.IsReversed,
                                        SendingContent = settingsDto.StaticWeight.SendingContent,
                                        SendingFormat = settingsDto.StaticWeight.SendingFormat
                                    },
                                    DynamicWeight = new DynamicWeightParamsModel() {
                                        DecimalPrecision = settingsDto.DynamicWeight.DecimalPrecision,
                                    },
                                    AdditionalWeight = new AdditionalWeightPropertiesModel() {
                                        IsUseActualWeightConversionRate = settingsDto.AdditionalWeight.IsUseActualWeightConversionRate,
                                        IsUseAppendedWeight = settingsDto.AdditionalWeight.IsUseAppendedWeight,
                                        IsUseFixedWeight = settingsDto.AdditionalWeight.IsUseFixedWeight,
                                        IsUseMergedWeightTimeout = settingsDto.AdditionalWeight.IsUseMergedWeightTimeout,
                                        AppendedWeightValue = settingsDto.AdditionalWeight.AppendedWeightValue,
                                        WeightConversionRate = settingsDto.AdditionalWeight.WeightConversionRate,
                                        FixedWeightValue = settingsDto.AdditionalWeight.FixedWeightValue,
                                        MergedWeightTimeout = settingsDto.AdditionalWeight.MergedWeightTimeout
                                    }
                                };
                            }
                        }
                        catch (Exception e) {
                            WeightSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败")}:{e.Message}");
                        }
                    }
                });
            }
        }

        public ICommand WeightParserCommand {
            get => new DelegateCommand<object>(WeightParserDelegate);
        }

        private async void WeightParserDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (!string.IsNullOrEmpty(WeightSourceContent) && ParsedWeight > 0) {
                    var identifier = string.Empty;
                    bool? isReversed = null;
                    int integerStartPosition, integerEndPosition, decimalStartPosition, decimalEndPosition;
                    var orDefault = WeightSourceContent
                        .Where(c => !char.IsDigit(c) && c != '.' && c != '-' && c != '+')
                        .GroupBy(c => c)
                        .Where(group => group.Count() == 1)
                        .Select(group => group.Key)
                        .FirstOrDefault();
                    if (orDefault == 0) {
                        WeightSettingsMessageQueue.Enqueue($"获取不到标识符,请检查源内容中是否有唯一标识,或在标识符位置填写标识符");
                    }
                    else {
                        identifier = orDefault.ToString();
                    }
                    //获取小数点位置
                    var indexOf = WeightSourceContent.IndexOf('.');
                    if (indexOf == 0 || indexOf == WeightSourceContent.Length - 1) {
                        WeightSettingsMessageQueue.Enqueue($"源内容中小数点不能在最前或者最后");
                        return;
                    }
                    //左边
                    var left = Regex.Match(WeightSourceContent, @"(([0-9]|-|\+)+)(?=\.)");
                    if (left.Success) {
                        var leftResult = left.Value.Trim();
                        var right = Regex.Match(WeightSourceContent, @"(?<=\.)(([0-9]|-\+)+)");
                        if (right.Success) {
                            var rightResult = right.Value.Trim();
                            //组合判断是否反转
                            var weightStr = $"{leftResult}.{rightResult}";
                            if (Math.Abs(Convert.ToSingle(weightStr) - ParsedWeight) == 0) {
                                //不用反转
                                isReversed = false;
                            }
                            else {
                                var reversedString = new string(weightStr.Reverse().ToArray());
                                if (Math.Abs(Convert.ToSingle(reversedString) - ParsedWeight) == 0) {
                                    isReversed = true;
                                }
                            }

                            if (isReversed is null) {
                                WeightSettingsMessageQueue.Enqueue($"重量和源内容无法匹配");
                                return;
                            }
                            else {
                                if (isReversed == true) {
                                    if (indexOf > WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal)) {
                                        //标识符在左

                                        decimalStartPosition = WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal) + 1;
                                        decimalEndPosition = indexOf - 1;
                                        integerStartPosition = indexOf + 1;
                                        integerEndPosition = WeightSourceContent.Length - 1;
                                    }
                                    else {
                                        //标识符在右
                                        decimalStartPosition = 0;
                                        decimalEndPosition = indexOf - 1;
                                        integerStartPosition = indexOf + 1;
                                        integerEndPosition = WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal) - 1;
                                    }
                                }
                                else {
                                    //不反转
                                    if (indexOf > WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal)) {
                                        //标识符在左

                                        integerStartPosition = WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal) + 1;
                                        integerEndPosition = indexOf - 1;
                                        decimalStartPosition = indexOf + 1;
                                        decimalEndPosition = WeightSourceContent.Length - 1;
                                    }
                                    else {
                                        //标识符在右
                                        integerStartPosition = 0;
                                        integerEndPosition = indexOf - 1;
                                        decimalStartPosition = indexOf + 1;
                                        decimalEndPosition = WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal) - 1;
                                    }
                                }

                                WeightSettingsInfo.StaticWeight.CharacterLength = WeightSourceContent.Length;

                                WeightSettingsInfo.StaticWeight.Identifier = identifier;
                                WeightSettingsInfo.StaticWeight.IdentifierPosition =
                                    WeightSourceContent.IndexOf(identifier, StringComparison.Ordinal);
                                WeightSettingsInfo.StaticWeight.IsReversed = isReversed ?? false;
                                WeightSettingsInfo.StaticWeight.IntegerStartPosition = integerStartPosition;
                                WeightSettingsInfo.StaticWeight.IntegerEndPosition = integerEndPosition;
                                WeightSettingsInfo.StaticWeight.DecimalStartPosition = decimalStartPosition;
                                WeightSettingsInfo.StaticWeight.DecimalEndPosition = decimalEndPosition;
                                WeightSettingsMessageQueue.Enqueue($"规则解析成功");
                            }
                        }
                        else {
                            WeightSettingsMessageQueue.Enqueue($"匹配不到小数点右边数据");
                        }
                    }
                    else {
                        WeightSettingsMessageQueue.Enqueue($"源内容未找到小数点");
                    }
                    //判断是否反转

                    //获取小数位置(小数最多3位)
                }
                else {
                    WeightSettingsMessageQueue.Enqueue($"源内容不能为空,重量不能等于0");
                }
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }
    }
}