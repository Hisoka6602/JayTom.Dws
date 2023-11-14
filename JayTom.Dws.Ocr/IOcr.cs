using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace JayTom.Dws.Ocr {

    public interface IOcr : IDisposable {

        //Ocr异常事件
        //Ocr初始化异常事件
        //Ocr识别到内容事件
        //鉴权异常事件
        //提交图片方法
        //设置参数方法
        //状态字段(未初始化、已断开、异常、运行中)
        /// <summary>
        /// 当发生OCR异常时触发的事件
        /// </summary>
        event EventHandler<OcrExceptionEventArgs> OcrExceptionOccurred;

        /// <summary>
        /// 当发生OCR初始化异常时触发的事件
        /// </summary>
        event EventHandler<OcrInitializationExceptionEventArgs> OcrInitializationExceptionOccurred;

        /// <summary>
        /// 当OCR识别到内容时触发的事件
        /// </summary>
        event EventHandler<OcrContentRecognizedEventArgs> OcrContentRecognized;

        /// <summary>
        /// 当发生鉴权异常时触发的事件
        /// </summary>
        event EventHandler<AuthenticationExceptionEventArgs> AuthenticationExceptionOccurred;

        /// <summary>
        /// 状态
        /// </summary>
        OcrStatus Status { get; }

        /// <summary>
        /// 提交图片进行OCR处理
        /// </summary>
        /// <param name="imageBytes"></param>
        void SubmitImage(Bitmap imageBytes);

        /// <summary>
        /// 设置OCR参数
        /// </summary>
        /// <param name="parameters"></param>
        void SetOcrParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// 初始化
        /// </summary>
        Task<KeyValuePair<bool, string>> Initialize();
    }

    public enum OcrStatus {

        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 已初始化
        /// </summary>
        Initialized,

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 故障
        /// </summary>
        Failure,

        /// <summary>
        /// 暂停中
        /// </summary>
        Paused
    }

    // 自定义事件参数类

    public class OcrExceptionEventArgs : EventArgs {

        /// <summary>
        /// 异常内容
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 异常时间
        /// </summary>
        public DateTime? ExceptionTime { get; set; } = DateTime.Now;
    }

    public class OcrInitializationExceptionEventArgs : OcrExceptionEventArgs {
        // 添加与OCR初始化异常相关的任何必要属性或信息
    }

    public class OcrContentRecognizedEventArgs : EventArgs {

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码。
        /// </summary>
        public string VirtualNumber { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人地址。
        /// </summary>
        public string RecipientAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人姓名。
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人电话。
        /// </summary>
        public string RecipientPhone { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人姓名。
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人电话。
        /// </summary>
        public string SenderPhone { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置三段码。
        /// </summary>
        public string ThreeSegmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码后四位。
        /// </summary>
        public string VirtualNumberLast4 { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置识别时间。
        /// </summary>
        public DateTime RecognitionTime { get; set; }

        /// <summary>
        /// 获取或设置耗时(ms)
        /// </summary>
        public long ElapsedTime { get; set; }

        /// <summary>
        /// 获取或设置图片。
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 获取或设置缩略图。
        /// </summary>
        public Bitmap? Thumbnail { get; set; }

        /// <summary>
        /// 获取或设置识别时间戳。
        /// </summary>
        public long RecognitionTimestamp { get; set; }
    }

    public class AuthenticationExceptionEventArgs : OcrExceptionEventArgs {
        // 添加与鉴权异常相关的任何必要属性或信息
    }
}