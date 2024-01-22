using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.CloudApiDto;

namespace JayTom.Dws.Application.Service.CloudApi {

    public interface ICloudAppService {

        /// <summary>
        /// 保存包裹信息
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="rootImagePath"></param>
        /// <param name="webImagePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> SavePackageInfo(PackageDto packageInfo,
            string rootImagePath,
            string webImagePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取包裹列表
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="startScanTime"></param>
        /// <param name="endScanTime"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="minWeight"></param>
        /// <param name="maxWeight"></param>
        /// <param name="requestStatus"></param>
        /// <param name="physicalExit"></param>
        /// <param name="sentInstruction"></param>
        /// <param name="logisticsName"></param>
        /// <param name="threeSegmentCode"></param>
        /// <param name="nodeName"></param>
        /// <param name="deviceName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> GetPackages(
            string? barcode,
            DateTime? startScanTime,
            DateTime? endScanTime,
            string? cameraSerialNumber,
            double? minWeight,
            double? maxWeight,
            int? requestStatus,
            string? physicalExit,
            string? sentInstruction,
            string? logisticsName,
            string? threeSegmentCode,
            string? nodeName,
            string? deviceName,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// 获取统计数据
        /// </summary>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <param name="deviceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> GetStatistics(DateTime? startDateTime, DateTime? endDateTime, string? deviceName, CancellationToken cancellationToken);
    }
}