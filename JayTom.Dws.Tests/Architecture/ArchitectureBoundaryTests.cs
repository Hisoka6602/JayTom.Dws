using System.Xml.Linq;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>
/// 验证本轮分层改造形成的关键编译期边界。
/// </summary>
public sealed class ArchitectureBoundaryTests {
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>
    /// 验证展示层不再直接引用相机厂商程序集或跨项目复制相机资产。
    /// </summary>
    [Fact]
    public void Client_must_not_reference_camera_vendor_assemblies_or_assets() {
        XDocument clientProject = ReadProjectDocument("JayTom.Dws.Client");
        string[] assemblyReferences = clientProject
            .Descendants("Reference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();
        string[] itemPaths = clientProject
            .Descendants()
            .SelectMany(element => element.Attributes())
            .Where(attribute => attribute.Name.LocalName is "Include" or "Update" or "HintPath")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.DoesNotContain(assemblyReferences, IsCameraVendorAssembly);
        Assert.DoesNotContain(
            itemPaths,
            path => path.Contains("JayTom.Dws.Camera\\ffmpegFiles", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 验证展示层源码不再直接使用厂商命名空间或旧的全局包裹管理器。
    /// </summary>
    [Fact]
    public void Client_source_must_depend_on_adapters_and_session_store() {
        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")) {
            string source = File.ReadAllText(path);

            Assert.DoesNotContain("using NetSDKCS", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using Dynamsoft", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PackageInfoManager", source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 验证本机资产由实际使用它们的相机适配项目负责发布。
    /// </summary>
    [Fact]
    public void Camera_project_must_own_its_native_runtime_assets() {
        XDocument cameraProject = ReadProjectDocument("JayTom.Dws.Camera");
        string[] assetPaths = cameraProject
            .Descendants("None")
            .Select(element => element.Attribute("Update")?.Value ?? string.Empty)
            .ToArray();

        Assert.Contains(
            assetPaths,
            path => path.Equals("ffmpegFiles\\**\\*", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 验证外部集成契约由跨平台契约项目唯一编译拥有。
    /// </summary>
    [Fact]
    public void Integration_contracts_must_have_a_single_compile_owner() {
        string[] expectedContractFiles = [
            "IDataUploader.cs",
            "IApiUploader.cs",
            "BaseApiParameters.cs",
            "ApiHttpClientNames.cs",
            "INetworkTime.cs"
        ];
        XDocument contractsProject = ReadProjectDocument("JayTom.Dws.Integrations.Contracts");
        XDocument implementationProject = ReadProjectDocument("JayTom.Dws.Interface");
        string[] linkedFiles = contractsProject
            .Descendants("Compile")
            .Select(element => element.Attribute("Link")?.Value ?? string.Empty)
            .ToArray();
        string[] removedFiles = implementationProject
            .Descendants("Compile")
            .Select(element => element.Attribute("Remove")?.Value ?? string.Empty)
            .ToArray();

        foreach (string expectedFile in expectedContractFiles) {
            Assert.Contains(expectedFile, linkedFiles, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(expectedFile, removedFiles, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 验证插件契约、WPF 扩展和设备适配程序集具有明确且稳定的身份。
    /// </summary>
    [Fact]
    public void Plugin_layers_must_publish_distinct_assembly_identities() {
        Assert.Equal("JayTom.Dws.Plugin.Contracts", ReadAssemblyName("JayTom.Dws.Plugin.Abstractions"));
        Assert.Equal("JayTom.Dws.Plugin.Presentation", ReadAssemblyName("JayTom.Dws.PluginInterface"));
        Assert.Equal("JayTom.Dws.DeviceAdapters", ReadAssemblyName("JayTom.Dws.Plugin"));

        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Plugin.Abstractions")) {
            Assert.DoesNotContain(
                "JayTom.Dws.PluginInterface",
                File.ReadAllText(path),
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 验证关系库与本地库仓储共享同一上下文生命周期基类。
    /// </summary>
    [Fact]
    public void Repository_bases_must_share_context_lifecycle_ownership() {
        string repositorySource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Infrastructure",
            "Repository",
            "RepositoryBase.cs"));
        string localRepositorySource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Infrastructure",
            "Repository",
            "LocalRepositoryBase.cs"));

        Assert.Contains(": RepositoryContextBase<TContext>", repositorySource, StringComparison.Ordinal);
        Assert.Contains(": RepositoryContextBase<TContext>", localRepositorySource, StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断程序集名称是否属于应被封装在相机项目内的厂商程序集。
    /// </summary>
    /// <param name="assemblyName">待判断的程序集名称。</param>
    /// <returns>属于相机厂商程序集时返回真。</returns>
    private static bool IsCameraVendorAssembly(string assemblyName) =>
        assemblyName.Equals("NetSDKCS", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.StartsWith("Dynamsoft", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 读取指定项目的程序集名称。
    /// </summary>
    /// <param name="projectName">项目名称。</param>
    /// <returns>程序集名称。</returns>
    private static string ReadAssemblyName(string projectName) =>
        ReadProjectDocument(projectName)
            .Descendants("AssemblyName")
            .First()
            .Value;

    /// <summary>
    /// 读取指定项目文件。
    /// </summary>
    /// <param name="projectName">项目名称。</param>
    /// <returns>项目 XML 文档。</returns>
    private static XDocument ReadProjectDocument(string projectName) {
        string path = Path.Combine(RepositoryRoot, projectName, $"{projectName}.csproj");
        return XDocument.Load(path);
    }

    /// <summary>
    /// 枚举项目目录中的生产源码文件。
    /// </summary>
    /// <param name="projectName">项目名称。</param>
    /// <returns>排除生成目录后的源码文件序列。</returns>
    private static IEnumerable<string> EnumerateSourceFiles(string projectName) =>
        Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, projectName),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    /// <summary>
    /// 从测试输出目录向上定位仓库根目录。
    /// </summary>
    /// <returns>仓库根目录的绝对路径。</returns>
    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln"))) {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("无法定位仓库根目录。");
    }
}
