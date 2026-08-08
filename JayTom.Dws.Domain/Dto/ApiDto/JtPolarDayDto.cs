namespace JayTom.Dws.Domain.Dto.ApiDto {

    /// <summary>
    /// 极兔极昼接口配置。
    /// </summary>
    public sealed class JtPolarDayDto {
        /// <summary>
        /// 极昼服务基础地址。
        /// </summary>
        public string BaseUrl { get; set; } =
            "https://sdsonline.jtexpress.com.cn/sdsOnlineApi";

        /// <summary>
        /// 应用标识。
        /// </summary>
        public string AppKey { get; set; } = string.Empty;

        /// <summary>
        /// 应用密钥。
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// 极昼图片服务基础地址。
        /// </summary>
        public string ImageServiceBaseUrl { get; set; } =
            "https://opa.jtexpress.com.cn";

        /// <summary>
        /// 极昼图片服务登录账号。
        /// </summary>
        public string ImageAccount { get; set; } = string.Empty;

        /// <summary>
        /// 极昼图片服务登录密码（保存原文，调用时按文档计算 MD5）。
        /// </summary>
        public string ImagePassword { get; set; } = string.Empty;

        /// <summary>
        /// 极昼图片服务应用标识。
        /// </summary>
        public string ImageAppKey { get; set; } = string.Empty;

        /// <summary>
        /// 极昼图片服务应用密钥。
        /// </summary>
        public string ImageAppSecret { get; set; } = string.Empty;

        /// <summary>
        /// 图片关联的扫描类型，默认 104（出仓）。
        /// </summary>
        public int ImageScanType { get; set; } = 104;

        /// <summary>
        /// 图片登录、链接申请和二进制上传超时毫秒数。
        /// </summary>
        public int ImageUploadTimeoutMilliseconds { get; set; } = 10000;

        /// <summary>
        /// 新版回传场地编码。
        /// </summary>
        public string SiteCode { get; set; } = "6398155";

        /// <summary>
        /// 兼容旧配置的 networkCode；保存时与场地编码保持一致。
        /// </summary>
        public string NetworkCode { get; set; } = "6398155";

        /// <summary>
        /// 设备编号。
        /// </summary>
        public string EquipmentCode { get; set; } = "ZXJCD6398155001";

        /// <summary>
        /// 分拣计划编码。
        /// </summary>
        public string SortingPlanCode { get; set; } = "6398155-001";

        /// <summary>
        /// 操作类型，1 出港、2 进港、3 进出港。
        /// </summary>
        public int OperateType { get; set; } = 1;

        /// <summary>
        /// 操作员 JMS 账号。
        /// </summary>
        public string Operator { get; set; } = "LS6398155001";

        /// <summary>
        /// 格口查询使用的可选主线编码。
        /// </summary>
        public string MainLineCode { get; set; } = string.Empty;

        /// <summary>
        /// 设备实际层数。
        /// </summary>
        public int EquipmentLayer { get; set; } = 1;

        /// <summary>
        /// 设备实际供件区数量。
        /// </summary>
        public int AreaNum { get; set; } = 1;

        /// <summary>
        /// 设备允许的最大循环圈数。
        /// </summary>
        public int MaxCircleNum { get; set; } = 1;

        /// <summary>
        /// 供件台编号；无供件台时填写供件区编号。
        /// </summary>
        public string SupplyDeskCode { get; set; } = string.Empty;

        /// <summary>
        /// 供件台在当前供件区内的连续序号。
        /// </summary>
        public string SupplyDeskSerialNo { get; set; } = "1";

        /// <summary>
        /// 供件方式，1 供包台、2 补码台、3 自动供包、4 人工供包、5 快手供件。
        /// </summary>
        public string SupplyDeskMethod { get; set; } = "1";

        /// <summary>
        /// 供件台所属供件区。
        /// </summary>
        public string SupplyDeskArea { get; set; } = string.Empty;

        /// <summary>
        /// 供件台所在层数。
        /// </summary>
        public int LayerNum { get; set; } = 1;

        /// <summary>
        /// 落格模式，1 就近、2 循环、3 瀑布、4 随机。
        /// </summary>
        public string ChuteModel { get; set; } = "1";

        /// <summary>
        /// 默认实际落格供件区编号。
        /// </summary>
        public int FallArea { get; set; } = 1;

        /// <summary>
        /// 重量来源，0 秤、1 系统或默认值。
        /// </summary>
        public string WeightSource { get; set; } = "0";

        /// <summary>
        /// 格口查询超时毫秒数。
        /// </summary>
        public int QueryTimeoutMilliseconds { get; set; } = 800;

        /// <summary>
        /// 数据上报超时毫秒数。
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 1000;

        /// <summary>
        /// 最大请求次数。
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 重试间隔毫秒数。
        /// </summary>
        public int RetryIntervalMilliseconds { get; set; } = 100;
    }
}
