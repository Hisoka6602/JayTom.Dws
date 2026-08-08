using Prism.Mvvm;
using JayTom.Dws.Interface.Jtexpress;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel
{

    /// <summary>
    /// 极兔极昼接口配置模型。
    /// </summary>
    public sealed class JtPolarDayApiModel : BindableBase
    {
        /// <summary>
        /// 极昼服务基础地址。
        /// </summary>
        private string _baseUrl =
            JtPolarDayApi.ProductionBaseUrl;

        /// <summary>
        /// 应用标识。
        /// </summary>
        private string _appKey = string.Empty;

        /// <summary>
        /// 应用密钥。
        /// </summary>
        private string _appSecret = string.Empty;

        /// <summary>
        /// 极昼图片服务基础地址。
        /// </summary>
        private string _imageServiceBaseUrl =
            JtPolarDayApi.DefaultImageServiceBaseUrl;

        /// <summary>
        /// 极昼图片服务登录账号。
        /// </summary>
        private string _imageAccount = string.Empty;

        /// <summary>
        /// 极昼图片服务登录密码。
        /// </summary>
        private string _imagePassword = string.Empty;

        /// <summary>
        /// 极昼图片服务应用标识。
        /// </summary>
        private string _imageAppKey = string.Empty;

        /// <summary>
        /// 极昼图片服务应用密钥。
        /// </summary>
        private string _imageAppSecret = string.Empty;

        /// <summary>
        /// 图片关联的扫描类型。
        /// </summary>
        private int _imageScanType = JtPolarDayApi.DefaultImageScanType;

        /// <summary>
        /// 图片服务超时毫秒数。
        /// </summary>
        private int _imageUploadTimeoutMilliseconds = 10000;

        /// <summary>
        /// 回传场地编码。
        /// </summary>
        private string _siteCode = JtPolarDayApi.DefaultSiteCode;

        /// <summary>
        /// 设备编号。
        /// </summary>
        private string _equipmentCode = JtPolarDayApi.DefaultEquipmentCode;

        /// <summary>
        /// 分拣计划编码。
        /// </summary>
        private string _sortingPlanCode =
            JtPolarDayApi.DefaultSortingPlanCode;

        /// <summary>
        /// 操作类型。
        /// </summary>
        private int _operateType = 1;

        /// <summary>
        /// 操作员。
        /// </summary>
        private string _operator = JtPolarDayApi.DefaultOperator;

        /// <summary>
        /// 格口查询使用的可选主线编码。
        /// </summary>
        private string _mainLineCode = string.Empty;

        /// <summary>
        /// 设备实际层数。
        /// </summary>
        private int _equipmentLayer = 1;

        /// <summary>
        /// 设备实际供件区数量。
        /// </summary>
        private int _areaNum = 1;

        /// <summary>
        /// 设备允许的最大循环圈数。
        /// </summary>
        private int _maxCircleNum = 1;

        /// <summary>
        /// 供件台编号。
        /// </summary>
        private string _supplyDeskCode = string.Empty;

        /// <summary>
        /// 供件台序号。
        /// </summary>
        private string _supplyDeskSerialNo = "1";

        /// <summary>
        /// 供件方式。
        /// </summary>
        private string _supplyDeskMethod = "1";

        /// <summary>
        /// 供件台所属供件区。
        /// </summary>
        private string _supplyDeskArea = string.Empty;

        /// <summary>
        /// 供件台所在层数。
        /// </summary>
        private int _layerNum = 1;

        /// <summary>
        /// 落格模式。
        /// </summary>
        private string _chuteModel = "1";

        /// <summary>
        /// 默认实际落格供件区编号。
        /// </summary>
        private int _fallArea = 1;

        /// <summary>
        /// 重量来源。
        /// </summary>
        private string _weightSource = "0";

        /// <summary>
        /// 格口查询超时毫秒数。
        /// </summary>
        private int _queryTimeoutMilliseconds = 800;

        /// <summary>
        /// 数据上报超时毫秒数。
        /// </summary>
        private int _timeoutMilliseconds = 1000;

        /// <summary>
        /// 最大请求次数。
        /// </summary>
        private int _retryCount = 3;

        /// <summary>
        /// 重试间隔毫秒数。
        /// </summary>
        private int _retryIntervalMilliseconds = 100;

        /// <summary>
        /// 极昼服务基础地址。
        /// </summary>
        public string BaseUrl
        {
            get => _baseUrl;
            set => SetProperty(ref _baseUrl, value);
        }

        /// <summary>
        /// 应用标识。
        /// </summary>
        public string AppKey
        {
            get => _appKey;
            set => SetProperty(ref _appKey, value);
        }

        /// <summary>
        /// 应用密钥。
        /// </summary>
        public string AppSecret
        {
            get => _appSecret;
            set => SetProperty(ref _appSecret, value);
        }

        /// <summary>
        /// 极昼图片服务基础地址。
        /// </summary>
        public string ImageServiceBaseUrl
        {
            get => _imageServiceBaseUrl;
            set => SetProperty(ref _imageServiceBaseUrl, value);
        }

        /// <summary>
        /// 极昼图片服务登录账号。
        /// </summary>
        public string ImageAccount
        {
            get => _imageAccount;
            set => SetProperty(ref _imageAccount, value);
        }

        /// <summary>
        /// 极昼图片服务登录密码。
        /// </summary>
        public string ImagePassword
        {
            get => _imagePassword;
            set => SetProperty(ref _imagePassword, value);
        }

        /// <summary>
        /// 极昼图片服务应用标识。
        /// </summary>
        public string ImageAppKey
        {
            get => _imageAppKey;
            set => SetProperty(ref _imageAppKey, value);
        }

        /// <summary>
        /// 极昼图片服务应用密钥。
        /// </summary>
        public string ImageAppSecret
        {
            get => _imageAppSecret;
            set => SetProperty(ref _imageAppSecret, value);
        }

        /// <summary>
        /// 图片关联的扫描类型。
        /// </summary>
        public int ImageScanType
        {
            get => _imageScanType;
            set => SetProperty(ref _imageScanType, value);
        }

        /// <summary>
        /// 图片登录、链接申请和二进制上传超时毫秒数。
        /// </summary>
        public int ImageUploadTimeoutMilliseconds
        {
            get => _imageUploadTimeoutMilliseconds;
            set => SetProperty(ref _imageUploadTimeoutMilliseconds, value);
        }

        /// <summary>
        /// 回传场地编码。
        /// </summary>
        public string SiteCode
        {
            get => _siteCode;
            set => SetProperty(ref _siteCode, value);
        }

        /// <summary>
        /// 设备编号。
        /// </summary>
        public string EquipmentCode
        {
            get => _equipmentCode;
            set => SetProperty(ref _equipmentCode, value);
        }

        /// <summary>
        /// 分拣计划编码。
        /// </summary>
        public string SortingPlanCode
        {
            get => _sortingPlanCode;
            set => SetProperty(ref _sortingPlanCode, value);
        }

        /// <summary>
        /// 操作类型，1 出港、2 进港、3 进出港。
        /// </summary>
        public int OperateType
        {
            get => _operateType;
            set => SetProperty(ref _operateType, value);
        }

        /// <summary>
        /// 操作员 JMS 账号。
        /// </summary>
        public string Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }

        /// <summary>
        /// 格口查询使用的可选主线编码。
        /// </summary>
        public string MainLineCode
        {
            get => _mainLineCode;
            set => SetProperty(ref _mainLineCode, value);
        }

        /// <summary>
        /// 设备实际层数。
        /// </summary>
        public int EquipmentLayer
        {
            get => _equipmentLayer;
            set => SetProperty(ref _equipmentLayer, value);
        }

        /// <summary>
        /// 设备实际供件区数量。
        /// </summary>
        public int AreaNum
        {
            get => _areaNum;
            set => SetProperty(ref _areaNum, value);
        }

        /// <summary>
        /// 设备允许的最大循环圈数。
        /// </summary>
        public int MaxCircleNum
        {
            get => _maxCircleNum;
            set => SetProperty(ref _maxCircleNum, value);
        }

        /// <summary>
        /// 供件台编号；无供件台时填写供件区编号。
        /// </summary>
        public string SupplyDeskCode
        {
            get => _supplyDeskCode;
            set => SetProperty(ref _supplyDeskCode, value);
        }

        /// <summary>
        /// 供件台在当前供件区内的连续序号。
        /// </summary>
        public string SupplyDeskSerialNo
        {
            get => _supplyDeskSerialNo;
            set => SetProperty(ref _supplyDeskSerialNo, value);
        }

        /// <summary>
        /// 供件方式，1 供包台、2 补码台、3 自动供包、4 人工供包、5 快手供件。
        /// </summary>
        public string SupplyDeskMethod
        {
            get => _supplyDeskMethod;
            set => SetProperty(ref _supplyDeskMethod, value);
        }

        /// <summary>
        /// 供件台所属供件区。
        /// </summary>
        public string SupplyDeskArea
        {
            get => _supplyDeskArea;
            set => SetProperty(ref _supplyDeskArea, value);
        }

        /// <summary>
        /// 供件台所在层数。
        /// </summary>
        public int LayerNum
        {
            get => _layerNum;
            set => SetProperty(ref _layerNum, value);
        }

        /// <summary>
        /// 落格模式，1 就近、2 循环、3 瀑布、4 随机。
        /// </summary>
        public string ChuteModel
        {
            get => _chuteModel;
            set => SetProperty(ref _chuteModel, value);
        }

        /// <summary>
        /// 默认实际落格供件区编号。
        /// </summary>
        public int FallArea
        {
            get => _fallArea;
            set => SetProperty(ref _fallArea, value);
        }

        /// <summary>
        /// 重量来源，0 秤、1 系统或默认值。
        /// </summary>
        public string WeightSource
        {
            get => _weightSource;
            set => SetProperty(ref _weightSource, value);
        }

        /// <summary>
        /// 格口查询超时毫秒数。
        /// </summary>
        public int QueryTimeoutMilliseconds
        {
            get => _queryTimeoutMilliseconds;
            set => SetProperty(ref _queryTimeoutMilliseconds, value);
        }

        /// <summary>
        /// 数据上报超时毫秒数。
        /// </summary>
        public int TimeoutMilliseconds
        {
            get => _timeoutMilliseconds;
            set => SetProperty(ref _timeoutMilliseconds, value);
        }

        /// <summary>
        /// 最大请求次数。
        /// </summary>
        public int RetryCount
        {
            get => _retryCount;
            set => SetProperty(ref _retryCount, value);
        }

        /// <summary>
        /// 重试间隔毫秒数。
        /// </summary>
        public int RetryIntervalMilliseconds
        {
            get => _retryIntervalMilliseconds;
            set => SetProperty(ref _retryIntervalMilliseconds, value);
        }
    }
}
