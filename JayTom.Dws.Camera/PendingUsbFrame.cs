using System.Drawing;

namespace JayTom.Dws.Camera;

/// <summary>保存已经脱离相机 SDK 生命周期的 USB 帧及其读码决策。</summary>
internal sealed record PendingUsbFrame(Bitmap Image, bool ShouldDecode);
