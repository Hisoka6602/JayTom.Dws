extern alias ClientRuntime;

using System.Collections.Concurrent;
using System.Reflection;
using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Integrations.Contracts;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Models.Package;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using DefaultSorting = ClientRuntime::JayTom.Dws.Client.Service.Sorting.DefaultSortingService;
using SortingConnection = ClientRuntime::JayTom.Dws.Client.Service.Sorting.ISortingConnectionService;
using SortingParam = ClientRuntime::JayTom.Dws.Client.Service.Sorting.SortingParam;
using SortingExitReceived = ClientRuntime::JayTom.Dws.Client.Service.Sorting.SortingExitReceived;
using ExceptionSortingReceived = ClientRuntime::JayTom.Dws.Client.Service.Sorting.ExceptionSortingReceived;
using UploadWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using ApiResponse = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService.ApiResponseReceived;
using ApiExceptionType = JayTom.Dws.Integrations.Contracts.ApiExceptionType;

namespace JayTom.Dws.Tests.Application;

/// <summary>通过实际分拣和上传后台服务验证完成事件的正常回复、异常兜底以及原车身份。</summary>
public sealed class SortingReplyContractTests
{
    /// <summary>正常空读走规则口，接口超时走异常口；两种决策都记录相同原包裹身份及原因。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_reply_and_fallback_decisions_have_correlated_file_evidence(bool timeout)
    {
        using var evidence = new PipelineEvidence();
        await using var fixture = new Fixture();
        var package = fixture.Complete("NoRead");
        fixture.Respond(package, timeout ? new UploadResponse { IsSuccess = false, ApiExceptionType = ApiExceptionType.Timeout }
            : Success());
        await fixture.Drain();
        fixture.AssertOneReply(package, timeout ? 99 : 8);
        var rows = evidence.ForPackage(package.Timestamp);
        var queued = rows.First(row => row.GetProperty("stage").GetString() == "sorting.reply.queued");
        Assert.Equal(package.Guid, queued.GetProperty("cartId").GetInt64());
        Assert.Equal(package.CreateTime.Ticks, queued.GetProperty("creationTimeTicks").GetInt64());
        Assert.Equal(timeout, queued.GetProperty("isFallback").GetBoolean());
        Assert.Equal(timeout ? 99 : 8, queued.GetProperty("exitId").GetInt64());
        if (timeout)
        {
            var fallback = rows.First(row => row.GetProperty("stage").GetString() == "sorting.fallback");
            Assert.Equal("NetworkTimeout", fallback.GetProperty("reasonCode").GetString());
        }
        else Assert.DoesNotContain(rows, row => row.GetProperty("stage").GetString() == "sorting.fallback");
    }
    [Theory]
    [InlineData("JT-VALID")]
    [InlineData("NoRead")]
    public async Task Successful_packages_including_NoRead_follow_the_configured_normal_rule(string barcode)
    {
        await using var fixture = new Fixture();
        var package = fixture.Complete(barcode);
        fixture.Respond(package, Success());
        await fixture.Drain();
        fixture.AssertOneReply(package, 8);
    }

    [Theory]
    [InlineData(ApiExceptionType.None)]
    [InlineData(ApiExceptionType.Timeout)]
    [InlineData(ApiExceptionType.UnreachableUrl)]
    [InlineData(ApiExceptionType.LogicValidationFailed)]
    [InlineData(ApiExceptionType.ContentParsingException)]
    [InlineData(ApiExceptionType.Other)]
    public async Task Failed_queries_use_the_abnormal_exit_even_when_a_failed_response_rule_matches(ApiExceptionType kind)
    {
        await using var fixture = new Fixture(ruleStatus: UploadStatus.Failed);
        var package = fixture.Complete("NoRead");
        fixture.Respond(package, new UploadResponse { IsSuccess = false, ApiExceptionType = kind,
            ResponseContent = "{\"chute\":8}" });
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Theory]
    [InlineData("null-response")]
    [InlineData("missing-rule")]
    [InlineData("inactive-exit")]
    [InlineData("locked-exit")]
    [InlineData("missing-binding")]
    [InlineData("empty-instructions")]
    [InlineData("blank-instruction")]
    [InlineData("send-rejected")]
    public async Task Unavailable_normal_routes_reply_once_to_the_original_cart_abnormal_exit(string fault)
    {
        await using var fixture = new Fixture(fault: fault);
        var package = fixture.Complete("JT-FAULT");
        fixture.Respond(package, fault == "null-response" ? null : Success());
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
        Assert.False(package.IsWaitingForSortingInstruction);
    }

