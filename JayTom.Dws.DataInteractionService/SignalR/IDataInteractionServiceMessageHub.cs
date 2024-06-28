using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using JayTom.Dws.CrossCutting.SignalR;
using JayTom.Dws.Domain.Entities.PackageEntities;

namespace JayTom.Dws.DataInteractionService.SignalR {

    public interface IDataInteractionServiceMessageHub : IBaseServerMessageHub {

        /// <summary>
        /// 插入失败的数据
        /// </summary>
        ConcurrentQueue<PackageInfoModel> FallInsertPackageInfoModels { get; }

        /// <summary>
        /// 更新失败的数据
        /// </summary>
        ConcurrentQueue<PackageInfoModel> FallUpdatePackageInfoModels { get; }

        /// <summary>
        /// 更新条码信息失败的数据队列
        /// </summary>
        ConcurrentQueue<BarCodeInfoModel> FallUpdateBarcodeInfoModels { get; }

        /// <summary>
        /// 更新称重信息失败的数据队列
        /// </summary>
        ConcurrentQueue<WeightInfoModel> FallUpdateWeightInfoModels { get; }

        /// <summary>
        /// 更新体积信息失败的数据队列
        /// </summary>
        ConcurrentQueue<VolumeInfoModel> FallUpdateVolumeInfoModels { get; }

        /// <summary>
        /// 更新上传数据失败的数据队列
        /// </summary>
        ConcurrentQueue<UploadInfoModel> FallUpdateUploadInfoModels { get; }

        /// <summary>
        /// 更新格口信息失败的数据队列
        /// </summary>
        ConcurrentQueue<ExitInfoModel> FallUpdateCompartmentInfoModels { get; }

        /// <summary>
        /// 更新分拣信息失败的数据队列
        /// </summary>
        ConcurrentQueue<SortingInfoModel> FallUpdateSortingInfoModels { get; }

        /// <summary>
        /// 更新Ocr信息失败的数据队列
        /// </summary>
        ConcurrentQueue<OcrInfoModel> FallUpdateOcrInfoModels { get; }

        /// <summary>
        /// 更新图片信息失败的数据队列
        /// </summary>
        ConcurrentQueue<ImageInfoModel> FallUpdateImageInfoModels { get; }

        /// <summary>
        /// 更新视频云信息失败的数据队列
        /// </summary>
        ConcurrentQueue<CloudVideoUploadInfoModel> FallUpdateVideoCloudInfoModels { get; }

        /// <summary>
        /// 添加包裹数据
        /// </summary>
        /// <param name="packageData">包裹数据对象</param>
        [HubMethodName("AddOrUpdatePackageData")]
        void AddOrUpdatePackageDataAsync(PackageInfoModel packageData);

        /// <summary>
        /// 更新包裹数据
        /// </summary>
        /// <param name="packageData">包裹数据对象</param>
        [HubMethodName("UpdatePackageData")]
        void UpdatePackageDataAsync(PackageInfoModel packageData);

        /// <summary>
        /// 更新条码信息
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="barcodeData">条码信息对象</param>
        [HubMethodName("UpdateBarcodeData")]
        void UpdateBarcodeDataAsync(long packageId, BarCodeInfoModel barcodeData);

        /// <summary>
        /// 更新称重信息
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="weightData">称重信息对象</param>
        [HubMethodName("UpdateWeightData")]
        void UpdateWeightDataAsync(long packageId, WeightInfoModel weightData);

        /// <summary>
        /// 更新体积信息
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="volumeData">体积信息对象</param>
        [HubMethodName("UpdateVolumeData")]
        void UpdateVolumeDataAsync(long packageId, VolumeInfoModel volumeData);

        /// <summary>
        /// 更新上传信息
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="uploadData">上传信息对象</param>
        [HubMethodName("UpdateUploadData")]
        void UpdateUploadDataAsync(long packageId, UploadInfoModel uploadData);

