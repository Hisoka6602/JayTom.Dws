extern alias ClientRuntime;

using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Application.UseCases;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Plugin;
using JayTom.Dws.Plugin.Excel;
using NPOI.XSSF.UserModel;
using Prism.Services.Dialogs;
using ViewModel = ClientRuntime::JayTom.Dws.Client.ViewModels.Pages.Preferences.DataManagementViewModel;
using PackageItem = ClientRuntime::JayTom.Dws.Client.Models.DataModels.PackageItemModel;
using ExcelItem = ClientRuntime::JayTom.Dws.Client.Models.PackageSorting.Excel.ExcelPackageItemModel;
using ProxyType = JayTom.Dws.Tests.Application.SortingReplyContractTests.RuntimeProxy;

namespace JayTom.Dws.Client.Tests;

/// <summary>使用真实 WPF 调度器和查询页面模型验证查询替代、故障隔离和流式导出。</summary>
public sealed class DataQueryResponsivenessTests
{
    /// <summary>慢查询期间 UI 仍可处理输入，后到的旧结果不会覆盖最新查询。</summary>
    [Fact]
    public Task Slow_query_does_not_block_UI_and_latest_result_wins() => OnDispatcher(async () =>
    {
        var old = new TaskCompletionSource<PackageHistoryPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken oldToken = default;
        var query = new QueryStub((request, token) =>
        {
            Assert.Equal(100, request.PageSize);
            if (request.Filter.Barcode == "OLD") { oldToken = token; return old.Task; }
            return Task.FromResult(Page("NoRead", 501));
        });
        var vm = Create(query);
        vm.BarCode = "OLD";
        var first = Load(vm);
        var pulse = false;
        await Dispatcher.CurrentDispatcher.InvokeAsync(() => pulse = true, DispatcherPriority.Input);
        Assert.True(pulse);
        Assert.False(first.IsCompleted);
        vm.BarCode = "NEW";
        await Load(vm);
        Assert.True(oldToken.IsCancellationRequested);
        Assert.Equal("NoRead", Assert.Single(vm.PackageItems).Barcode);
        Assert.Equal(6, vm.PageCount);
        old.SetResult(Page("OLD"));
        await first;
        Assert.Equal("NoRead", Assert.Single(vm.PackageItems).Barcode);
    });

