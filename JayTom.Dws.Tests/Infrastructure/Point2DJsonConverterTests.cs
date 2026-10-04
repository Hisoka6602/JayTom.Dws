using JayTom.Dws.Abstractions.Devices;
using JayTom.Dws.Infrastructure.Configuration;
using JayTom.Dws.Legacy.Contracts.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JayTom.Dws.Tests.Infrastructure;

/// <summary>旧配置升级时坐标不应让整份输出配置加载失败，也不能丢失其他字段。</summary>
public sealed class Point2DJsonConverterTests
{
    /// <summary>历史输出配置中的坐标字符串和其余开关、按键及延迟一起加载。</summary>
    /// <summary>旧格式坐标恢复时保留输出配置的其余字段。</summary>
    [Fact]
    public void LegacyCoordinatesPreserveOutputSettings()
    {
        const string json = """
            {"IsUseTcpOutput":true,"LocationOutputSettingsInfo":{
            "BarcodeOutputPosition":"0, 0","WeightOutputPosition":"120, -30",
            "OperationDelay":123,"BarcodeOutputKey":"Enter","IsOutputBarcode":true}}
            """;
        var settings = JsonConvert.DeserializeObject<ResultOutputSettingsDto>(
            json, new Point2DJsonConverter());

        Assert.NotNull(settings);
        Assert.True(settings.IsUseTcpOutput);
        Assert.Equal(new Point2D(0, 0), settings.LocationOutputSettingsInfo.BarcodeOutputPosition);
        Assert.Equal(new Point2D(120, -30), settings.LocationOutputSettingsInfo.WeightOutputPosition);
        Assert.Equal(123, settings.LocationOutputSettingsInfo.OperationDelay);
        Assert.Equal("Enter", settings.LocationOutputSettingsInfo.BarcodeOutputKey);
        Assert.True(settings.LocationOutputSettingsInfo.IsOutputBarcode);
    }

    /// <summary>当前对象、大小写兼容对象和历史字符串读出相同坐标。</summary>
    /// <summary>新旧坐标格式表达相同位置。</summary>
    [Theory]
    [InlineData("{\"X\":120,\"Y\":-30}")]
    [InlineData("{\"x\":120,\"y\":-30}")]
    [InlineData("\" 120 , -30 \"")]
    public void BothFormatsReadIdenticalCoordinates(string json)
    {
        Assert.Equal(new Point2D(120, -30),
            JsonConvert.DeserializeObject<Point2D>(json, new Point2DJsonConverter()));
    }

    /// <summary>读取兼容逻辑不改变新配置的坐标对象格式。</summary>
    /// <summary>写出坐标时沿用对象格式。</summary>
    [Fact]
    public void SavingCoordinatesStillWritesAnObject()
    {
        var json = JsonConvert.SerializeObject(new Point2D(120, -30), new Point2DJsonConverter());
        var value = JObject.Parse(json);
        Assert.Equal(120, value.Value<int>("X"));
        Assert.Equal(-30, value.Value<int>("Y"));
    }

    /// <summary>错误坐标必须显式报错，不能静默覆盖已有输出位置。</summary>
    /// <summary>无效坐标必须提供显式反序列化错误。</summary>
    [Theory]
    [InlineData("\"invalid\"")]
    [InlineData("\"1,2,3\"")]
    [InlineData("\"2147483648,0\"")]
    [InlineData("{\"X\":\"invalid\",\"Y\":0}")]
    public void InvalidCoordinatesAreNotSilentlyReset(string json)
    {
        Assert.Throws<JsonSerializationException>(() =>
            JsonConvert.DeserializeObject<Point2D>(json, new Point2DJsonConverter()));
    }
}
