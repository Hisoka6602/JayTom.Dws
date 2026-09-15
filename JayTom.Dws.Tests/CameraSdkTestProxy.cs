using System.Reflection;

namespace JayTom.Dws.Tests;

/// <summary>在无需厂商原生库或物理相机的测试中模拟 SDK 接口。</summary>
public class CameraSdkTestProxy : DispatchProxy {
    /// <summary>处理模拟 SDK 方法及属性访问。</summary>
    private Func<MethodInfo, object?[], object?>? _handler;

    /// <summary>创建指定厂商接口的可观测代理。</summary>
    public static T Create<T>(Func<MethodInfo, object?[], object?> handler) where T : class {
        var instance = Create<T, CameraSdkTestProxy>();
        ((CameraSdkTestProxy)(object)instance)._handler = handler;
        return instance;
    }

    /// <summary>将调用及引用参数交给测试提供的处理器。</summary>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        _handler!(targetMethod!, args ?? []);
}
