using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证预期失败、稳定错误代码和异常之间的边界。</summary>
public sealed class ResultContractTests
{
    /// <summary>验证预期验证失败通过错误值返回，不依赖异常控制流。</summary>
    [Fact]
    public void Expected_failure_carries_a_stable_error_value()
    {
        Error error = Error.Validation("缺少配置");
        OperationResult<string> result = OperationResult<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.failed", result.ErrorCode);
        Assert.Equal(error, result.Error);
        Assert.Equal("缺少配置", result.ErrorMessage);
    }

    /// <summary>验证失败结果禁止使用“无错误”，防止成功失败语义混淆。</summary>
    [Fact]
    public void Failure_must_not_use_the_none_error()
    {
        Assert.Throws<ArgumentException>(() =>
            OperationResult<string>.Failure(Error.None));
        Assert.Throws<ArgumentException>(() =>
            Result.Failure(Error.None));
    }

    /// <summary>验证失败的强类型结果读取值时抛出编程错误，而预期失败本身保持可检查。</summary>
    [Fact]
    public void Failed_typed_result_rejects_value_access()
    {
        Result<int> result = Result<int>.Failure(Error.Cancelled);

        Assert.True(result.IsFailure);
        Assert.Equal("operation.cancelled", result.Error.Code);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
