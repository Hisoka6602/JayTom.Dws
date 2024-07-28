using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.ComponentModel;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Device.GrayscaleDevice {

    public interface IGrayscaleDevice : ITcpOperations {

        /// <summary>
        /// 触发获取到小车集合
        /// </summary>
        event EventHandler<GrayscaleResult> ParcelLocationReceived;

        /// <summary>
        /// 触发但未识别到包裹
        /// </summary>
        event EventHandler ParcelLocationNotReceived;

        /// <summary>
        /// 发送小车号
        /// </summary>
        /// <param name="carNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SendCarNumber(int carNumber, CancellationToken token = default);

        /// <summary>
        /// 发送小车号并获取结果
        /// </summary>
        /// <param name="carNumber"></param>
        /// <param name="timeOut"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<GrayscaleResult> SendCarNumber(int carNumber, int timeOut, CancellationToken token = default);

        /// <summary>
        /// 设置矩形大小
        /// </summary>
        /// <param name="attachmentRectangle"></param>
        /// <param name="mainRectangle"></param>
        /// <param name="additionalBoxSpacePercentage"></param>
        /// <param name="minSendInterval"></param>
        public void SetRectangleSizes(Coordinates attachmentRectangle, Coordinates mainRectangle,
            int additionalBoxSpacePercentage = 20, int minSendInterval = 300);

        /// <summary>
        /// 设置区域小车数量
        /// </summary>
        /// <param name="regionCarCount"></param>
        public void SetRegionCarCount(int regionCarCount);

        /// <summary>
        /// 设置方向是否取反
        /// </summary>
        /// <param name="isReversed"></param>
        void SetDirectionReversed(bool isReversed);

        /// <summary>
        /// 设置环形数组小车数量
        /// </summary>
        /// <param name="carCount"></param>
        /// <param name="offset"></param>
        void SetCircularArrayCarCount(int carCount, int offset);

        /// <summary>
        /// 增加小车数量计算
        /// </summary>
        /// <param name="carNum"></param>
        /// <param name="additionalCarCount"></param>
        int IncreaseCarCount(int carNum, int additionalCarCount);
    }

    public class GrayscaleResult {

        /// <summary>
        /// 小车号
        /// </summary>
        [Description("小车号")]
        public int CarNumber { get; set; }

        /// <summary>
        /// 附件框信息
        /// </summary>
        [Description("附件框信息")]
        public BoxPackageInfo AttachmentRectangleBoxInfo { get; set; } = new();

        /// <summary>
        /// 主框信息
        /// </summary>
        [Description("主框信息")]
        public List<BoxPackageInfo> MainRectangleBoxInfos { get; set; } = new();

        /// <summary>
        /// 联动小车数量
        /// </summary>
        [Description("联动小车数量")]
        public int LinkedCarCount { get; set; }

        /// <summary>
        /// 中心点
        /// </summary>
        [Description("中心点")]
        public Point CenterPoint { get; set; }

        /// <summary>
        /// 返回结果时间
        /// </summary>
        [Description("返回结果时间")]
        public DateTime ResultTime { get; set; }

        public override string ToString() {
            // 构建字符串表示
            var result = $"小车号: {CarNumber}\n";
            result += $"附件框信息:\n{AttachmentRectangleBoxInfo}\n";
            result += "主框信息:\n";
            result = MainRectangleBoxInfos.Aggregate(result, (current, boxInfo) => current + $"{boxInfo}\n");
            result += $"联动小车数量: {LinkedCarCount}\n";
            result += $"中心点: {CenterPoint}\n";

            return result;
        }
    }

    public class BoxPackageInfo {

        /// <summary>
        /// 是否存在包裹
        /// </summary>
        public bool IsPackagePresent { get; set; }

        /// <summary>
        /// 包裹区域坐标
        /// </summary>
        public Coordinates PackageRegionCoordinates { get; set; }

        /// <summary>
        /// 包裹偏向
        /// </summary>
        public PackageOrientation PackageOrientation { get; set; } = PackageOrientation.Center;

        /// <summary>
        /// 偏向值
        /// </summary>
        public int OrientationValue { get; set; }

        /// <summary>
        /// 偏向百分比
        /// </summary>
        public decimal OffsetPercentage { get; set; }

        /// <summary>
        /// 包裹占比
        /// </summary>
        public decimal PackageRatio { get; set; }

        public override string ToString() {
            return $"是否存在包裹: {IsPackagePresent}, " +
                   $"包裹区域坐标: {PackageRegionCoordinates}, " +
                   $"包裹偏向: {PackageOrientation}, " +
                   $"偏向值: {OrientationValue}, " +
                   $"偏向百分比: {OffsetPercentage:P2}, " +
                   $"包裹占比: {PackageRatio:P2}";
        }
    }

    /// <summary>
    /// 包裹偏向
    /// </summary>
    public enum PackageOrientation {
        /// <summary>
        /// 偏左
        /// </summary>

        [Description("偏左")]
        Left,

        /// <summary>
        /// 偏右
        /// </summary>
        [Description("偏右")]
        Right,

        /// <summary>
        /// 居中
        /// </summary>
        [Description("居中")]
        Center
    }

    public struct Coordinates {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }

        public Coordinates(int x1, int y1, int x2, int y2) {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public override string ToString() {
            return $"({X1}, {Y1}), ({X2}, {Y2})";
        }
    }
}