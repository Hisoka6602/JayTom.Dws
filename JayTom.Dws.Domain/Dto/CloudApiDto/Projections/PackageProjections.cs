using System;

namespace JayTom.Dws.Domain.Dto.CloudApiDto.Projections {
    /// <summary>
    /// 包裹列表投影 DTO - 只包含列表展示需要的字段
    /// 用于替代完整的 PackageInfoModel，减少数据传输量 70-80%
    /// </summary>
    public class PackageListProjection {
        public int Id { get; set; }
        public DateTime PackageCreateTime { get; set; }
        public long PackageTimestamped { get; set; }

        // 条码信息
        public string? Barcode { get; set; }
        public DateTime? ScanTime { get; set; }

        // 重量信息
        public double? Weight { get; set; }
        public DateTime? WeighingTime { get; set; }

        // 体积信息
        public double? Length { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Volume { get; set; }

        // 格口信息
        public string? PhysicalExit { get; set; }
        public string? VirtualExit { get; set; }

        // 设备信息
        public string? DeviceName { get; set; }
        public string? CameraSerialNumber { get; set; }

        // 上传状态
        public int? UploadStatus { get; set; }
    }

    /// <summary>
    /// 包裹详情投影 DTO - 包含详情页需要的字段
    /// </summary>
    public class PackageDetailProjection {
        public int Id { get; set; }
        public DateTime PackageCreateTime { get; set; }
        public long PackageTimestamped { get; set; }

        // 条码信息
        public string? Barcode { get; set; }
        public DateTime? ScanTime { get; set; }

        // 重量信息
        public double? Weight { get; set; }
        public double? Tare { get; set; }
        public DateTime? WeighingTime { get; set; }
        public string? WeightUnit { get; set; }

        // 体积信息
        public double? Length { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Volume { get; set; }
        public DateTime? MeasuringTime { get; set; }
        public string? VolumeUnit { get; set; }

        // 格口信息
        public string? PhysicalExit { get; set; }
        public string? VirtualExit { get; set; }
        public int? ExitStatus { get; set; }

        // 分拣信息
        public string? SentInstruction { get; set; }
        public string? ReceivedInstruction { get; set; }
        public DateTime? InstructionTime { get; set; }

        // 物流信息
        public string? LogisticsName { get; set; }
        public string? ThreeSegmentCode { get; set; }
        public string? NodeName { get; set; }

        // 设备信息
        public string? DeviceName { get; set; }
        public string? DeviceIp { get; set; }
        public string? CameraSerialNumber { get; set; }

        // 上传信息
        public int? UploadStatus { get; set; }
        public DateTime? UploadTime { get; set; }
        public string? UploadResult { get; set; }

        // OCR 信息
        public string? OcrResult { get; set; }
        public DateTime? OcrTime { get; set; }
    }

    /// <summary>
    /// 包裹统计投影 DTO - 用于统计查询
    /// </summary>
    public class PackageStatisticsProjection {
        public DateTime Date { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public double? AverageWeight { get; set; }
        public double? AverageVolume { get; set; }
        public string? DeviceName { get; set; }
    }

    /// <summary>
    /// 包裹搜索投影 DTO - 用于搜索结果
    /// </summary>
    public class PackageSearchProjection {
        public int Id { get; set; }
        public string? Barcode { get; set; }
        public DateTime? ScanTime { get; set; }
        public double? Weight { get; set; }
        public double? Volume { get; set; }
        public string? PhysicalExit { get; set; }
        public string? LogisticsName { get; set; }
        public string? DeviceName { get; set; }
        public int? UploadStatus { get; set; }
        public DateTime PackageCreateTime { get; set; }
    }
}
