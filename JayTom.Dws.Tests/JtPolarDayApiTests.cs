using System.Drawing;
using JayTom.Dws.Interface;
using JayTom.Dws.Interface.Jtexpress;

namespace JayTom.Dws.Tests;

/// <summary>
/// 验证极昼查询和设备信息回传的关键容错行为。
/// </summary>
public sealed class JtPolarDayApiTests
{
    /// <summary>
    /// 图片服务失败不能阻断 scanInfo 和 packageInfo 设备报文。
    /// </summary>
    [Fact]
    public async Task DeviceInfoStillUploadsWhenImageServiceFails()
    {
        var requestPaths = new List<string>();
        using var handler = new StubHttpMessageHandler(request =>
        {
            requestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            return request.RequestUri?.AbsolutePath.Contains(
                       "/opa/smartLogin",
                       StringComparison.OrdinalIgnoreCase) == true
                ? StubHttpMessageHandler.CreateOkResponse(
                    "{\"succ\":false,\"msg\":\"login failed\"}")
                : StubHttpMessageHandler.CreateOkResponse(
                    "{\"code\":1,\"msg\":\"success\",\"data\":null}");
        });
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        var parameterResult = await api.SetParameters(CreateParameters());
        Assert.True(parameterResult.Key, parameterResult.Value);

        using var image = new Bitmap(2, 2);
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

        Assert.Contains(
            requestPaths,
            path => path.EndsWith(
                "/opa/smartLogin",
                StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            2,
            requestPaths.Count(path => path.EndsWith(
                "/polarDay/upload/deviceInfo",
                StringComparison.OrdinalIgnoreCase)));
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
        ImageServiceBaseUrl = "http://image.unit.test",
        ImageAccount = "image-account",
        ImagePassword = "image-password",
        ImageAppKey = "image-app-key",
        ImageAppSecret = "image-app-secret",
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
