using System;
using MediatR;
using System.Linq;
using System.Text;
using System.Reflection;
using JayTom.Dws.Domain.Dto;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Domain.Service.Client.Package {

    /// <summary>
    /// 包裹管理器
    /// </summary>
    public class PackageService : BaseMediator, IPackageService {
        private readonly IConfigRepository _configRepository;
        private readonly ConcurrentDictionary<DateTime, PackageInfoModel> _packageInfos = new();
        private SemaphoreSlim _packageSlim = new(1);
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private SupplyCounterSettingsDto _supplyCounterSettingsDto = new();

        public PackageService(IMediator mediator,
            IConfigRepository configRepository) : base(mediator) {
            _configRepository = configRepository;
        }

        public override async Task Handle(GenericMessage request, CancellationToken cancellationToken = default) {
            //程序启动

            if (request is {
                Type: GenericMessageType.System,
                Content: SystemMessageInfo info
            }) {
                switch (info.Type) {
                    //读组包设置
                    case SystemMessageType.Start:
                        _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(
                                                        "CreatePackageSettings", cancellationToken) ??
                                                    new CreatePackageSettingsDto();

                        _supplyCounterSettingsDto = await _configRepository.FirstOrDefaultEntity<SupplyCounterSettingsDto>("SupplyCounterSettings", cancellationToken)
                                                    ?? new SupplyCounterSettingsDto();

                        break;

                    case SystemMessageType.Stop: {
                            if (_createPackageSettingsDto.ClearPackageQueueOnStop) {
                                try {
                                    await _packageSlim.WaitAsync(cancellationToken);
                                    _packageInfos.Clear();
                                }
                                finally {
                                    _packageSlim.Release();
                                }
                            }

                            break;
                        }
                }
            }
            else if (request is {
                Type: GenericMessageType.Setting,
                Content: SettingMessageInfo { SettingsName: "CreatePackageSettings" }
            }) {
                _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(
                                                "CreatePackageSettings", cancellationToken) ??
                                            new CreatePackageSettingsDto();
            }
            else if (request is {
                Type: GenericMessageType.Setting,
                Content: SettingMessageInfo { SettingsName: "SupplyCounterSettings" }
            }) {
                _supplyCounterSettingsDto = await _configRepository.FirstOrDefaultEntity<SupplyCounterSettingsDto>("SupplyCounterSettings", cancellationToken)
                                            ?? new SupplyCounterSettingsDto();
            }

            //更新包裹各种信息
        }

        public event EventHandler<PackageInfoMessage>? PackageCreated;

        public event EventHandler<PackageInfoMessage>? PackageIntercepted;

        public event EventHandler<PackageInfoMessage>? PackageRemoved;

        public event EventHandler? PackagesCleared;

        public event EventHandler<PackageInfoMessage>? PackageUpdated;

        public event EventHandler<PackageInfoMessage>? PackageAppended;

        public async Task<KeyValuePair<bool, PackageInfoModel?>> CreatePackage(PackageCreationMethodsEnum packageCreationMethod, long packageTimestamped, PackageInfoModel packageInfo) {
            try {
                await _packageSlim.WaitAsync();
                //判断创建间隔
                var info = _packageInfos.OrderBy(o => o.Key)?.LastOrDefault()
                    .Value;
                if (_createPackageSettingsDto.PackageCreationInterval > 0) {
                    if (info is not null &&
                        packageInfo.PackageCreateTime.Subtract(info.PackageCreateTime).TotalMilliseconds <
                        _createPackageSettingsDto.PackageCreationInterval) {
                        //拦截
                        OnPackageIntercepted(new PackageInfoMessage() {
                            Description = "创建时间小于最小间隔",
                            Info = packageInfo,
                            IsSuccess = false,
                            Type = PackagingType.PackageIntercepted
                        });
                        return new KeyValuePair<bool, PackageInfoModel?>(false, null);
                    }
                }
                if (info is not null && _supplyCounterSettingsDto is { IsUseSupplyCounterMode: true, IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage: true } &&
                    info.SortingInfo?.InstructionInfos?.Any(a => a.InstructionType == InstructionType.ReceivePreSignalReply) != true) {
                    //使用新条码
                    if (packageInfo?.BarCodeInfo is not null) {
                        info.BarCodeInfo = new BarCodeInfoModel() {
                            Barcode = packageInfo.BarCodeInfo.Barcode,
                            CameraSerialNumber = packageInfo.BarCodeInfo.CameraSerialNumber,
                            ScanTime = packageInfo.BarCodeInfo.ScanTime,
                            Source = packageInfo.BarCodeInfo.Source
                        };
                    }
                    //判断创建方式(如果是供包台模式则需要使用判断前置信号回复)
                    OnPackageIntercepted(new PackageInfoMessage() {
                        Description = "前置信号未回复",
                        Info = packageInfo,
                        IsSuccess = false,
                        Type = PackagingType.PackageIntercepted
                    });
                    return new KeyValuePair<bool, PackageInfoModel?>(false, null);
                }
                //判断是否使用NoRead,如果是相机创建并且不使用NoRead的话则拦截创建
                if ((packageCreationMethod & PackageCreationMethodsEnum.ScanBarcodeCamera) != 0 &&
                    !_createPackageSettingsDto.IsUseNoRead) {
                    //判断创建方式(如果是供包台模式则需要使用判断前置信号回复)
                    OnPackageIntercepted(new PackageInfoMessage() {
                        Description = "设置了不使用NoRead创建",
                        Info = packageInfo,
                        IsSuccess = false,
                        Type = PackagingType.PackageIntercepted
                    });
                    return new KeyValuePair<bool, PackageInfoModel?>(false, null);
                }
                //判断是否使用灰度仪联合创建
                //其他拦截
                //创建

                packageInfo.PackageTimestamped =
                    new DateTimeOffset(packageInfo.PackageCreateTime).ToUnixTimeMilliseconds();

                if (_createPackageSettingsDto is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }) {
                    packageInfo.EmptyPackageExpirationTime = new Timer(EmptyPackageExpireItem, packageInfo,
                        TimeSpan.FromMilliseconds(_createPackageSettingsDto.EmptyPackageExpiryTime),
                        Timeout.InfiniteTimeSpan);
                }

                if (_createPackageSettingsDto is { IsUsePackageExpiry: true, PackageExpiryTime: > 0 }) {
                    packageInfo.ExpirationTime = new Timer(PackageExpireItem, packageInfo,
                        TimeSpan.FromMilliseconds(_createPackageSettingsDto.PackageExpiryTime),
                        Timeout.InfiniteTimeSpan);
                }
                var tryAdd = _packageInfos.TryAdd(packageInfo.PackageCreateTime, packageInfo);
                if (tryAdd) {
                    OnPackageCreated(new PackageInfoMessage() {
                        Description = $"包裹创建{(tryAdd ? "成功" : "失败")}",
                        IsSuccess = tryAdd,
                        Type = PackagingType.CreatePackage,
                        Info = packageInfo
                    });
                }
                return new KeyValuePair<bool, PackageInfoModel?>(tryAdd, packageInfo);
            }
            finally {
                _packageSlim.Release();
            }
        }

        public async Task<KeyValuePair<bool, PackageInfoModel?>> RemovePackage(PackageRemoveMethodsEnum packageRemoveMethod, PackageInfoModel packageInfo) {
            try {
                await _packageSlim.WaitAsync();

                var tryRemove = _packageInfos.TryRemove(packageInfo.PackageCreateTime, out var info);

                OnPackageRemoved(new PackageInfoMessage() {
                    Description = $"移除包裹[{info?.BarCodeInfo?.Barcode}]{(tryRemove ? "成功" : "失败")},移除原因:{GetDescription(packageRemoveMethod)}",
                    Info = packageInfo,
                    IsSuccess = tryRemove,
                    Type = PackagingType.RemovePackage,
                });

                return new KeyValuePair<bool, PackageInfoModel?>(tryRemove, info);
            }
            finally {
                _packageSlim.Release();
            }
        }

        public async Task<KeyValuePair<bool, PackageInfoModel?>> RemovePackage(PackageRemoveMethodsEnum packageRemoveMethod, long packageTimestamped) {
            try {
                await _packageSlim.WaitAsync();
                var (key, value) = _packageInfos.FirstOrDefault(f => f.Value.PackageTimestamped.Equals(packageTimestamped));
                if (value is not null) {
                    var tryRemove = _packageInfos.TryRemove(key, out var info);

                    OnPackageRemoved(new PackageInfoMessage() {
                        Description = $"移除包裹[{info?.BarCodeInfo?.Barcode}]{(tryRemove ? "成功" : "失败")},移除原因:{GetDescription(packageRemoveMethod)}",
                        Info = value,
                        IsSuccess = tryRemove,
                        Type = PackagingType.RemovePackage,
                    });
                    return new KeyValuePair<bool, PackageInfoModel?>(tryRemove, info);
                }
                return new KeyValuePair<bool, PackageInfoModel?>(false, null);
            }
            finally {
                _packageSlim.Release();
            }
        }

        public async Task<bool> ClearPackages() {
            try {
                await _packageSlim.WaitAsync();
                _packageInfos.Clear();
                OnPackagesCleared();
            }
            finally {
                _packageSlim.Release();
            }

            return false;
        }

        public async Task<KeyValuePair<bool, PackageInfoModel?>> UpdatePackage(Expression<Func<PackageInfoModel, bool>> where, BasePackageForeignKeyInfoModel info, NecessaryAttributes attributes = NecessaryAttributes.BarcodeInfo) {
            //判断必要的信息是否填充完成
            try {
                await _packageSlim.WaitAsync();
                var packageInfoModel = _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                    _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.FirstOrDefault(where.Compile()) :
                    _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.LastOrDefault(where.Compile());

                if (packageInfoModel is not null) {
                    switch (info) {
                        case BarCodeInfoModel barCodeInfo:
                            packageInfoModel.BarCodeInfo = barCodeInfo;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充条码信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillBarcodeInfo
                            });
                            break;

                        case WeightInfoModel weightInfoModel:
                            packageInfoModel.WeightInfo = weightInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充重量信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillWeightInfo
                            });
                            break;

                        case VolumeInfoModel volumeInfoModel:
                            packageInfoModel.VolumeInfo = volumeInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充体积信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillVolumeInfo
                            });
                            break;

                        case UploadInfoModel uploadInfoModel:
                            packageInfoModel.UploadInfo = uploadInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充上传信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillApiInfo
                            });
                            break;

                        case ExitInfoModel exitInfoModel:
                            packageInfoModel.ExitInfo = exitInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充格口信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillExitInfo
                            });
                            break;

                        case SortingInfoModel sortingInfoModel:
                            packageInfoModel.SortingInfo = sortingInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充分拣信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillSortingInfo
                            });
                            break;

                        case LogisticsInfoModel logisticsInfoModel:
                            packageInfoModel.LogisticsInfo = logisticsInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充物流信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillLogisticsInfo
                            });
                            break;

                        case OcrInfoModel ocrInfoModel:
                            packageInfoModel.OcrInfo = ocrInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充Ocr信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillOcrInfo
                            });
                            break;

                        case CloudVideoUploadInfoModel cloudVideoUploadInfoModel:
                            packageInfoModel.CloudVideoUploadInfo = cloudVideoUploadInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充云端信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillCloudInfo
                            });
                            break;

                        case DeviceInfoModel deviceInfoModel:
                            packageInfoModel.DeviceInfo = deviceInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充设备信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillDeviceInfo
                            });
                            break;

                        case AggregatePackagesInfoModel aggregatePackagesInfoModel:
                            packageInfoModel.AggregatePackagesInfo = aggregatePackagesInfoModel;
                            OnPackageUpdated(new PackageInfoMessage() {
                                Description = "填充集包信息",
                                Info = packageInfoModel,
                                IsSuccess = true,
                                Type = PackagingType.FillAggregatePackages
                            });
                            break;
                    }

                    if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.FillInformation) {
                        //验证信息是否填充完,填充完则移除
                        var isPackageInfoComplete = IsPackageInfoComplete(packageInfoModel, attributes);
                        if (isPackageInfoComplete) {
                            await RemovePackage(PackageRemoveMethodsEnum.FillInformation, packageInfoModel);
                        }
                    }

                    return new KeyValuePair<bool, PackageInfoModel?>(true, packageInfoModel);
                }
            }
            finally {
                _packageSlim.Release();
            }

            return new KeyValuePair<bool, PackageInfoModel?>(false, null);
        }

        public async Task<KeyValuePair<bool, PackageInfoModel?>> AppendImageInfo(Expression<Func<PackageInfoModel, bool>> where, ImageInfoModel info,
            NecessaryAttributes attributes = NecessaryAttributes.BarcodeInfo) {
            try {
                await _packageSlim.WaitAsync();
                var packageInfoModel = _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                    _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.FirstOrDefault(where.Compile()) :
                    _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.LastOrDefault(where.Compile());

                if (packageInfoModel is not null) {
                    packageInfoModel.ImageInfos ??= new List<ImageInfoModel>();
                    packageInfoModel.ImageInfos.Add(info);
                    OnPackageAppended(new PackageInfoMessage() {
                        Description = "包裹填充图片信息",
                        Info = packageInfoModel,
                        IsSuccess = true,
                        Type = PackagingType.FillImageInfo
                    });
                    if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.FillInformation) {
                        //验证信息是否填充完,填充完则移除
                        var isPackageInfoComplete = IsPackageInfoComplete(packageInfoModel, attributes);
                        if (isPackageInfoComplete) {
                            await RemovePackage(PackageRemoveMethodsEnum.FillInformation, packageInfoModel);
                        }
                    }

                    return new KeyValuePair<bool, PackageInfoModel?>(true, packageInfoModel);
                }
            }
            finally {
                _packageSlim.Release();
            }

            return new KeyValuePair<bool, PackageInfoModel?>(false, null);
        }

        public async Task<KeyValuePair<bool, PackageInfoModel?>> AppendInstructionInfo(Expression<Func<PackageInfoModel, bool>> where, InstructionInfoModel info,
            NecessaryAttributes attributes = NecessaryAttributes.BarcodeInfo) {
            try {
                await _packageSlim.WaitAsync();
                var packageInfoModel = _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                    _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.FirstOrDefault(where.Compile()) :
                    _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.LastOrDefault(where.Compile());

                if (packageInfoModel?.SortingInfo != null) {
                    packageInfoModel.SortingInfo.InstructionInfos ??= new List<InstructionInfoModel>();
                    packageInfoModel.SortingInfo.InstructionInfos.Add(info);
                    OnPackageAppended(new PackageInfoMessage() {
                        Description = "包裹填充图片信息",
                        Info = packageInfoModel,
                        IsSuccess = true,
                        Type = PackagingType.FillInstructionInfo
                    });
                    if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.FillInformation) {
                        //验证信息是否填充完,填充完则移除
                        var isPackageInfoComplete = IsPackageInfoComplete(packageInfoModel, attributes);
                        if (isPackageInfoComplete) {
                            await RemovePackage(PackageRemoveMethodsEnum.FillInformation, packageInfoModel);
                        }
                    }

                    return new KeyValuePair<bool, PackageInfoModel?>(true, packageInfoModel);
                }
            }
            finally {
                _packageSlim.Release();
            }

            return new KeyValuePair<bool, PackageInfoModel?>(false, null);
        }

        public PackageInfoModel? FindPackage(Expression<Func<PackageInfoModel, bool>> where, CancellationToken token) {
            return _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.FirstOrDefault(where.Compile()) :
                _packageInfos.Values.OrderBy(o => o.PackageCreateTime)?.LastOrDefault(where.Compile());
        }

        private bool IsPackageInfoComplete(PackageInfoModel info, NecessaryAttributes attributes) {
            if (attributes.HasFlag(NecessaryAttributes.BarcodeInfo) && info.BarCodeInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.WeightInfo) && info.WeightInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.VolumeInfo) && info.VolumeInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.UploadInfo) && info.UploadInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.ExitInfo) && info.ExitInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.SortingInfo) && info.SortingInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.LogisticsInfo) && info.LogisticsInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.OcrInfo) && info.OcrInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.ImageInfo) && info.ImageInfos is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.VideoCloudInfo) && info.CloudVideoUploadInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.DeviceInfo) && info.DeviceInfo is null) {
                return false;
            }
            if (attributes.HasFlag(NecessaryAttributes.AggregatePackageInfo) && info.AggregatePackagesInfo is null) {
                return false;
            }

            return true;
        }

        private async void EmptyPackageExpireItem(object? state) {
            if (state is PackageInfoModel packageInfo) {
                try {
                    await _packageSlim.WaitAsync();

                    var tryRemove = _packageInfos.TryRemove(packageInfo.PackageCreateTime, out var info);

                    OnPackageRemoved(new PackageInfoMessage() {
                        Description = $"移除包裹[{info?.BarCodeInfo?.Barcode}]{(tryRemove ? "成功" : "失败")},移除原因:空包裹过期",
                        Info = packageInfo,
                        IsSuccess = tryRemove,
                        Type = PackagingType.RemovePackage,
                    });
                }
                finally {
                    _packageSlim.Release();
                }
            }
        }

        private async void PackageExpireItem(object? state) {
            if (state is PackageInfoModel packageInfo) {
                try {
                    await _packageSlim.WaitAsync();

                    var tryRemove = _packageInfos.TryRemove(packageInfo.PackageCreateTime, out var info);

                    OnPackageRemoved(new PackageInfoMessage() {
                        Description = $"移除包裹[{info?.BarCodeInfo?.Barcode}]{(tryRemove ? "成功" : "失败")},移除原因:包裹超过有效期",
                        Info = packageInfo,
                        IsSuccess = tryRemove,
                        Type = PackagingType.RemovePackage,
                    });
                }
                finally {
                    _packageSlim.Release();
                }
            }
        }

        protected virtual async void OnPackageCreated(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageCreated?.Invoke(this, e);
        }

        protected virtual async void OnPackageIntercepted(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageIntercepted?.Invoke(this, e);
        }

        protected virtual async void OnPackageRemoved(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageRemoved?.Invoke(this, e);
        }

        protected virtual async void OnPackagesCleared() {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = new PackageInfoMessage {
                    Description = "清空包裹",
                    IsSuccess = true,
                    Type = PackagingType.ClearPackages
                }
            });
            PackagesCleared?.Invoke(this, EventArgs.Empty);
        }

        protected virtual async void OnPackageUpdated(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageUpdated?.Invoke(this, e);
        }

        protected virtual async void OnPackageAppended(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageAppended?.Invoke(this, e);
        }
    }
}