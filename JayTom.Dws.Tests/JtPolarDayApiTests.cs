using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Integrations;
using JayTom.Dws.Integrations.Jtexpress;

namespace JayTom.Dws.Tests;

/// <summary>
/// 验证极昼查询和设备信息回传的关键容错行为。
/// </summary>
public sealed class JtPolarDayApiTests
{
    /// <summary>
    /// 已指定的测试和本地环境不能被默认正式地址覆盖。
    /// </summary>
    [Theory]
    [InlineData(JtPolarDayApi.TestBaseUrl)]
    [InlineData(JtPolarDayApi.TestBaseUrl + "/")]
    [InlineData("http://localhost:8887")]
    public void ExplicitEnvironmentIsPreserved(string baseUrl)
    {
        Assert.Equal(baseUrl, JtPolarDayApi.NormalizeProductionBaseUrl(baseUrl));
        Assert.Equal(JtPolarDayApi.ProductionBaseUrl,
            JtPolarDayApi.NormalizeProductionBaseUrl(null));
    }

    /// <summary>
    /// 查询使用文档规定的字段、毫秒时间戳和 MD5 十六进制文本的 Base64 签名。
    /// </summary>
    [Fact]
    public async Task QueryUsesDocumentedJsonAndSignature()
    {
        var parameters = CreateParameters();
        parameters.BaseUrl = JtPolarDayApi.TestBaseUrl;
        using var handler = new StubHttpMessageHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(JtPolarDayApi.TestBaseUrl + "/polarDay/query/queryChute",
                request.RequestUri?.AbsoluteUri);
            Assert.Equal("application/json", request.Content?.Headers.ContentType?.MediaType);
            var body = await request.Content!.ReadAsStringAsync(token);
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            Assert.Equal("UT3000603933460", root.GetProperty("waybillNo").GetString());
            Assert.Equal(parameters.EquipmentCode, root.GetProperty("equipmentCode").GetString());
            Assert.Equal(parameters.SortingPlanCode, root.GetProperty("sortingPlanCode").GetString());
            Assert.Equal(1.24m, root.GetProperty("weight").GetDecimal());
            Assert.False(root.TryGetProperty("operator", out _));
            Assert.False(root.TryGetProperty("mainLineCode", out _));
            var timestampValues = request.Headers.GetValues("timestamp").ToArray();
            Assert.Collection(timestampValues,
                value => Assert.True(long.TryParse(value, out _)));
            var timestamp = timestampValues[0];
            Assert.True(long.TryParse(timestamp, out var milliseconds));
            Assert.InRange(milliseconds, DateTimeOffset.Now.AddMinutes(-1).ToUnixTimeMilliseconds(),
                DateTimeOffset.Now.AddMinutes(1).ToUnixTimeMilliseconds());
            var source = Encoding.UTF8.GetBytes(parameters.AppSecret + timestamp + body);
            // DWS-HEX-COMPACT: 协议验签要求无分隔符的小写十六进制摘要。
            var digest = Convert.ToHexStringLower(MD5.HashData(source));
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes(digest)),
                request.Headers.GetValues("token").First());
            Assert.Equal(parameters.AppKey, request.Headers.GetValues("appKey").First());
            Assert.True(Guid.TryParse(request.Headers.GetValues("X-Trace-Id").First(), out _));
            return StubHttpMessageHandler.CreateOkResponse(
                "{\"code\":1,\"msg\":\"success\",\"data\":{\"codeList\":[{\"sortingPlanCode\":\"6398155-001\",\"operateType\":2}]}}");
        });
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var parameterResult = await api.SetParameters(parameters);
        Assert.True(parameterResult.Key, parameterResult.Value);
        var response = await api.UploadFixedPointData("UT3000603933460", 1.235m);
        Assert.True(response.IsSuccess, response.ExceptionMsg);
        Assert.Equal(2, JtPolarDayApi.GetQueryOperateType(response.ResponseContent,
            parameters.SortingPlanCode));
    }

    /// <summary>
    /// 文档的查询测试只需应用、设备和计划参数，上报字段应在上报时才校验。
    /// </summary>
    [Fact]
    public async Task QueryWorksWithoutUploadMetadataAndUploadStillValidatesIt()
    {
        using var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.CreateOkResponse(
                "{\"code\":1,\"msg\":\"success\",\"data\":null}"));
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var parameters = CreateParameters();
        parameters.Operator = string.Empty;
        parameters.SiteCode = string.Empty;
        parameters.SupplyDeskCode = string.Empty;
        parameters.SupplyDeskArea = string.Empty;
        var result = await api.SetParameters(parameters);
        Assert.True(result.Key, result.Value);
        Assert.True((await api.UploadFixedPointData("UT3000603933460", 0)).IsSuccess);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            api.UploadInBackground("UT3000603933460", 0, DateTime.Now));
        Assert.Equal(1, handler.RequestCount);
    }

    /// <summary>
    /// 云端必须先申请链接，再 PUT 带水印的二进制图片，并在成功后回传半路径。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnlineImageUsesSignedLinkThenUnsignedBinaryPut(bool responseIsArray)
    {
        var paths = new List<string>();
        var parameters = CreateParameters();
        parameters.BaseUrl = JtPolarDayApi.TestBaseUrl;
        var uploadData = "{\"halfPath\":\"test/JT001.jpg\",\"fullPath\":\"https://storage.test/JT001.jpg?signature=temporary\",\"contentType\":\"image/jpeg\"}";
        using var handler = new StubHttpMessageHandler(async (request, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            paths.Add(path);
            if (path.EndsWith("/getUploadUrl", StringComparison.Ordinal))
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.True(request.Headers.Contains("token"));
                using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                var root = document.RootElement;
                Assert.Equal(parameters.EquipmentCode, root.GetProperty("equipmentCode").GetString());
                Assert.Equal(parameters.ImageReqUserCode, root.GetProperty("reqUserCode").GetString());
                var fileNames = root.GetProperty("fileNames");
                Assert.Equal(1, fileNames.GetArrayLength());
                var fileName = fileNames[0].GetString();
                Assert.Matches("^scan_[a-f0-9]{32}\\.jpg$", fileName!);
                return StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":1,\"msg\":\"success\",\"data\":" +
                    (responseIsArray ? "[" + uploadData + "]" : uploadData) + "}");
            }

            if (request.Method == HttpMethod.Put)
            {
                Assert.Equal("storage.test", request.RequestUri.Host);
                Assert.Equal("?signature=temporary", request.RequestUri.Query);
                Assert.Equal("image/jpeg", request.Content!.Headers.ContentType!.ToString());
                Assert.False(request.Headers.Contains("appKey"));
                Assert.False(request.Headers.Contains("token"));
                Assert.False(request.Headers.Contains("timestamp"));
                var bytes = await request.Content.ReadAsByteArrayAsync(token);
                Assert.InRange(bytes.Length, 4, JtPolarDayScanImageProcessor.MaxImageSizeBytes);
                Assert.Equal(0xff, bytes[0]);
                Assert.Equal(0xd8, bytes[1]);
                using var stream = new MemoryStream(bytes);
                using var image = new Bitmap(stream);
                Assert.True(image.Height > 2);
                Assert.True(image.Width >= 480);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/polarDay/upload/deviceInfo", path);
            using var deviceDocument = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal(1, deviceDocument.RootElement.GetArrayLength());
            var record = deviceDocument.RootElement[0];
            Assert.Equal("test/JT001.jpg", record.GetProperty("halfPath").GetString());
            Assert.Equal(2, record.GetProperty("operateType").GetInt32());
            Assert.Equal(1.24m, record.GetProperty("weight").GetDecimal());
            Assert.Equal("2026-08-12 08:01:02.000", record.GetProperty("scanTime").GetString());
            Assert.False(record.TryGetProperty("siteCode", out _));
            return StubHttpMessageHandler.CreateOkResponse("{\"code\":1,\"msg\":\"success\",\"data\":null}");
        });
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var result = await api.SetParameters(parameters);
        Assert.True(result.Key, result.Value);
        using var source = new Bitmap(2, 2);
        using var imageHandle = ImageHandle.TakeOwnership((Bitmap)source.Clone());
        await api.UploadInBackground("JT001", 1.235m,
            new DateTime(2026, 8, 12, 8, 1, 2),
            imageInfo: new UploadImageInfo { Image = imageHandle },
            other: new JtPolarDayApi.UploadContext
            {
                OperateType = 2, CarNum = "1", GridNo = "01", GridCode = "111"
            });
        Assert.Equal(4, paths.Count);
        Assert.EndsWith("/getUploadUrl", paths[0]);
        Assert.Equal("/JT001.jpg", paths[1]);
        Assert.Equal(2, source.Height);
    }

    /// <summary>
    /// 存储服务没有返回 HTTP 200 时不得将图片半路径写入设备报文。
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FailedBinaryPutDoesNotPublishHalfPath(HttpStatusCode statusCode)
    {
        var parameters = CreateParameters();
        parameters.BaseUrl = JtPolarDayApi.TestBaseUrl;
        parameters.RetryCount = 1;
        using var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/getUploadUrl", StringComparison.Ordinal))
            {
                return StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":1,\"msg\":\"success\",\"data\":[{\"halfPath\":\"failed.jpg\",\"fullPath\":\"https://storage.test/failed.jpg\",\"contentType\":\"image/jpeg\"}]}");
            }

            return request.Method == HttpMethod.Put
                ? new HttpResponseMessage(statusCode)
                : StubHttpMessageHandler.CreateOkResponse("{\"code\":1,\"msg\":\"success\",\"data\":null}");
        });
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var result = await api.SetParameters(parameters);
        Assert.True(result.Key, result.Value);
        using var image = ImageHandle.TakeOwnership(new Bitmap(2, 2));
        await api.UploadInBackground("JT001", 0, new DateTime(2026, 8, 12, 8, 1, 2),
            imageInfo: new UploadImageInfo { Image = image },
            other: new JtPolarDayApi.UploadContext
            {
                CarNum = "1", GridNo = "01", GridCode = "111", HalfPath = "stale.jpg"
            });
        Assert.Equal(4, handler.RequestCount);
        Assert.All(handler.RequestContents.Skip(2), body =>
        {
            using var document = JsonDocument.Parse(body);
            Assert.Equal(1, document.RootElement.GetArrayLength());
            Assert.False(document.RootElement[0].TryGetProperty("halfPath", out _));
        });
    }

    /// <summary>
    /// 文档没有定义操作类型 3，配置时应明确拒绝。
    /// </summary>
    [Fact]
    public async Task UndefinedOperateTypeIsRejected()
    {
        using var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.CreateOkResponse("{}"));
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var parameters = CreateParameters();
        parameters.OperateType = 3;
        Assert.False((await api.SetParameters(parameters)).Key);
        Assert.Equal(0, handler.RequestCount);
    }

    /// <summary>
    /// 本地图片暂存失败时仍应继续发送无图 scanInfo 和 packageInfo。
    /// </summary>
    [Fact]
    public async Task DeviceInfoUploadsWithoutImageWhenLocalImageStagingFails()
    {
        var requestPaths = new List<string>();
        using var handler = new StubHttpMessageHandler(request =>
        {
            requestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            return request.RequestUri?.AbsolutePath.Contains(
                       "/polarDay/upload/sortingImage",
                       StringComparison.OrdinalIgnoreCase) == true
                ? StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":0,\"msg\":\"staging failed\",\"data\":null}")
                : StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":1,\"msg\":\"success\",\"data\":null}");
        });
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var parameterResult = await api.SetParameters(CreateParameters());
        Assert.True(parameterResult.Key, parameterResult.Value);

        using var image = ImageHandle.TakeOwnership(new Bitmap(2, 2));
        await api.UploadInBackground(
            "JT5513378378679",
            1,
            new DateTime(2026, 8, 10, 16, 38, 38),
            imageInfo: new UploadImageInfo
            {
                Image = image,
                CameraSerialNumber = "CAM01"
            },
            other: new JtPolarDayApi.UploadContext
            {
                CarNum = "84",
                GridNo = "05",
                GridCode = "01",
                FallTime = new DateTime(2026, 8, 10, 16, 38, 41)
            });

        Assert.Equal(3, requestPaths.Count);
        Assert.EndsWith(
            "/polarDay/upload/sortingImage",
            requestPaths[0],
            StringComparison.OrdinalIgnoreCase);
        Assert.All(
            requestPaths.Skip(1),
            path => Assert.EndsWith(
                "/polarDay/upload/deviceInfo",
                path,
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            handler.RequestContents,
            content => content.Contains(
                "\"eventType\":\"packageInfo\"",
                StringComparison.Ordinal));
    }

    /// <summary>
    /// 图片必须先暂存到本地适配服务，再把 halfPath 传给设备报文。
    /// </summary>
    [Fact]
    public async Task ImageStagesLocallyBeforeDeviceInfoWithoutDirectOpaCall()
    {
        var requestPaths = new List<string>();
        using var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            requestPaths.Add(path);
            return path.EndsWith(
                    "/polarDay/upload/sortingImage",
                    StringComparison.OrdinalIgnoreCase)
                ? StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":1,\"msg\":\"success\",\"data\":{\"halfPath\":\"staging\\\\JT001.jpg\"}}")
                : StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":1,\"msg\":\"success\",\"data\":null}");
        });
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var parameterResult = await api.SetParameters(CreateParameters());
        Assert.True(parameterResult.Key, parameterResult.Value);

        using var image = ImageHandle.TakeOwnership(new Bitmap(2, 2));
        await api.UploadInBackground(
            "JT001",
            0,
            new DateTime(2026, 8, 12, 8, 1, 2),
            imageInfo: new UploadImageInfo
            {
                Image = image,
                CameraSerialNumber = "CAM01"
            },
            other: new JtPolarDayApi.UploadContext
            {
                CarNum = "1",
                GridNo = "01",
                GridCode = "111",
                FallTime = new DateTime(2026, 8, 12, 8, 1, 3)
            });

        Assert.Equal(
            "/polarDay/upload/sortingImage",
            requestPaths[0]);
        Assert.DoesNotContain(
            requestPaths,
            path => path.Contains(
                "/opa/",
                StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, requestPaths.Count);
        Assert.All(
            handler.RequestContents.Skip(1),
            content => Assert.Contains(
                "staging\\\\JT001.jpg",
                content,
                StringComparison.Ordinal));
    }

    /// <summary>
    /// 创建满足极昼参数校验的测试配置。
    /// </summary>
    /// <returns>测试使用的极昼参数。</returns>
    private static JtPolarDayApi.ApiParameter CreateParameters() => new()
    {
        BaseUrl = "http://unit.test",
        AppKey = "app-key",
        AppSecret = "app-secret",
        ImageReqUserCode = "123",
        SiteCode = "6398155",
        EquipmentCode = "ZXJCD6398155001",
        SortingPlanCode = "6398155-001",
        Operator = "LS6398155001",
        EquipmentLayer = 1,
        AreaNum = 1,
        MaxCircleNum = 1,
        SupplyDeskCode = "1",
        SupplyDeskSerialNo = "1",
        SupplyDeskMethod = "1",
        SupplyDeskArea = "sanmenxia",
        LayerNum = 1,
        ChuteModel = "1",
        FallArea = 1,
        WeightSource = "0"
    };
}
