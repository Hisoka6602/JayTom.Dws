using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace JayTom.Dws.Ocr;

/// <summary>在 Windows 图像适配边界转换位图与平台中立 OCR 图像载荷。</summary>
public static class OcrBitmapAdapter
{
    /// <summary>将位图编码为不持有原生资源的 OCR 图像载荷。</summary>
    public static OcrImageFrame Encode(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return new OcrImageFrame
        {
            Data = stream.GetBuffer().AsMemory(0, checked((int)stream.Length)),
            MediaType = "image/png",
            Width = bitmap.Width,
            Height = bitmap.Height
        };
    }

    /// <summary>将 OCR 图像载荷解码为由调用方负责释放的新位图。</summary>
    public static Bitmap Decode(OcrImageFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        using var stream = new MemoryStream(frame.Data.Length);
        stream.Write(frame.Data.Span);
        stream.Position = 0;
        using var decoded = new Bitmap(stream);
        return new Bitmap(decoded);
    }
}
