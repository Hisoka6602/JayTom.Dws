using System;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Specifications;

namespace JayTom.Dws.Domain.Specifications.PackageSpecifications {
    /// <summary>
    /// 包裹基本信息规范 - 只加载必要的关联
    /// </summary>
    public class PackageBasicInfoSpecification : BaseSpecification<PackageInfoModel> {
        public PackageBasicInfoSpecification() {
            // 只加载基本信息，不加载所有关联
        }

        public PackageBasicInfoSpecification(string barcode) 
            : base(p => p.BarCodeInfo != null && p.BarCodeInfo.Barcode == barcode) {
            AddInclude(p => p.BarCodeInfo!);
        }
    }

    /// <summary>
    /// 包裹详细信息规范 - 加载必要的关联用于详情展示
    /// </summary>
    public class PackageDetailSpecification : BaseSpecification<PackageInfoModel> {
        public PackageDetailSpecification(int packageId) 
            : base(p => p.Id == packageId) {
            AddInclude(p => p.BarCodeInfo!);
            AddInclude(p => p.WeightInfo!);
            AddInclude(p => p.VolumeInfo!);
            AddInclude(p => p.SortingInfo!);
            AddInclude(p => p.ExitInfo!);
        }
    }

    /// <summary>
    /// 包裹完整信息规范 - 加载所有关联（仅在确实需要时使用）
    /// </summary>
    public class PackageFullInfoSpecification : BaseSpecification<PackageInfoModel> {
        public PackageFullInfoSpecification(int packageId) 
            : base(p => p.Id == packageId) {
            AddInclude(p => p.BarCodeInfo!);
            AddInclude(p => p.WeightInfo!);
            AddInclude(p => p.VolumeInfo!);
            AddInclude(p => p.UploadInfo!);
            AddInclude(p => p.ExitInfo!);
            AddInclude(p => p.SortingInfo!);
            AddInclude(p => p.LogisticsInfo!);
            AddInclude("OcrInfo.OcrDetailedInfos"); // 使用字符串 Include 处理嵌套
            AddInclude(p => p.ImageInfos!);
            AddInclude(p => p.NvrInfos!);
            AddInclude(p => p.DeviceInfo!);
        }
    }

    /// <summary>
    /// 包裹列表规范 - 分页查询，只加载列表需要的字段
    /// </summary>
    public class PackageListSpecification : BaseSpecification<PackageInfoModel> {
        public PackageListSpecification(
            DateTime? startTime, 
            DateTime? endTime,
            int pageIndex, 
            int pageSize) {
            
            // 构建查询条件
            if (startTime.HasValue && endTime.HasValue) {
                Criteria = p => p.PackageCreateTime >= startTime.Value && p.PackageCreateTime <= endTime.Value;
            } else if (startTime.HasValue) {
                Criteria = p => p.PackageCreateTime >= startTime.Value;
            } else if (endTime.HasValue) {
                Criteria = p => p.PackageCreateTime <= endTime.Value;
            }

            // 只加载列表必需的关联
            AddInclude(p => p.BarCodeInfo!);
            AddInclude(p => p.WeightInfo!);
            AddInclude(p => p.VolumeInfo!);
            AddInclude(p => p.ExitInfo!);

            // 排序和分页
            ApplyOrderByDescending(p => p.PackageCreateTime);
            ApplyPaging(pageIndex * pageSize, pageSize);
        }
    }

    /// <summary>
    /// 包裹搜索规范 - 支持多条件搜索
    /// </summary>
    public class PackageSearchSpecification : BaseSpecification<PackageInfoModel> {
        public PackageSearchSpecification(
            string? barcode,
            DateTime? startScanTime,
            DateTime? endScanTime,
            string? cameraSerialNumber,
            double? minWeight,
            double? maxWeight,
            string? deviceName,
            int pageIndex,
            int pageSize) {
            
            // 构建复杂的查询条件
            if (!string.IsNullOrEmpty(barcode)) {
                Criteria = p => p.BarCodeInfo != null && p.BarCodeInfo.Barcode.Contains(barcode);
            }

            // 添加必要的关联
            AddInclude(p => p.BarCodeInfo!);
            AddInclude(p => p.WeightInfo!);
            AddInclude(p => p.VolumeInfo!);
            AddInclude(p => p.ExitInfo!);
            AddInclude(p => p.DeviceInfo!);

            // 排序和分页
            ApplyOrderByDescending(p => p.PackageCreateTime);
            ApplyPaging(pageIndex * pageSize, pageSize);
        }
    }
}
