using System.Net;

namespace JayTom.Dws.Integrations;

/// <summary>为外部接口提供集中、并发安全且不重复提交非幂等请求的韧性管道。</summary>
internal sealed class IntegrationResilienceHandler : DelegatingHandler
{
    /// <summary>统一韧性参数。</summary>
    private readonly IntegrationResilienceOptions _options;
    /// <summary>用于熔断窗口判断的时间源。</summary>
    private readonly TimeProvider _timeProvider;
    /// <summary>保护熔断状态的同步对象。</summary>
    private readonly object _circuitSync = new();
    /// <summary>连续瞬态失败次数。</summary>
    private int _consecutiveFailures;
    /// <summary>熔断截止时间。</summary>
    private DateTimeOffset _circuitOpenUntil;

    /// <summary>创建外部接口韧性处理器。</summary>
    public IntegrationResilienceHandler(
        IntegrationResilienceOptions options,
        TimeProvider timeProvider)
    {
        options.Validate();
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <summary>通过集中策略发送外部请求，并保留调用方取消语义。</summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ThrowIfCircuitOpen();

        byte[]? body = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        int retryLimit = IsIdempotent(request.Method) ? _options.RetryAttempts : 0;

        for (int attempt = 0; ; attempt++)
        {
            using HttpRequestMessage attemptRequest = CloneRequest(request, body);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_options.RequestTimeout);
            try
            {
                HttpResponseMessage response = await base.SendAsync(
                        attemptRequest,
                        timeoutSource.Token)
                    .ConfigureAwait(false);
                if (!IsTransient(response.StatusCode))
                {
                    ResetCircuit();
                    return response;
                }

                RegisterFailure();
                if (attempt >= retryLimit)
                {
                    return response;
                }
                response.Dispose();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                RegisterFailure();
                if (attempt >= retryLimit)
                {
                    throw new TimeoutException(
                        $"External HTTP request exceeded {_options.RequestTimeout}.");
                }
            }
            catch (HttpRequestException)
            {
                RegisterFailure();
                if (attempt >= retryLimit)
                {
                    throw;
                }
            }

            await Task.Delay(_options.RetryDelay, cancellationToken).ConfigureAwait(false);
            ThrowIfCircuitOpen();
        }
    }

    /// <summary>克隆请求，确保重试不会重复发送同一个消息实例。</summary>
    private static HttpRequestMessage CloneRequest(HttpRequestMessage source, byte[]? body)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };
        foreach (KeyValuePair<string, IEnumerable<string>> header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (KeyValuePair<string, object?> option in source.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }
        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            foreach (KeyValuePair<string, IEnumerable<string>> header in source.Content!.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return clone;
    }

    /// <summary>判断请求是否能够在传输失败后安全重放。</summary>
    private static bool IsIdempotent(HttpMethod method) =>
        method == HttpMethod.Get ||
        method == HttpMethod.Head ||
        method == HttpMethod.Options ||
        method == HttpMethod.Trace ||
        method == HttpMethod.Put ||
        method == HttpMethod.Delete;

    /// <summary>判断响应状态是否属于瞬态故障。</summary>
    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    /// <summary>在熔断窗口内快速拒绝请求。</summary>
    private void ThrowIfCircuitOpen()
    {
        lock (_circuitSync)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_circuitOpenUntil > now)
            {
                throw new HttpRequestException(
                    $"External HTTP circuit is open until {_circuitOpenUntil:O}.");
            }
            if (_circuitOpenUntil != default)
            {
                _circuitOpenUntil = default;
                _consecutiveFailures = 0;
            }
        }
    }

    /// <summary>登记一次瞬态失败并在达到阈值后打开熔断器。</summary>
    private void RegisterFailure()
    {
        lock (_circuitSync)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _options.CircuitFailureThreshold)
            {
                _circuitOpenUntil = _timeProvider.GetUtcNow() + _options.CircuitBreakDuration;
            }
        }
    }

    /// <summary>成功响应后关闭熔断器并清零连续失败计数。</summary>
    private void ResetCircuit()
    {
        lock (_circuitSync)
        {
            _consecutiveFailures = 0;
            _circuitOpenUntil = default;
        }
    }
}
