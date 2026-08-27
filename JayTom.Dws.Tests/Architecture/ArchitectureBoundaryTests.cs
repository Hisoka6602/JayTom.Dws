using System.Xml.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    /// 验证外部集成契约由跨平台契约项目物理拥有，且不再通过链接编译跨层借用源码。
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
        foreach (string expectedFile in expectedContractFiles) {
            Assert.True(File.Exists(Path.Combine(
                RepositoryRoot,
                "JayTom.Dws.Integrations.Contracts",
                expectedFile)));
            Assert.False(File.Exists(Path.Combine(
                RepositoryRoot,
                "JayTom.Dws.Interface",
                expectedFile)));
        }

        Assert.DoesNotContain(
            ReadProjectDocument("JayTom.Dws.Integrations.Contracts").Descendants("Compile"),
            element => element.Attribute("Include")?.Value.Contains("..\\", StringComparison.Ordinal) == true);
    }

    /// <summary>验证应用服务和视频基础设施实现由目标项目物理拥有。</summary>
    [Fact]
    public void Migrated_implementations_must_be_physically_owned_by_their_target_projects() {
        string[] applicationServices = [
            Path.Combine("Services", "Cloud", "CloudService.cs"),
            Path.Combine("Services", "Licensing", "LicenseApplicationService.cs"),
            Path.Combine("Services", "Licensing", "LicenseCodeService.cs"),
            Path.Combine("Services", "Licensing", "LicenseLogService.cs"),
            Path.Combine("Services", "Licensing", "LicenseUserService.cs")
        ];

        foreach (string relativePath in applicationServices) {
            Assert.True(File.Exists(Path.Combine(
                RepositoryRoot,
                "JayTom.Dws.Application",
                relativePath)));
        }

        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Infrastructure",
            "Service",
            "VideoApi",
            "VideoBarCodeService.cs")));

        foreach (string projectName in new[] {
                     "JayTom.Dws.Application",
                     "JayTom.Dws.Domain",
                     "JayTom.Dws.Legacy.Contracts",
                     "JayTom.Dws.Infrastructure",
                     "JayTom.Dws.Integrations.Contracts",
                     "JayTom.Dws.Interface"
                 }) {
            Assert.DoesNotContain(
                ReadProjectDocument(projectName).Descendants("Compile"),
                element => element.Attribute("Include")?.Value.Contains("..\\", StringComparison.Ordinal) == true);
        }
    }

    /// <summary>验证所有生产项目都不再通过跨项目链接编译共享源码。</summary>
    [Fact]
    public void Production_projects_must_not_link_source_files_from_other_projects() {
        foreach (string projectPath in Directory.EnumerateFiles(
                     RepositoryRoot,
                     "JayTom.Dws.*.csproj",
                     SearchOption.AllDirectories)
                 .Where(path => !path.Contains(
                     $"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}",
                     StringComparison.OrdinalIgnoreCase))) {
            XDocument project = XDocument.Load(projectPath);
            Assert.DoesNotContain(
                project.Descendants("Compile"),
                element => element.Attribute("Include")?.Value.Contains(
                    "..\\",
                    StringComparison.Ordinal) == true);
        }
    }

    /// <summary>验证包裹会话注册表是实例状态，且旧静态管理器已退出生产源码。</summary>
    [Fact]
    public void Session_registry_must_be_instance_scoped() {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Legacy.Contracts",
            "Manager",
            "PackageSessionRegistry.cs"));

        Assert.Contains("public sealed class PackageSessionRegistry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("static class PackageInfoManager", source, StringComparison.Ordinal);
        Assert.DoesNotContain("static readonly ConcurrentDictionary", source, StringComparison.Ordinal);
    }

    /// <summary>验证后台服务重启会创建新作用域和新实例，首次启动失败不会伪装成功。</summary>
    [Fact]
    public void Hosted_service_supervision_must_recreate_faulted_services() {
        string supervisor = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "Service",
            "Runtime",
            "HostedServiceSupervisor.cs"));
        string registration = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "Composition",
            "HostedWorkflowRegistration.cs"));

        Assert.Contains("CreateAsyncScope()", supervisor, StringComparison.Ordinal);
        Assert.Contains("TrySetException", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("IEnumerable<IHostedService>", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHostedService<", registration, StringComparison.Ordinal);
        Assert.Contains("AddTransient<TService>()", registration, StringComparison.Ordinal);
    }

    /// <summary>验证 200 项实施台账编号完整且没有遗漏。</summary>
    [Fact]
    public void Architecture_implementation_register_must_track_exactly_200_items() {
        string registerPath = Path.Combine(
            RepositoryRoot,
            "docs",
            "architecture",
            "implementation-register-200.md");
        string[] itemRows = File.ReadAllLines(registerPath)
            .Where(line => line.Length > 6 &&
                           line[0] == '|' &&
                           char.IsWhiteSpace(line[1]) &&
                           char.IsDigit(line[2]))
            .ToArray();

        Assert.Equal(200, itemRows.Length);
        for (var index = 0; index < itemRows.Length; index++) {
            Assert.StartsWith($"| {index + 1:000} |", itemRows[index], StringComparison.Ordinal);
        }
    }

    /// <summary>验证历史记录页面只依赖应用查询边界，不直接依赖具体持久化仓储。</summary>
    [Fact]
    public void View_models_must_use_the_history_application_boundary() {
        string directory = Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "Preferences",
            "LogsViewModel");
        string[] forbiddenRepositories = [
            "IApiLogRepository",
            "IAppLogRepository",
            "ICameraLogRepository",
            "IFtpLogRepository",
            "IOutputLogRepository",
            "ISortingLogRepository",
            "IVolumeLogRepository",
            "IWeighingLogRepository"
        ];

        foreach (string path in Directory.EnumerateFiles(directory, "*LogPageViewModel.cs")) {
            string source = File.ReadAllText(path);
            foreach (string repositoryName in forbiddenRepositories) {
                Assert.DoesNotContain(repositoryName, source, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>验证展示模型通过应用层出口用例访问数据，而不是注入出口仓储。</summary>
    [Fact]
    public void View_models_must_use_the_exit_application_boundary() {
        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")
                     .Where(path => path.Contains(
                         $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}",
                         StringComparison.OrdinalIgnoreCase))) {
            Assert.DoesNotContain(
                "IPackageExitDefinitionRepository",
                File.ReadAllText(path),
                StringComparison.Ordinal);
        }
    }

    /// <summary>验证已迁移的展示模型持久化依赖不会绕过应用层用例。</summary>
    [Fact]
    public void View_models_must_keep_migrated_persistence_boundaries_closed() {
        string[] forbiddenRepositories = [
            "IPackageExitDefinitionRepository",
            "IApiLogRepository",
            "IAppLogRepository",
            "ICameraLogRepository",
            "IFtpLogRepository",
            "IOutputLogRepository",
            "ISortingLogRepository",
            "IVolumeLogRepository",
            "IWeighingLogRepository",
            "ISoundRepository",
            "ICommunicationConnectionConfigRepository",
            "IPackageRepository",
            "IWeightSortingRepository",
            "IVolumeSortingRepository",
            "IOcrSortingRepository",
            "ILogisticsSortingRepository",
            "IBarCodeSortingRepository",
            "IApiSortingRepository",
            "ILogisticsCodeRecognitionRepository",
            "IWeightSortingRuleRepository",
            "IVolumeSortingRuleRepository",
            "IOcrSortingRuleRepository",
            "ILogisticsSortingRuleRepository",
            "IBarCodeSortingRuleRepository",
            "IApiSortingRuleRepository",
            "IBarCodeRepository",
            "ISortingInstructionBindingRepository",
            "ISortingInstructionRepository",
            "IPackageExitLockBindingRepository",
            "IBarcodeScannerCameraConfigRepository",
            "IPanoramaCameraConfigRepository",
            "IVolumeCameraConfigRepository",
            "IUsbCameraConfigRepository",
            "IIpcNvrConfigRepository",
            "INvrCameraBindingRepository",
            "INvrWatermarkConfigRepository"
        ];

        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")
                     .Where(path => path.Contains(
                         $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}",
                         StringComparison.OrdinalIgnoreCase))) {
            string source = File.ReadAllText(path);
            foreach (string repositoryName in forbiddenRepositories) {
                Assert.DoesNotContain(repositoryName, source, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>验证展示模型和客户端服务都不能新增仓储接口依赖。</summary>
    [Fact]
    public void Client_orchestration_must_not_name_repository_contracts() {
        var repositoryContract = new Regex(
            @"\bI[A-Za-z0-9]+Repository\b",
            RegexOptions.CultureInvariant);

        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")
                     .Where(path => IsClientOrchestrationSource(path))) {
            Assert.DoesNotMatch(repositoryContract, File.ReadAllText(path));
        }
    }

    /// <summary>验证展示模型不直接引用 EF Core 或数据库上下文。</summary>
    [Fact]
    public void View_models_must_not_access_entity_framework_or_db_contexts() {
        foreach (string path in EnumerateViewModelSourceFiles()) {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(
                new Regex(@"\b[A-Za-z0-9_]*DbContext\b", RegexOptions.CultureInvariant),
                source);
        }
    }

    /// <summary>验证展示模型通过设备服务或工厂获得设备，不直接构造 SDK 适配器。</summary>
    [Fact]
    public void View_models_must_not_construct_device_implementations() {
        var deviceConstruction = new Regex(
            @"\bnew\s+[A-Za-z_][A-Za-z0-9_]*(?:Camera|NVR|Nvr|BarCodeReader|Scale)\s*\(",
            RegexOptions.CultureInvariant);

        foreach (string path in EnumerateViewModelSourceFiles()) {
            Assert.DoesNotMatch(deviceConstruction, File.ReadAllText(path));
        }
    }

    /// <summary>验证静态容器只存在于 WPF 组合根，业务编排不新增服务定位器。</summary>
    [Fact]
    public void Static_service_location_must_remain_in_the_composition_root() {
        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")) {
            string relativePath = Path.GetRelativePath(
                Path.Combine(RepositoryRoot, "JayTom.Dws.Client"),
                path);
            if (relativePath.Equals("App.xaml.cs", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string source = File.ReadAllText(path);
            Assert.DoesNotContain("Container.Resolve", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ServiceLocator", source, StringComparison.Ordinal);
        }
    }

    /// <summary>验证生产源码只能通过依赖注入使用应用事件总线。</summary>
    [Fact]
    public void Client_must_not_use_the_static_event_bus_entrypoint() {
        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")) {
            Assert.DoesNotContain(
                "EventAggregator.Instance",
                File.ReadAllText(path),
                StringComparison.Ordinal);
        }
    }

    /// <summary>验证异步事件处理器使用可观察的有序订阅通道。</summary>
    [Fact]
    public void Client_async_event_handlers_must_use_the_async_subscription_boundary() {
        var asyncVoidSubscription = new Regex(
            @"_eventBus\.Subscribe(?:Package)?<[^\r\n]+>\(async",
            RegexOptions.CultureInvariant);

        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Client")) {
            Assert.DoesNotMatch(asyncVoidSubscription, File.ReadAllText(path));
        }

        string contract = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Application",
            "Messaging",
            "IEventBus.cs"));
        Assert.Contains("IDisposable Subscribe<", contract, StringComparison.Ordinal);
        Assert.Contains("IDisposable SubscribeAsync<", contract, StringComparison.Ordinal);
    }

    /// <summary>验证核心业务层不再直接读取墙上时钟，并保留可注入本地时间语义。</summary>
    [Fact]
    public void Core_business_time_must_use_time_provider() {
        foreach (string projectName in new[] {
                     "JayTom.Dws.Domain",
                     "JayTom.Dws.Legacy.Contracts",
                     "JayTom.Dws.Application"
                 }) {
            foreach (string path in EnumerateSourceFiles(projectName)) {
                string source = File.ReadAllText(path);
                Assert.DoesNotContain("DateTime.Now", source, StringComparison.Ordinal);
                Assert.DoesNotContain("DateTime.UtcNow", source, StringComparison.Ordinal);
            }
        }

        string packageSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Legacy.Contracts",
            "Packages",
            "PackageInfo.cs"));
        Assert.Contains("PackageInfo(TimeProvider timeProvider)", packageSource, StringComparison.Ordinal);
        Assert.Contains("GetLocalNow().DateTime", packageSource, StringComparison.Ordinal);
    }

    /// <summary>验证相机生命周期与运行参数只通过强类型、可取消的异步契约传递。</summary>
    [Fact]
    public void Camera_contracts_must_use_typed_configuration() {
        string cameraContract = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Camera",
            "ICamera.cs"));
        string nvrContract = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Camera",
            "Nvr",
            "INvrDeviceService.cs"));

        Assert.Contains("CameraInfo camera", cameraContract, StringComparison.Ordinal);
        Assert.Contains("Start(CancellationToken cancellationToken", cameraContract, StringComparison.Ordinal);
        Assert.Contains("CameraRuntimeSettings settings", cameraContract, StringComparison.Ordinal);
        Assert.DoesNotContain("object param", cameraContract, StringComparison.Ordinal);
        Assert.DoesNotContain("Dictionary<string, object>", cameraContract, StringComparison.Ordinal);
        Assert.DoesNotContain("object param", nvrContract, StringComparison.Ordinal);
    }

    /// <summary>验证 NVR 契约与厂商实现只有 Camera 项目这一个编译所有者。</summary>
    [Fact]
    public void Nvr_adapter_must_have_a_single_project_owner() {
        Assert.False(File.Exists(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Nvr",
            "JayTom.Dws.Nvr.csproj")));
        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Camera",
            "Nvr",
            "Legacy",
            "INvrManager.cs")));
        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Camera",
            "Nvr",
            "Legacy",
            "DaHuaNvr.cs")));

        string clientProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "JayTom.Dws.Client.csproj"));
        Assert.DoesNotContain("JayTom.Dws.Nvr.csproj", clientProject, StringComparison.Ordinal);
    }

    /// <summary>验证 OCR 公共契约使用平台中立载荷，且 OCR 引擎项目不依赖 WPF。</summary>
    [Fact]
    public void Ocr_contract_must_not_expose_bitmap_or_wpf() {
        string ocrContract = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Ocr",
            "IOcr.cs"));
        string ocrProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Ocr",
            "JayTom.Dws.Ocr.csproj"));

        Assert.Contains("OcrImageFrame image", ocrContract, StringComparison.Ordinal);
        Assert.DoesNotContain("Bitmap", ocrContract, StringComparison.Ordinal);
        Assert.DoesNotContain("<UseWPF>", ocrProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PresentationFramework", ocrProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WindowsBase", ocrProject, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>验证全部生产项目程序集身份唯一，插件相关职责使用稳定的发布名称。</summary>
    [Fact]
    public void Production_assembly_identities_must_be_unique_and_stable() {
        using JsonDocument policy = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "ArchitecturePolicy.json")));
        string[] projectNames = policy.RootElement
            .GetProperty("projectReferences")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();
        string[] assemblyNames = projectNames
            .Select(projectName => {
                XDocument project = ReadProjectDocument(projectName);
                return project.Descendants("AssemblyName").FirstOrDefault()?.Value ?? projectName;
            })
            .ToArray();

        Assert.Equal(
            assemblyNames.Length,
            assemblyNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("JayTom.Dws.Plugin.Contracts", assemblyNames, StringComparer.Ordinal);
        Assert.Contains("JayTom.Dws.Plugin.Presentation", assemblyNames, StringComparer.Ordinal);
        Assert.Contains("JayTom.Dws.DeviceAdapters", assemblyNames, StringComparer.Ordinal);
    }

    /// <summary>验证 App 仅协调 WPF 生命周期，业务启动和停机编排归入独立协调器。</summary>
    [Fact]
    public void App_entrypoint_must_remain_a_thin_lifecycle_adapter() {
        string appSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "App.xaml.cs"));

        Assert.True(appSource.Split('\n').Length <= 350, "App.xaml.cs 不应重新膨胀为组合和业务实现中心。");
        Assert.Contains("IApplicationLifecycleCoordinator", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("InitializeConfigurationAsync", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StopApplicationServicesAsync", appSource, StringComparison.Ordinal);
    }

    /// <summary>验证后台工作流只注册生产服务，并显式声明生产者在消费者之后启动。</summary>
    [Fact]
    public void Hosted_workflows_must_declare_production_startup_order() {
        string registration = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "Composition",
            "HostedWorkflowRegistration.cs"));

        Assert.DoesNotContain("TestBackgroundService", registration, StringComparison.Ordinal);
        int consumer = registration.IndexOf("LogProcessingService", StringComparison.Ordinal);
        int producer = registration.IndexOf("YunShanPackageBackgroundService", StringComparison.Ordinal);
        Assert.True(consumer >= 0 && producer > consumer);
        Assert.Contains("AddSupervisedHostedService", registration, StringComparison.Ordinal);
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

    /// <summary>验证配置契约不泄漏 EF 实体，并提供取消与显式错误结果。</summary>
    [Fact]
    public void Configuration_boundary_must_be_persistence_agnostic_and_result_based() {
        string storeContract = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Application",
            "Configuration",
            "ISettingsStore.cs"));
        string accessContract = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Application",
            "Configuration",
            "ISettingsAccess.cs"));

        Assert.DoesNotContain("EntityFrameworkCore", storeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigInfoModel", storeContract, StringComparison.Ordinal);
        Assert.Contains("CancellationToken cancellationToken", accessContract, StringComparison.Ordinal);
        Assert.Contains("OperationResult<TSettings?>", accessContract, StringComparison.Ordinal);
        Assert.Contains("Task<Result> SaveAsync", accessContract, StringComparison.Ordinal);
    }

    /// <summary>验证 ViewModel 只能通过应用配置边界访问设置。</summary>
    [Fact]
    public void View_models_must_not_read_configuration_repositories() {
        foreach (string path in EnumerateViewModelSourceFiles()) {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain("IConfigRepository", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(
                new Regex(@"\bConfigInfoModel\b", RegexOptions.CultureInvariant),
                source);
        }
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

    /// <summary>枚举展示模型源码。</summary>
    private static IEnumerable<string> EnumerateViewModelSourceFiles() =>
        EnumerateSourceFiles("JayTom.Dws.Client")
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase));

    /// <summary>判断客户端源文件是否属于展示模型或服务编排目录。</summary>
    /// <param name="path">待判断的绝对文件路径。</param>
    /// <returns>属于客户端编排目录时返回真。</returns>
    private static bool IsClientOrchestrationSource(string path) =>
        path.Contains(
            $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase) ||
        path.Contains(
            $"{Path.DirectorySeparatorChar}Service{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);

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
