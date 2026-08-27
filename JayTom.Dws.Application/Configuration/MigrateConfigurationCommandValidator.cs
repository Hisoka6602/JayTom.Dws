using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.UseCases;

namespace JayTom.Dws.Application.Configuration;

/// <summary>集中校验配置迁移命令。</summary>
public sealed class MigrateConfigurationCommandValidator :
    IApplicationRequestValidator<MigrateConfigurationCommand>
{
    /// <summary>校验目标版本必须为非负数。</summary>
    public IReadOnlyList<Error> Validate(MigrateConfigurationCommand request) =>
        request.TargetVersion < 0
            ? [new Error("configuration.invalid_target_version", "目标配置版本不能为负数。")]
            : [];
}
