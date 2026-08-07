using JayTom.Dws.Client.Models;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Interface.Jtexpress;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    /// <summary>
    /// 极兔极昼接口配置页面模型。
    /// </summary>
    public sealed class JtPolarDayApiPageViewModel :
        SettingsPageTemplateViewModel
    {
        /// <summary>
        /// HTTP 客户端工厂。
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 对话框服务。
        /// </summary>
        private readonly IDialogService _dialogService;

        /// <summary>
        /// 页面是否已经加载。
        /// </summary>
        private bool _isLoaded;

        /// <summary>
        /// 目标格口查询测试并发门闩。
        /// </summary>
        private int _uploadingGate;

        /// <summary>
        /// 旧版回传测试并发门闩。
        /// </summary>
        private int _legacyTestingGate;

        /// <summary>
        /// 当前配置模型。
        /// </summary>
        private JtPolarDayApiModel _polarDayApiInfo = new();

        /// <summary>
        /// 初始化极昼配置页面模型。
        /// </summary>
        /// <param name="configRepository">配置仓储。</param>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        /// <param name="dialogService">对话框服务。</param>
        public JtPolarDayApiPageViewModel(
            IConfigRepository configRepository,
            IHttpClientFactory httpClientFactory,
            IDialogService dialogService) : base(configRepository)
        {
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(dialogService);
            _httpClientFactory = httpClientFactory;
            _dialogService = dialogService;
            UploadCommand = new DelegateCommand(UploadDelegate);
            LegacyTestUploadCommand =
                new DelegateCommand(
                    () => _ = LegacyTestUploadAsync());
            _polarDayApiInfo.PropertyChanged +=
                PolarDayApiInfoPropertyChanged;
            RefreshOperateTypeItems();
        }

        /// <summary>
        /// 当前配置模型。
        /// </summary>
        public JtPolarDayApiModel PolarDayApiInfo
        {
            get => _polarDayApiInfo;
            set
            {
                if (ReferenceEquals(_polarDayApiInfo, value))
                {
                    return;
                }

                _polarDayApiInfo.PropertyChanged -=
                    PolarDayApiInfoPropertyChanged;
                if (SetProperty(ref _polarDayApiInfo, value))
                {
                    _polarDayApiInfo.PropertyChanged +=
                        PolarDayApiInfoPropertyChanged;
                    RefreshOperateTypeItems();
                }
            }
        }

        /// <summary>
        /// 当前协议允许的操作类型。
        /// </summary>
        public ObservableCollection<IntegerItemModel> OperateTypeItems
        {
            get;
        } = [];

        /// <summary>
        /// 供件方式选项。
        /// </summary>
        public ObservableCollection<StringItemModel> SupplyDeskMethodItems
        {
            get;
        } =
        [
            new() { Name = "1：供包台", Value = "1" },
            new() { Name = "2：补码台", Value = "2" },
            new() { Name = "3：自动供包", Value = "3" },
            new() { Name = "4：人工供包", Value = "4" },
            new() { Name = "5：快手供件", Value = "5" }
        ];

        /// <summary>
        /// 重量来源选项。
        /// </summary>
        public ObservableCollection<StringItemModel> WeightSourceItems
        {
            get;
        } =
        [
            new() { Name = "0：称重获取", Value = "0" },
            new() { Name = "1：系统配置值", Value = "1" }
        ];

        /// <summary>
        /// 落格模式选项。
        /// </summary>
        public ObservableCollection<StringItemModel> ChuteModelItems
        {
            get;
        } =
        [
            new() { Name = "1：就近模式", Value = "1" },
            new() { Name = "2：循环模式", Value = "2" },
            new() { Name = "3：瀑布模式", Value = "3" },
            new() { Name = "4：随机模式", Value = "4" }
        ];

        /// <summary>
        /// 是否正在执行目标格口查询测试。
        /// </summary>
        public bool IsUploading
        {
            get;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 是否正在执行旧版测试回传。
        /// </summary>
        public bool IsLegacyTesting
        {
            get;
            private set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 测试条码。
        /// </summary>
        public string Barcode
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 测试重量，单位千克。
        /// </summary>
        public decimal Weight
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 旧版回传测试运单号。
        /// </summary>
        public string LegacyTestBarcode
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 旧版回传测试重量，单位千克。
        /// </summary>
        public decimal LegacyTestWeight
        {
            get;
            set => SetProperty(ref field, value);
        } = 1m;

        /// <summary>
        /// 执行目标格口查询测试的命令。
        /// </summary>
        public ICommand UploadCommand { get; }

        /// <summary>
        /// 执行旧版小件测试回传的命令。
        /// </summary>
        public ICommand LegacyTestUploadCommand { get; }

        /// <summary>
        /// 对话框标识。
        /// </summary>
        public override string Identifier =>
            "JtPolarDayApiParametersDialogHost";

        /// <summary>
        /// 配置名称。
        /// </summary>
        public override string SettingsName => "JtPolarDayApiParameters";

        /// <summary>
        /// 页面加载时读取配置。
        /// </summary>
        /// <param name="parameter">页面参数。</param>
        public override async void LoadedDelegate(object parameter)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            var settings = await _configRepository
                .FirstOrDefaultEntity<JtPolarDayDto>(SettingsName) ??
                           new JtPolarDayDto();
            PolarDayApiInfo = new JtPolarDayApiModel
            {
                BaseUrl = JtPolarDayApi.NormalizeProductionBaseUrl(
                    settings.BaseUrl),
                AppKey = settings.AppKey,
                AppSecret = settings.AppSecret,
                UseLegacyUpload = settings.UseLegacyUpload,
                LegacyUploadUrl = JtPolarDayApi
                    .NormalizeLegacyProductionUrl(
                        settings.LegacyUploadUrl),
                LegacyAppKey = settings.LegacyAppKey,
                LegacyAppSecret = settings.LegacyAppSecret,
                SiteCode = DefaultIfBlank(
                    settings.SiteCode,
                    JtPolarDayApi.DefaultSiteCode),
                CrossBeltMac = settings.CrossBeltMac,
                SupplyDeskMac = settings.SupplyDeskMac,
                EquipmentCode = DefaultIfBlank(
                    settings.EquipmentCode,
                    JtPolarDayApi.DefaultEquipmentCode),
                SortingPlanCode = DefaultIfBlank(
                    settings.SortingPlanCode,
                    JtPolarDayApi.DefaultSortingPlanCode),
                OperateType = settings.OperateType,
                Operator = DefaultIfBlank(
                    settings.Operator,
                    JtPolarDayApi.DefaultOperator),
                MainLineCode = settings.MainLineCode,
                EquipmentLayer = settings.EquipmentLayer,
                AreaNum = settings.AreaNum,
                MaxCircleNum = settings.MaxCircleNum,
                SupplyDeskCode = settings.SupplyDeskCode,
                SupplyDeskSerialNo = settings.SupplyDeskSerialNo,
                SupplyDeskMethod = settings.SupplyDeskMethod,
                SupplyDeskArea = settings.SupplyDeskArea,
                LayerNum = settings.LayerNum,
                ChuteModel = settings.ChuteModel,
                FallArea = settings.FallArea,
                WeightSource = settings.WeightSource,
                QueryTimeoutMilliseconds =
                    settings.QueryTimeoutMilliseconds,
                TimeoutMilliseconds = settings.TimeoutMilliseconds,
                RetryCount = settings.RetryCount,
                RetryIntervalMilliseconds =
                    settings.RetryIntervalMilliseconds
            };
        }

        /// <summary>
        /// 保存极昼配置。
        /// </summary>
        /// <returns>是否保存成功。</returns>
        protected override async Task<bool> SaveSettingsProcess()
        {
            var settings = new JtPolarDayDto
            {
                BaseUrl = PolarDayApiInfo.BaseUrl,
                AppKey = PolarDayApiInfo.AppKey,
                AppSecret = PolarDayApiInfo.AppSecret,
                UseLegacyUpload = PolarDayApiInfo.UseLegacyUpload,
                LegacyUploadUrl = PolarDayApiInfo.LegacyUploadUrl,
                LegacyAppKey = PolarDayApiInfo.LegacyAppKey,
                LegacyAppSecret = PolarDayApiInfo.LegacyAppSecret,
                SiteCode = PolarDayApiInfo.SiteCode,
                NetworkCode = PolarDayApiInfo.SiteCode,
                CrossBeltMac = PolarDayApiInfo.CrossBeltMac,
                SupplyDeskMac = PolarDayApiInfo.SupplyDeskMac,
                EquipmentCode = PolarDayApiInfo.EquipmentCode,
                SortingPlanCode = PolarDayApiInfo.SortingPlanCode,
                OperateType = PolarDayApiInfo.OperateType,
                Operator = PolarDayApiInfo.Operator,
                MainLineCode = PolarDayApiInfo.MainLineCode,
                EquipmentLayer = PolarDayApiInfo.EquipmentLayer,
                AreaNum = PolarDayApiInfo.AreaNum,
                MaxCircleNum = PolarDayApiInfo.MaxCircleNum,
                SupplyDeskCode = PolarDayApiInfo.SupplyDeskCode,
                SupplyDeskSerialNo =
                    PolarDayApiInfo.SupplyDeskSerialNo,
                SupplyDeskMethod = PolarDayApiInfo.SupplyDeskMethod,
                SupplyDeskArea = PolarDayApiInfo.SupplyDeskArea,
                LayerNum = PolarDayApiInfo.LayerNum,
                ChuteModel = PolarDayApiInfo.ChuteModel,
                FallArea = PolarDayApiInfo.FallArea,
                WeightSource = PolarDayApiInfo.WeightSource,
                QueryTimeoutMilliseconds =
                    PolarDayApiInfo.QueryTimeoutMilliseconds,
                TimeoutMilliseconds =
                    PolarDayApiInfo.TimeoutMilliseconds,
                RetryCount = PolarDayApiInfo.RetryCount,
                RetryIntervalMilliseconds =
                    PolarDayApiInfo.RetryIntervalMilliseconds
            };
            var succeeded = await _configRepository.InsertOrUpdate(
                new ConfigInfoModel
                {
                    ConfigName = SettingsName,
                    Value = JsonConvert.SerializeObject(settings)
                });
            MessageQueue.Enqueue(
                succeeded
                    ? "极昼接口配置保存成功"
                    : "极昼接口配置保存失败");
            return succeeded;
        }

        /// <summary>
        /// 使用页面中的即时配置执行一次真实目标格口查询。
        /// </summary>
        private async void UploadDelegate()
        {
            if (Interlocked.CompareExchange(
                    ref _uploadingGate,
                    1,
                    0) != 0)
            {
                return;
            }

            IsUploading = true;
            try
            {
                if (string.IsNullOrWhiteSpace(Barcode))
                {
                    MessageQueue.Enqueue("请输入测试条码");
                    return;
                }

                if (Weight < 0)
                {
                    MessageQueue.Enqueue("测试重量不能小于零");
                    return;
                }

                var polarDayApi =
                    new JtPolarDayApi(_httpClientFactory);
                var parameterResult = await polarDayApi.SetParameters(
                    CreateApiParameter());
                if (!parameterResult.Key)
                {
                    MessageQueue.Enqueue(parameterResult.Value);
                    return;
                }

                var uploadResponse =
                    await polarDayApi.UploadFixedPointData(
                        Barcode.Trim(),
                        Weight);
                _dialogService.ShowDialog(
                    "ApiTestDialog",
                    new DialogParameters {
                        { "UploadResponse", uploadResponse }
                    },
                    null);
            }
            catch (Exception exception)
            {
                MessageQueue.Enqueue(
                    $"极昼目标格口查询失败：{exception.Message}");
            }
            finally
            {
                IsUploading = false;
                Volatile.Write(ref _uploadingGate, 0);
            }
        }

        /// <summary>
        /// 使用页面中的即时配置执行一次旧版小件真实回传。
        /// </summary>
        private async Task LegacyTestUploadAsync()
        {
            if (Interlocked.CompareExchange(
                    ref _legacyTestingGate,
                    1,
                    0) != 0)
            {
                return;
            }

            IsLegacyTesting = true;
            try
            {
                if (!PolarDayApiInfo.UseLegacyUpload)
                {
                    MessageQueue.Enqueue("请先选择旧版小件回传");
                    return;
                }

                if (string.IsNullOrWhiteSpace(LegacyTestBarcode))
                {
                    MessageQueue.Enqueue("请输入旧版测试运单号");
                    return;
                }

                if (LegacyTestWeight <= 0)
                {
                    MessageQueue.Enqueue("旧版测试重量必须大于零");
                    return;
                }

                var polarDayApi =
                    new JtPolarDayApi(_httpClientFactory);
                var parameterResult = await polarDayApi.SetParameters(
                    CreateApiParameter());
                if (!parameterResult.Key)
                {
                    MessageQueue.Enqueue(parameterResult.Value);
                    return;
                }

                var uploadResponse = await polarDayApi
                    .TestLegacySmallItemUploadAsync(
                        LegacyTestBarcode.Trim(),
                        LegacyTestWeight);
                _dialogService.ShowDialog(
                    "ApiTestDialog",
                    new DialogParameters {
                        { "UploadResponse", uploadResponse }
                    },
                    null);
            }
            catch (Exception exception)
            {
                MessageQueue.Enqueue(
                    $"极昼旧版测试回传失败：{exception.Message}");
            }
            finally
            {
                IsLegacyTesting = false;
                Volatile.Write(ref _legacyTestingGate, 0);
            }
        }

        /// <summary>
        /// 根据页面当前输入创建极昼接口参数。
        /// </summary>
        /// <returns>极昼接口参数。</returns>
        private JtPolarDayApi.ApiParameter CreateApiParameter()
        {
            return new JtPolarDayApi.ApiParameter
            {
                BaseUrl = PolarDayApiInfo.BaseUrl,
                AppKey = PolarDayApiInfo.AppKey,
                AppSecret = PolarDayApiInfo.AppSecret,
                UseLegacyUpload = PolarDayApiInfo.UseLegacyUpload,
                LegacyUploadUrl = PolarDayApiInfo.LegacyUploadUrl,
                LegacyAppKey = PolarDayApiInfo.LegacyAppKey,
                LegacyAppSecret = PolarDayApiInfo.LegacyAppSecret,
                SiteCode = PolarDayApiInfo.SiteCode,
                CrossBeltMac = PolarDayApiInfo.CrossBeltMac,
                SupplyDeskMac = PolarDayApiInfo.SupplyDeskMac,
                EquipmentCode = PolarDayApiInfo.EquipmentCode,
                SortingPlanCode = PolarDayApiInfo.SortingPlanCode,
                OperateType = PolarDayApiInfo.OperateType,
                Operator = PolarDayApiInfo.Operator,
                MainLineCode = PolarDayApiInfo.MainLineCode,
                EquipmentLayer = PolarDayApiInfo.EquipmentLayer,
                AreaNum = PolarDayApiInfo.AreaNum,
                MaxCircleNum = PolarDayApiInfo.MaxCircleNum,
                SupplyDeskCode = PolarDayApiInfo.SupplyDeskCode,
                SupplyDeskSerialNo =
                    PolarDayApiInfo.SupplyDeskSerialNo,
                SupplyDeskMethod = PolarDayApiInfo.SupplyDeskMethod,
                SupplyDeskArea = PolarDayApiInfo.SupplyDeskArea,
                LayerNum = PolarDayApiInfo.LayerNum,
                ChuteModel = PolarDayApiInfo.ChuteModel,
                FallArea = PolarDayApiInfo.FallArea,
                WeightSource = PolarDayApiInfo.WeightSource,
                QueryTimeoutMilliseconds =
                    PolarDayApiInfo.QueryTimeoutMilliseconds,
                TimeoutMilliseconds =
                    PolarDayApiInfo.TimeoutMilliseconds,
                RetryCount = PolarDayApiInfo.RetryCount,
                RetryIntervalMilliseconds =
                    PolarDayApiInfo.RetryIntervalMilliseconds
            };
        }

        /// <summary>
        /// 配置模型属性变化时同步协议相关选项。
        /// </summary>
        private void PolarDayApiInfoPropertyChanged(
            object? sender,
            PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName ==
                nameof(JtPolarDayApiModel.UseLegacyUpload))
            {
                RefreshOperateTypeItems();
            }
        }

        /// <summary>
        /// 根据新旧回传协议刷新可选操作类型。
        /// </summary>
        private void RefreshOperateTypeItems()
        {
            if (OperateTypeItems.Count == 0)
            {
                OperateTypeItems.Add(
                    new IntegerItemModel {
                        Name = "1：出港",
                        Value = 1
                    });
                OperateTypeItems.Add(
                    new IntegerItemModel {
                        Name = "2：进港",
                        Value = 2
                    });
            }

            var combinedType = OperateTypeItems
                .FirstOrDefault(item => item.Value == 3);
            if (PolarDayApiInfo.UseLegacyUpload)
            {
                if (PolarDayApiInfo.OperateType == 3)
                {
                    PolarDayApiInfo.OperateType = 1;
                }

                if (combinedType is not null)
                {
                    OperateTypeItems.Remove(combinedType);
                }

                return;
            }

            if (combinedType is null)
            {
                OperateTypeItems.Add(
                    new IntegerItemModel {
                        Name = "3：进出港",
                        Value = 3
                    });
            }
        }

        /// <summary>
        /// 空配置使用指定默认值，非空配置保持不变。
        /// </summary>
        private static string DefaultIfBlank(
            string? value,
            string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value;
        }
    }
}
