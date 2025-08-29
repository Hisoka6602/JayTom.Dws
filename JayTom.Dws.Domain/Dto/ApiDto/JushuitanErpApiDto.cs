using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.ApiDto {

    public class JushuitanErpApiDto {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;

        /// <summary>
        /// AppKey
        /// </summary>
        public string AppKey { get; set; } = string.Empty;

        /// <summary>
        /// AppSecret
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// AccessToken
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; set; } = 2;

        /// <summary>
        /// 是否上传重量（默认值 true）
        /// </summary>
        public bool IsUploadWeight { get; set; } = true;

        /// <summary>
        /// 称重类型（默认值为 1）
        /// 0: 验货后称重
        /// 1: 验货后称重并发货
        /// 2: 无须验货称重
        /// 3: 无须验货称重并发货
        /// 4: 发货后称重
        /// 5: 自动判断称重并发货
        /// </summary>
        public int Type { get; set; } = 1;

        /// <summary>
        /// 是否为国际运单号（默认值 false，表示国内快递）
        /// </summary>
        public bool IsUnLid { get; set; } = false;

        /// <summary>
        /// 称重来源备注（会显示在订单操作日志中）
        /// </summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// 上次更新 Token 的时间
        /// </summary>
        public DateTime LastTokenUpdateTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Token 到期时间
        /// </summary>
        public DateTime TokenExpireTime { get; set; } = DateTime.MinValue;
    }
}