        /// <summary>
        /// 更新格口信息
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="compartmentData">格口信息对象</param>
        [HubMethodName("UpdateExitData")]
        void UpdateExitDataAsync(long packageId, ExitInfoModel compartmentData);

        /// <summary>
        /// 更新分拣信息
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="sortingData">分拣信息对象</param>
        [HubMethodName("UpdateSortingData")]
        void UpdateSortingDataAsync(long packageId, SortingInfoModel sortingData);

        /// <summary>
        /// 更新Ocr信息
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="ocrData">Ocr信息对象</param>
        [HubMethodName("UpdateOcrData")]
        void UpdateOcrDataAsync(long packageId, OcrInfoModel ocrData);

        /// <summary>
        /// 更新图片信息
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="imageData">图片信息对象</param>
        [HubMethodName("UpdateImageData")]
        void UpdateImageDataAsync(long packageId, ImageInfoModel imageData);

        /// <summary>
        /// 更新视频云信息
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="videoCloudData">视频云信息对象</param>
        [HubMethodName("UpdateVideoCloudData")]
        void UpdateVideoCloudDataAsync(long packageId, CloudVideoUploadInfoModel videoCloudData);

        /// <summary>
        /// 保存图片信息，并返回图片路径
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="rootPath"></param>
        /// <param name="imageData">图片数据</param>
        /// <returns>保存成功时返回图片路径，否则返回null</returns>
        [HubMethodName("SaveImageData")]
        Task<KeyValuePair<PackageInfoModel, string>> SaveImageDataAsync(PackageInfoModel packageInfo, string rootPath, byte[] imageData);

        /// <summary>
        /// 查询包裹数据
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="compartment"></param>
        /// <param name="barcode"></param>
        /// <param name="minWeight"></param>
        /// <param name="maxWeight"></param>
        /// <param name="uploadStatus"></param>
        /// <param name="deviceName"></param>
        /// <param name="nodeName"></param>
        /// <param name="logisticsName"></param>
        /// <param name="aggregatedPackageCode"></param>
        /// <returns>包裹数据对象</returns>
        [HubMethodName("GetPackageData")]
        Task<PackageInfoEntities> GetPackageDataAsync(long? packageId = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            string? compartment = null,
            string? barcode = null,
            double? minWeight = null,
            double? maxWeight = null,
            bool? uploadStatus = null,
            string? deviceName = null,
            string? nodeName = null,
            string? logisticsName = null,
            string? aggregatedPackageCode = null);

        /// <summary>
        /// 删除包裹数据
        /// </summary>
        /// <param name="packageId">包裹ID</param>
        [HubMethodName("DeletePackageData")]
        Task<bool> DeletePackageDataAsync(string packageId);

        /// <summary>
        /// 删除N天之前的包裹信息
        /// </summary>
        /// <param name="days">天数</param>
        [HubMethodName("DeletePackagesOlderThan")]
        Task<bool> DeletePackagesOlderThanAsync(int days);

        // 配置相关方法

        /// <summary>
        /// 查询配置
        /// </summary>
        /// <param name="configKey">配置键</param>
        /// <returns>配置值</returns>
        [HubMethodName("GetConfig")]
        Task<object> GetConfigAsync(string configKey);

        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="configKey">配置键</param>
        /// <param name="configValue">配置值</param>
        [HubMethodName("AddOrUpdateConfig")]
        Task<bool> AddOrUpdateConfigAsync(string configKey, object configValue);

        /// <summary>
        /// 查询日志
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <returns>日志对象</returns>
        [HubMethodName("GetLog")]
        Task<object> GetLogAsync(string logId);

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="log">日志对象</param>
        [HubMethodName("AddLog")]
        void AddLogAsync(object log);

        /// <summary>
        /// 清理日志
        /// </summary>
        [HubMethodName("ClearLogs")]
        Task<bool> ClearLogsAsync();

        /// <summary>
        /// 删除N天之前的日志
        /// </summary>
        /// <param name="days">天数</param>
        [HubMethodName("DeleteLogsOlderThan")]
        Task<bool> DeleteLogsOlderThanAsync(int days);
    }
}