using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Interface.Jtexpress;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    /// <summary>
    /// 极兔极昼接口配置页面模型。
    /// </summary>
    public sealed class JtPolarDayApiPageViewModel :
        SettingsPageTemplateViewModel {
        /// <summary>
        /// HTTP 客户端工厂。
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 对话框服务。
        /// </summary>
        private readonly IDialogService _dialogService;

        /// <summary>
        /// 当前配置模型。
        /// </summary>
        private JtPolarDayApiModel _polarDayApiInfo = new();

        /// <summary>
        /// 页面是否已经加载。
        /// </summary>
        private bool _isLoaded;

        /// <summary>
        /// 目标格口查询测试并发门闩。
        /// </summary>
        private int _uploadingGate;

        /// <summary>
        /// 是否正在执行目标格口查询测试。
        /// </summary>
        private bool _isUploading;

        /// <summary>
        /// 测试条码。
        /// </summary>
        private string _barcode = string.Empty;

        /// <summary>
        /// 测试重量。
        /// </summary>
        private decimal _weight;

        /// <summary>
        /// 初始化极昼配置页面模型。
        /// </summary>
        /// <param name="configRepository">配置仓储。</param>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        /// <param name="dialogService">对话框服务。</param>
        public JtPolarDayApiPageViewModel(
            IConfigRepository configRepository,
            IHttpClientFactory httpClientFactory,
            IDialogService dialogService) : base(configRepository) {
            _httpClientFactory = httpClientFactory ??
                                 throw new ArgumentNullException(
                                     nameof(httpClientFactory));
            _dialogService = dialogService ??
                             throw new ArgumentNullException(
                                 nameof(dialogService));
            UploadCommand = new DelegateCommand(UploadDelegate);
        }

        /// <summary>
        /// 当前配置模型。
        /// </summary>
        public JtPolarDayApiModel PolarDayApiInfo {
            get => _polarDayApiInfo;
            set => SetProperty(ref _polarDayApiInfo, value);
        }

        /// <summary>
        /// 是否正在执行目标格口查询测试。
        /// </summary>
        public bool IsUploading {
            get => _isUploading;
            private set => SetProperty(ref _isUploading, value);
        }

        /// <summary>
        /// 测试条码。
        /// </summary>
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 测试重量，单位千克。
        /// </summary>
        public decimal Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 执行目标格口查询测试的命令。
        /// </summary>
        public ICommand UploadCommand { get; }

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
        public override async void LoadedDelegate(object parameter) {
            if (_isLoaded) {
                return;
            }

            _isLoaded = true;
            var settings = await _configRepository
                .FirstOrDefaultEntity<JtPolarDayDto>(SettingsName) ??
                           new JtPolarDayDto();
            PolarDayApiInfo = new JtPolarDayApiModel {
                BaseUrl = settings.BaseUrl,
                AppKey = settings.AppKey,
                AppSecret = settings.AppSecret,
                EquipmentCode = settings.EquipmentCode,
                SortingPlanCode = settings.SortingPlanCode,
                OperateType = settings.OperateType,
                Operator = settings.Operator,
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
        protected override async Task<bool> SaveSettingsProcess() {
            var settings = new JtPolarDayDto {
                BaseUrl = PolarDayApiInfo.BaseUrl,
                AppKey = PolarDayApiInfo.AppKey,
                AppSecret = PolarDayApiInfo.AppSecret,
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
                new ConfigInfoModel {
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
        private async void UploadDelegate() {
            if (Interlocked.CompareExchange(
                    ref _uploadingGate,
                    1,
                    0) != 0) {
                return;
            }

            IsUploading = true;
            try {
                if (string.IsNullOrWhiteSpace(Barcode)) {
                    MessageQueue.Enqueue("请输入测试条码");
                    return;
                }

                if (Weight < 0) {
                    MessageQueue.Enqueue("测试重量不能小于零");
                    return;
                }

                var polarDayApi =
                    new JtPolarDayApi(_httpClientFactory);
                var parameterResult = await polarDayApi.SetParameters(
                    CreateApiParameter());
                if (!parameterResult.Key) {
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
            catch (Exception exception) {
                MessageQueue.Enqueue(
                    $"极昼目标格口查询失败：{exception.Message}");
            }
            finally {
                IsUploading = false;
                Volatile.Write(ref _uploadingGate, 0);
            }
        }

        /// <summary>
        /// 根据页面当前输入创建极昼接口参数。
        /// </summary>
        /// <returns>极昼接口参数。</returns>
        private JtPolarDayApi.ApiParameter CreateApiParameter() {
            return new JtPolarDayApi.ApiParameter {
                BaseUrl = PolarDayApiInfo.BaseUrl,
                AppKey = PolarDayApiInfo.AppKey,
                AppSecret = PolarDayApiInfo.AppSecret,
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
    }
}
