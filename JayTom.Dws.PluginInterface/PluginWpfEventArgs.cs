// DWS-COHESIVE-CONTRACTS: WPF 事件、弹窗请求与 ABI 版本必须同步发布。
using System;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface;

/// <summary>表示 WPF 插件 ABI 中的不可变、可版本化 UI 事件。</summary>
public sealed class PluginWpfEventArgs : EventArgs {
    /// <summary>创建 UI 事件。</summary>
    public PluginWpfEventArgs(
        string eventName,
        IReadOnlyDictionary<string, string>? values = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        EventName = eventName;
        Values = values ?? new Dictionary<string, string>();
    }

    /// <summary>获取事件名称。</summary>
    public string EventName { get; }

    /// <summary>获取只读字符串载荷。</summary>
    public IReadOnlyDictionary<string, string> Values { get; }
}

/// <summary>表示 WPF 插件弹窗请求。</summary>
public sealed record PluginDialogRequest(
    string Title,
    string Message,
    IReadOnlyDictionary<string, string> Values);

/// <summary>定义宿主与 WPF 插件间的显式 ABI 版本。</summary>
public static class PluginWpfAbi {
    /// <summary>当前 ABI 主版本。</summary>
    public const int MajorVersion = 1;
}
