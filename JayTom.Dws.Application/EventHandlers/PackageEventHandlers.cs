using System;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Domain.Events;
using JayTom.Dws.Domain.Repository.CloudApi;
using JayTom.Dws.Data.CloudApiData;
using NLog;

namespace JayTom.Dws.Application.EventHandlers {

    /// <summary>
    /// 包裹创建事件处理器
    /// Package created event handler
    /// </summary>
    public class PackageCreatedEventHandler {
        
        private readonly ICloudPackageRepository _packageRepository;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public PackageCreatedEventHandler(ICloudPackageRepository packageRepository) {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        /// <summary>
        /// 处理包裹创建事件
        /// Handle package created event
        /// </summary>
        public async Task HandleAsync(PackageCreatedEvent @event, CancellationToken cancellationToken) {
            try {
                _logger.Info($"Handling PackageCreatedEvent: PackageId={@event.PackageId}, Barcode={@event.Barcode}");

                // 创建包裹基本信息
                if (!int.TryParse(@event.PackageId, out int packageId))
                {
                    _logger.Warn($"Invalid PackageId format: {@event.PackageId}");
                    return;
                }
                var package = new PackageInfoModel {
                    Id = packageId,
                    PackageCreateTime = @event.CreateTime,
                    PackageTimestamped = new DateTimeOffset(@event.CreateTime).ToUnixTimeMilliseconds(),
                    BarCodeInfo = new BarCodeInfoModel {
                        Barcode = @event.Barcode,
                        ScanTime = @event.CreateTime
                    }
                };

                // 保存到数据库
                var success = await _packageRepository.AddOrUpdateAsync(package, cancellationToken);
                
                if (success) {
                    _logger.Info($"Package created successfully: PackageId={@event.PackageId}");
                } else {
                    _logger.Warn($"Failed to create package: PackageId={@event.PackageId}");
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error handling PackageCreatedEvent: PackageId={@event.PackageId}");
                throw;
            }
        }
    }

    /// <summary>
    /// 包裹称重事件处理器
    /// Package weight measured event handler
    /// </summary>
    public class PackageWeightMeasuredEventHandler {
        
        private readonly ICloudPackageRepository _packageRepository;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public PackageWeightMeasuredEventHandler(ICloudPackageRepository packageRepository) {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        /// <summary>
        /// 处理包裹称重事件
        /// Handle package weight measured event
        /// </summary>
        public async Task HandleAsync(PackageWeightMeasuredEvent @event, CancellationToken cancellationToken) {
            try {
                _logger.Info($"Handling PackageWeightMeasuredEvent: PackageId={@event.PackageId}, Weight={@event.Weight}");

                // 获取现有包裹
                var package = await _packageRepository.GetByIdAsync(int.Parse(@event.PackageId), cancellationToken);
                if (package == null) {
                    _logger.Warn($"Package not found: PackageId={@event.PackageId}");
                    return;
                }

                // 更新重量信息
                package.WeightInfo = new WeightInfoModel {
                    Weight = @event.Weight,
                    WeighingTime = @event.MeasuredAt
                };

                // 保存更新
                var success = await _packageRepository.AddOrUpdateAsync(package, cancellationToken);
                
                if (success) {
                    _logger.Info($"Package weight updated: PackageId={@event.PackageId}, Weight={@event.Weight}");
                } else {
                    _logger.Warn($"Failed to update package weight: PackageId={@event.PackageId}");
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error handling PackageWeightMeasuredEvent: PackageId={@event.PackageId}");
                throw;
            }
        }
    }

    /// <summary>
    /// 包裹体积测量事件处理器
    /// Package volume measured event handler
    /// </summary>
    public class PackageVolumeMeasuredEventHandler {
        
        private readonly ICloudPackageRepository _packageRepository;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public PackageVolumeMeasuredEventHandler(ICloudPackageRepository packageRepository) {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        /// <summary>
        /// 处理包裹体积测量事件
        /// Handle package volume measured event
        /// </summary>
        public async Task HandleAsync(PackageVolumeMeasuredEvent @event, CancellationToken cancellationToken) {
            try {
                _logger.Info($"Handling PackageVolumeMeasuredEvent: PackageId={@event.PackageId}, L={@event.Length}, W={@event.Width}, H={@event.Height}");

                // 获取现有包裹
                var package = await _packageRepository.GetByIdAsync(int.Parse(@event.PackageId), cancellationToken);
                if (package == null) {
                    _logger.Warn($"Package not found: PackageId={@event.PackageId}");
                    return;
                }

                // 更新体积信息
                var volume = @event.Length * @event.Width * @event.Height;
                package.VolumeInfo = new VolumeInfoModel {
                    Length = @event.Length,
                    Width = @event.Width,
                    Height = @event.Height,
                    Volume = volume,
                    MeasuringTime = @event.MeasuredAt
                };

                // 保存更新
                var success = await _packageRepository.AddOrUpdateAsync(package, cancellationToken);
                
                if (success) {
                    _logger.Info($"Package volume updated: PackageId={@event.PackageId}, Volume={volume}");
                } else {
                    _logger.Warn($"Failed to update package volume: PackageId={@event.PackageId}");
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error handling PackageVolumeMeasuredEvent: PackageId={@event.PackageId}");
                throw;
            }
        }
    }

    /// <summary>
    /// 包裹分拣事件处理器
    /// Package sorted event handler
    /// </summary>
    public class PackageSortedEventHandler {
        
        private readonly ICloudPackageRepository _packageRepository;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public PackageSortedEventHandler(ICloudPackageRepository packageRepository) {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        /// <summary>
        /// 处理包裹分拣事件
        /// Handle package sorted event
        /// </summary>
        public async Task HandleAsync(PackageSortedEvent @event, CancellationToken cancellationToken) {
            try {
                _logger.Info($"Handling PackageSortedEvent: PackageId={@event.PackageId}, ExitCode={@event.ExitCode}");

                // 获取现有包裹
                var package = await _packageRepository.GetByIdAsync(int.Parse(@event.PackageId), cancellationToken);
                if (package == null) {
                    _logger.Warn($"Package not found: PackageId={@event.PackageId}");
                    return;
                }

                // 更新分拣信息
                package.ExitInfo = new ExitInfoModel {
                    PhysicalExit = @event.ExitCode,
                    ExitTime = @event.SortedAt
                };

                // 保存更新
                var success = await _packageRepository.AddOrUpdateAsync(package, cancellationToken);
                
                if (success) {
                    _logger.Info($"Package sorted: PackageId={@event.PackageId}, ExitCode={@event.ExitCode}");
                } else {
                    _logger.Warn($"Failed to update package sorting: PackageId={@event.PackageId}");
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error handling PackageSortedEvent: PackageId={@event.PackageId}");
                throw;
            }
        }
    }

    /// <summary>
    /// 包裹上传事件处理器
    /// Package uploaded event handler
    /// </summary>
    public class PackageUploadedEventHandler {
        
        private readonly ICloudPackageRepository _packageRepository;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public PackageUploadedEventHandler(ICloudPackageRepository packageRepository) {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        /// <summary>
        /// 处理包裹上传事件
        /// Handle package uploaded event
        /// </summary>
        public async Task HandleAsync(PackageUploadedEvent @event, CancellationToken cancellationToken) {
            try {
                _logger.Info($"Handling PackageUploadedEvent: PackageId={@event.PackageId}, IsSuccessful={@event.IsSuccessful}");

                // 获取现有包裹
                var package = await _packageRepository.GetByIdAsync(int.Parse(@event.PackageId), cancellationToken);
                if (package == null) {
                    _logger.Warn($"Package not found: PackageId={@event.PackageId}");
                    return;
                }

                // 更新上传信息
                package.UploadInfo = new UploadInfoModel {
                    UploadStatus = @event.IsSuccessful ? 1 : 0,
                    UploadTime = @event.UploadedAt,
                    UploadResult = @event.IsSuccessful ? "Success" : "Failed"
                };

                // 保存更新
                var success = await _packageRepository.AddOrUpdateAsync(package, cancellationToken);
                
                if (success) {
                    _logger.Info($"Package upload status updated: PackageId={@event.PackageId}");
                } else {
                    _logger.Warn($"Failed to update package upload status: PackageId={@event.PackageId}");
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error handling PackageUploadedEvent: PackageId={@event.PackageId}");
                throw;
            }
        }
    }
}
