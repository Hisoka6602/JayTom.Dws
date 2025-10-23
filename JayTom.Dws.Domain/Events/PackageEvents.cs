using System;

namespace JayTom.Dws.Domain.Events {

    /// <summary>
    /// 领域事件基类
    /// </summary>
    public abstract record DomainEvent {
        
        /// <summary>
        /// 事件唯一标识
        /// </summary>
        public Guid EventId { get; init; } = Guid.NewGuid();
        
        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 包裹创建事件
    /// </summary>
    public record PackageCreatedEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 条码
        /// </summary>
        public required string Barcode { get; init; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; init; }
    }

    /// <summary>
    /// 包裹称重事件
    /// </summary>
    public record PackageWeightMeasuredEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 重量
        /// </summary>
        public double Weight { get; init; }
        
        /// <summary>
        /// 称重时间
        /// </summary>
        public DateTime MeasuredAt { get; init; }
    }

    /// <summary>
    /// 包裹体积测量事件
    /// </summary>
    public record PackageVolumeMeasuredEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 长度
        /// </summary>
        public double Length { get; init; }
        
        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; init; }
        
        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; init; }
        
        /// <summary>
        /// 测量时间
        /// </summary>
        public DateTime MeasuredAt { get; init; }
    }

    /// <summary>
    /// 包裹分拣事件
    /// </summary>
    public record PackageSortedEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 出口代码
        /// </summary>
        public required string ExitCode { get; init; }
        
        /// <summary>
        /// 分拣时间
        /// </summary>
        public DateTime SortedAt { get; init; }
    }

    /// <summary>
    /// 包裹上传事件
    /// </summary>
    public record PackageUploadedEvent : DomainEvent {
        
        /// <summary>
        /// 包裹ID
        /// </summary>
        public required string PackageId { get; init; }
        
        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UploadedAt { get; init; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccessful { get; init; }
    }
}
