using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class CreatePackageSettingsDto {

        /// <summary>
        /// 是否使用包裹过期
        /// </summary>
        public bool IsUsePackageExpiry { get; set; }

        /// <summary>
        /// 包裹过期时间(设置为0则不验证)
        /// </summary>
        public int PackageExpiryTime { get; set; }

        /// <summary>
        /// 是否使用空包裹过期
        /// </summary>
        public bool IsUseEmptyPackageExpiry { get; set; }

        /// <summary>
        /// 空包裹过期时间(设置为0则不验证)
        /// </summary>
        public int EmptyPackageExpiryTime { get; set; }

        /// <summary>
        /// 多条码返回处理方式
        /// </summary>
        public BarcodeHandlingMethodEnum BarcodeHandlingMethod { get; set; } = BarcodeHandlingMethodEnum.UseOneBarcode;

        /// <summary>
        /// 创建包裹方式
        /// </summary>
        public PackageCreationMethodsEnum PackageCreationMethods { get; set; } =
            PackageCreationMethodsEnum.ScanBarcodeCamera;

        /// <summary>
        /// 是否使用NoRead
        /// </summary>
        public bool IsUseNoRead { get; set; } = true;

        /// <summary>
        /// 填充条码队列
        /// </summary>
        public BarcodeQueueOrderEnum BarcodeQueueOrder { get; set; } = BarcodeQueueOrderEnum.TimeAscending;

        /// <summary>
        /// 移除包裹方式
        /// </summary>
        public PackageRemoveMethodsEnum PackageRemoveMethods { get; set; } = PackageRemoveMethodsEnum.FillInformation;

        /// <summary>
        /// 停止时是否清空包裹
        /// </summary>
        public bool ClearPackageQueueOnStop { get; set; } = true;
    }

    public enum BarcodeHandlingMethodEnum {

        /// <summary>
        /// 使用一个
        /// </summary>
        UseOneBarcode,

        /// <summary>
        /// 合并条码
        /// </summary>
        MergeBarcodes
    }

    [Flags]
    public enum PackageCreationMethodsEnum {

        /// <summary>
        /// 扫码相机
        /// </summary>
        ScanBarcodeCamera = 1,

        /// <summary>
        /// 稳定重量
        /// </summary>
        StableWeight = 2,

        /// <summary>
        /// 控件输入
        /// </summary>
        ControlInput = 4,

        /// <summary>
        /// 体积输入
        /// </summary>
        VolumeInput = 8,

        /// <summary>
        /// 下位机创建
        /// </summary>
        LowerMachineCreation = 16,

        /// <summary>
        /// Tcp内容输入
        /// </summary>
        TcpInput = 32,

        /// <summary>
        /// Ocr信息
        /// </summary>
        OcrInfo = 64
    }

    public enum PackageRemoveMethodsEnum {

        /// <summary>
        /// 填充完整信息
        /// </summary>
        FillInformation,

        /// <summary>
        /// 下位机移除
        /// </summary>
        LowerMachineRemoval
    }

    public enum BarcodeQueueOrderEnum {

        /// <summary>
        /// 时间正序
        /// </summary>
        TimeAscending,

        /// <summary>
        /// 时间倒序
        /// </summary>
        TimeDescending
    }
}