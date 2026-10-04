extern alias ClientRuntime;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Camera;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Infrastructure.DependencyInjection;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Models.Package;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Workflow = ClientRuntime::JayTom.Dws.Client.Service.ProcessingServices.YunShanPackageBackgroundService;
using DataWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.DataProcessingBackgroundService;
using UploadWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using ImageWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SaveImageBackgroundService;
using ExitWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.PackageExitUpdateBackgroundService;
using SortingService = ClientRuntime::JayTom.Dws.Client.Service.Sorting.ISortingService;
using DefaultDevice = ClientRuntime::JayTom.Dws.Client.Service.Device.DefaultDeviceService;
using DeviceService = ClientRuntime::JayTom.Dws.Client.Service.Device.IDeviceService;
using DeviceState = ClientRuntime::JayTom.Dws.Client.Service.Device.DeviceRuntimeState;
using ExitMonitor = ClientRuntime::JayTom.Dws.Client.Service.Sorting.IExitMonitor;

// This executable hosts the actual Windows client services. Only camera SDK callbacks
// and physical devices are substituted; matching/upload/protocol/persistence stay real.
internal static class Program
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: RuntimeHost <fixture-config.json> <empty-run-directory>");
            return 2;
        }
        var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0])).RootElement.Clone();
        var httpPort = Port(config, "http_port", 18086);
        var plcPort = Port(config, "plc_port", 19001);
        var cameraPort = Port(config, "camera_port", 19002);
        var root = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(root);
        if (File.Exists(Path.Combine(root, "Data.db"))) throw new InvalidOperationException("Use a new run directory.");
        var paths = new SimulationPaths(root);
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        var loggingXml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Nlog.config"))
            .Replace("${basedir}", root.Replace('\\', '/'));
        using var loggingReader = System.Xml.XmlReader.Create(new StringReader(loggingXml));
        var nlog = new XmlLoggingConfiguration(loggingReader, null, LogManager.LogFactory);
        var simulationLog = new NLog.Targets.Wrappers.AsyncTargetWrapper(new FileTarget("simulation")
        {
            FileName = Path.Combine(root, "runtime.log"),
            Layout = "${longdate}|${level}|${logger}|${message}|${exception:format=tostring}"
        }, 20000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Discard) { Name = "simulation-async" };
        nlog.AddTarget(simulationLog);
        nlog.LoggingRules.Insert(0, new LoggingRule("JayTom.Dws.*", NLog.LogLevel.Info, simulationLog));
        LogManager.Configuration = nlog;
        ThreadPool.GetMinThreads(out var workers, out var io);
        ThreadPool.SetMinThreads(Math.Max(workers, 64), Math.Max(io, 64));
        using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpPort}"), Timeout = TimeSpan.FromSeconds(15) };
        await Post(http, "/configure", config);

        var services = new ServiceCollection();
        services.AddDwsPersistence();
        var assembly = typeof(Workflow).Assembly;
        Register(assembly, services, "PlatformAdapterRegistration", "AddDwsPlatformAdapters");
        Register(assembly, services, "ApplicationServiceRegistration", "AddDwsApplicationServices");
        services.AddSingleton<IApplicationPathProvider>(paths);
        services.AddTransient<DataWorkflow>();
        services.AddTransient<UploadWorkflow>();
        services.AddTransient<ImageWorkflow>();
        services.AddTransient<ExitWorkflow>();
        services.AddTransient<Workflow>();
        await using var provider = services.BuildServiceProvider();
        await Seed(provider, config, paths, httpPort, plcPort);
        var queryStressEnabled = config.TryGetProperty("query_stress", out var queryStress) && queryStress.GetBoolean();
        if (queryStressEnabled)
        {
            using var process = Process.GetCurrentProcess();
            var allowed = unchecked((ulong)process.ProcessorAffinity.ToInt64());
            process.ProcessorAffinity = (nint)unchecked((long)(allowed & (0UL - allowed)));
            Console.WriteLine("Query stress: actual Windows runtime limited to one logical CPU.");
        }
        var bus = provider.GetRequiredService<IEventBus>();
        var observer = new Observer(bus);
        var device = (DefaultDevice)provider.GetRequiredService<DeviceService>();
        var sorting = provider.GetRequiredService<SortingService>();
        sorting.ExceptionOccurred += (_, error) => observer.Errors.Enqueue("sorting:" + error.ExceptionMessage);
        var workflows = new IHostedService[]
        {
            provider.GetRequiredService<DataWorkflow>(), provider.GetRequiredService<UploadWorkflow>(),
            provider.GetRequiredService<ImageWorkflow>(), provider.GetRequiredService<ExitWorkflow>(),
            provider.GetRequiredService<Workflow>()
        };
        foreach (var workflow in workflows) await workflow.StartAsync(default);
        // Exercise actual device event forwarding while avoiding vendor SDK/hardware discovery.
        var cameras = new List<ICamera>();
        for (var i = 1; i <= config.GetProperty("cameras").GetInt32(); i++)
        {
            var camera = (ICamera)Activator.CreateInstance(typeof(ICamera).Assembly.GetType(
                "JayTom.Dws.Camera.Testing.SimulatedCamera", throwOnError: true)!, nonPublic: true)!;
            camera.BindingType = CameraBindingType.ScannerCamera;
            await camera.Initialize(new CameraInfo { SerialNumber = $"SIM-CAMERA-{i}", Name = "Docker SDK fixture" });
            await camera.Start();
            cameras.Add(camera);
        }
        InvokeDevice(device, "OnCameraInitialized", cameras);
        InvokeDevice(device, "SetRuntimeState", DeviceState.Running);
        using var cameraConnection = new TcpClient { NoDelay = true };
        await cameraConnection.ConnectAsync("127.0.0.1", cameraPort);
        using var cancellation = new CancellationTokenSource();
        var cameraResults = FeedCamera(device, cameraConnection, cancellation.Token);
        var start = await sorting.Start();
        if (!start.Key) throw new InvalidOperationException("Actual sorting service failed: " + start.Value);
        // Exit lock is a live sensor state, not an EF-mapped configuration column.
        // Deliver the Docker sensor oracle at the real monitor's hardware callback boundary.
        var sensorState = await http.GetFromJsonAsync<JsonElement>("/status");
        var exitMonitor = provider.GetRequiredService<ExitMonitor>();
        foreach (var locked in sensorState.GetProperty("locked_exits").EnumerateArray())
            exitMonitor.GetType().GetMethod("OnLockExitEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(exitMonitor, [new PackageExitDefinitionInfoModel
                { Id = locked.GetInt64(), ExitName = $"CHUTE-{locked.GetInt64()}", IsActive = true, IsLockExit = true }]);
        await Task.Delay(700); // Allow production BackgroundService settings loads to finish.
        await using var querySession = queryStressEnabled ? await DataQueryStressSession.StartAsync(provider, root) : null;
        await Post(http, "/start", new { });
        var clock = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;
        long peakWorkingSet = 0;
        var lastPrint = TimeSpan.FromSeconds(-10);
        // 总截止时间必须包含允许的后台收尾窗口；单核场景原来的 35 秒上限会提前截断 60 秒收尾。
        var persistenceDrainLimit = TimeSpan.FromSeconds(60);
        var runLimit = TimeSpan.FromMilliseconds(config.GetProperty("count").GetInt32() * config.GetProperty("interval_ms").GetDouble())
                       + persistenceDrainLimit + TimeSpan.FromSeconds(15);
        TimeSpan? doneAt = null;
        TimeSpan? sortingFinishedAt = null;
        // 压力测试允许后台业务写入延后，PLC 分拣完成时刻仍单独核对。
        JsonElement status = default;
        DatabaseCounts counts = new(0, 0, 0, 0, 0, 0);
        while (clock.Elapsed < runLimit)
        {
            status = await http.GetFromJsonAsync<JsonElement>("/status");
            counts = await CountDatabase(provider);
            using (var process = Process.GetCurrentProcess())
                peakWorkingSet = Math.Max(peakWorkingSet, process.WorkingSet64);
            if (clock.Elapsed - lastPrint >= TimeSpan.FromSeconds(10))
            {
                using var process = Process.GetCurrentProcess();
                Console.WriteLine(JsonConvert.SerializeObject(new
                {
                    elapsed = Math.Round(clock.Elapsed.TotalSeconds, 1),
                    created = status.GetProperty("created").GetInt32(), completed = observer.Completed.Count,
                    physical = observer.Physical.Count, counts, memory_mb = process.WorkingSet64 / 1048576
                }));
                lastPrint = clock.Elapsed;
            }
            if (status.GetProperty("done").GetBoolean())
            {
                doneAt ??= clock.Elapsed;
                var expected = status.GetProperty("expected").GetInt32();
                if (observer.Completed.Count == expected && observer.Displayed.Count == expected &&
                    observer.Physical.Count == expected) sortingFinishedAt ??= clock.Elapsed;
                if (observer.Completed.Count == expected && observer.Displayed.Count == expected &&
                    observer.Physical.Count == expected && counts.Packages == expected &&
                    counts.Uploads == expected && counts.Images == expected && counts.Physical == expected &&
                    counts.Sorts == expected && counts.Instructions == expected * 3 &&
                    clock.Elapsed - doneAt >= TimeSpan.FromSeconds(2)) break;
                if (clock.Elapsed - doneAt >= persistenceDrainLimit) break;
            }
            await Task.Delay(500);
        }
        var cameraCount = await cameraResults.WaitAsync(TimeSpan.FromSeconds(3));
        var oracle = await Post(http, "/verify", new { completed = observer.Completed.ToArray(), displayed = observer.Displayed.ToArray() });
        var databaseErrors = await ValidateDatabase(provider, observer.Completed.ToArray(), config);
        var queryReport = querySession is null ? null : await querySession.FinishAsync();
        if (queryReport is not null)
        {
            var queryResult = System.Text.Json.JsonSerializer.SerializeToElement(queryReport);
            if (!queryResult.GetProperty("passed").GetBoolean()) databaseErrors.Add("Concurrent query/export/UI validation failed");
        }
        var expectedCount = oracle.GetProperty("status").GetProperty("expected").GetInt32();
        if (observer.Physical.Count != expectedCount) databaseErrors.Add($"physical events: {observer.Physical.Count}/{expectedCount}");
        if (counts.Images != expectedCount) databaseErrors.Add($"saved image metadata: {counts.Images}/{expectedCount}");
        if (counts.Uploads != expectedCount) databaseErrors.Add($"upload records: {counts.Uploads}/{expectedCount}");
        LogManager.Flush();
        var pipelineLogging = SnapshotPipelineLogging(typeof(Workflow).Assembly);
        if (pipelineLogging.pipeline_dropped != 0 || pipelineLogging.anomaly_dropped != 0 ||
            pipelineLogging.failed_writes != 0 || pipelineLogging.target_error_reports != 0)
            databaseErrors.Add("Pipeline logging lost events or failed to write; inspect pipeline_logging and logs.");
        var errorLogLines = new List<string>();
        using (var stream = new FileStream(Path.Combine(root, "runtime.log"), FileMode.Open,
                   FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(stream))
            while (await reader.ReadLineAsync() is { } line)
                if (line.Contains("|Error|", StringComparison.Ordinal) || line.Contains("|Fatal|", StringComparison.Ordinal))
                    errorLogLines.Add(line);
        var report = new
        {
            passed = oracle.GetProperty("passed").GetBoolean() && databaseErrors.Count == 0 && observer.Errors.IsEmpty && errorLogLines.Count == 0,
            config, elapsed_seconds = clock.Elapsed.TotalSeconds, camera_results = cameraCount,
            sorting_finished_elapsed_seconds = sortingFinishedAt?.TotalSeconds,
            persistence_drain_seconds = doneAt is { } completedAt ? (clock.Elapsed - completedAt).TotalSeconds : (double?)null,
            persistence_drain_limit_seconds = persistenceDrainLimit.TotalSeconds,
            started_at_utc = startedAt, peak_working_set_mb = peakWorkingSet / 1048576,
            client_dll_sha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(typeof(Workflow).Assembly.Location))),
            runtime_executable = Environment.ProcessPath, runtime_os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            pipeline_logging = pipelineLogging,
            counts, oracle, query_test = queryReport, runtime_errors = observer.Errors.ToArray(), database_errors = databaseErrors,
            error_log_lines_before_shutdown = errorLogLines,
            physical_events = observer.Physical.Count,
            scope = "Actual Windows client services and SQLite; Docker TCP PLC, SDK-boundary camera stream and HTTP API; no WPF window or vendor native SDK."
        };
        await File.WriteAllTextAsync(Path.Combine(root, "report.json"), System.Text.Json.JsonSerializer.Serialize(report, JsonOptions));
        await File.WriteAllTextAsync(Path.Combine(root, "completed.json"), System.Text.Json.JsonSerializer.Serialize(observer.Completed.ToArray(), JsonOptions));
        await File.WriteAllTextAsync(Path.Combine(root, "physical.json"), System.Text.Json.JsonSerializer.Serialize(observer.Physical.ToArray(), JsonOptions));
        cancellation.Cancel();
        cameraConnection.Close();
        await sorting.Stop();
        for (var i = workflows.Length - 1; i >= 0; i--)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await workflows[i].StopAsync(stop.Token);
        }
        foreach (var camera in cameras) camera.Dispose();
        GC.KeepAlive(observer);
        LogManager.Flush();
        Console.WriteLine($"RESULT passed={report.passed} report={Path.Combine(root, "report.json")}");
        return report.passed ? 0 : 1;
    }

    /// <summary>读取实际客户端日志计数并生成健康证据，不通过增加丢弃事件订阅改变 NLog 行为。</summary>
    private static PipelineLoggingSnapshot SnapshotPipelineLogging(Assembly clientAssembly)
    {
        var type = clientAssembly.GetType("JayTom.Dws.Client.Observability.PipelineFileLog", throwOnError: true)!;
        var recorder = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        type.GetMethod("ReportHealth", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(recorder, null);
        LogManager.Flush();
        long Counter(string name) => (long)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(recorder)!;
        return new(Counter("_pipelineDrops"), Counter("_anomalyDrops"), Counter("_failedWrites"), Counter("_targetErrorReports"));
    }

    /// <summary>固定字段的落盘健康证据，确保默认 JSON 序列化保留所有计数。</summary>
    private sealed record PipelineLoggingSnapshot(long pipeline_dropped, long anomaly_dropped, long failed_writes, long target_error_reports);

    private static void Register(Assembly assembly, IServiceCollection services, string type, string method) =>
        assembly.GetType("JayTom.Dws.Client.Composition." + type, true)!.GetMethod(method)!.Invoke(null, [services]);

    private static void InvokeDevice(DefaultDevice device, string method, object argument) =>
        typeof(DefaultDevice).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(device, [argument]);

    private static async Task<JsonElement> Post(HttpClient http, string path, object body)
    {
        using var response = await http.PostAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(path + ":" + content);
        return JsonDocument.Parse(content).RootElement.Clone();
    }

    private static async Task<int> FeedCamera(DefaultDevice device, TcpClient client, CancellationToken token)
    {
        var scanned = typeof(DefaultDevice).GetMethod("OnBarcodeScanned", BindingFlags.NonPublic | BindingFlags.Instance)!
            .CreateDelegate<Action<BarcodeReadEventArgs>>(device);
        var missed = typeof(DefaultDevice).GetMethod("OnNotBarcodeHitEvent", BindingFlags.NonPublic | BindingFlags.Instance)!
            .CreateDelegate<Action<BarcodeReadEventArgs>>(device);
        using var reader = new StreamReader(client.GetStream());
        var count = 0;
        while (await reader.ReadLineAsync(token) is { } line)
        {
            using var message = JsonDocument.Parse(line);
            var root = message.RootElement;
            if (root.TryGetProperty("end", out _)) return count;
            using var encoded = new MemoryStream(Convert.FromBase64String(root.GetProperty("image").GetString()!));
            using var decoded = new Bitmap(encoded);
            var args = new BarcodeReadEventArgs
            {
                Barcode = root.GetProperty("barcode").GetString()!,
                CameraSerialNumber = root.GetProperty("camera").GetString()!,
                ScanTime = DateTime.Parse(root.GetProperty("scan_time").GetString()!, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind).ToLocalTime(),
                Image = new Bitmap(decoded), IsFiltered = root.GetProperty("filtered").GetBoolean(),
                FrameNo = 0, Timestamp = 0, TriggerIndex = null, SdkFrameNo = null
            };
            if (args.Barcode == "NoRead" && !args.IsFiltered) missed(args); else scanned(args);
            count++;
        }
        return count;
    }

    private static int Port(JsonElement config, string name, int fallback) =>
        config.TryGetProperty(name, out var value) ? value.GetInt32() : fallback;

    private static async Task Seed(ServiceProvider provider, JsonElement config, SimulationPaths paths, int httpPort, int plcPort)
    {
        var settings = provider.GetRequiredService<ISettingsStore>();
        await settings.SaveAsync("CreatePackageSettings", new CreatePackageSettingsDto
        {
            PackageCreationMethods = PackageCreationMethodsEnum.LowerMachineCreation,
            PackageRemoveMethods = config.TryGetProperty("removal", out var removal) && removal.GetString() == "lower"
                ? PackageRemoveMethodsEnum.LowerMachineRemoval : PackageRemoveMethodsEnum.FillInformation,
            IsUseNoRead = config.GetProperty("use_noread").GetBoolean(), PackageCreationInterval = 150,
            ClearPackageQueueOnStop = true, IsUseBarcodeAssignmentInterval = config.GetProperty("window").GetBoolean(),
            MinimumAssignmentTime = 50, MaximumAssignmentTime = 500
        });
        await settings.SaveAsync("SortingMethodSettings", new SortingMethodDto { SortMode = SortMode.ApiResponseSorting });
        await settings.SaveAsync("WeightSettings", new WeightSettingsDto
        {
            Mode = WeightMode.None, CommonWeight = new CommonWeightParams { MinWeight = 0, MaxWeight = 100 }
        });
        await settings.SaveAsync("BarcodeFilterSettings", new BarcodeFilterSettingsDto { MergeTimeout = 2000 });
        await settings.SaveAsync("ApiSettings", new ApiSettingsDto { Type = ApiType.DefaultApi });
        await settings.SaveAsync("DefaultApiParameters", new DefaultApiDto
        {
            Url = $"http://127.0.0.1:{httpPort}/query", Timeout = TimeSpan.FromMilliseconds(700), IsUseJsonUpload = true,
            JsonTemplate = "{\"barcode\":\"BarCodeValue\",\"scan_time\":\"ScanTimeValue\",\"weight\":WeightValue}",
            ValidationMode = ResponseValidationMode.StringContains, StringContains = "\"ok\":true"
        });
        await settings.SaveAsync("SaveImageSettings", new ImageSettingsDto
        {
            ImageRootDirectory = Path.Combine(paths.DataDirectory, "images"), IsSaveBarcodeImage = true,
            IsSaveOriginalImage = true,
            SubDirectoryTemplate = [new ItemTemplateInfo { Type = 1, ApplicationType = ItemApplicationType.SubDirectory, Content = "{Year}" }],
            ImageNamingTemplate = [new ItemTemplateInfo { Type = 1, ApplicationType = ItemApplicationType.ImageNaming, Content = "{TimestampedGuid}" }]
        });
        await using var db = await provider.GetRequiredService<IDbContextFactory<SqliteConfContext>>().CreateDbContextAsync();
        var connection = new CommunicationConnectionConfigInfoModel
        {
            Id = 1, ConnectionName = "docker-plc", CommunicationType = (int)CommunicationsType.TCP,
            CommunicationProtocol = "Wxkc", IsActive = true,
            TcpConnectionConfigInfo = new TcpConnectionConfigInfoModel
            {
                Id = 1, CommunicationConnectionId = 1, ConnectionMode = 0, DataFormat = 0,
                TcpConfigItems = [new TcpConfigInfoModel { Id = 1, TcpConnectionConfigId = 1, Type = 0, IpAddress = "127.0.0.1", Port = plcPort }]
            }
        };
        db.Add(connection);
        var replyFaults = config.TryGetProperty("reply_faults", out var replyFaultSetting) && replyFaultSetting.GetBoolean();
        if (replyFaults) db.Add(new CommunicationConnectionConfigInfoModel
        {
            Id = 2, ConnectionName = "disabled-normal-output", IsActive = false,
            CommunicationType = (int)CommunicationsType.None, CommunicationProtocol = "Wxkc"
        });
        foreach (var chute in Enumerable.Range(1, 8).Append(99))
        {
            db.Add(new PackageExitDefinitionInfoModel
            {
                Id = chute, CommunicationConnectionId = replyFaults && chute == 7 ? 2 : 1,
                IsActive = !replyFaults || chute != 5,
                ExitName = $"CHUTE-{chute}",
                Type = chute == 99 ? ExitType.AbnormalExit : ExitType.PackageExit
            });
            if (!replyFaults || chute != 4) db.Add(new SortingInstructionBindingInfoModel
            {
                Id = chute, ExitId = chute, IsActive = true,
                InstructionItems = [new SortingInstructionInfoModel { Id = chute, InstructionBindingId = chute,
                    Instruction = replyFaults && chute == 2 ? "GG GG" : replyFaults && chute == 3 ? " " : $"00 {chute:X2}" }]
            });
            if (chute == 99) continue;
            db.Add(new ApiSortingInfoModel
            {
                Id = chute, ExitId = chute, SortingName = $"HTTP-CHUTE-{chute}",
                ApiRuleItems = [new ApiRuleInfoModel
                {
                    Id = chute, ApiSortingId = chute, JsonContent = JsonConvert.SerializeObject(new ApiRuleJsonDto
                    {
                        ResponseStatus = UploadStatus.Succeeded, IsUseStringComparison = true, IsUseJsonField = true,
                        JsonField = "chute", JsonFieldValue = chute.ToString(CultureInfo.InvariantCulture)
                    })
                }]
            });
        }
        for (var i = 1; i <= config.GetProperty("cameras").GetInt32(); i++)
            db.Add(new BarcodeScannerCameraConfigInfoModel { Id = i, SerialNumber = $"SIM-CAMERA-{i}" });
        await db.SaveChangesAsync();
        await using var data = await provider.GetRequiredService<IDbContextFactory<SqliteContext>>().CreateDbContextAsync();
        if (config.TryGetProperty("query_stress", out var queryStress) && queryStress.GetBoolean())
            await DataQueryStressSession.SeedHistoryAsync(data, config.GetProperty("seed_history_count").GetInt32());
    }

    private static async Task<DatabaseCounts> CountDatabase(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<SqliteContext>>().CreateDbContextAsync();
        return new(await db.Set<PackageInfoModel>().CountAsync(item => item.PackageTimestamped > 0), await db.Set<UploadInfoModel>().CountAsync(item => item.PackageId > 0),
            await db.Set<ImageInfoModel>().CountAsync(), await db.Set<SortingInfoModel>().CountAsync(),
            await db.Set<ExitInfoModel>().CountAsync(exit => exit.PhysicalExitId > 0),
            await db.Set<InstructionInfoModel>().CountAsync());
    }

    private static async Task<List<string>> ValidateDatabase(ServiceProvider provider, Row[] completed, JsonElement config)
    {
        var errors = new List<string>();
        await using var db = await provider.GetRequiredService<IDbContextFactory<SqliteContext>>().CreateDbContextAsync();
        var rows = await db.Set<PackageInfoModel>().Where(row => row.PackageTimestamped > 0).Include(row => row.BarCodeInfo).Include(row => row.UploadInfo)
            .Include(row => row.SortingInfo)!.ThenInclude(sort => sort!.InstructionInfos)
            .Include(row => row.ExitInfo).Include(row => row.ImageInfos).ToListAsync();
        var expected = new Dictionary<long, Row>();
        foreach (var original in completed)
            if (!expected.TryAdd(original.timestamp, original)) errors.Add("Duplicate business timestamp " + original.timestamp);
        if (rows.Count != expected.Count) errors.Add($"package database count {rows.Count}/{expected.Count}");
        foreach (var row in rows)
        {
            if (!expected.TryGetValue(row.PackageTimestamped, out var original))
            {
                errors.Add("Unexpected database timestamp " + row.PackageTimestamped);
                continue;
            }
            var chute = config.GetProperty("api_faults").GetBoolean() && original.barcode != "NoRead" &&
                        (original.id % 499 == 0 || original.id % 997 == 0)
                ? 99 : original.barcode == "NoRead" ? 8 : original.id % 8 + 1;
            if (config.TryGetProperty("reply_faults", out var replyFaults) && replyFaults.GetBoolean() &&
                original.barcode != "NoRead" && (original.id % 131 == 0 || original.id % 173 == 0 ||
                    original.id % 211 == 0 || original.id % 8 + 1 is >= 2 and <= 7)) chute = 99;
            if (row.BarCodeInfo?.Barcode != original.barcode || row.SortingInfo?.SortingCode != original.id.ToString())
                errors.Add($"database barcode/cart mismatch: {original.id}");
            if (row.ExitInfo?.PhysicalExitId != chute || row.ExitInfo?.TheoreticalExit != $"CHUTE-{chute}")
                errors.Add($"database physical/theoretical chute mismatch: {original.id}");
            if (row.UploadInfo is null || row.ImageInfos?.Count != 1)
                errors.Add($"database upload/image missing: {original.id}");
            if (row.SortingInfo?.InstructionInfos?.Count(instruction => instruction.InstructionType == InstructionType.CreatePackage) != 1 ||
                row.SortingInfo?.InstructionInfos?.Count(instruction => instruction.InstructionType == InstructionType.SendSorting) != 1 ||
                row.SortingInfo?.InstructionInfos?.Count(instruction => instruction.InstructionType == InstructionType.SignalCallback) != 1)
                errors.Add($"database create/send/callback instructions missing/duplicated: {original.id}");
            foreach (var image in row.ImageInfos ?? []) if (!File.Exists(image.LocalPath)) errors.Add("image file missing: " + original.id);
        }
        return errors;
    }

    private sealed record DatabaseCounts(int Packages, int Uploads, int Images, int Sorts, int Physical, int Instructions);
    private sealed record Row(long id, string barcode, string scan_time, long timestamp);

    private sealed class Observer
    {
        public readonly ConcurrentQueue<Row> Completed = new(), Displayed = new();
        public readonly ConcurrentQueue<object> Physical = new();
        public readonly ConcurrentQueue<string> Errors = new();
        public Observer(IEventBus bus)
        {
            bus.SubscribePackage<PackageInfo>(OnCompleted);
            bus.Subscribe<PackageInfo>(OnDisplayed);
            bus.Subscribe<PackageExitUpdateEvent>(OnExit);
            bus.Subscribe<AppLogInfoModel>(OnLog);
        }
        private static Row Snapshot(PackageInfo item) => new(Convert.ToInt64(item.Guid), item.BarCodeInfo!.Barcode,
            item.BarCodeInfo.ScanTime.ToUniversalTime().ToString("yyyyMMddHHmmssfff"), item.Timestamp);
        private void OnCompleted(PackageInfo item) => Completed.Enqueue(Snapshot(item));
        private void OnDisplayed(PackageInfo item) => Displayed.Enqueue(Snapshot(item));
        private void OnExit(PackageExitUpdateEvent item)
        {
            if (item.ExitType == SortingExitType.PhysicalExit) Physical.Enqueue(new { item.Timestamp, item.ExitId, item.InstructionType });
        }
        private void OnLog(AppLogInfoModel item)
        {
            if (item.Type == LogType.Exception) Errors.Enqueue(item.Message);
        }
    }

    private sealed class SimulationPaths : IApplicationPathProvider
    {
        public SimulationPaths(string root)
        {
            DataDirectory = root;
            ConfigurationDirectory = Path.Combine(root, "configuration");
            LogDirectory = Path.Combine(root, "logs");
            ModelDirectory = Path.Combine(root, "models");
            AdapterPackDirectory = Path.Combine(root, "sdk");
            foreach (var path in new[] { DataDirectory, ConfigurationDirectory, LogDirectory, ModelDirectory, AdapterPackDirectory }) Directory.CreateDirectory(path);
        }
        public string DataDirectory { get; }
        public string ConfigurationDirectory { get; }
        public string LogDirectory { get; }
        public string ModelDirectory { get; }
        public string AdapterPackDirectory { get; }
        public string GetDatabasePath(string name) => Path.Combine(DataDirectory, Path.GetFileName(name));
    }
}
