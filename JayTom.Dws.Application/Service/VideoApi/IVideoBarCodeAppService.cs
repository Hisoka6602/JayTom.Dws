using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.VideoApi;

namespace JayTom.Dws.Application.Service.VideoApi {

    public interface IVideoBarCodeAppService {
        /*
         * 1.接收上传的信息(json格式数据->同时接收图片[全景图(组)、扫码图(单)])

{
  "BarCode": "BarCodeValue",
  "Weight": "WeightValue",
  "ScanTime": "ScanTimeValue",
  "TimestampedGuid": "TimestampedGuidValue",
  "Length": "LengthValue",
  "Width": "WidthValue",
  "Height": "HeightValue",
  "Volume": "VolumeValue"
}
         */

        /// <summary>
        /// 添加或修改条码信息
        /// </summary>
        /// <returns></returns>
        public Task<KeyValuePair<bool, string>> AddOrUpdateBarcodeInfo(BarcodeImageDto barcodeImageInfo,
            List<BarcodeImageDto> panoramaImageInfos, ScanNodeDto scanNodeInfo, string rootImagePath);

        /// <summary>
        /// 获取节点分组
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> GroupedNodeNames(CancellationToken token = default);

        /// <summary>
        /// 查询扫码数据
        /// </summary>
        /// <param name="barCode"></param>
        /// <param name="nodeStartDateTime"></param>
        /// <param name="nodeEndDateTime"></param>
        /// <param name="nodeName"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="cameraName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> GetBarcodeInfos(string barCode, DateTime? nodeStartDateTime,
            DateTime? nodeEndDateTime, string? nodeName, string? cameraSerialNumber, string? cameraName, int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default);

        /// <summary>
        /// 查询扫码条数
        /// </summary>
        /// <param name="barCode"></param>
        /// <param name="nodeStartDateTime"></param>
        /// <param name="nodeEndDateTime"></param>
        /// <param name="nodeName"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="cameraName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> GetBarcodeTotal(string barCode, DateTime? nodeStartDateTime,
            DateTime? nodeEndDateTime, string? nodeName, string? cameraSerialNumber, string? cameraName, CancellationToken token = default);

        /// <summary>
        /// 获取指定日期的条码总数
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> BarcodeTotalForDate(DateTime date);

        /// <summary>
        /// 获取指定日期之间的条码总数
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> BarcodeTotalForDateBetween(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 删除多少天之前的数据
        /// </summary>
        /// <param name="days"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days, CancellationToken token = default);
    }
}