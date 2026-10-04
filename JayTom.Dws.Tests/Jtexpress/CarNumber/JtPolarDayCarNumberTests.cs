using System.Text.Json;
using JayTom.Dws.Integrations.Jtexpress;

namespace JayTom.Dws.Tests;

/// <summary>下位机分拣编码是实际小车号，不能与目标格口混用，也不能依赖未接入的灰度设备。</summary>
public sealed class JtPolarDayCarNumberTests
{
    /// <summary>验证实际设备报文中的小车号、格口和图片路径，覆盖未接入灰度设备的现场。</summary>
    /// <summary>从下位机创建的包裹沿用实际分拣流水号作为小车号。</summary>
    [Theory]
    [InlineData(42L, null)]
    [InlineData(42L, 99)]
    [InlineData(0L, null)]
    public async Task LowerMachineSortingCodeIsUploadedAsCarNumber(long sortingCode, int? grayscaleCarNumber)
    {
        using var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.CreateOkResponse("{\"code\":1,\"msg\":\"success\"}"));
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        Assert.True((await api.SetParameters(new JtPolarDayApi.ApiParameter
        {
            BaseUrl = "http://unit.test", AppKey = "app-key", AppSecret = "app-secret",
            SupplyDeskCode = "1", SupplyDeskSerialNo = "1", SupplyDeskArea = "sanmenxia"
        })).Key);
        var scanTime = new DateTime(2026, 10, 3, 11, 3, 38);
        var context = new JtPolarDayApi.UploadContext
        {
            CarNum = JtPolarDayApi.GetUploadCarNumber(true, sortingCode, grayscaleCarNumber),
            GridNo = "44", GridCode = "111", HalfPath = "scan/image.jpg",
            LandOnCarTime = scanTime, FallTime = scanTime.AddSeconds(12)
        };

        await api.UploadInBackground("JT553105744335", 1.235m, scanTime, other: context);

        Assert.Equal(2, handler.RequestCount);
        using var scan = JsonDocument.Parse(handler.RequestContents[0]);
        Assert.Equal("scanInfo", scan.RootElement[0].GetProperty("eventType").GetString());
        using var fall = JsonDocument.Parse(handler.RequestContents[1]);
        Assert.Equal("packageInfo", fall.RootElement[0].GetProperty("eventType").GetString());
        Assert.Equal(sortingCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
            fall.RootElement[0].GetProperty("carNum").GetString());
        Assert.Equal("44", fall.RootElement[0].GetProperty("gridNo").GetString());
        Assert.Equal("111", fall.RootElement[0].GetProperty("gridCode").GetString());
        Assert.Equal("scan/image.jpg", fall.RootElement[0].GetProperty("halfPath").GetString());
    }

    /// <summary>软件创建的包裹序号不作为小车号，只有实际灰度设备结果可用于上报。</summary>
    /// <summary>其他创建方式仅使用实际灰度设备提供的小车号。</summary>
    [Fact]
    public void OtherCreationSourcesUseOnlyAnActualGrayscaleCarNumber()
    {
        Assert.Equal("99", JtPolarDayApi.GetUploadCarNumber(false, 123456789, 99));
        Assert.Equal(string.Empty, JtPolarDayApi.GetUploadCarNumber(false, 123456789, null));
        Assert.Equal(string.Empty, JtPolarDayApi.GetUploadCarNumber(true, -1, null));
    }
}
