using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera {

    public interface ISecurityCamera : ICamera {

        /// <summary>
        /// 实时预览事件
        /// </summary>
        event EventHandler<RealPreviewEventArgs> RealPreview;

        /// <summary>
        /// 保存流
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SaveStream(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> Zoom(double zoomFactor, CancellationToken cancellationToken = default);

        /// <summary>
        /// 云台控制
        /// </summary>
        /// <param name="panAngle"></param>
        /// <param name="tiltAngle"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> ControlPtz(double panAngle, double tiltAngle, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置步长
        /// </summary>
        /// <param name="stepSize"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetStepSize(int stepSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置焦距
        /// </summary>
        /// <param name="focalLength"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetFocalLength(double focalLength, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置光圈
        /// </summary>
        /// <param name="aperture"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetAperture(double aperture, CancellationToken cancellationToken = default);

        /// <summary>
        /// 开启实时预览
        /// </summary>
        Task<KeyValuePair<bool, string>> StartPreview(CancellationToken cancellationToken = default);

        /// <summary>
        /// 关闭实时预览
        /// </summary>
        void StopPreview(CancellationToken cancellationToken = default);

        /// <summary>
        /// 照片回调
        /// </summary>
        public event EventHandler<PhotoTakenEventArgs> PhotoTaken;

        /// <summary>
        /// 拍照
        /// </summary>
        /// <returns></returns>
        Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default);
    }

    public class RealPreviewEventArgs {
    }
}