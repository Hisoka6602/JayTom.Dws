using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using JayTom.Dws.CrossCutting.SignalR;
using JayTom.Dws.Domain.Entities.LogsEntities;
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
        /// 指令失败数据
        /// </summary>
        ConcurrentQueue<InstructionInfoModel> FallUpdateInstructionInfoModels { get; }

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
        /// 添加指令信息
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="instructionData"></param>
        /// <param name="sortingInfo"></param>
        [HubMethodName("AddInstructionData")]
        void AddInstructionDataAsync(long packageId, InstructionInfoModel instructionData);

        /// <summary>
        /// 保存图片信息，并返回图片路径
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="type"></param>
        /// <param name="imageData">图片数据</param>
        /// <returns>保存成功时返回图片路径，否则返回null</returns>
        [HubMethodName("SaveImageData")]
        Task SaveImageDataAsync(PackageInfoModel packageInfo, SaveImageType type, byte[] imageData);

        /// <summary>
        /// 查询包裹数据
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="packageId">包裹ID</param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="exitName"></param>
        /// <param name="barcode"></param>
        /// <param name="minWeight"></param>
        /// <param name="maxWeight"></param>
        /// <param name="uploadStatus"></param>
        /// <param name="deviceName"></param>
        /// <param name="nodeName"></param>
        /// <param name="logisticsName"></param>
        /// <param name="aggregatedPackageCode"></param>
        /// <param name="pageIndex"></param>
        /// <returns>包裹数据对象</returns>
        [HubMethodName("GetPackageData")]
        Task<PackageInfoEntities> GetPackageDataAsync(int pageIndex, int pageSize,
            long? packageId = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            string? exitName = null,
            string? barcode = null,
            double? minWeight = null,
            double? maxWeight = null,
            int? uploadStatus = null,
            string? deviceName = null,
            string? nodeName = null,
            string? logisticsName = null,
            string? aggregatedPackageCode = null);

        /// <summary>
        /// 查询配置
        /// </summary>
        /// <param name="configKey">配置键</param>
        /// <returns>配置值</returns>
        [HubMethodName("GetConfig")]
        Task<string> GetConfigAsync(string configKey);

        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="config"></param>
        [HubMethodName("AddOrUpdateConfig")]
        Task<bool> AddOrUpdateConfigAsync(ConfigInfoModel config);

        /// <summary>
        /// 查询相机配置
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        [HubMethodName("GetCameraConfig")]
        Task<object?> GetCameraConfigAsync(string configName);

        /// <summary>
        /// 添加或更新相机配置
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HubMethodName("AddOrUpdateCameraConfig")]
        Task<bool> AddOrUpdateCameraConfigAsync(string configName, object value);

        /// <summary>
        /// 查询云端配置
        /// </summary>
        /// <returns></returns>
        [HubMethodName("GetCloudConfig")]
        Task<object?> GetNvrConfigAsync();

        /// <summary>
        /// 添加或更新云端配置
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HubMethodName("AddOrUpdateCloudConfig")]
        Task<bool> AddOrUpdateNvrConfigAsync(object value);

        /// <summary>
        /// 查询分拣配置
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        [HubMethodName("GetSortingConfig")]
        Task<object?> GetSortingConfigAsync(string configName);

        /// <summary>
        /// 添加或更新分拣配置
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HubMethodName("AddOrUpdateSortingConfig")]
        Task<bool> AddOrUpdateSortingConfigAsync(string configName, object value);

        /// <summary>
        /// 查询日志
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <param name="pageSize"></param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="keyword">关键字</param>
        /// <param name="pageIndex"></param>
        /// <returns>日志对象</returns>
        [HubMethodName("GetLog")]
        Task<LogsInfoEntities> GetLogAsync(string configName, int pageIndex, int pageSize, DateTime? startTime = null, DateTime? endTime = null, string? keyword = null);

        /// <summary>
        /// 添加日志
        /// </summary>
        [HubMethodName("AddLog")]
        void AddLogAsync(string configName, object value);

        /// <summary>
        /// 清理日志
        /// </summary>
        [HubMethodName("ClearLogs")]
        Task<bool> ClearLogsAsync(string configName);
    }
}