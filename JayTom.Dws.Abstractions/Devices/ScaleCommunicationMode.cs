using System.ComponentModel;

namespace JayTom.Dws.Abstractions.Devices;

/// <summary>电子秤通信方式。</summary>
public enum ScaleCommunicationMode {
    /// <summary>串口通信。</summary>
    [Description("串口")]
    SerialPort,

    /// <summary>TCP 通信。</summary>
    [Description("TCP")]
    Tcp
}
