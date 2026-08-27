using System.Drawing.Imaging;

namespace JayTom.Dws.Camera.Nvr.Legacy;

/// <summary>封装 NVR 实时预览帧数据。</summary>
public class RealTimePreviewEventArgs : EventArgs {
    /// <summary>获取或设置 YUV 帧数据。</summary>
    public byte[]? YuvData { get; set; }

    /// <summary>获取或设置原始帧数据流。</summary>
    public Stream? Data { get; set; }

    /// <summary>获取或设置本机内存区段地址。</summary>
    public IntPtr Section { get; set; }

    /// <summary>获取或设置像素格式。</summary>
    public PixelFormat Format { get; set; }

    /// <summary>获取或设置每行字节跨度。</summary>
    public int Stride { get; set; }

    /// <summary>获取或设置帧数据偏移。</summary>
    public int Offset { get; set; }
}
