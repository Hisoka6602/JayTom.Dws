using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.UseCases;

/// <summary>应用请求输入校验器。</summary>
public interface IApplicationRequestValidator<in TRequest>
{
    /// <summary>校验请求并返回全部稳定错误。</summary>
    IReadOnlyList<Error> Validate(TRequest request);
}
