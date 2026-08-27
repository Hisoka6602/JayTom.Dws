using System.Text.Json;
using JayTom.Dws.Plugin.Contracts;
using JayTom.Dws.Plugin.Runtime;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Plugin;

/// <summary>验证插件动态发现、失败隔离与卸载契约。</summary>
public sealed class PluginRuntimeTests
{
    /// <summary>测试宿主与清单共享的语义版本。</summary>
    private static readonly Version HostVersion = new(1, 0, 0);

    /// <summary>验证有效插件由独立可回收上下文加载并可显式释放。</summary>
    [Fact]
    public async Task Discovers_plugin_in_collectible_context_and_unloads_it()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            await WritePluginAsync(root, "sample-plugin");
            var runtime = new PluginRuntime(
                new PluginManifestValidator(),
                HostVersion,
                contractMajorVersion: 1,
                new DevelopmentPluginPackageVerifier());

            PluginDiscoveryResult result = await runtime.DiscoverAsync(root);

            Assert.Collection(result.Plugins, _ => { });
            PluginHandle plugin = result.Plugins[0];
            Assert.Empty(result.Diagnostics);
            Assert.Equal(nameof(DynamicTestPlugin), plugin.Instance.Name);
            Assert.True(plugin.LoadContextReference.IsAlive);

            await plugin.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => plugin.Instance);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>验证损坏插件不会阻止同目录中的有效插件完成加载。</summary>
    [Fact]
    public async Task Invalid_plugin_is_isolated_from_valid_plugins()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            string validDirectory = Path.Combine(root, "valid");
            Directory.CreateDirectory(validDirectory);
            await WritePluginAsync(validDirectory, "valid-plugin");
            string invalidDirectory = Path.Combine(root, "invalid");
            Directory.CreateDirectory(invalidDirectory);
            await File.WriteAllTextAsync(
                Path.Combine(invalidDirectory, "plugin.json"),
                "{ invalid json");
            var runtime = new PluginRuntime(
                new PluginManifestValidator(),
                HostVersion,
                contractMajorVersion: 1,
                new DevelopmentPluginPackageVerifier());

            PluginDiscoveryResult result = await runtime.DiscoverAsync(root);

            Assert.Collection(result.Plugins, _ => { });
            Assert.Collection(result.Diagnostics, _ => { });
            PluginHandle plugin = result.Plugins[0];
            PluginLoadDiagnostic diagnostic = result.Diagnostics[0];
            Assert.Equal(PluginLoadStatus.InvalidManifest, diagnostic.Status);
            Assert.Equal("valid-plugin", plugin.Manifest.PluginKey);
            await plugin.DisposeAsync();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>创建本次测试独占的临时插件根目录。</summary>
    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"dws-plugin-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>复制测试插件程序集并写入兼容清单。</summary>
    private static async Task WritePluginAsync(string directory, string pluginKey)
    {
        Directory.CreateDirectory(directory);
        const string assemblyFileName = "DynamicTestPlugin.dll";
        File.Copy(
            typeof(DynamicTestPlugin).Assembly.Location,
            Path.Combine(directory, assemblyFileName),
            overwrite: true);
        var manifest = new PluginManifest
        {
            PluginKey = pluginKey,
            Name = "动态测试插件",
            Version = HostVersion.ToString(3),
            MinimumHostVersion = HostVersion.ToString(3),
            ContractMajorVersion = 1,
            EntryPoint = $"{assemblyFileName}::{typeof(DynamicTestPlugin).FullName}"
        };
        await File.WriteAllTextAsync(
            Path.Combine(directory, "plugin.json"),
            JsonSerializer.Serialize(manifest));
    }
}
