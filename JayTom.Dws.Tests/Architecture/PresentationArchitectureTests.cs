namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定 ViewModel 与 WPF 展示基础设施之间的边界。</summary>
public sealed class PresentationArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>ViewModel 只能通过统一门面访问对话框宿主。</summary>
    [Fact]
    public void View_models_use_dialog_boundary()
    {
        string globalUsings = Read("JayTom.Dws.Client", "GlobalUsings.cs");
        string facade = Read("JayTom.Dws.Client", "Presentation", "UserDialogService.cs");
        string viewModels = ReadViewModels();

        Assert.Contains(
            "global using DialogHost = JayTom.Dws.Client.Presentation.UserDialogService;",
            globalUsings,
            StringComparison.Ordinal);
        Assert.Contains("MaterialDesignThemes.Wpf.DialogHost.Show", facade, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "MaterialDesignThemes.Wpf.DialogHost",
            viewModels,
            StringComparison.Ordinal);
    }

    /// <summary>ViewModel 只能通过统一 UI 线程门面访问 Dispatcher。</summary>
    [Fact]
    public void View_models_use_central_ui_thread_boundary()
    {
        string viewModels = ReadViewModels();

        Assert.Contains("UiThread.Dispatcher", viewModels, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "System.Windows.Application.Current.Dispatcher",
            viewModels,
            StringComparison.Ordinal);
    }

    /// <summary>属性通知使用 BindableBase 的 SetProperty，避免重复手写通知样板。</summary>
    [Fact]
    public void View_models_use_set_property_instead_of_manual_notifications()
    {
        string viewModels = ReadViewModels();

        Assert.Contains("SetProperty(", viewModels, StringComparison.Ordinal);
        Assert.False(
            System.Text.RegularExpressions.Regex.IsMatch(
                viewModels,
                @"\bPropertyChanged\s*\?\.\s*Invoke\s*\(",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant),
            "ViewModel 不应直接触发 PropertyChanged，应统一使用 SetProperty。");
    }

    /// <summary>耗时命令通过统一控制器公开忙碌状态、重入保护与取消命令。</summary>
    [Fact]
    public void Long_running_view_model_commands_use_shared_operation_controller()
    {
        string bulkOperations = Read(
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "BulkOperationsTemplateViewModel.cs");
        string listOperations = Read(
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "ListOperationBaseViewModel.cs");

        foreach (string source in new[] { bulkOperations, listOperations })
        {
            Assert.Contains("AsyncOperationController", source, StringComparison.Ordinal);
            Assert.Contains("public bool IsBusy", source, StringComparison.Ordinal);
            Assert.Contains("CancelOperationCommand", source, StringComparison.Ordinal);
            Assert.Contains("TryRunAsync", source, StringComparison.Ordinal);
        }
    }

    /// <summary>ViewModel 通过强类型请求导航，不再向 Prism 直接传递字符串或 URI。</summary>
    [Fact]
    public void View_models_use_typed_navigation_requests()
    {
        string viewModels = ReadViewModels();
        string navigationRequest = Read(
            "JayTom.Dws.Client",
            "Presentation",
            "NavigationRequest.cs");

        Assert.Contains("NavigationRequest.To", viewModels, StringComparison.Ordinal);
        Assert.Contains("NavigationRegion Region", navigationRequest, StringComparison.Ordinal);
        Assert.Contains("NavigationDestination Destination", navigationRequest, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestNavigate(", viewModels, StringComparison.Ordinal);
    }

    /// <summary>ViewModel 的分页业务规则可在不引用 WPF 客户端的测试项目中验证。</summary>
    [Fact]
    public void View_model_business_rules_are_tested_without_wpf_reference()
    {
        string testProject = Read("JayTom.Dws.Tests", "JayTom.Dws.Tests.csproj");
        string listOperations = Read(
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "ListOperationBaseViewModel.cs");
        string paginationTests = Read(
            "JayTom.Dws.Tests",
            "Application",
            "PaginationStateTests.cs");

        Assert.DoesNotContain(
            "ProjectReference Include=\"..\\JayTom.Dws.Client",
            testProject,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PaginationState.Create", listOperations, StringComparison.Ordinal);
        Assert.Contains("PaginationState.Create", paginationTests, StringComparison.Ordinal);
    }

    /// <summary>大 ViewModel 持续拆出独立职责，并受单文件行数预算约束。</summary>
    [Fact]
    public void View_models_respect_file_size_budget_and_extract_sdk_policy()
    {
        string root = Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "ViewModels");
        string[] oversized = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadLines(path).Count() > 1150)
            .Select(path => Path.GetRelativePath(RepositoryRoot, path))
            .ToArray();
        string cameraFinder = Read(
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "Preferences",
            "CameraConfiguration",
            "CameraFinderViewModel.cs");

        Assert.Empty(oversized);
        Assert.Contains("ICameraSdkDeploymentService", cameraFinder, StringComparison.Ordinal);
        Assert.DoesNotContain("Directory.GetFiles", cameraFinder, StringComparison.Ordinal);
        Assert.DoesNotContain("File.Copy", cameraFinder, StringComparison.Ordinal);
    }

    /// <summary>读取全部 ViewModel 源文件。</summary>
    private static string ReadViewModels()
    {
        string root = Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "ViewModels");
        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));
    }

    /// <summary>读取仓库内文件。</summary>
    private static string Read(params string[] segments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. segments]));

    /// <summary>定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
