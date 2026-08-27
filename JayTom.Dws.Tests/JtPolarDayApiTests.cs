using System.Drawing;
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
