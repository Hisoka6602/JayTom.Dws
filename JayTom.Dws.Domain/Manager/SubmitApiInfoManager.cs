using System;
using System.Net;
using System.Linq;
using System.Text;
using NPOI.POIFS.Crypt;
using System.Reflection;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Interface.Attributes;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Domain.Manager {

    public static class SubmitApiInfoManager {
        private static IApiUploader<BaseApiParameters>? _submissionUploader;

        public static event EventHandler<PackageInfo>? ApiResponseEvent;

        public static event EventHandler<UploadResponse>? ConsolidationReportEvent;

        public static async Task<IApiUploader<BaseApiParameters>?> ApiInitialization(IHttpClientFactory httpClientFactory, string apiName,
            IConfigRepository configRepository, Type apiParametersType, string settingsName, CancellationToken token = default) {
            _submissionUploader = CreateInstanceByApiName(httpClientFactory, apiName);
            if (_submissionUploader is not null) {
                //设置参数
                var parametersName = GetParametersName(_submissionUploader.GetType());
                var defaultEntity = await CallConfigRepositoryFirstOrDefaultEntity(configRepository, _submissionUploader.Parameters.GetType(), parametersName, token);
                _submissionUploader.SetParameters(defaultEntity ?? new object());
            }

            return _submissionUploader;
        }

        private static async Task<object?> CallConfigRepositoryFirstOrDefaultEntity(IConfigRepository configRepository, Type apiParametersType, string settingsName, CancellationToken token = default) {
            try {
                // 获取方法信息
                var method = typeof(IConfigRepository).GetMethod("FirstOrDefaultEntity");

                // 创建泛型方法
                var genericMethod = method.MakeGenericMethod(apiParametersType);

                // 调用泛型方法
                var resultTask = (Task)genericMethod.Invoke(configRepository, new object[] { settingsName, token });

                // 等待任务完成
                if (resultTask != null) {
                    await resultTask;

                    // 获取任务结果
                    var resultProperty = resultTask.GetType().GetProperty("Result");
                    var result = resultProperty?.GetValue(resultTask);

                    return result;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return null;
        }

        private static IApiUploader<BaseApiParameters>? CreateInstanceByApiName(IHttpClientFactory httpClientFactory, string apiName) {
            try {
                var interfaceType = typeof(IApiUploader<>);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t is { IsClass: true, IsAbstract: false })
                    .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType))
                    .ToList();

                foreach (var type in types) {
                    var apiNameAttribute = type.GetCustomAttribute<ApiClassAttribute>();
                    if (apiNameAttribute != null && apiNameAttribute.Name == apiName) {
                        var constructor = type.GetConstructor(new[] { typeof(IHttpClientFactory) });
                        if (constructor != null) {
                            var instance = (IApiUploader<BaseApiParameters>)constructor.Invoke(new object[] { httpClientFactory });
                            return instance;
                        }
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return null;
        }

        private static string? GetParametersName(Type type) {
            var attribute = type.GetCustomAttribute<ApiClassAttribute>();
            return attribute?.ParametersName;
        }

        /// <summary>
        /// 请求格口
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="cancellation"></param>
        public static async void SubmitUploadInformation(PackageInfo? packageInfo, CancellationToken cancellation = default) {
            if (packageInfo is not null && _submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.UploadInformation) == true) {
                var uploadInformation = await _submissionUploader.UploadInformation(packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                    packageInfo.WeightInfo?.FormattedWeight ?? 0,
                    packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                    packageInfo.VolumeInfo?.FormattedLength ?? 0,
                    packageInfo.VolumeInfo?.FormattedWidth ?? 0,
                    packageInfo.VolumeInfo?.FormattedHeight ?? 0,
                    packageInfo.VolumeInfo?.FormattedVolume ?? 0,
                    packageInfo.Timestamp,
                    new UploadImageInfo() {
                        Image = packageInfo.Image,
                        CameraCustomName = packageInfo.BarCodeInfo?.CameraCustomName ?? string.Empty,

                        CameraName = packageInfo.BarCodeInfo?.CameraName ?? string.Empty,
                        CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                    }, token: cancellation);
                packageInfo.UploadResponses.Add(uploadInformation);
                OnApiResponseEvent(packageInfo);
                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                    IsSuccess = uploadInformation?.IsSuccess ?? false,
                    TriggerPosition = TriggerPositionEnum.HttpOutput,
                    Description = "请求格口返回",
                    PackageInfo = packageInfo
                });
            }
        }

        /// <summary>
        /// 发送扫描信息
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="cancellation"></param>
        public static void ScanPackage(PackageInfo? packageInfo, CancellationToken cancellation = default) {
            if (packageInfo is not null && _submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.ScanPackage) == true) {
                _submissionUploader.ScanPackage(packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                   packageInfo.WeightInfo?.FormattedWeight ?? 0,
                   packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                   packageInfo.VolumeInfo?.FormattedLength ?? 0,
                   packageInfo.VolumeInfo?.FormattedWidth ?? 0,
                   packageInfo.VolumeInfo?.FormattedHeight ?? 0,
                   packageInfo.VolumeInfo?.FormattedVolume ?? 0,
                   packageInfo.Timestamp,
                   new UploadImageInfo() {
                       Image = packageInfo.Image,
                       CameraCustomName = packageInfo.BarCodeInfo?.CameraCustomName ?? string.Empty,

                       CameraName = packageInfo.BarCodeInfo?.CameraName ?? string.Empty,
                       CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                   }, token: cancellation);
            }
        }

        /// <summary>
        /// 发送分拣报告
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="cancellation"></param>
        public static async void SendSortingReport(PackageInfo? packageInfo, CancellationToken cancellation = default) {
            if (packageInfo is not null && _submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendSortingReport) == true) {
                var uploadInformation = await _submissionUploader.SendSortingReport(packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                    packageInfo.WeightInfo?.FormattedWeight ?? 0,
                    packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                    packageInfo.VolumeInfo?.FormattedLength ?? 0,
                    packageInfo.VolumeInfo?.FormattedWidth ?? 0,
                    packageInfo.VolumeInfo?.FormattedHeight ?? 0,
                    packageInfo.VolumeInfo?.FormattedVolume ?? 0,
                    packageInfo.Timestamp,
                    new UploadImageInfo() {
                        Image = packageInfo.Image,
                        CameraCustomName = packageInfo.BarCodeInfo?.CameraCustomName ?? string.Empty,

                        CameraName = packageInfo.BarCodeInfo?.CameraName ?? string.Empty,
                        CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                    }, token: cancellation);
                packageInfo.UploadResponses.Add(uploadInformation);
                OnApiResponseEvent(packageInfo);
            }
        }

        /// <summary>
        /// 发送揽件信息
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <param name="cancellation"></param>
        public static async void SendPickupReport(PackageInfo? packageInfo, CancellationToken cancellation = default) {
            if (packageInfo is not null && _submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendPickupReport) == true) {
                var uploadInformation = await _submissionUploader.SendPickupReport(packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                    packageInfo.WeightInfo?.FormattedWeight ?? 0,
                    packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                    packageInfo.VolumeInfo?.FormattedLength ?? 0,
                    packageInfo.VolumeInfo?.FormattedWidth ?? 0,
                    packageInfo.VolumeInfo?.FormattedHeight ?? 0,
                    packageInfo.VolumeInfo?.FormattedVolume ?? 0,
                    packageInfo.Timestamp,
                    new UploadImageInfo() {
                        Image = packageInfo.Image,
                        CameraCustomName = packageInfo.BarCodeInfo?.CameraCustomName ?? string.Empty,

                        CameraName = packageInfo.BarCodeInfo?.CameraName ?? string.Empty,
                        CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                    }, token: cancellation);
                packageInfo.UploadResponses.Add(uploadInformation);
                OnApiResponseEvent(packageInfo);
            }
        }

        /// <summary>
        /// 发送集包报告
        /// </summary>
        /// <param name="packageExit"></param>
        /// <param name="aggregatePackageCode"></param>
        /// <param name="packagingTime"></param>
        /// <param name="packageItems"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <param name="cancellation"></param>
        public static async void SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems, object? other = null, CancellationToken token = default,
            CancellationToken cancellation = default) {
            if (_submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendPickupReport) == true) {
                var uploadResponse = await _submissionUploader.SendConsolidationReport(packageExit, aggregatePackageCode, packagingTime,
                    packageItems, other, token);
                OnConsolidationReportEvent(uploadResponse);
            }
        }

        /// <summary>
        /// 发送图片
        /// </summary>
        public static async void SendImage(string barcode,
            List<UploadImageInfo> uploadImagesInfos,
            CancellationToken token = default) {
            if (_submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendImage) == true) {
                var uploadResponse = await _submissionUploader.SendImage(barcode, uploadImagesInfos, token);
                OnConsolidationReportEvent(uploadResponse);
            }
        }

        /// <summary>
        /// 发送锁格指令
        /// </summary>
        public static async void SendLockCommand(
            [NotNull] string lockIdentifier,
            object? other = null,
            CancellationToken token = default) {
            if (_submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendLockCommand) == true) {
                var uploadResponse = await _submissionUploader.SendLockCommand(lockIdentifier, other, token);
                OnConsolidationReportEvent(uploadResponse);
            }
        }

        /// <summary>
        /// 发送解除锁格指令
        /// </summary>
        public static async void SendUnlockCommand(
            [NotNull] string lockIdentifier,
            object? other = null,
            CancellationToken token = default) {
            if (_submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendUnlockCommand) == true) {
                var uploadResponse = await _submissionUploader.SendUnlockCommand(lockIdentifier, other, token);
                OnConsolidationReportEvent(uploadResponse);
            }
        }

        /// <summary>
        /// 发送设备信息报告
        /// </summary>
        public static async void SendDeviceReport(
            string deviceIdentifier,
            string deviceStatus,
            object? other = null,
            CancellationToken token = default) {
            if (_submissionUploader is not null &&
                _submissionUploader.GetType()?.GetCustomAttribute<ApiClassAttribute>()
                    ?.ExecTypes.HasFlag(ExecutionType.SendDeviceReport) == true) {
                var uploadResponse = await _submissionUploader.SendDeviceReport(deviceIdentifier, deviceStatus, other, token);
                OnConsolidationReportEvent(uploadResponse);
            }
        }

        private static void OnApiResponseEvent(PackageInfo e) {
            ApiResponseEvent?.Invoke(null, e);
        }

        private static void OnConsolidationReportEvent(UploadResponse e) {
            ConsolidationReportEvent?.Invoke(null, e);
        }
    }
}