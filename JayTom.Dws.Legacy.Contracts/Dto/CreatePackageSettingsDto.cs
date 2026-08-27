using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto {

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

        /// <summary>
        /// 是否使用NoRead过滤
        /// </summary>
        public bool IsUseNoReadFilter { get; set; }

        /// <summary>
        /// NoRead间隔时间
        /// </summary>
        public int FilterInterval { get; set; } = 100;

        /// <summary>
        /// 最小创建包裹间隔时间
        /// </summary>
        public int PackageCreationInterval { get; set; } = 150;

        /// <summary>
        /// 是否使用条码赋值间隔
        /// </summary>
        public bool IsUseBarcodeAssignmentInterval { get; set; }

        /// <summary>
        /// 最小赋值间隔
        /// </summary>
        public int MinimumAssignmentTime { get; set; }

        /// <summary>
        /// 最大赋值间隔
        /// </summary>
        public int MaximumAssignmentTime { get; set; }

        /// <summary>
        /// 校验创建包裹及条码赋值配置，所有时间边界均使用客户端提供的值。
        /// </summary>
        /// <param name="validationMessage">配置无效时返回可直接展示的原因。</param>
        /// <returns>配置可以安全用于运行时热路径时返回 <see langword="true"/>。</returns>
        public bool TryValidate(out string validationMessage) {
            if (PackageCreationInterval < 0) {
                validationMessage = "最小创建包裹间隔不能小于 0ms";
                return false;
            }

            if (IsUsePackageExpiry && PackageExpiryTime <= 0) {
                validationMessage = "启用包裹过期后，包裹有效时间必须大于 0ms";
                return false;
            }

            if (IsUseEmptyPackageExpiry && EmptyPackageExpiryTime <= 0) {
                validationMessage = "启用空包裹过期后，空包裹有效时间必须大于 0ms";
                return false;
            }

            if (IsUseBarcodeAssignmentInterval) {
                if (MinimumAssignmentTime < 0 || MaximumAssignmentTime <= 0) {
                    validationMessage = "条码赋值最小间隔不能小于 0ms，最大间隔必须大于 0ms";
                    return false;
                }

                if (MaximumAssignmentTime < MinimumAssignmentTime) {
                    validationMessage = "条码赋值最大间隔不能小于最小间隔";
                    return false;
                }
            }

            validationMessage = string.Empty;
            return true;
        }
    }

    public enum BarcodeHandlingMethodEnum {

        /// <summary>
        /// 使用一个
        /// </summary>
        [Description("使用一个")]
        UseOneBarcode,

        /// <summary>
        /// 使用多个条码
        /// </summary>
        [Description("使用多个条码")]
        UseMultipleBarcodes,

        /// <summary>
        /// 合并条码
        /// </summary>
        [Description("合并条码")]
        MergeBarcodes,

        //使用多个条码
    }

    [Flags]
    public enum PackageCreationMethodsEnum {

        /// <summary>
        /// 扫码相机
        /// </summary>
        [Description("扫码相机")]
        ScanBarcodeCamera = 1,

        /// <summary>
        /// 稳定重量
        /// </summary>
        [Description("稳定重量")]
        StableWeight = 2,

        /// <summary>
        /// 控件输入
        /// </summary>
        [Description("控件输入")]
        ControlInput = 4,

        /// <summary>
        /// 体积输入
        /// </summary>
        [Description("体积输入")]
        VolumeInput = 8,

        /// <summary>
        /// 下位机创建
        /// </summary>
        [Description("下位机创建")]
        LowerMachineCreation = 16,

        /// <summary>
        /// Tcp内容输入
        /// </summary>
        [Description("Tcp内容输入")]
        TcpInput = 32,

        /// <summary>
        /// Ocr信息
        /// </summary>
        [Description("Ocr信息")]
        OcrInfo = 64,

        /// <summary>
        /// 扫码枪输入
        /// </summary>
        [Description("扫码枪输入")]
        BarcodeScannerInput = 128
    }

    public enum PackageRemoveMethodsEnum {

        /// <summary>
        /// 不移除
        /// </summary>
        [Description("无")]
        None,

        /// <summary>
        /// 填充完整信息
        /// </summary>
        [Description("填充完整信息")]
        FillInformation,

        /// <summary>
        /// 下位机移除
        /// </summary>
        [Description("下位机移除")]
        LowerMachineRemoval,
    }

    public enum BarcodeQueueOrderEnum {

        /// <summary>
        /// 时间正序
        /// </summary>
        [Description("时间正序")]
        TimeAscending,

        /// <summary>
        /// 时间倒序
        /// </summary>
        [Description("时间倒序")]
        TimeDescending
    }
}
