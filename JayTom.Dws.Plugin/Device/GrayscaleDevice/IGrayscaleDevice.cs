using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Device.GrayscaleDevice {

    public interface IGrayscaleDevice : IDisposable {

        /// <summary>
        /// 中心坐标
        /// </summary>
        Point CenterCoordinates { get; }

        /// <summary>
        /// 触发获取到小车集合
        /// </summary>
        event EventHandler<GrayscaleResult> ParcelLocationReceived;

        /// <summary>
        /// 触发但未识别到包裹
        /// </summary>
        event EventHandler ParcelLocationNotReceived;

        /// <summary>
        /// 连接
        /// </summary>
        Task<bool> Connect(string ip, int port, CancellationToken token);

        /// <summary>
        /// 发送小车号
        /// </summary>
        /// <param name="carNumber"></param>
        /// <returns></returns>
        Task<bool> SendCarNumber(int carNumber, CancellationToken token);
    }

    public class GrayscaleResult {

        /// <summary>
        /// 小车号
        /// </summary>
        [Description("小车号")]
        public int CarNumber { get; set; }

        /// <summary>
        /// 是否存在小车框
        /// </summary>
        [Description("是否存在小车框")]
        public bool CarFrameExists { get; set; }

        /// <summary>
        ///  小车中心点坐标
        /// </summary>
        [Description("小车中心点坐标")]
        public Point CarCenter { get; set; }

        /// <summary>
        /// 是否存在风琴罩
        /// </summary>
        [Description("是否存在风琴罩")]
        public bool AccordionExists { get; set; }

        /// <summary>
        /// 风琴罩中心点坐标
        /// </summary>
        [Description("风琴罩中心点坐标")]
        public Point AccordionCenter { get; set; }

        /// <summary>
        /// 小车上的包裹面积
        /// </summary>
        [Description("小车上的包裹面积")]
        public double ParcelAreaOnCar { get; set; }

        /// <summary>
        /// 风琴罩上的包裹面积
        /// </summary>
        [Description("风琴罩上的包裹面积")]
        public double ParcelAreaOnAccordion { get; set; }

        /// <summary>
        /// 是否超过小车左边
        /// </summary>
        [Description("是否超过小车左边")]
        public bool IsExceedsCarLeft { get; set; }

        /// <summary>
        /// 是否超过小车右边
        /// </summary>
        [Description("是否超过小车右边")]
        public bool IsExceedsCarRight { get; set; }

        /// <summary>
        /// 是否超过风琴罩左边
        /// </summary>
        [Description("是否超过风琴罩左边")]
        public bool IsExceedsAccordionLeft { get; set; }
    }
}