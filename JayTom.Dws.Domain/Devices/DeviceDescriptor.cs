// DWS-COHESIVE-CONTRACTS: 设备标识、能力和值对象共同描述一个设备。
namespace JayTom.Dws.Domain.Devices;

/// <summary>表示稳定设备标识。</summary>
public readonly record struct DeviceId(string Value) {
    /// <summary>创建规范化设备标识。</summary>
    public static DeviceId From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new DeviceId(value.Trim());
    }
}

/// <summary>定义设备可声明的正交能力。</summary>
[Flags]
public enum DeviceCapabilities {
    /// <summary>无能力。</summary>
    None = 0,
    /// <summary>采集条码。</summary>
    Barcode = 1,
    /// <summary>采集重量。</summary>
    Weight = 2,
    /// <summary>采集体积。</summary>
    Volume = 4,
    /// <summary>采集图像。</summary>
    Image = 8,
    /// <summary>发送分拣指令。</summary>
    SortingOutput = 16,
    /// <summary>支持健康检查。</summary>
    HealthCheck = 32
}

/// <summary>表示设备及其能力的不可变描述。</summary>
public sealed record DeviceDescriptor(
    DeviceId Id,
    string AdapterKey,
    DeviceCapabilities Capabilities) {
    /// <summary>判断设备是否具备全部指定能力。</summary>
    public bool Supports(DeviceCapabilities required) =>
        (Capabilities & required) == required;
}
