using System.Text.RegularExpressions;

namespace JayTom.Dws.Abstractions.Observability;

/// <summary>统一隐藏凭据、令牌和授权头等敏感字段。</summary>
public static partial class SensitiveDataRedactor
{
    /// <summary>日志中替代敏感值的固定文本。</summary>
    public const string RedactedValue = "***REDACTED***";

    /// <summary>判断字段名是否表示凭据。</summary>
    public static bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }
        return key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("passwd", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("apiKey", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("accessKey", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("key", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("ak", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("sk", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("salt", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("sid", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("license", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>按字段名和常见凭据文本模式生成可安全记录的值。</summary>
    public static string Redact(string key, string? value)
    {
        if (IsSensitiveKey(key))
        {
            return RedactedValue;
        }
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        return CredentialPattern().Replace(value, match => $"{match.Groups[1].Value}={RedactedValue}");
    }

    /// <summary>隐藏异常消息或外部响应中的常见凭据片段。</summary>
    public static string RedactMessage(string? message) => Redact(string.Empty, message);

    /// <summary>匹配查询参数、JSON 和授权头中的敏感键值。</summary>
    [GeneratedRegex(
        "(?i)(password|passwd|secret|token|authorization|api[_-]?key|access[_-]?key|license|salt|sid)\\s*[:=]\\s*[^&\\s,;}]+",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 100)]
    private static partial Regex CredentialPattern();
}
