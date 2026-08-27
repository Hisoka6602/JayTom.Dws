using System.Reflection;
using System.Runtime.Loader;
using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Plugin.Runtime;

/// <summary>在可回收上下文中解析单个插件的私有依赖。</summary>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    /// <summary>插件程序集依赖解析器。</summary>
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>创建一个支持卸载的插件加载上下文。</summary>
    public PluginLoadContext(string mainAssemblyPath)
        : base($"dws-plugin:{Path.GetFileNameWithoutExtension(mainAssemblyPath)}", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    /// <summary>解析插件私有托管依赖，同时复用默认上下文中的公共契约。</summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == typeof(IPlugin).Assembly.GetName().Name)
        {
            return null;
        }

        string? dependencyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return dependencyPath is null ? null : LoadFromAssemblyPath(dependencyPath);
    }

    /// <summary>解析插件私有原生依赖。</summary>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? dependencyPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return dependencyPath is null
            ? nint.Zero
            : LoadUnmanagedDllFromPath(dependencyPath);
    }
}
