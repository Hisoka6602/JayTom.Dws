using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Service.Client.Package {

    public interface IPackageManager {

        /// <summary>
        /// 创建包裹完成事件，当成功创建包裹时触发。
        /// </summary>
        event EventHandler<PackageInfoMessage> PackageCreated;

        /// <summary>
        /// 拦截创建包裹事件，当创建包裹被拦截时触发。
        /// </summary>
        event EventHandler<PackageInfoMessage> PackageIntercepted;

        /// <summary>
        /// 移除包裹事件，当包裹被移除时触发。
        /// </summary>
        event EventHandler<PackageInfoMessage> PackageRemoved;

        /// <summary>
        /// 清空包裹事件，当所有包裹被清空时触发。
        /// </summary>
        event EventHandler PackagesCleared;

        /// <summary>
        /// 更新包裹事件，当包裹信息被更新时触发。
        /// </summary>
        event EventHandler<PackageInfoMessage> PackageUpdated;

        /// <summary>
        /// 追加包裹信息事件，当包裹信息被追加时触发。
        /// </summary>
        event EventHandler<PackageInfoMessage> PackageAppended;

        /// <summary>
        /// 创建包裹方法，用于向包裹管理器添加新的包裹。
        /// </summary>
        Task<KeyValuePair<bool, PackageInfoModel?>> CreatePackage(PackageCreationMethodsEnum packageCreationMethod, long packageTimestamped, PackageInfoModel packageInfo);

        /// <summary>
        /// 移除包裹方法，用于从包裹管理器中移除指定追踪号的包裹。
        /// </summary>
        Task<KeyValuePair<bool, PackageInfoModel?>> RemovePackage(PackageInfoModel packageInfo);

        /// <summary>
        /// 移除包裹方法，用于从包裹管理器中移除指定追踪号的包裹。
        /// </summary>
        Task<KeyValuePair<bool, PackageInfoModel?>> RemovePackage(long packageTimestamped);

        /// <summary>
        /// 清空包裹方法，用于清空包裹管理器中的所有包裹。
        /// </summary>
        Task<bool> ClearPackages();

        /// <summary>
        /// 更新包裹方法，用于更新指定追踪号的包裹信息。
        /// </summary>
        Task<KeyValuePair<bool, PackageInfoModel?>> UpdatePackage(Expression<Func<PackageInfoModel, bool>> @where, BasePackageForeignKeyInfoModel info);

        /// <summary>
        /// 追加包裹信息方法，用于向指定追踪号的包裹添加额外信息。
        /// </summary>
        Task<KeyValuePair<bool, PackageInfoModel?>> AppendPackageInfo(Expression<Func<PackageInfoModel, bool>> @where, BasePackageForeignKeyInfoModel info);

        /// <summary>
        /// 查询包裹方法，用于根据追踪号查询指定的包裹。
        /// </summary>
        Task<PackageInfoModel>? FindPackage(Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token);

        //BasePackageForeignKeyInfoModel
    }

    public class PackageInfoMessage {

        /// <summary>
        /// 包裹信息类型
        /// </summary>
        public PackagingType Type { get; set; }

        /// <summary>
        /// 包裹信息
        /// </summary>
        public PackageInfoModel? Info { get; set; }

        /// <summary>
        /// 是否成功标志
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 包裹信息类型枚举
    /// </summary>
    public enum PackagingType {

        /// <summary>
        /// 创建包裹
        /// </summary>
        [Description("创建包裹")]
        CreatePackage,

        /// <summary>
        /// 移除包裹
        /// </summary>
        [Description("移除包裹")]
        RemovePackage,

        /// <summary>
        /// 清空包裹
        /// </summary>
        [Description("清空包裹")]
        ClearPackages,

        /// <summary>
        /// 包裹填充完整信息
        /// </summary>
        [Description("包裹填充完整信息")]
        FillCompleteInfo,

        /// <summary>
        /// 包裹填充条码信息
        /// </summary>
        [Description("包裹填充条码信息")]
        FillBarcodeInfo,

        /// <summary>
        /// 包裹填充体积信息
        /// </summary>
        [Description("包裹填充体积信息")]
        FillVolumeInfo,

        /// <summary>
        /// 包裹填充重量信息
        /// </summary>
        [Description("包裹填充重量信息")]
        FillWeightInfo,

        /// <summary>
        /// 包裹填充全景图信息
        /// </summary>
        [Description("包裹填充全景图信息")]
        FillPanoramaInfo,

        /// <summary>
        /// 包裹填充Api信息
        /// </summary>
        [Description("包裹填充Api信息")]
        FillApiInfo,

        /// <summary>
        /// 包裹填充落格信息
        /// </summary>
        [Description("包裹填充落格信息")]
        FillGridInfo,

        /// <summary>
        /// 包裹填充分拣信息
        /// </summary>
        [Description("包裹填充分拣信息")]
        FillSortingInfo,

        /// <summary>
        /// 包裹填充物流信息
        /// </summary>
        [Description("包裹填充物流信息")]
        FillLogisticsInfo,

        /// <summary>
        /// 包裹填充Ocr信息
        /// </summary>
        [Description("包裹填充Ocr信息")]
        FillOcrInfo
    }
}