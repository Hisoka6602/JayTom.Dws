using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证持久化实体到历史读模型的集中映射。</summary>
public sealed class PackageHistoryMapperTests
{
    /// <summary>映射结果不得保留 EF 导航集合或可变实体引用。</summary>
    [Fact]
    public void Mapping_creates_detached_immutable_read_model()
    {
        var source = new PackageInfoModel
        {
            PackageTimestamped = 123,
            PackageCreateTime = new DateTime(2026, 8, 14),
            BarCodeInfo = new BarCodeInfoModel
            {
                Barcode = "JT-MAP",
                SerialNumber = "CAM-1",
                ScanTime = new DateTime(2026, 8, 14, 1, 2, 3)
            },
            ImageInfos =
            [
                new ImageInfoModel { Type = 1, LocalPath = "before.jpg" }
            ]
        };

        PackageHistoryItem mapped = PackageHistoryMapper.Map(source);
        source.BarCodeInfo.Barcode = "MUTATED";
        source.ImageInfos.First().LocalPath = "after.jpg";
        source.ImageInfos.Clear();

        Assert.Equal("JT-MAP", mapped.BarCodeInfo?.Barcode);
        Assert.Collection(
            mapped.ImageInfos,
            item => Assert.Equal("before.jpg", item.LocalPath));
        Assert.False(mapped.ImageInfos is List<PackageHistoryImage>);
    }
}