    /// <summary>旧提供商的取消回调仍在阻塞时，新查询和 WPF 输入可以继续，取消不占用 UI 线程。</summary>
    [Fact]
    public Task Slow_cancellation_callback_does_not_block_UI_or_replacement_query() => OnDispatcher(async () =>
    {
        var old = new TaskCompletionSource<PackageHistoryPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var exited = 0;
        CancellationTokenRegistration registration = default;
        var vm = Create(new QueryStub((request, token) =>
        {
            if (request.Filter.Barcode != "OLD") return Task.FromResult(Page("NEW"));
            registration = token.Register(() =>
            {
                entered.TrySetResult();
                release.Wait(TimeSpan.FromSeconds(3));
                Interlocked.Exchange(ref exited, 1);
            });
            return old.Task;
        }));
        vm.BarCode = "OLD";
        var first = Load(vm);
        Task? replacement = null;
        try
        {
            vm.BarCode = "NEW";
            replacement = Load(vm);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, Volatile.Read(ref exited));
            var pulse = false;
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => pulse = true, DispatcherPriority.Input);
            Assert.True(pulse);
            await replacement;
            Assert.Equal("NEW", Assert.Single(vm.PackageItems).Barcode);
            Assert.Equal(0, Volatile.Read(ref exited));
        }
        finally
        {
            release.Set();
            old.TrySetResult(Page("OLD"));
            await first;
            if (replacement is not null) await replacement;
            await registration.DisposeAsync();
        }
    });

    /// <summary>查询故障保留旧页面，后续查询成功，异常不会穿透 WPF 消息循环。</summary>
    [Fact]
    public Task Query_failure_keeps_current_data_and_next_query_recovers() => OnDispatcher(async () =>
    {
        var failure = false;
        var vm = Create(new QueryStub((_, _) => failure
            ? Task.FromException<PackageHistoryPage>(new IOException("slow disk failure"))
            : Task.FromResult(Page("NORMAL"))));
        await Load(vm);
        failure = true;
        await Load(vm);
        Assert.Equal("NORMAL", Assert.Single(vm.PackageItems).Barcode);
        failure = false;
        await Load(vm);
        Assert.Equal("NORMAL", Assert.Single(vm.PackageItems).Barcode);
    });

    /// <summary>离开页面取消读取，忽略晚到数据且可以重新读取。</summary>
    [Fact]
    public Task Unloading_cancels_pending_read_and_does_not_publish_late_data() => OnDispatcher(async () =>
    {
        var source = new TaskCompletionSource<PackageHistoryPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken token = default;
        var vm = Create(new QueryStub((_, cancellation) => { token = cancellation; return source.Task; }));
        var load = Load(vm);
        vm.UnloadedCommand.Execute(null);
        Assert.True(token.IsCancellationRequested);
        source.SetResult(Page("LATE"));
        await load;
        Assert.Empty(vm.PackageItems);
    });

    /// <summary>清空查询的总数和页数不遗留上次成功查询的数据。</summary>
    [Fact]
    public Task Empty_results_reset_pagination_and_items() => OnDispatcher(async () =>
    {
        var empty = false;
        var vm = Create(new QueryStub((_, _) => Task.FromResult(empty ? new PackageHistoryPage(0, []) : Page("NoRead", 1000))));
        await Load(vm);
        Assert.Equal(10, vm.PageCount);
        empty = true;
        await Load(vm);
        Assert.Equal(0, vm.PageCount);
        Assert.Empty(vm.PackageItems);
    });

    /// <summary>列表省略大报文后，正常条码和 NoRead 的详情仍按原标识完整读取，不更改列表数据。</summary>
    [Theory]
    [InlineData("NoRead")]
    [InlineData("NORMAL-DETAIL")]
    public Task Details_restore_full_payload_for_the_original_package(string barcode) => OnDispatcher(async () =>
    {
        var body = new string('R', 8192);
        var row = Page(barcode).Items[0] with
        {
            UploadInfo = new(UploadStatus.Succeeded, "", "", new DateTime(2026, 10, 4),
                new DateTime(2026, 10, 4), 1, "", "", "")
        };
        var query = new QueryStub((request, _) =>
        {
            if (request.Filter.PackageTimestamp is not null)
            {
                Assert.Equal(row.PackageTimestamped, request.Filter.PackageTimestamp);
                Assert.Equal(1, request.PageSize);
                return Task.FromResult(new PackageHistoryPage(1,
                    [row with { UploadInfo = row.UploadInfo with { RequestContent = body, ResponseContent = "FULL-RESPONSE" } }]));
            }
            return Task.FromResult(new PackageHistoryPage(1, [row]));
        });
        PackageItem? shown = null;
        var uiThread = Environment.CurrentManagedThreadId;
        var dialog = Proxy<IDialogService>();
        ((ProxyType)(object)dialog).Handler = (method, arguments) =>
        {
            if (method.Name == "Show")
            {
                Assert.Equal(uiThread, Environment.CurrentManagedThreadId);
                Assert.Equal("PackageDetailsDialog", arguments![0]);
                shown = ((IDialogParameters)arguments[1]!).GetValue<PackageItem>("PackageItem");
            }
            return null;
        };
        var vm = new ViewModel(dialog, Proxy<IExcel>(), Proxy<ISettingsStore>(), Proxy<IPackageExitCatalog>(), query, Proxy<IEventBus>());
        await Load(vm);
        var original = Assert.Single(vm.PackageItems);
        original.Num = 73;
        await (Task)typeof(ViewModel).GetMethod("LoadDetailsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(vm, [original, "PackageDetailsDialog"])!;
        Assert.NotNull(shown);
        Assert.Equal(73, shown.Num);
        Assert.Equal(barcode, shown.Barcode);
        Assert.Equal(body, shown.UploadInfo.RequestContent);
        Assert.Equal("FULL-RESPONSE", shown.UploadInfo.ResponseContent);
        Assert.Equal("", original.UploadInfo.RequestContent);
    });

    /// <summary>真实流式导出五千条记录，所有条码、序号和 NoRead 均可从文件正确读回。</summary>
    [Fact]
    public async Task Streaming_export_round_trips_5000_records_including_NoRead()
    {
        var path = TemporaryPath();
        try
        {
            var exporter = new NpoiExport();
            var completed = 0;
            Assert.True(await exporter.ExportStreamingAsync(path, "Packages", "Packages", Records(5000), [],
                value => completed = value, error => throw new IOException("unexpected export error", error)));
            Assert.Equal(100, completed);
            using var stream = File.OpenRead(path);
            using var book = new XSSFWorkbook(stream);
            var sheet = book.GetSheetAt(0);
            Assert.Equal(5001, sheet.LastRowNum);
            for (var i = 1; i <= 5000; i++)
            {
                Assert.Equal(i.ToString(), sheet.GetRow(i + 1).GetCell(0).StringCellValue);
                Assert.Equal(i % 5 == 0 ? "NoRead" : "PACK-" + i, sheet.GetRow(i + 1).GetCell(2).StringCellValue);
            }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    /// <summary>导出中途取消保留已有目标文件、清理临时文件，并允许下次导出成功。</summary>
    [Fact]
    public async Task Cancelled_streaming_export_preserves_destination_and_can_recover()
    {
        var path = TemporaryPath();
        try
        {
            await File.WriteAllTextAsync(path, "KEEP-ORIGINAL");
            var exporter = new NpoiExport();
            using var cancellation = new CancellationTokenSource();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exporter.ExportStreamingAsync(path, "Packages", "Packages",
                Records(5000, i => { if (i == 250) cancellation.Cancel(); }), [], _ => { }, _ => { }, cancellation.Token));
            Assert.Equal("KEEP-ORIGINAL", await File.ReadAllTextAsync(path));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.tmp"));
            Assert.True(await exporter.ExportStreamingAsync(path, "Packages", "Packages", Records(3), [], _ => { }, _ => { }));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    /// <summary>构造独立测试文件名。</summary>
    private static string TemporaryPath() => Path.Combine(Path.GetTempPath(), "dws-stream-" + Guid.NewGuid().ToString("N") + ".xlsx");
    /// <summary>按行生成记录，避免测试夹具预先积累完整导出列表。</summary>
    private static async IAsyncEnumerable<ExcelItem> Records(int count, Action<int>? afterRow = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ExcelItem { Num = i, TimestampMilliseconds = i, Barcode = i % 5 == 0 ? "NoRead" : "PACK-" + i };
            afterRow?.Invoke(i);
            if (i % 1000 == 0) await Task.Yield();
        }
    }
    /// <summary>构造与历史查询相同的不可变返回数据。</summary>
    private static PackageHistoryPage Page(string barcode, int total = 1) => new(total,
        [new PackageHistoryItem(1, new DateTime(2026, 10, 4), "", new(barcode, new DateTime(2026, 10, 4), "CAM"),
            null, null, null, null, null, null, [], false)]);
    /// <summary>创建真实查询视图模型，只替代查询及未使用的弹窗和设备依赖。</summary>
    private static ViewModel Create(QueryStub query) => new(null!, Proxy<IExcel>(), Proxy<ISettingsStore>(),
        Proxy<IPackageExitCatalog>(), query, Proxy<IEventBus>());
    /// <summary>调用真实异步查询入口并等待所有 UI 发布完成。</summary>
    private static Task Load(ViewModel vm) => (Task)typeof(ViewModel).GetMethod("LoadDataAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, [1])!;
    /// <summary>使用既有类型安全代理替代本用例不使用的边界。</summary>
    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, ProxyType>();
    /// <summary>运行真实 STA WPF 消息循环并把断言异常交回测试线程。</summary>
    private static Task OnDispatcher(Func<Task> test)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            async Task Run()
            {
                try { await test(); completion.TrySetResult(); }
                catch (Exception error) { completion.TrySetException(error); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            }
            _ = dispatcher.InvokeAsync(Run).Task.Unwrap();
            Dispatcher.Run();
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
    /// <summary>只控制查询结果和延迟，保留视图模型的取消、分页和映射实现。</summary>
    private sealed class QueryStub(Func<SearchPackageHistoryQuery, CancellationToken, Task<PackageHistoryPage>> handler)
        : IApplicationQueryHandler<SearchPackageHistoryQuery, PackageHistoryPage>
    {
        /// <summary>调用可控查询夹具。</summary>
        public Task<PackageHistoryPage> HandleAsync(SearchPackageHistoryQuery query, CancellationToken cancellationToken = default) => handler(query, cancellationToken);
    }
}
