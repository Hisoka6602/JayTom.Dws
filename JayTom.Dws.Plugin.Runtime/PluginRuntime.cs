using System.Reflection;
using System.Text.Json;
using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Plugin.Runtime;

/// <summary>发现、验证、隔离加载并卸载目录插件。</summary>
public sealed class PluginRuntime
{
    /// <summary>允许读取的清单最大字节数。</summary>
    private const long MaximumManifestLength = 64 * 1024;

    /// <summary>插件兼容性验证器。</summary>
    private readonly IPluginManifestValidator _manifestValidator;

    /// <summary>当前宿主版本。</summary>
    private readonly Version _hostVersion;

    /// <summary>当前宿主支持的契约主版本。</summary>
    private readonly int _contractMajorVersion;

    /// <summary>在程序集进入加载上下文前执行的信任验证器。</summary>
    private readonly IPluginPackageVerifier _packageVerifier;

    /// <summary>创建插件运行时。</summary>
    public PluginRuntime(
        IPluginManifestValidator manifestValidator,
        Version hostVersion,
        int contractMajorVersion,
        IPluginPackageVerifier packageVerifier)
    {
        ArgumentNullException.ThrowIfNull(manifestValidator);
        ArgumentNullException.ThrowIfNull(hostVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(contractMajorVersion, 1);
        _manifestValidator = manifestValidator;
        _hostVersion = hostVersion;
        _contractMajorVersion = contractMajorVersion;
        _packageVerifier = packageVerifier ?? throw new ArgumentNullException(nameof(packageVerifier));
    }

    /// <summary>递归发现插件清单，并将每个插件的失败与其他插件隔离。</summary>
    public async Task<PluginDiscoveryResult> DiscoverAsync(
        string pluginRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginRoot);
        string normalizedRoot = Path.GetFullPath(pluginRoot);
        if (!Directory.Exists(normalizedRoot))
        {
            return new PluginDiscoveryResult
            {
                Plugins = Array.Empty<PluginHandle>(),
                Diagnostics = Array.Empty<PluginLoadDiagnostic>()
            };
        }

        var plugins = new List<PluginHandle>();
        var diagnostics = new List<PluginLoadDiagnostic>();
        var pluginKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string manifestPath in Directory
                     .EnumerateFiles(normalizedRoot, "plugin.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            (PluginHandle? plugin, PluginLoadDiagnostic? diagnostic) =
                await TryLoadAsync(manifestPath, normalizedRoot, cancellationToken)
                    .ConfigureAwait(false);
            if (diagnostic is not null)
            {
                diagnostics.Add(diagnostic);
                continue;
            }

            if (plugin is null)
            {
                continue;
            }

            if (!pluginKeys.Add(plugin.Manifest.PluginKey))
            {
                diagnostics.Add(CreateDiagnostic(
                    manifestPath,
                    plugin.Manifest.PluginKey,
                    PluginLoadStatus.DuplicateKey,
                    "插件标识重复，后发现的插件已隔离。"));
                await plugin.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            plugins.Add(plugin);
        }

        return new PluginDiscoveryResult
        {
            Plugins = plugins,
            Diagnostics = diagnostics
        };
    }

    /// <summary>读取并加载单个插件，所有预期失败均转换为诊断。</summary>
    private async Task<(PluginHandle? Plugin, PluginLoadDiagnostic? Diagnostic)> TryLoadAsync(
        string manifestPath,
        string pluginRoot,
        CancellationToken cancellationToken)
    {
        PluginManifest? manifest = null;
        PluginLoadContext? loadContext = null;
        try
        {
            var manifestFile = new FileInfo(manifestPath);
            if (manifestFile.Length is <= 0 or > MaximumManifestLength)
            {
                return Failure(PluginLoadStatus.InvalidManifest, "插件清单大小无效。");
            }

            await using FileStream stream = new(
                manifestPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            manifest = await JsonSerializer.DeserializeAsync<PluginManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken).ConfigureAwait(false);
            if (manifest is null)
            {
                return Failure(PluginLoadStatus.InvalidManifest, "插件清单内容为空。");
            }

            PluginCompatibilityResult compatibility = _manifestValidator.Validate(
                manifest,
                _hostVersion,
                _contractMajorVersion);
            if (!compatibility.IsCompatible)
            {
                return Failure(PluginLoadStatus.Incompatible, compatibility.Message);
            }

            string[] entryPointParts = manifest.EntryPoint.Split(
                "::",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (entryPointParts.Length != 2)
            {
                return Failure(
                    PluginLoadStatus.InvalidEntryPoint,
                    "入口点必须使用“程序集文件::完整类型名”格式。");
            }

            string pluginDirectory = Path.GetDirectoryName(manifestPath)!;
            string assemblyPath = Path.GetFullPath(
                Path.Combine(pluginDirectory, entryPointParts[0]));
            if (!IsWithinRoot(assemblyPath, pluginDirectory) ||
                !IsWithinRoot(assemblyPath, pluginRoot) ||
                !Path.GetExtension(assemblyPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    PluginLoadStatus.InvalidEntryPoint,
                    "入口程序集必须是插件目录内的 DLL 文件。");
            }
            if (!File.Exists(assemblyPath))
            {
                return Failure(PluginLoadStatus.AssemblyNotFound, "入口程序集不存在。");
            }

            PluginPackageVerificationResult verification =
                await _packageVerifier.VerifyAsync(
                    manifest,
                    assemblyPath,
                    cancellationToken).ConfigureAwait(false);
            if (!verification.IsTrusted)
            {
                return Failure(PluginLoadStatus.UntrustedPackage, verification.Message);
            }

            loadContext = new PluginLoadContext(assemblyPath);
            await using FileStream assemblyStream = new(
                assemblyPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            Assembly assembly = loadContext.LoadFromStream(assemblyStream);
            Type? entryType = assembly.GetType(entryPointParts[1], throwOnError: false);
            if (entryType is null)
            {
                loadContext.Unload();
                return Failure(PluginLoadStatus.TypeNotFound, "入口类型不存在。");
            }
            if (!typeof(IPlugin).IsAssignableFrom(entryType))
            {
                loadContext.Unload();
                return Failure(PluginLoadStatus.ContractMismatch, "入口类型未实现 IPlugin。");
            }
            if (Activator.CreateInstance(entryType) is not IPlugin instance)
            {
                loadContext.Unload();
                return Failure(PluginLoadStatus.ActivationFailed, "入口类型无法创建实例。");
            }

            if (instance is IInitializePlugin initializable)
            {
                var initialization = initializable.Initialize();
                if (initialization.IsFailure)
                {
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    loadContext.Unload();
                    return Failure(
                        PluginLoadStatus.ActivationFailed,
                        $"插件初始化失败：{initialization.Error.Code}");
                }
            }

            return (new PluginHandle(manifest, instance, loadContext), null);
        }
        catch (OperationCanceledException)
        {
            loadContext?.Unload();
            throw;
        }
        catch (Exception exception)
        {
            loadContext?.Unload();
            return Failure(
                manifest is null
                    ? PluginLoadStatus.InvalidManifest
                    : PluginLoadStatus.ActivationFailed,
                "插件加载失败。",
                exception);
        }

        /// <summary>将当前清单的失败转换为隔离诊断。</summary>
        (PluginHandle?, PluginLoadDiagnostic) Failure(
            PluginLoadStatus status,
            string message,
            Exception? exception = null) =>
            (null, CreateDiagnostic(
                manifestPath,
                manifest?.PluginKey,
                status,
                message,
                exception));
    }

    /// <summary>判断候选路径是否位于指定根目录内。</summary>
    private static bool IsWithinRoot(string candidatePath, string rootPath)
    {
        string normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath))
                                + Path.DirectorySeparatorChar;
        return candidatePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>创建不持有插件程序集异常对象的诊断。</summary>
    private static PluginLoadDiagnostic CreateDiagnostic(
        string manifestPath,
        string? pluginKey,
        PluginLoadStatus status,
        string message,
        Exception? exception = null) =>
        new()
        {
            ManifestPath = manifestPath,
            PluginKey = pluginKey,
            Status = status,
            Message = message,
            ExceptionType = exception?.GetType().FullName
        };
}
