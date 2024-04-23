using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.ObjectModel;
using JayTom.Dws.Interface.Jtexpress;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class JtExpressApiPageViewModel : SettingsPageTemplateViewModel {
        private readonly IHttpClientFactory _httpClientFactory;
        private JtExpressApiModel _jtExpressApiInfo = new();
        private bool _isLoaded;

        private ObservableCollection<StringItemModel> _scanTypeCodeItems = new()
        {
            new StringItemModel()
            {
                Name = "90: 中心到件(转运中心到件)",
                Value = "90"
            },
            new StringItemModel()
            {
                Name = "91: 集货到件-集散出港",
                Value = "91"
            },
            new StringItemModel()
            {
                Name = "92: 到件扫描-集散/网点进港",
                Value = "92"
            },
        };

        private ObservableCollection<StringItemModel> _transportTypeCodeItems = new()
        {
            new StringItemModel()
            {
                Name = "02: 公路运输",
                Value = "02"
            }
        };

        private ObservableCollection<IntegerItemModel> _scanTypeItems = new()
        {
            new IntegerItemModel()
            {
                Name = "按运单",
                Value = 1,
            },
            new IntegerItemModel()
            {
                Name = "按包",
                Value = 2,
            },
            new IntegerItemModel()
            {
                Name = "空",
                Value = 3,
            },
        };

        private ObservableCollection<StringItemModel> _weightFlagItems = new()
        {
            new StringItemModel()
            {
                Name = "手输重",
                Value = "1"
            },
            new StringItemModel()
            {
                Name = "称重",
                Value = "2"
            },
        };

        private ObservableCollection<IntegerItemModel> _typeItems = new()
        {
            new IntegerItemModel()
            {
                Name = "到件扫描",
                Value = 0
            },
            new IntegerItemModel()
            {
                Name = "出仓扫描",
                Value = 1
            },
            new IntegerItemModel()
            {
                Name = "到派一体",
                Value = 2
            },
        };

        private string _networkId = string.Empty;
        private string _networkCode = string.Empty;
        private string _networkName = string.Empty;
        private string _name = string.Empty;
        private bool _isLoginSuccessful;
        private bool _isLoggingIn;

        public JtExpressApiPageViewModel(IConfigRepository configRepository,
            IHttpClientFactory httpClientFactory) : base(configRepository) {
            _httpClientFactory = httpClientFactory;
        }

        public JtExpressApiModel JtExpressApiInfo {
            get => _jtExpressApiInfo;
            set => SetProperty(ref _jtExpressApiInfo, value);
        }

        public ObservableCollection<StringItemModel> ScanTypeCodeItems {
            get => _scanTypeCodeItems;
            set => SetProperty(ref _scanTypeCodeItems, value);
        }

        public ObservableCollection<StringItemModel> TransportTypeCodeItems {
            get => _transportTypeCodeItems;
            set => SetProperty(ref _transportTypeCodeItems, value);
        }

        public ObservableCollection<IntegerItemModel> ScanTypeItems {
            get => _scanTypeItems;
            set => SetProperty(ref _scanTypeItems, value);
        }

        public ObservableCollection<StringItemModel> WeightFlagItems {
            get => _weightFlagItems;
            set => SetProperty(ref _weightFlagItems, value);
        }

        public ObservableCollection<IntegerItemModel> TypeItems {
            get => _typeItems;
            set => SetProperty(ref _typeItems, value);
        }

        /// <summary>
        /// 登录人的网点编码
        /// </summary>
        public string NetworkId {
            get => _networkId;
            set => SetProperty(ref _networkId, value);
        }

        /// <summary>
        /// 网点代码
        /// </summary>
        public string NetworkCode {
            get => _networkCode;
            set => SetProperty(ref _networkCode, value);
        }

        /// <summary>
        /// 登录人的网点名称
        /// </summary>
        public string NetworkName {
            get => _networkName;
            set => SetProperty(ref _networkName, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 是否成功登录
        /// </summary>
        public bool IsLoginSuccessful {
            get => _isLoginSuccessful;
            set => SetProperty(ref _isLoginSuccessful, value);
        }

        /// <summary>
        /// 是否登录中
        /// </summary>
        public bool IsLoggingIn {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public ICommand LogInCommand => new DelegateCommand<object>(LogInDelegate);

        private async void LogInDelegate(object obj) {
            if (!IsLoggingIn) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    IsLoggingIn = true;
                    IsLoginSuccessful = false;
                    //ApiParameter
                    await new JtExpressApi(_httpClientFactory).SetParameters(new JtExpressApi.ApiParameter() {
                        AppKey = JtExpressApiInfo.AppKey,
                        AppSecret = JtExpressApiInfo.AppSecret,
                        BusinessType = (JtExpressApi.BusinessType)JtExpressApiInfo.BusinessType.Value,
                        Password = JtExpressApiInfo.Password,
                        ScanPda = JtExpressApiInfo.ScanPda,
                        SegmentCodeTimeOut = JtExpressApiInfo.SegmentCodeTimeOut,
                        ScanType = JtExpressApiInfo.ScanType.Value,
                        ScanTypeCode = JtExpressApiInfo.ScanTypeCode.Value,
                        SegmentCodeUrl = JtExpressApiInfo.SegmentCodeUrl,
                        TimeOut = JtExpressApiInfo.SegmentCodeTimeOut,
                        TransportTypeCode = JtExpressApiInfo.TransportTypeCode.Value,
                        UserName = JtExpressApiInfo.UserName,
                        Url = JtExpressApiInfo.Url,
                    });
                    var (key, value) = await new JtExpressApi(_httpClientFactory).LogIn(JtExpressApiInfo.UserName,
                        JtExpressApiInfo.Password, JtExpressApiInfo.AppKey,
                        JtExpressApiInfo.AppSecret);

                    if (key) {
                        Name = value.Name;
                        NetworkId = value.NetworkId;
                        NetworkCode = value.NetworkCode;
                        NetworkName = value.NetworkName;
                        base.MessageQueue.Enqueue("登录成功");
                    }
                    else {
                        base.MessageQueue.Enqueue($"登录失败,{value.ExceptionMsg}");
                    }
                    IsLoginSuccessful = key;
                    IsLoggingIn = false;
                });
            }
        }

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var settingsDto = await _configRepository.FirstOrDefaultEntity<JtExpressDto>(SettingsName) ?? new JtExpressDto();

                    JtExpressApiInfo = new JtExpressApiModel() {
                        AppKey = settingsDto.AppKey,
                        AppSecret = settingsDto.AppSecret,
                        Url = settingsDto.Url,
                        UserName = settingsDto.UserName,
                        Password = settingsDto.Password,
                        ScanPda = settingsDto.ScanPda,
                        ScanType = ScanTypeItems.FirstOrDefault(f => f.Value.Equals(settingsDto.ScanType)) ??
                                   new IntegerItemModel(),
                        BusinessType =
                            TypeItems.FirstOrDefault(f => f.Value.Equals((int)settingsDto.BusinessType)) ??
                            new IntegerItemModel(),
                        ScanTypeCode =
                            ScanTypeCodeItems.FirstOrDefault(f => f.Value.Equals(settingsDto.ScanTypeCode)) ??
                            new StringItemModel(),
                        SegmentCodeTimeOut = settingsDto.SegmentCodeTimeOut,
                        SegmentCodeUrl = settingsDto.SegmentCodeUrl,
                        WeightFlag =
                            WeightFlagItems.FirstOrDefault(f => f.Value.Equals(settingsDto.WeightFlag)) ??
                            new StringItemModel(),
                        TimeOut = settingsDto.TimeOut,
                        TransportTypeCode =
                            TransportTypeCodeItems.FirstOrDefault(f =>
                                f.Value.Equals(settingsDto.TransportTypeCode)) ?? new StringItemModel(),
                        IsUploadAfterReturn = settingsDto.IsUploadAfterReturn,
                    };
                });
            }
        }

        public override string Identifier => "JtExpressApiParametersDialogHost";
        public override string SettingsName => "JtExpressApiParameters";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new JtExpressDto() {
                    AppKey = JtExpressApiInfo.AppKey,
                    AppSecret = JtExpressApiInfo.AppSecret,
                    Url = JtExpressApiInfo.Url,
                    UserName = JtExpressApiInfo.UserName,
                    Password = JtExpressApiInfo.Password,
                    ScanPda = JtExpressApiInfo.ScanPda,
                    ScanType = JtExpressApiInfo.ScanType.Value,
                    BusinessType = (BusinessType)JtExpressApiInfo.BusinessType.Value,
                    ScanTypeCode = JtExpressApiInfo.ScanTypeCode.Value,
                    SegmentCodeTimeOut = JtExpressApiInfo.SegmentCodeTimeOut,
                    SegmentCodeUrl = JtExpressApiInfo.SegmentCodeUrl,
                    WeightFlag = JtExpressApiInfo.WeightFlag.Value,
                    TimeOut = JtExpressApiInfo.TimeOut,
                    TransportTypeCode = JtExpressApiInfo.TransportTypeCode.Value,
                    IsUploadAfterReturn = JtExpressApiInfo.IsUploadAfterReturn
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }
    }
}