    [Theory]
    [InlineData(SortMode.None)]
    [InlineData(SortMode.CombinedWorkflowSorting)]
    [InlineData((SortMode)999)]
    [InlineData(SortMode.OcrSorting)]
    public async Task Modes_without_a_usable_sorting_result_do_not_swallow_completed_packages(SortMode mode)
    {
        await using var fixture = new Fixture(mode: mode);
        var package = fixture.Complete("NoRead");
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Fact]
    public async Task Api_sorting_without_a_configured_query_replies_to_the_abnormal_exit()
    {
        await using var fixture = new Fixture(apiType: ApiType.None);
        var package = fixture.Complete("JT-NO-QUERY");
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Duplicate_completion_and_query_results_cannot_replace_the_first_reply(bool failureFirst)
    {
        await using var fixture = new Fixture();
        var package = fixture.Complete("NoRead");
        fixture.Bus.PublishPackage(package);
        fixture.Respond(package, failureFirst ? new UploadResponse() : Success());
        await fixture.Drain();
        fixture.Bus.PublishPackage(package);
        fixture.Respond(package, Success());
        fixture.Respond(package, new UploadResponse());
        await fixture.Drain();
        fixture.AssertOneReply(package, failureFirst ? 99 : 8);
    }

    [Fact]
    public async Task No_abnormal_exit_releases_the_exception_package_without_a_phantom_PLC_reply()
    {
        await using var fixture = new Fixture(abnormalExit: false);
        var package = fixture.Complete("JT-NO-EXCEPTION-EXIT");
        fixture.Respond(package, new UploadResponse());
        await fixture.Drain();
        Assert.Empty(fixture.Sent);
        Assert.False(package.IsWaitingForSortingInstruction);
        Assert.False(package.IsWaitingForFallConfirmation);
        Assert.Empty(fixture.Errors);
    }

    [Theory]
    [InlineData("abnormal-missing-binding")]
    [InlineData("abnormal-inactive")]
    public async Task Incomplete_abnormal_exit_configuration_is_reported_instead_of_claiming_a_successful_send(string fault)
    {
        await using var fixture = new Fixture(fault: fault);
        var package = fixture.Complete("JT-BAD-EXCEPTION-EXIT");
        fixture.Respond(package, new UploadResponse());
        await fixture.Drain();
        Assert.Empty(fixture.Sent);
        Assert.True(package.IsWaitingForSortingInstruction);
        Assert.Contains(fixture.Errors, message => message.Contains("已设置异常口但无法发送"));
    }

    [Theory]
    [InlineData("invalid-json")]
    [InlineData("null-content")]
    public async Task Invalid_success_contents_are_safely_routed_to_the_abnormal_exit(string fault)
    {
        await using var fixture = new Fixture();
        var package = fixture.Complete("JT-BAD-RESULT");
        fixture.Respond(package, new UploadResponse { IsSuccess = true,
            ResponseContent = fault == "null-content" ? null! : "{invalid" });
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Fact]
    public async Task A_bad_barcode_regex_still_produces_an_abnormal_exit_reply()
    {
        await using var fixture = new Fixture(mode: SortMode.BarcodeSorting);
        SetField(fixture.Sorting, "_barCodeRegexInfos", new List<BarCodeRegexInfoModel>
        { new() { RegexPattern = "[", BarCodeSortingId = 8 } });
        var package = fixture.Complete("NoRead");
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Fact]
    public async Task Ocr_wait_timeout_replies_to_the_original_cart_and_late_recognition_cannot_override_it()
    {
        await using var fixture = new Fixture(mode: SortMode.OcrSorting);
        SetField(fixture.Sorting, "_ocrSettingsDto", new OcrSettingsDto { IsUseOcr = true, RecognitionTimeout = 30 });
        var package = fixture.Complete("JT-OCR");
        await fixture.WaitForReply();
        fixture.Bus.Publish(new PackageOcrInfo { SubmitTimestamp = package.Timestamp, BarCode = "JT-OCR" });
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Theory]
    [InlineData("NoRead")]
    [InlineData("JT-OCR")]
    public async Task Correlated_Ocr_results_follow_normal_rules_with_original_package_identity(string barcode)
    {
        await using var fixture = new Fixture(mode: SortMode.OcrSorting);
        SetField(fixture.Sorting, "_ocrSettingsDto", new OcrSettingsDto { IsUseOcr = true, RecognitionTimeout = 1000 });
        SetField(fixture.Sorting, "_ocrRuleInfoModels", new List<OcrRuleInfoModel>
        { new() { Id = 8, OcrSortingId = 8,
            JsonContent = "{\"IsUseSenderAddressValidation\":true,\"SenderAddressContainsChars\":\"MATCH\"}" } });
        SetField(fixture.Sorting, "_ocrSortingInfoModels", new List<OcrSortingInfoModel> { new() { Id = 8, ExitId = 8 } });
        var package = fixture.Complete(barcode);
        fixture.Bus.Publish(new PackageOcrInfo { SubmitTimestamp = package.Timestamp, BarCode = barcode,
            SenderAddress = "MATCH", RecognitionTimestamp = 999, RecognitionTime = DateTime.Now.AddMinutes(3) });
        await fixture.Drain();
        fixture.AssertOneReply(package, 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Notification_subscriber_exceptions_cannot_prevent_the_PLC_reply(bool abnormal)
    {
        await using var fixture = new Fixture();
        fixture.Bus.Subscribe<SortingExitReceived>(_ => throw new InvalidOperationException("view failure"));
        fixture.Bus.Subscribe<ExceptionSortingReceived>(_ => throw new InvalidOperationException("view failure"));
        var package = fixture.Complete("NoRead");
        fixture.Respond(package, abnormal ? new UploadResponse() : Success());
        await fixture.Drain();
        fixture.AssertOneReply(package, abnormal ? 99 : 8);
    }

    [Fact]
    public async Task Removed_cart_delayed_instruction_and_response_cannot_target_a_reused_cart_id()
    {
        await using var fixture = new Fixture(delayMilliseconds: 50);
        var oldPackage = fixture.Complete("JT-OLD");
        fixture.Respond(oldPackage, Success());
        await fixture.Drain();
        fixture.Store.RemovePackage(oldPackage.CreateTime);
        var currentPackage = fixture.Complete("JT-NEW");
        fixture.Respond(oldPackage, Success());
        fixture.Respond(currentPackage, Success());
        await fixture.WaitForReply();
        await fixture.Drain();
        fixture.AssertOneReply(currentPackage, 8);
    }

    [Fact]
    public async Task Exit_locked_after_rule_selection_is_rechecked_before_the_delayed_write()
    {
        await using var fixture = new Fixture(delayMilliseconds: 100);
        var package = fixture.Complete("NoRead");
        fixture.Respond(package, Success());
        await fixture.Drain();
        typeof(DefaultSorting).GetMethod("ReplaceExitLockStatus", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(fixture.Sorting, [new PackageExitDefinitionInfoModel { Id = 8, IsLockExit = true }]);
        await fixture.WaitForReply();
        await fixture.Drain();
        fixture.AssertOneReply(package, 99);
    }

    [Theory]
    [InlineData("missing-provider")]
    [InlineData("throwing-provider")]
    [InlineData("canceling-provider")]
    [InlineData("null-provider-response")]
    public async Task Actual_upload_worker_publishes_one_terminal_failure_without_infinite_query_retry(string fault)
    {
        await using var fixture = new Fixture();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var calls = 0;
        var uploader = Proxy<IDataUploader>((method, _) => method.Name switch
        {
            "SetParameters" => Task.FromResult(new KeyValuePair<bool, string>(true, "")),
            "UploadData" => Query(),
            _ => DefaultReturn(method.ReturnType)
        });
        Task<UploadResponse> Query()
        {
            Interlocked.Increment(ref calls);
            return fault switch
            {
                "null-provider-response" => Task.FromResult<UploadResponse>(null!),
                "canceling-provider" => Task.FromException<UploadResponse>(new OperationCanceledException("provider timeout")),
                _ => Task.FromException<UploadResponse>(new InvalidOperationException("provider bug"))
            };
        }
        var registry = Proxy<IProviderRegistry<IDataUploader>>((method, args) =>
        {
            if (method.Name != "TryResolve") return DefaultReturn(method.ReturnType);
            args![1] = fault == "missing-provider" ? null : uploader;
            return fault != "missing-provider";
        });
        var uploads = new UploadWorkflow(registry, fixture.Settings, Proxy<IImageStorageService>(), cache, fixture.Bus);
        try
        {
            await uploads.StartAsync(default);
            var package = fixture.Complete("NoRead");
            await fixture.WaitForReply();
            await fixture.Drain();
            fixture.AssertOneReply(package, 99);
            var response = Assert.Single(fixture.Bus.PackageEvents.OfType<ApiResponse>());
            Assert.Equal(package.Guid, response.Guid);
            Assert.Equal(package.Timestamp, response.Timestamp);
            Assert.Equal(package.CreateTime, response.PackageCreationTime);
            Assert.Equal(package.BarCodeInfo!.ScanTime, response.ScanTime);
            Assert.False(response.UploadResponse!.IsSuccess);
            Assert.Equal(fault == "missing-provider" ? 0 : 1, calls);
        }
        finally { await uploads.StopAsync(default); uploads.Dispose(); }
    }

    private static UploadResponse Success() => new() { IsSuccess = true, ResponseContent = "{\"chute\":8}" };
    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private sealed class Fixture : IAsyncDisposable
    {
        public PackageSessionStore Store { get; } = new();
        public RecordingBus Bus { get; } = new();
        public DefaultSorting Sorting { get; }
        public ISettingsStore Settings { get; }
        public ConcurrentQueue<InstructionsAttach> Sent { get; } = new();
        public ConcurrentQueue<string> Errors { get; } = new();
        private readonly SemaphoreSlim _replies = new(0);
        private int _packages;

        public Fixture(string? fault = null, bool abnormalExit = true, SortMode mode = SortMode.ApiResponseSorting,
            ApiType apiType = ApiType.DefaultApi, UploadStatus ruleStatus = UploadStatus.Succeeded, int delayMilliseconds = 0)
        {
            Settings = Proxy<ISettingsStore>((method, args) =>
            {
                if (method.Name == "GetAsync" && args![0] is "ApiSettings")
                    return Task.FromResult<ApiSettingsDto?>(new() { Type = apiType });
                return DefaultReturn(method.ReturnType);
            });
            var connection = Proxy<SortingConnection>((method, args) =>
            {
                if (method.Name != "SendInstructions" || args![4] is not InstructionsAttach attach)
                    return DefaultReturn(method.ReturnType);
                if (fault == "send-rejected" && attach.ExitId == 8)
                    attach.OnSendFailedBeforeWrite!(new InvalidOperationException("normal exit encoding failed"));
                else if (attach.ValidateBeforeSend!() is null)
                {
                    Sent.Enqueue(attach);
                    attach.OnSendSucceeded!();
                    _replies.Release();
                }
                return null;
            });
            var constructor = typeof(DefaultSorting).GetConstructors().Single();
            Sorting = (DefaultSorting)constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.Name switch
            {
                "settingsStore" => (object)Settings, "packageSessionStore" => Store,
                "eventBus" => Bus, "sortingConnectionService" => connection,
                _ => DispatchProxy.Create(parameter.ParameterType, typeof(RuntimeProxy))
            }).ToArray());
            SetField(Sorting, "_eventProcessingReady", 1);
            SetField(Sorting, "_sortingMethodDto", new SortingMethodDto { SortMode = mode });
            SetField(Sorting, "_apiSettingsDto", new ApiSettingsDto { Type = apiType });
            Sorting.ExceptionOccurred += (_, args) => Errors.Enqueue(args.ExceptionMessage);
            var exits = new List<PackageExitDefinitionInfoModel>
            { new() { Id = 8, ExitName = "NORMAL", IsActive = fault != "inactive-exit",
                IsLockExit = fault == "locked-exit", Type = ExitType.PackageExit } };
            if (abnormalExit) exits.Add(new() { Id = 99, ExitName = "ABNORMAL",
                IsActive = fault != "abnormal-inactive", Type = ExitType.AbnormalExit });
            typeof(DefaultSorting).GetMethod("PublishExitDefinitionSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Sorting, [exits.ToArray()]);
            var bindings = exits.Where(exit => exit.Id == 99 ? fault != "abnormal-missing-binding" : fault != "missing-binding")
                .Select(exit => new SortingInstructionBindingInfoModel { Id = exit.Id, ExitId = exit.Id, IsActive = true,
                    DelaySendMilliseconds = exit.Id == 8 ? delayMilliseconds : 0 }).ToArray();
            var instructions = bindings.Where(binding => binding.Id != 8 || fault != "empty-instructions")
                .Select(binding => new SortingInstructionInfoModel { Id = binding.Id, InstructionBindingId = binding.Id,
                    Instruction = binding.Id == 8 && fault == "blank-instruction" ? " " : $"00 {binding.Id:X2}" }).ToArray();
            typeof(DefaultSorting).GetMethod("RebuildInstructionLookup", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Sorting, [bindings, instructions]);
            var evaluator = typeof(DefaultSorting).GetField("_apiRuleEvaluator", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Sorting)!;
            evaluator.GetType().GetMethod("Replace")!.Invoke(evaluator,
            [fault == "missing-rule" ? Array.Empty<ApiRuleInfoModel>() : new[] { new ApiRuleInfoModel
            { Id = 8, ApiSortingId = 8, JsonContent = JsonConvert.SerializeObject(new ApiRuleJsonDto
            { ResponseStatus = ruleStatus, IsUseStringComparison = true, IsUseJsonField = true, JsonField = "chute", JsonFieldValue = "8" }) } },
                new[] { new ApiSortingInfoModel { Id = 8, ExitId = 8 } }]);
        }

        public PackageInfo Complete(string barcode)
        {
            var sequence = Interlocked.Increment(ref _packages);
            var package = new PackageInfo { CreateTime = DateTime.Now.AddTicks(sequence), Guid = 101,
                Timestamp = 1_700_000_000_000 + sequence, IsCreatedByLowerMachine = true,
                BarCodeInfo = new BarCodeInfoModel { Barcode = barcode, ScanTime = DateTime.Now, SerialNumber = "camera-1" },
                PackageCreationInstruction = "F9 12 00 65 00 00 01 00" };
            Store.AddPackage(package, []);
            package.MarkCompleted();
            Bus.PublishPackage(package);
            return package;
        }

        public void Respond(PackageInfo package, UploadResponse? response) => Bus.PublishPackage(new ApiResponse
        {
            Guid = package.Guid, Barcode = package.BarCodeInfo?.Barcode, Timestamp = package.Timestamp,
            ScanTime = package.BarCodeInfo!.ScanTime, PackageCreationTime = package.CreateTime,
            PackageCreationInstruction = package.PackageCreationInstruction, IsCreatedByLowerMachine = true,
            UploadResponse = response
        });
        public async Task WaitForReply() => Assert.True(await _replies.WaitAsync(TimeSpan.FromSeconds(5)), "No PLC reply within 5 seconds");
        public async Task Drain()
        {
            var dispatcher = (AsyncOrderedDispatcher<Func<Task>>)typeof(DefaultSorting)
                .GetField("_sortingDispatcher", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Sorting)!;
            for (var i = 0; i < 4; i++)
            {
                var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Assert.True(dispatcher.TryEnqueue(() => { reached.SetResult(); return Task.CompletedTask; }));
                await reached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        public void AssertOneReply(PackageInfo package, long exitId)
        {
            var reply = Assert.Single(Sent);
            Assert.Equal(exitId, reply.ExitId);
            Assert.Equal(package.Guid, reply.Guid);
            Assert.Equal(package.Timestamp, reply.Timestamp);
            Assert.Equal(package.CreateTime, reply.PackageCreationTime);
            Assert.Equal(package.BarCodeInfo!.Barcode, reply.BarCode);
            Assert.Equal(package.BarCodeInfo.ScanTime, reply.ScanTime);
            Assert.True(reply.IsCreatedByLowerMachine);
            Assert.Empty(Errors);
        }
        public async ValueTask DisposeAsync()
        {
            await Sorting.DisposeAsync();
            Store.ClearAllPackages();
            _replies.Dispose();
        }
    }

    public class RuntimeProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler?.Invoke(method!, args) ?? DefaultReturn(method!.ReturnType);
    }
    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?>? handler = null) where T : class
    {
        var result = DispatchProxy.Create<T, RuntimeProxy>();
        ((RuntimeProxy)(object)result).Handler = handler;
        return result;
    }
    private static object? DefaultReturn(Type type)
    {
        if (type == typeof(void)) return null;
        if (type == typeof(Task)) return Task.CompletedTask;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = type.GenericTypeArguments[0];
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType)
                .Invoke(null, [resultType.IsValueType ? Activator.CreateInstance(resultType) : null]);
        }
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private sealed class RecordingBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, Delegate> _ordinary = new();
        private readonly ConcurrentDictionary<Type, Delegate> _business = new();
        public ConcurrentQueue<object> PackageEvents { get; } = new();
        public void Publish<T>(T item) { if (_ordinary.TryGetValue(typeof(T), out var action)) ((Action<T>)action)(item); }
        public void PublishPackage<T>(T item)
        {
            PackageEvents.Enqueue(item!);
            if (_business.TryGetValue(typeof(T), out var action)) ((Action<T>)action)(item);
        }
        public IDisposable Subscribe<T>(Action<T> action) => Subscribe(_ordinary, action);
        public IDisposable SubscribePackage<T>(Action<T> action) => Subscribe(_business, action);
        private static IDisposable Subscribe<T>(ConcurrentDictionary<Type, Delegate> dictionary, Action<T> action)
        {
            dictionary.AddOrUpdate(typeof(T), action, (_, existing) => Delegate.Combine(existing, action));
            return new Subscription();
        }
        public IDisposable SubscribeAsync<T>(Func<T, Task> action) => new Subscription();
        public IDisposable SubscribePackageAsync<T>(Func<T, Task> action) => new Subscription();
        public void Unsubscribe<T>(Action<T> action) { }
        public void UnsubscribePackage<T>(Action<T> action) { }
        private sealed class Subscription : IDisposable { public void Dispose() { } }
    }
}
