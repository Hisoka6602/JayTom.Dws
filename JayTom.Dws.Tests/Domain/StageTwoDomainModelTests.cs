using JayTom.Dws.Domain.Licensing;
using JayTom.Dws.Domain.Packages;
using JayTom.Dws.Domain.Policies;
using JayTom.Dws.Domain.Sorting;

namespace JayTom.Dws.Tests.Domain;

/// <summary>验证第二阶段领域模型的不变量和纯业务规则。</summary>
public sealed class StageTwoDomainModelTests {
    /// <summary>包裹聚合应原子演进状态并拒绝重复条码绑定。</summary>
    [Fact]
    public void Package_aggregate_enforces_lifecycle_invariants() {
        var timestamp = new DateTimeOffset(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);
        var package = new PackageAggregate(PackageId.From(42), timestamp);

        var first = package.TryBindBarcode(new PackageBarcode("JT-001"), timestamp);
        var duplicate = package.TryBindBarcode(new PackageBarcode("JT-002"), timestamp);
        var ready = package.TryMarkReady(timestamp);
        var complete = package.TryComplete(timestamp);
        var completeAgain = package.TryComplete(timestamp);

        Assert.True(first);
        Assert.False(duplicate);
        Assert.True(ready);
        Assert.True(complete);
        Assert.False(completeAgain);
        Assert.Equal(PackageState.Completed, package.State);
        Assert.Equal(3, package.DomainEvents.Count);
    }

    /// <summary>强类型标识应拒绝非正数据库标识。</summary>
    [Fact]
    public void Package_identifier_rejects_non_positive_values() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PackageId.From(0));
        Assert.Equal(7, PackageId.From(7).Value);
    }

    /// <summary>类型化分拣规则应可组合且无需 JSON 解释器。</summary>
    [Fact]
    public void Sorting_rules_compose_over_typed_facts() {
        SortingRule rule = new AllOfSortingRule([
            new BarcodePrefixRule("JT"),
            new WeightRangeRule(1.0m, 3.0m)
        ]);

        Assert.True(rule.IsMatch(new SortingFacts("jt-001", 2.5m, null)));
        Assert.False(rule.IsMatch(new SortingFacts("SF-001", 2.5m, null)));
    }

    /// <summary>重试退避必须按整数时长增长并在预算上限饱和。</summary>
    [Fact]
    public void Retry_policy_saturates_at_maximum_delay() {
        var policy = RetryPolicy.Create(
            maximumAttempts: 10,
            initialDelay: TimeSpan.FromMilliseconds(100),
            maximumDelay: TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.DelayForAttempt(1));
        Assert.Equal(TimeSpan.FromMilliseconds(800), policy.DelayForAttempt(4));
        Assert.Equal(TimeSpan.FromSeconds(1), policy.DelayForAttempt(10));
    }

    /// <summary>授权聚合应同时执行时间、功能和撤销不变量。</summary>
    [Fact]
    public void License_aggregate_is_independent_of_file_format() {
        var issuedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var license = new LicenseAggregate(
            new LicenseId("license-001"),
            issuedAt,
            issuedAt.AddDays(30),
            ["camera", "sorting"]);

        Assert.True(license.IsValidAt(issuedAt.AddDays(1)));
        Assert.True(license.HasFeature("CAMERA"));
        license.Revoke();
        Assert.False(license.IsValidAt(issuedAt.AddDays(1)));
    }
}
