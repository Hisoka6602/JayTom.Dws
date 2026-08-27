using System.Text.RegularExpressions;

namespace JayTom.Dws.Integrations;

/// <summary>隔离默认上传接口的响应解析规则，使传输与业务判定可以独立演进。</summary>
internal sealed class DefaultApiResponseEvaluator
{
    /// <summary>按配置的精确、包含或正则规则判断响应是否代表成功。</summary>
    public bool IsSuccess(
        string responseContent,
        int validationMode,
        string completeMatch,
        string stringContains,
        string regularExpression)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return false;
        }
        return validationMode switch
        {
            0 => responseContent.Equals(completeMatch, StringComparison.Ordinal),
            1 => responseContent.Contains(stringContains, StringComparison.Ordinal),
            2 => Regex.IsMatch(
                responseContent,
                regularExpression,
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(250)),
            _ => false
        };
    }
}
