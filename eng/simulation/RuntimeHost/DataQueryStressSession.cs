extern alias ClientRuntime;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Xml;
using JayTom.Dws.Abstractions.Threading;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Application.UseCases;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Plugin.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViewModel = ClientRuntime::JayTom.Dws.Client.ViewModels.Pages.Preferences.DataManagementViewModel;
using ExcelItem = ClientRuntime::JayTom.Dws.Client.Models.PackageSorting.Excel.ExcelPackageItemModel;

/// <summary>在实际分拣服务运行期间使用真实 WPF 查询模型、SQLite 和 Excel 导出，不打开生产窗口。</summary>
internal sealed class DataQueryStressSession : IAsyncDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentQueue<string> _errors = new();
    private readonly ConcurrentQueue<double> _uiGaps = new();
    private readonly ConcurrentQueue<object> _diagnostics = new();
    private DispatcherTimer? _pulseTimer;
    private long _lastPulse;
    private TimeSpan _lastGcPause;
    private double _maximumLayout;
    private readonly string _exportPath;
    private readonly Dispatcher _dispatcher;
    private readonly ViewModel _viewModel;
    private readonly Task _queries;
    private readonly Task<bool> _export;
    private int _requested;
    private int _rendered;
    private int _maximumPending;
    private int _maximumItems;
    private long _maximumPersistence;
    private int _queryRoundsDuringBacklog;
    private readonly MemoryMappedFile? _heartbeatMap;
    private readonly MemoryMappedViewAccessor? _heartbeatView;

    /// <summary>创建实际查询页面并在独立 STA 消息循环中驱动查询和数据绑定。</summary>
    public static async Task<DataQueryStressSession> StartAsync(ServiceProvider services, string root)
    {
        var ready = new TaskCompletionSource<(Dispatcher, ViewModel)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                var viewModel = new ViewModel(null!, new NpoiExport(), services.GetRequiredService<ISettingsStore>(),
                    services.GetRequiredService<IPackageExitCatalog>(),
                    services.GetRequiredService<IApplicationQueryHandler<SearchPackageHistoryQuery, PackageHistoryPage>>(),
                    services.GetRequiredService<IEventBus>());
                ready.SetResult((dispatcher, viewModel));
                Dispatcher.Run();
            }
            catch (Exception error) { ready.TrySetException(error); }
        }) { IsBackground = true, Name = "SimulationQueryUi" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        var ui = await ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        return new(ui.Item1, ui.Item2, root);
    }

    private DataQueryStressSession(Dispatcher dispatcher, ViewModel viewModel, string root)
    {
        _dispatcher = dispatcher;
        _viewModel = viewModel;
        _exportPath = Path.Combine(root, "concurrent-history-export.xlsx");
        if (Environment.GetEnvironmentVariable("DWS_SIMULATION_CAPTURE_STACKS") == "1")
        {
            _heartbeatMap = MemoryMappedFile.CreateNew("DwsQuerySimulationHeartbeat-" + Environment.ProcessId, 16);
            _heartbeatView = _heartbeatMap.CreateViewAccessor();
            File.WriteAllText(Path.Combine(root, "runtime.pid"), Environment.ProcessId.ToString());
        }
        _dispatcher.InvokeAsync(() =>
        {
            var grid = new DataGrid { AutoGenerateColumns = false, EnableRowVirtualization = true, EnableColumnVirtualization = true, Height = 800, Width = 1500 };
            foreach (var property in new[] { "Num", "TimestampMilliseconds", "Barcode", "Weight", "ScanTime", "ExitName", "RequestStatus" })
                grid.Columns.Add(new DataGridTextColumn { Header = property, Binding = new Binding(property) });
            grid.DataContext = _viewModel;
            grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("PackageItems"));
            _viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != "PackageItems") return;
                _rendered++;
                _maximumItems = Math.Max(_maximumItems, _viewModel.PackageItems.Count);
                if (_viewModel.PackageItems.Count > 100) _errors.Enqueue("UI page exceeded the 100 row bound");
                if (_viewModel.PackageItems.Any(item => item.UploadInfo.RequestContent.Length > 0 || item.UploadInfo.ResponseContent.Length > 0))
                    _errors.Enqueue("List materialized full HTTP bodies");
                var layoutStarted = Stopwatch.GetTimestamp();
                grid.Measure(new Size(1500, 800));
                grid.Arrange(new Rect(0, 0, 1500, 800));
                grid.UpdateLayout();
                var duration = Stopwatch.GetElapsedTime(layoutStarted).TotalMilliseconds;
                _maximumLayout = Math.Max(_maximumLayout, duration);
                if (duration > 100) _diagnostics.Enqueue(new { stage = "grid-layout", duration_ms = duration, rendered = _rendered });
            };
            _lastPulse = Stopwatch.GetTimestamp();
            _lastGcPause = GC.GetTotalPauseDuration();
            _pulseTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(20) };
            _pulseTimer.Tick += (_, _) =>
            {
                var now = Stopwatch.GetTimestamp();
                var gap = Stopwatch.GetElapsedTime(_lastPulse, now).TotalMilliseconds;
                var pause = GC.GetTotalPauseDuration();
                _uiGaps.Enqueue(gap);
                if (gap > 200) _diagnostics.Enqueue(new { stage = "ui-pulse", gap_ms = gap, gc_pause_ms = (pause - _lastGcPause).TotalMilliseconds,
                    threads = ThreadPool.ThreadCount, pending_work = ThreadPool.PendingWorkItemCount });
                _lastPulse = now; _lastGcPause = pause;
                _heartbeatView?.Write(0, now);
            };
            _pulseTimer.Start();
        }).Task.ContinueWith(task => { if (task.Exception is { } error) _errors.Enqueue(error.ToString()); }, TaskScheduler.Default);
        _queries = DriveQueries();
        var exporter = new NpoiExport();
        var exportRecords = (IAsyncEnumerable<ExcelItem>)typeof(ViewModel).GetMethod("ReadExportItemsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(_viewModel, [new PackageHistoryQuery(Barcode: "HISTORY-EXPORT-"), new Action<int>(_ => { }), _stop.Token])!;
        _export = new LowPriorityWorkScheduler("SimulationHistoryExport", 1).RunAsync(() =>
            exporter.ExportStreamingAsync(_exportPath, "Packages", "Packages", exportRecords, [], _ => { },
                error => _errors.Enqueue(error.ToString()), _stop.Token), _stop.Token);
    }

    private async Task DriveQueries()
    {
        var round = 0;
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    var pendingWrites = RealtimeWorkPressure.PendingPersistenceCount;
                    _maximumPersistence = Math.Max(_maximumPersistence, pendingWrites);
                    if (pendingWrites >= 128) _queryRoundsDuringBacklog++;
                    var burst = ++round % 5 == 0 ? 20 : 1;
                    for (var i = 0; i < burst; i++)
                    {
                        _requested++;
                        _viewModel.BarCode = i == burst - 1 ? (round % 3 == 0 ? "NoRead" : round % 3 == 1 ? "HISTORY-" : "") : "obsolete-" + i;
                        _viewModel.SearchDataCommand.Execute(null);
                    }
                    _maximumPending = Math.Max(_maximumPending, LowPriorityWorkScheduler.Shared.PendingCount);
                }, DispatcherPriority.Background, _stop.Token);
                await Task.Delay(350, _stop.Token);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error) { _errors.Enqueue(error.ToString()); }
    }

    /// <summary>查询在全程持续运行；最后核对导出的五千个历史包裹和 UI 心跳。</summary>
    public async Task<object> FinishAsync()
    {
        try
        {
            if (!await _export.WaitAsync(TimeSpan.FromSeconds(60))) _errors.Enqueue("Concurrent export failed");
            if (CountExportRows() != 5000) _errors.Enqueue("Concurrent export row count was not 5000");
        }
        catch (Exception error) { _errors.Enqueue(error.ToString()); }
        _stop.Cancel();
        await _dispatcher.InvokeAsync(() => { _pulseTimer?.Stop(); _viewModel.UnloadedCommand.Execute(null); });
        await _queries;
        var samples = _uiGaps.Order().ToArray();
        if (_rendered < 5) _errors.Enqueue("Fewer than five real pages were rendered");
        if (samples.Length < 50) _errors.Enqueue("Insufficient WPF heartbeat samples");
        if (samples.Length > 0 && samples[^1] > 1000) _errors.Enqueue("WPF heartbeat stalled for more than 1000 ms");
        return new { passed = _errors.IsEmpty, requested_queries = _requested, rendered_pages = _rendered,
            maximum_pending_queries = _maximumPending, maximum_page_items = _maximumItems,
            maximum_pending_persistence = _maximumPersistence, query_rounds_during_backlog = _queryRoundsDuringBacklog,
            ui_heartbeat_samples = samples.Length, ui_max_gap_ms = samples.Length == 0 ? 0 : samples[^1],
            ui_p99_gap_ms = samples.Length == 0 ? 0 : samples[(int)((samples.Length - 1) * .99)],
            exported_rows = File.Exists(_exportPath) ? CountExportRows() : 0,
            errors = _errors.ToArray(), export_path = _exportPath, maximum_grid_layout_ms = _maximumLayout,
            diagnostics = _diagnostics.ToArray(), gc_pause_ms = GC.GetTotalPauseDuration().TotalMilliseconds,
            server_gc = System.Runtime.GCSettings.IsServerGC,
            scope = "Actual DataManagementViewModel, WPF STA dispatcher/DataGrid binding, SQLite history reads and paged streaming Excel export; no visible production window." };
    }

    private int CountExportRows()
    {
        using var archive = ZipFile.OpenRead(_exportPath);
        using var stream = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
        var rows = 0;
        while (xml.Read()) if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "row") rows++;
        return rows - 2;
    }

    /// <summary>用负时间戳区分历史夹具与本次真实分拣记录，历史记录不会进入设备业务队列。</summary>
    public static async Task SeedHistoryAsync(SqliteContext db, int count)
    {
        await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand();
        var total = command.CreateParameter(); total.ParameterName = "$count"; total.Value = count; command.Parameters.Add(total);
        var body = command.CreateParameter(); body.ParameterName = "$body"; body.Value = new string('R', 8192); command.Parameters.Add(body);
        command.CommandText = """
            WITH RECURSIVE n(x) AS (VALUES(1) UNION ALL SELECT x+1 FROM n WHERE x<$count)
            INSERT INTO Data_PackageInfo(Id,PackageTimestamped,PackageCreateTime,Other)
            SELECT -x,-x,'2026-01-01 00:00:00',printf('%01024d',x) FROM n;
            WITH RECURSIVE n(x) AS (VALUES(1) UNION ALL SELECT x+1 FROM n WHERE x<$count)
            INSERT INTO Data_BarCodeInfo(Id,Barcode,ScanTime,Source,SerialNumber,DisplayIdentifier,PackageId)
            SELECT -x,CASE WHEN x<=5000 THEN printf('HISTORY-EXPORT-%08d',x) ELSE printf('HISTORY-%08d',x) END,
                   '2026-01-01 00:00:00',0,'HISTORY-CAMERA','HISTORY',-x FROM n;
            INSERT INTO Data_UploadInfo(Id,RequestStatus,RequestContent,ResponseContent,RequestTime,ResponseTime,
                DurationInSeconds,InterfaceParameters,RequestUrl,ExceptionMessage,ApiExceptionType,PackageId)
            SELECT Id,1,$body,$body,'2026-01-01 00:00:00','2026-01-01 00:00:01',1,'','fixture://history','',0,Id
            FROM Data_PackageInfo WHERE Id BETWEEN -5000 AND -1;
            """;
        command.ExecuteNonQuery();
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        await _dispatcher.InvokeAsync(() => { _pulseTimer?.Stop(); _viewModel.UnloadedCommand.Execute(null); });
        await _queries;
        try { await _export; }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error) { _errors.Enqueue(error.ToString()); }
        _dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        _heartbeatView?.Dispose();
        _heartbeatMap?.Dispose();
        _stop.Dispose();
    }
}
