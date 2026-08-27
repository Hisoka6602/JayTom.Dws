using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.CloudApiDto;

namespace JayTom.Dws.Legacy.Contracts.Services.CloudApi {

    public interface ICloudService {

        /// <summary>
        /// 保存包裹信息
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> SavePackageInfo(PackageDto packageInfo, CancellationToken cancellationToken = default);

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
            decimal? minWeight,
            decimal? maxWeight,
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

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="days"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days,
            CancellationToken token = default);

        /// <summary>
        /// 添加异常分类
        /// </summary>
        /// <param name="exceptionColor"></param>
        /// <param name="token"></param>
        /// <param name="exceptionName"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> AddExceptionType(string exceptionName, string exceptionColor, CancellationToken token = default);

        /// <summary>
        /// 修改异常分类
        /// </summary>
        /// <param name="exceptionCategoryId"></param>
        /// <param name="exceptionColor"></param>
        /// <param name="token"></param>
        /// <param name="exceptionName"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> UpdateExceptionType(long exceptionCategoryId, string exceptionName, string exceptionColor, CancellationToken token = default);

        /// <summary>
        /// 删除异常分类
        /// </summary>
        /// <param name="exceptionCategoryId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DeleteExceptionType(long exceptionCategoryId, CancellationToken token = default);

        /// <summary>
        /// 添加异常匹配规则
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="token"></param>
        /// <param name="keywords"></param>
        /// <param name="customRegex"></param>
        /// <param name="dataSource"></param>
        /// <param name="exceptionTypeName"></param>
        /// <param name="exceptionTypeId"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> AddExceptionRule(string keywords,
            string customRegex,
            int dataSource,
            string exceptionTypeName,
            long exceptionTypeId,
            int priority,
            CancellationToken token = default);

        /// <summary>
        /// 修改异常匹配规则
        /// </summary>
        /// <param name="exceptionRuleId"></param>
        /// <param name="priority"></param>
        /// <param name="token"></param>
        /// <param name="keywords"></param>
        /// <param name="customRegex"></param>
        /// <param name="dataSource"></param>
        /// <param name="exceptionTypeName"></param>
        /// <param name="exceptionTypeId"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> UpdateExceptionRule(long exceptionRuleId,
            string keywords,
            string customRegex,
            int dataSource,
            string exceptionTypeName,
            long exceptionTypeId,
            int priority, CancellationToken token = default);

        /// <summary>
        /// 删除异常匹配规则
        /// </summary>
        /// <param name="exceptionRuleId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DeleteExceptionRule(long exceptionRuleId, CancellationToken token = default);

        /// <summary>
        /// 异常分类列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ExceptionTypes(CancellationToken token = default);

        /// <summary>
        /// 异常匹配列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ExceptionRule(CancellationToken token = default);
    }
}