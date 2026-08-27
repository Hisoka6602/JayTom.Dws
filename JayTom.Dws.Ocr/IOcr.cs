using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Ocr {

    public interface IOcr : IDisposable {

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
        event EventHandler<OcrResult> OcrContentRecognized;

        /// <summary>
        /// 当发生鉴权异常时触发的事件
        /// </summary>
        event EventHandler<AuthenticationExceptionEventArgs> AuthenticationExceptionOccurred;

        /// <summary>
        /// 状态
        /// </summary>
        OcrStatus OcrStatus { get; }

        /// <summary>
        /// 提交图片进行OCR处理
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="cameraSerialNumber"></param>
        Task SubmitImage(
            OcrImageFrame image,
            string cameraSerialNumber,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析Ocr
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <returns></returns>
        OcrResult? ParseOcrResult(OcrImageFrame image);

        /// <summary>
        /// 临时解析Ocr
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="cropImageModelPath"></param>
        /// <param name="confidenceThreshold"></param>
        /// <param name="rectangleScale"></param>
        /// <returns></returns>
        OcrResult? ParseOcrTemporarilyResult(OcrImageFrame image, string cropImageModelPath,
            decimal confidenceThreshold, decimal rectangleScale);

        /// <summary>
        /// 解析Ocr
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <returns></returns>
        Task<OcrResult?> ParseOcrResultAsync(
            OcrImageFrame image,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置OCR参数
        /// </summary>
        /// <param name="parameters"></param>
        Task<KeyValuePair<bool, string>> SetOcrParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// 设置截图算法文件路径
        /// </summary>
        /// <param name="onnxModelPath"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetOnnxModelPath(string onnxModelPath);

        /// <summary>
        /// 设置置信度
        /// </summary>
        /// <param name="confidenceThreshold"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetConfidenceThreshold(decimal confidenceThreshold);

        /// <summary>
        /// 设置区域扩展
        /// </summary>
        /// <param name="rectangleScale"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetRectangleScale(decimal rectangleScale);

        /// <summary>
        /// 设置解析超时时间
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetRecognitionTimeout(TimeSpan timeout);

        /// <summary>
        /// 是否使用二次确认
        /// </summary>
        /// <param name="isUse"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetIsSecondConfirmationEnabled(bool isUse);

        /// <summary>
        /// 初始化
        /// </summary>
        Task<KeyValuePair<bool, string>> Initialize();
    }

    public class OcrResult : EventArgs {

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
        /// 发件人地址
        /// </summary>
        public string SenderAddress { get; set; } = string.Empty;

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
        /// 获取或设置识别耗时（毫秒）。
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>旧版识别耗时字段；仅为二进制迁移保留。</summary>
        [Obsolete("请改用 ElapsedMilliseconds；该兼容属性将在 v2 删除。", false)]
        public long ElapsedTime
        {
            get => ElapsedMilliseconds;
            set => ElapsedMilliseconds = value;
        }

        /// <summary>
        /// 获取或设置图片。
        /// </summary>
        public OcrImageFrame? Image { get; set; }

        /// <summary>
        /// 获取或设置缩略图。
        /// </summary>
        public OcrImageFrame? Thumbnail { get; set; }

        /// <summary>
        /// 获取事件分发期间借用的裁剪图片；需要延长生命周期时调用 <see cref="TakeCropImage"/>。
        /// </summary>
        public OcrImageFrame? CropImage { get; set; }

        /// <summary>取得裁剪图片所有权，并从事件参数中移除该图片。</summary>
        public OcrImageFrame? TakeCropImage() {
            var image = CropImage;
            CropImage = null;
            return image;
        }

        /// <summary>
        /// 裁剪区域
        /// </summary>
        public Rectangle? CropRectangle { get; set; }

        /// <summary>
        /// 获取或设置识别时间戳。
        /// </summary>
        public long RecognitionTimestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 提交图时间
        /// </summary>
        public long SubmitTimestamp { get; set; }

        /// <summary>
        /// 条码区域
        /// </summary>
        public List<decimal>? BarcodeArea { get; set; }

        /// <summary>
        /// 三段码区域
        /// </summary>
        public List<decimal>? ThreeSegmentArea { get; set; }

        /// <summary>
        /// 收件人地址区域
        /// </summary>
        public List<decimal>? RecipientAddressArea { get; set; }

        /// <summary>
        /// 寄件人地址区域
        /// </summary>
        public List<decimal>? SenderAddressArea { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
    }

}
