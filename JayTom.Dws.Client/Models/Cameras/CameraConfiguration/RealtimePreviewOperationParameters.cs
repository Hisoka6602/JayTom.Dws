using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using EFCore.BulkExtensions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras.CameraConfiguration {

    public class RealtimePreviewOperationParameters : BindableBase {
        public NvrRealTimePreviewItemInfo? ItemInfo { get; set; }

        /// <summary>
        /// 实时预览操作
        /// </summary>
        public NvrPreviewAction Action { get; set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        public NvrPreviewOperationType Type { get; set; }

        public RealtimePreviewOperationParameters(NvrRealTimePreviewItemInfo itemInfo, NvrPreviewAction action, NvrPreviewOperationType type) {
            ItemInfo = itemInfo;
            Action = action;
            Type = type;
        }
    }

    // Nvr实时预览枚举: 缩放、焦距、自动聚焦
    public enum NvrPreviewAction {

        /// <summary>
        /// 增加缩放倍率
        /// </summary>
        IncreaseZoom,

        /// <summary>
        /// 减少缩放倍率
        /// </summary>
        DecreaseZoom,

        /// <summary>
        /// 增加焦距
        /// </summary>
        IncreaseFocus,

        /// <summary>
        /// 减少焦距
        /// </summary>
        DecreaseFocus,

        /// <summary>
        /// 自动焦距
        /// </summary>
        AutoFocus
    }

    public enum NvrPreviewOperationType {

        /// <summary>
        /// 开始
        /// </summary>
        Start,

        /// <summary>
        /// 停止
        /// </summary>
        Stop,

        /// <summary>
        /// 自动
        /// </summary>
        Auto
    }
}