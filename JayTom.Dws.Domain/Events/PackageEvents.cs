using System;

namespace JayTom.Dws.Domain.Events {

    /// <summary>
    /// 领域事件基类
    /// Base class for all domain events
    /// </summary>
    public abstract record DomainEvent {
        
        /// <summary>
        /// 事件唯一标识
        /// Event unique identifier
        /// </summary>
        public Guid EventId { get; init; } = Guid.NewGuid();
        
        /// <summary>
        /// 事件发生时间
        /// Event occurrence timestamp
        /// </summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 包裹创建事件
    /// Package created event
    /// </summary>
    public record PackageCreatedEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// Package ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 条码
        /// Barcode
        /// </summary>
        public required string Barcode { get; init; }
        
        /// <summary>
        /// 创建时间
        /// Create time
        /// </summary>
        public DateTime CreateTime { get; init; }
    }

    /// <summary>
    /// 包裹称重事件
    /// Package weight measured event
    /// </summary>
    public record PackageWeightMeasuredEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// Package ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 重量
        /// Weight
        /// </summary>
        public double Weight { get; init; }
        
        /// <summary>
        /// 称重时间
        /// Measured time
        /// </summary>
        public DateTime MeasuredAt { get; init; }
    }

    /// <summary>
    /// 包裹体积测量事件
    /// Package volume measured event
    /// </summary>
    public record PackageVolumeMeasuredEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// Package ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 长度
        /// Length
        /// </summary>
        public double Length { get; init; }
        
        /// <summary>
        /// 宽度
        /// Width
        /// </summary>
        public double Width { get; init; }
        
        /// <summary>
        /// 高度
        /// Height
        /// </summary>
        public double Height { get; init; }
        
        /// <summary>
        /// 测量时间
        /// Measured time
        /// </summary>
        public DateTime MeasuredAt { get; init; }
    }

    /// <summary>
    /// 包裹分拣事件
    /// Package sorted event
    /// </summary>
    public record PackageSortedEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// Package ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 出口代码
        /// Exit code
        /// </summary>
        public required string ExitCode { get; init; }
        
        /// <summary>
        /// 分拣时间
        /// Sorted time
        /// </summary>
        public DateTime SortedAt { get; init; }
    }

    /// <summary>
    /// 包裹上传事件
    /// Package uploaded event
    /// </summary>
    public record PackageUploadedEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// Package ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 上传时间
        /// Upload time
        /// </summary>
        public DateTime UploadedAt { get; init; }
        
        /// <summary>
        /// 是否成功
        /// Is successful
        /// </summary>
        public bool IsSuccessful { get; init; }
    }
}
