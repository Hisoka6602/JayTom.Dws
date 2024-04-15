using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.AppDto {

    public class SyncSettingsDto {

        /// <summary>
        /// 是否使用设置同步
        /// </summary>
        public bool IsUseSyncSettings { get; set; }

        /// <summary>
        /// 同步的 URL。
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用算法配置同步。
        /// </summary>
        public bool IsUseAlgorithmSync { get; set; }

        /// <summary>
        /// 是否使用 API 接口配置同步。
        /// </summary>
        public bool IsUseApiSync { get; set; }

        /// <summary>
        /// 是否使用存图配置同步。
        /// </summary>
        public bool IsUseImageStorageSync { get; set; }

        /// <summary>
        /// 是否使用过滤配置同步。
        /// </summary>
        public bool IsUseFilterSync { get; set; }

        /// <summary>
        /// 是否使用内容输入配置同步。
        /// </summary>
        public bool IsUseContentInputSync { get; set; }

        /// <summary>
        /// 是否使用连接配置同步。
        /// </summary>
        public bool IsUseConnectionSync { get; set; }

        /// <summary>
        /// 是否使用出口配置同步。
        /// </summary>
        public bool IsUseExitSync { get; set; }

        /// <summary>
        /// 是否使用指令配置同步。
        /// </summary>
        public bool IsUseInstructionSync { get; set; }

        /// <summary>
        /// 是否使用物流配置同步。
        /// </summary>
        public bool IsUseLogisticsSync { get; set; }

        /// <summary>
        /// 是否使用分拣模式配置同步。
        /// </summary>
        public bool IsUseSortingModeSync { get; set; }

        /// <summary>
        /// 是否使用锁格配置同步。
        /// </summary>
        public bool IsUseLockerExitSync { get; set; }

        /// <summary>
        /// 是否使用叠包配置同步。
        /// </summary>
        public bool IsUseStackingSync { get; set; }

        /// <summary>
        /// 是否使用供包台配置同步
        /// </summary>
        public bool IsUseSupplyCounterSync { get; set; }

        /// <summary>
        /// 是否组包配置同步。
        /// </summary>
        public bool IsUsePackagingSync { get; set; }

        /// <summary>
        /// 是否使用 OCR 配置同步。
        /// </summary>
        public bool IsUseOcrSync { get; set; }

        /// <summary>
        /// 是否使用云端配置同步。
        /// </summary>
        public bool IsUseCloudSync { get; set; }

        /// <summary>
        /// 是否使用空间清理配置同步。
        /// </summary>
        public bool IsUseSpaceCleaningSync { get; set; }
    }
}