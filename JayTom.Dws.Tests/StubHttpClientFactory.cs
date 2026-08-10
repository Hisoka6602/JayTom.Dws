using System.Net.Http;

namespace JayTom.Dws.Tests;

/// <summary>
/// 为接口测试返回使用指定消息处理器的客户端。
/// </summary>
internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    /// <summary>
    /// 测试使用的 HTTP 消息处理器。
    /// </summary>
    private readonly HttpMessageHandler _handler;

    /// <summary>
    /// 创建测试客户端工厂。
    /// </summary>
    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// 创建不会释放共享消息处理器的测试客户端。
    /// </summary>
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, disposeHandler: false);
    }
}
