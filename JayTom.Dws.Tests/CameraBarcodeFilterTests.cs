using JayTom.Dws.Camera.FilterContainer;
using JayTom.Dws.Camera;

namespace JayTom.Dws.Tests;

/// <summary>验证相机条码过滤器在高并发和观测时间窗口下保持原子去重语义。</summary>
public sealed class CameraBarcodeFilterTests {
    /// <summary>验证同一条码在过期窗口内被拒绝，超过窗口后可以再次通过。</summary>
    [Fact]
    public void ValidateData_UsesObservationTimeForExpiration() {
        BarCodeFilterContainer.ResetFilter();
        var filter = CreateFilter(TimeSpan.FromMilliseconds(150));
        var firstObservation = new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Local);

        Assert.True(Validate(filter, "JT100", firstObservation).IsValidationPassed);
        Assert.False(Validate(filter, "JT100", firstObservation.AddMilliseconds(150)).IsValidationPassed);
        Assert.True(Validate(filter, "JT100", firstObservation.AddMilliseconds(151)).IsValidationPassed);
    }

    /// <summary>验证多个相机线程同时提交同一条码时只有一个提交者可以通过。</summary>
    [Fact]
    public void ValidateData_ConcurrentDuplicate_AllowsExactlyOne() {
        BarCodeFilterContainer.ResetFilter();
        var filter = CreateFilter(TimeSpan.FromSeconds(1));
        var observationTime = new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Local);
        var passed = 0;

        Parallel.For(0, 128, _ => {
            if (Validate(filter, "JT-CONCURRENT", observationTime).IsValidationPassed) {
                Interlocked.Increment(ref passed);
            }
        });

        Assert.Equal(1, passed);
    }

    /// <summary>创建关闭规则过滤、仅启用时间去重的测试过滤器。</summary>
    private static BarCodeFilterContainer CreateFilter(TimeSpan expiration) {
        return new BarCodeFilterContainer {
            MaxSize = 0,
            ExpirationTime = expiration,
            BarCodeFilterMode = BarCodeFilterMode.None
        };
    }

    /// <summary>使用指定观测时间验证一个条码。</summary>
    private static ValidationResult Validate(
        BarCodeFilterContainer filter,
        string barcode,
        DateTime observationTime) {
        return filter.ValidateData(new BarCodeFilterInfo {
            BarCode = barcode,
            ScanTime = observationTime
        });
    }
}
