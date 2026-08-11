using System.Net;
using System.Net.Http;

namespace JayTom.Dws.Tests;

/// <summary>
/// 捕获请求并返回预设响应的 HTTP 消息处理器。
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    /// <summary>
    /// 用于创建测试响应的回调。
    /// </summary>
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    /// <summary>
    /// 创建测试消息处理器。
    /// </summary>
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    /// <summary>
    /// 获取已经接收的请求数量。
    /// </summary>
    public int RequestCount { get; private set; }

    /// <summary>
    /// 获取最后一次请求的正文。
    /// </summary>
    public string LastRequestContent { get; private set; } = string.Empty;

    /// <summary>
    /// 获取按调用顺序捕获的全部请求正文。
    /// </summary>
    public List<string> RequestContents { get; } = [];

    /// <summary>
    /// 捕获请求信息并返回预设响应。
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequestContent = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        RequestContents.Add(LastRequestContent);
        return _responseFactory(request);
    }

    /// <summary>
    /// 创建成功状态的纯文本响应。
    /// </summary>
    public static HttpResponseMessage CreateOkResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };
    }
}
