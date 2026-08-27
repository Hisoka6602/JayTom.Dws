using JayTom.Dws.Legacy.Contracts.Dto;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证创建包裹客户端配置的边界校验。</summary>
public sealed class CreatePackageSettingsTests
{
    /// <summary>非固定的客户端时间边界应能通过校验。</summary>
    [Fact]
    public void TryValidate_AcceptsClientConfiguredTimingValues()
    {
        var settings = new CreatePackageSettingsDto
        {
            IsUseEmptyPackageExpiry = true,
            EmptyPackageExpiryTime = 333,
            PackageCreationInterval = 91,
            IsUseBarcodeAssignmentInterval = true,
            MinimumAssignmentTime = 73,
            MaximumAssignmentTime = 287
        };

        var valid = settings.TryValidate(out var message);

        Assert.True(valid, message);
    }

    /// <summary>上下限倒置会导致所有赋值被拒绝，客户端保存前必须拦截。</summary>
    [Fact]
    public void TryValidate_RejectsReversedAssignmentWindow()
    {
        var settings = new CreatePackageSettingsDto
        {
            IsUseBarcodeAssignmentInterval = true,
            MinimumAssignmentTime = 300,
            MaximumAssignmentTime = 200
        };

        var valid = settings.TryValidate(out var message);

        Assert.False(valid);
        Assert.NotEmpty(message);
    }

    /// <summary>启用零长度窗口会让机械链路几乎无法命中，必须在保存前拦截。</summary>
    [Fact]
    public void TryValidate_RejectsZeroLengthEnabledAssignmentWindow()
    {
        var settings = new CreatePackageSettingsDto
        {
            IsUseBarcodeAssignmentInterval = true,
            MinimumAssignmentTime = 0,
            MaximumAssignmentTime = 0
        };

        Assert.False(settings.TryValidate(out _));
    }

    /// <summary>禁用赋值窗口时，历史的零值不应阻止其他配置保存。</summary>
    [Fact]
    public void TryValidate_AllowsDisabledAssignmentWindow()
    {
        var settings = new CreatePackageSettingsDto
        {
            PackageCreationInterval = 42,
            IsUseBarcodeAssignmentInterval = false
        };

        Assert.True(settings.TryValidate(out var message), message);
    }
}
