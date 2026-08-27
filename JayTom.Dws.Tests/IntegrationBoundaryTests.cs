using System.Net;
using JayTom.Dws.Abstractions.Observability;
using JayTom.Dws.Integrations;

namespace JayTom.Dws.Tests;

/// <summary>验证外部接口命名客户端的韧性、解析、脱敏与沙箱语义。</summary>
public sealed class IntegrationBoundaryTests
{
    /// <summary>验证幂等 GET 瞬态失败后按集中策略重试并最终成功。</summary>
    [Fact]
    public async Task Resilience_handler_retries_idempotent_transient_failures()
    {
        int transportRequestCount = 0;
        using var countingTransport = new StubHttpMessageHandler(request =>
        {
            transportRequestCount++;
            return transportRequestCount < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : StubHttpMessageHandler.CreateOkResponse("ok");
        });
        using var handler = CreateHandler(countingTransport, retryAttempts: 2, failureThreshold: 10);
        using var invoker = new HttpMessageInvoker(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://sandbox.invalid/health"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, transportRequestCount);
    }

    /// <summary>验证非幂等 POST 即使遇到瞬态响应也不会被透明重复提交。</summary>
    [Fact]
    public async Task Resilience_handler_does_not_retry_post_requests()
    {
        using var transport = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var handler = CreateHandler(transport, retryAttempts: 3, failureThreshold: 10);
        using var invoker = new HttpMessageInvoker(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "https://sandbox.invalid/packages")
            {
                Content = new StringContent("{}")
            },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, transport.RequestCount);
    }

    /// <summary>验证达到失败阈值后后续请求在沙箱传输层之前被熔断。</summary>
    [Fact]
    public async Task Resilience_handler_opens_circuit_after_threshold()
    {
        using var transport = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var handler = CreateHandler(transport, retryAttempts: 0, failureThreshold: 1);
        using var invoker = new HttpMessageInvoker(handler);

        using HttpResponseMessage first = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "https://sandbox.invalid/packages"),
            CancellationToken.None);
        await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://sandbox.invalid/health"),
            CancellationToken.None));

        Assert.Equal(1, transport.RequestCount);
    }

    /// <summary>验证统一请求时限会取消挂起传输并返回明确的超时失败。</summary>
    [Fact]
    public async Task Resilience_handler_enforces_central_timeout()
    {
        using var transport = new StubHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return StubHttpMessageHandler.CreateOkResponse("unreachable");
        });
        var options = new IntegrationResilienceOptions(
            TimeSpan.FromMilliseconds(25),
            0,
            TimeSpan.Zero,
            10,
            TimeSpan.FromSeconds(1));
        using var handler = new IntegrationResilienceHandler(options, TimeProvider.System)
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsAsync<TimeoutException>(() => invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://sandbox.invalid/slow"),
            CancellationToken.None));
    }

    /// <summary>验证接口参数快照递归清除凭据和 URL 查询参数中的令牌。</summary>
    [Fact]
    public void Parameter_snapshot_redacts_nested_credentials()
    {
        string snapshot = IntegrationParameterSerializer.Serialize(new
        {
            UserName = "operator",
            Password = "secret-value",
            Nested = new
            {
                AccessToken = "access-value",
                Url = "https://sandbox.invalid/?token=query-value"
            }
        });

        Assert.Contains("operator", snapshot, StringComparison.Ordinal);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-value", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("access-value", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("query-value", snapshot, StringComparison.Ordinal);
    }

    /// <summary>验证响应解析规则与 HTTP 传输无关并覆盖三种业务判定方式。</summary>
    [Fact]
    public void Response_evaluator_supports_explicit_business_rules()
    {
        var evaluator = new DefaultApiResponseEvaluator();

        Assert.True(evaluator.IsSuccess("accepted", 0, "accepted", string.Empty, string.Empty));
        Assert.True(evaluator.IsSuccess("status:ok", 1, string.Empty, "ok", string.Empty));
        Assert.True(evaluator.IsSuccess("code=200", 2, string.Empty, string.Empty, "code=\\d+"));
        Assert.False(evaluator.IsSuccess("", 0, string.Empty, string.Empty, string.Empty));
    }

    /// <summary>创建使用内存沙箱传输的韧性处理器。</summary>
    private static IntegrationResilienceHandler CreateHandler(
        HttpMessageHandler transport,
        int retryAttempts,
        int failureThreshold)
    {
        var options = new IntegrationResilienceOptions(
            TimeSpan.FromSeconds(1),
            retryAttempts,
            TimeSpan.Zero,
            failureThreshold,
            TimeSpan.FromSeconds(30));
        return new IntegrationResilienceHandler(options, TimeProvider.System)
        {
            InnerHandler = transport
        };
    }
}
