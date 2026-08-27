using JayTom.Dws.Abstractions.Observability;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JayTom.Dws.Integrations;

/// <summary>生成可安全写入审计记录的外部接口参数快照。</summary>
internal static class IntegrationParameterSerializer
{
    /// <summary>序列化参数并递归清除凭据和嵌入字符串中的敏感片段。</summary>
    public static string Serialize(object? value)
    {
        JToken root = value is null ? JValue.CreateNull() : JToken.FromObject(value);
        Redact(root);
        return root.ToString(Formatting.None);
    }

    /// <summary>递归处理对象、数组和字符串值。</summary>
    private static void Redact(JToken token)
    {
        if (token is JObject jsonObject)
        {
            foreach (JProperty property in jsonObject.Properties().ToArray())
            {
                if (SensitiveDataRedactor.IsSensitiveKey(property.Name))
                {
                    property.Value = SensitiveDataRedactor.RedactedValue;
                }
                else
                {
                    Redact(property.Value);
                }
            }
            return;
        }
        if (token is JArray array)
        {
            foreach (JToken item in array)
            {
                Redact(item);
            }
            return;
        }
        if (token is JValue { Type: JTokenType.String, Value: string text } value)
        {
            value.Value = SensitiveDataRedactor.RedactMessage(text);
        }
    }
}
