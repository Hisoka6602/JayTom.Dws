using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Camera;
using JayTom.Dws.Ocr;
using JayTom.Dws.Plugin.Contracts;
using System.Reflection;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定关键模块对外发布的稳定契约和版本化迁移规则。</summary>
public sealed class StablePublicApiTests
{
    /// <summary>验证关键公共契约仍由正确程序集公开。</summary>
    [Fact]
    public void Critical_modules_must_publish_their_stable_contracts()
    {
        Assert.True(typeof(Result).IsPublic);
        Assert.True(typeof(Result<>).IsPublic);
        Assert.True(typeof(ISettingsStore).IsPublic);
        Assert.True(typeof(ICamera).IsPublic);
        Assert.True(typeof(IOcr).IsPublic);
        Assert.True(typeof(IPlugin).IsPublic);

        Assert.Equal("JayTom.Dws.Abstractions", typeof(Result).Assembly.GetName().Name);
        Assert.Equal("JayTom.Dws.Application", typeof(ISettingsStore).Assembly.GetName().Name);
        Assert.Equal("JayTom.Dws.Camera", typeof(ICamera).Assembly.GetName().Name);
        Assert.Equal("JayTom.Dws.Ocr", typeof(IOcr).Assembly.GetName().Name);
        Assert.Equal("JayTom.Dws.Plugin.Contracts", typeof(IPlugin).Assembly.GetName().Name);
    }

    /// <summary>验证稳定契约保留关键生命周期与原子操作成员。</summary>
    [Fact]
    public void Stable_contracts_must_keep_required_operations()
    {
        AssertPublicMethod(typeof(ISettingsStore), "GetSnapshotAsync");
        AssertPublicMethod(typeof(ISettingsStore), "ReplaceSnapshotAsync");
        AssertPublicMethod(typeof(ICamera), "Initialize");
        AssertPublicMethod(typeof(ICamera), "ApplySettingsAsync");
        AssertPublicMethod(typeof(IOcr), "SubmitImage");
        Assert.NotNull(typeof(IPlugin).GetEvent("PluginLoaded"));
        Assert.NotNull(typeof(IPlugin).GetEvent("PluginExceptionOccurred"));
    }

    /// <summary>验证耗时字段采用带单位名称，旧 API 具有明确版本化迁移标记。</summary>
    [Fact]
    public void Deprecated_duration_api_must_forward_to_the_unit_named_member()
    {
        PropertyInfo current = typeof(OcrResult).GetProperty(nameof(OcrResult.ElapsedMilliseconds))!;
        PropertyInfo legacy = typeof(OcrResult).GetProperty("ElapsedTime")!;
        ObsoleteAttribute? obsolete = legacy.GetCustomAttribute<ObsoleteAttribute>();
        var result = new OcrResult { ElapsedMilliseconds = 42 };

        Assert.Equal(typeof(long), current.PropertyType);
        Assert.NotNull(obsolete);
        Assert.Contains("v2", obsolete.Message, StringComparison.OrdinalIgnoreCase);
#pragma warning disable CS0618
        Assert.Equal(result.ElapsedMilliseconds, result.ElapsedTime);
        result.ElapsedTime = 84;
#pragma warning restore CS0618
        Assert.Equal(84, result.ElapsedMilliseconds);
    }

    /// <summary>验证指定公共方法存在。</summary>
    private static void AssertPublicMethod(Type type, string methodName)
    {
        Assert.Contains(type.GetMethods(), method =>
            method.Name.Equals(methodName, StringComparison.Ordinal));
    }
}
