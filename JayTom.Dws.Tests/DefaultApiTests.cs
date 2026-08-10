using JayTom.Dws.Interface;
using Newtonsoft.Json.Linq;

namespace JayTom.Dws.Tests;

/// <summary>
/// 验证默认上传接口的请求执行和 JSON 模板行为。
/// </summary>
public sealed class DefaultApiTests
{
    /// <summary>
    /// 验证不带扫码时间的重载仍然会真实发送请求。
    /// </summary>
    [Fact]
    public async Task UploadDataWithoutScanTimeSendsRequest()
    {
        using var handler = new StubHttpMessageHandler(
            _ => StubHttpMessageHandler.CreateOkResponse("true"));
        var api = new DefaultApi(new StubHttpClientFactory(handler));
        await api.SetParameters(new DefaultApi.DefaultApiParameters
        {
            IsUseJsonUpload = true,
            JsonTemplate = "{\"barcode\":\"BarCodeValue\"}",
            Url = "http://unit.test/upload",
            ValidationMode = 0,
            CompleteMatch = "true"
        });

        var response = await api.UploadData("A100", 1);

        Assert.Equal(1, handler.RequestCount);
        Assert.True(response.IsSuccess);
        Assert.Contains("A100", handler.LastRequestContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// 验证条码中的引号、反斜杠和换行不会破坏 JSON 模板。
    /// </summary>
    [Fact]
    public void ParseJsonTemplateEscapesStringValues()
    {
        var api = new DefaultApi(new StubHttpClientFactory(
            new StubHttpMessageHandler(_ => StubHttpMessageHandler.CreateOkResponse("true"))));
        const string barcode = "A\"B\\C\nD";

        var json = api.ParseJsonTemplate(
            "{\"barcode\":\"BarCodeValue\",\"camera\":\"CameraSerialNumberValue\"}",
            barcode,
            1,
            new DateTime(2026, 1, 2, 3, 4, 5),
            1,
            1,
            1,
            1,
            "CAM\"01");
        var parsed = JObject.Parse(json);

        Assert.Equal(barcode, parsed["barcode"]?.Value<string>());
        Assert.Equal("CAM\"01", parsed["camera"]?.Value<string>());
    }

    /// <summary>
    /// 验证后台上传入口会调用真实上传实现。
    /// </summary>
    [Fact]
    public async Task UploadInBackgroundSendsRequest()
    {
        using var handler = new StubHttpMessageHandler(
            _ => StubHttpMessageHandler.CreateOkResponse("true"));
        var api = new DefaultApi(new StubHttpClientFactory(handler));
        await api.SetParameters(new DefaultApi.DefaultApiParameters
        {
            IsUseJsonUpload = true,
            JsonTemplate = "{\"barcode\":\"BarCodeValue\"}",
            Url = "http://unit.test/upload",
            ValidationMode = 0,
            CompleteMatch = "true"
        });

        await api.UploadInBackground("A200", 2, DateTime.Now);

        Assert.Equal(1, handler.RequestCount);
    }
}
