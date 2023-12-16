using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.VideoApiClient.Api {

    public interface IVideoApi {

        /// <summary>
        /// 查询条码
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> BarcodeInfos(string? barCode, DateTime? nodeStartDateTime,
            DateTime? nodeEndDateTime, string? nodeName, string? cameraSerialNumber,
            string? cameraName, int pageIndex = 0, int pageSize = 1000, CancellationToken cancellationToken = default);

        /// <summary>
        /// 节点列表
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> GroupedNodeNames(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定条码数量
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> BarcodeTotalForDate(DateTime date,
            CancellationToken cancellationToken = default);
    }

    public class ApiResult {
        public bool Result { get; set; }
        public object? Data { get; set; }
        public string Msg { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class ApiBarCodesInfo {
        public long Id { get; set; }

        /// <summary>
        /// 时间戳Id
        /// </summary>
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 节点
        /// </summary>
        public List<ScanNodeInfo> ScanNodeInfos { get; set; } = new();
    }

    public class ScanNodeInfo {
        /// <summary>
        /// 节点名称
        /// </summary>

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点扫描时间
        /// </summary>

        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 说明
        /// </summary>

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 图片
        /// </summary>
        public List<BarcodeImageInfo> BarcodeImageInfos = new();
    }

    public class BarcodeImageInfo {

        /// <summary>
        /// 图片名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 图片路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 图片类型(0=扫码图、1=全景图)
        /// </summary>
        public int ImageType { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;
    }
}