using System;
using System.Linq;
using ConsoleApp2;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using JayTom.Dws.Camera;
using System.Collections;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using JayTom.Dws.Interface.Wdt;
using System.Collections.Generic;
using Org.BouncyCastle.Utilities;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.Szjy188;
using static System.Text.Json.JsonElement;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Domain.DownstreamProtocols;
using static JayTom.Dws.Interface.Szjy188.SzjyApi;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Irayple;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;

internal class Program {

    private static async Task Main(string[] args) {
        var tryParse = float.TryParse("+1.288", out var result);
        return;
        var daHuaVolumeCamera = new DaHuaVolumeCamera();
        daHuaVolumeCamera.VolumeCaptured += delegate (object? sender, VolumeCapturedEventArgs eventArgs) {
            Console.WriteLine($"长:{eventArgs.Length}--宽:{eventArgs.Width}--高:{eventArgs.Height}");
        };
        var (key, value) = await daHuaVolumeCamera.Initialize(null);
        if (key) {
            var (b, s) = await daHuaVolumeCamera.Start(null);
            if (!b) {
                Console.WriteLine(s);
            }
        }
        else {
            Console.WriteLine(value);
        }

        Console.ReadKey();

        daHuaVolumeCamera.Dispose();
        return;
        var data = new JtstCommunicationProtocol().
              EncodeData(FunctionType.SendExit,
                  0, "05", "9720074634557");

        return;

        var xorChecksum = WxkcCommunicationProtocol.XorChecksum(new byte[] { 0xFC, 0x12, 0x01, 0x01, 0x00, 0x00, 0x01 });
        var encodeData = new WxkcCommunicationProtocol().EncodeData(FunctionType.Heartbeat, 0, "00 00", null);

        int num = 256;
        var base64String = BitConverter.ToString(new byte[] { 0x11, 0x1A });
        string hexString = num.ToString("X4");

        string json = "{\"key1\":{\"key2\":{\"field\":\"valueaa\"}},\"key3\":[{\"field1\":\"value2\"}]}";
        string fieldName = "key";
        JsonDocument jsonDocument = JsonDocument.Parse(json);
        JsonElement? fieldValue = FindFieldValue(jsonDocument.RootElement, fieldName);
        if (fieldValue.HasValue) {
            Console.WriteLine($"Field '{fieldName}' exists. Value: {fieldValue.Value}");
        }
        else {
            Console.WriteLine($"Field '{fieldName}' does not exist.");
        }
        return;

        var szjyApi = new SzjyApi(null);
        szjyApi.SetParameters(new SzjyApi.ApiParameter() {
            UserName = "quanlai07",
            Password = "Ql123456",
            Url = "https://www.szjy188.com/auto-entry"
        });
        var uploadResponse = await szjyApi.UploadData("1234567890aa",
            1.05, DateTime.Now, 10, 20,
            30, 40, null, null, "Box");
        Console.WriteLine(uploadResponse);
        Console.ReadLine();
    }

    private static JsonElement? FindFieldValue(JsonElement root, string fieldName) {
        try {
            var stack = new Stack<JsonElement>();
            stack.Push(root);

            while (stack.Count > 0) {
                var element = stack.Pop();

                if (element.ValueKind == JsonValueKind.Object) {
                    if (element.TryGetProperty(fieldName, out JsonElement field)) {
                        return field;
                    }

                    foreach (JsonProperty property in element.EnumerateObject()) {
                        stack.Push(property.Value);
                    }
                }
                else if (element.ValueKind == JsonValueKind.Array) {
                    foreach (JsonElement arrayElement in element.EnumerateArray()) {
                        stack.Push(arrayElement);
                    }
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
        }

        return null;
    }

    private static JsonElement? FindFieldValue1(JsonElement element, string fieldName) {
        if (element.ValueKind == JsonValueKind.Object) {
            if (element.TryGetProperty(fieldName, out JsonElement field)) {
                return field;
            }

            foreach (JsonProperty property in element.EnumerateObject()) {
                JsonElement? fieldValue = FindFieldValue(property.Value, fieldName);
                if (fieldValue.HasValue) {
                    return fieldValue;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement arrayElement in element.EnumerateArray()) {
                JsonElement? fieldValue = FindFieldValue(arrayElement, fieldName);
                if (fieldValue.HasValue) {
                    return fieldValue;
                }
            }
        }

        return null;
    }

    public static Dictionary<string, object> SearchFields(JsonElement element) {
        Dictionary<string, object> jsonDictionary = new();
        if (element.ValueKind == JsonValueKind.Object) {
            var objectEnumerator = element.EnumerateObject();
            foreach (var variable in objectEnumerator) {
                jsonDictionary.Add(variable.Name, variable.Value);
                if (variable.Value.ValueKind != JsonValueKind.Undefined) {
                    SearchFields(variable.Value);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array) {
            foreach (var arrayElement in element.EnumerateArray()) {
                if (arrayElement.ValueKind != JsonValueKind.Undefined) {
                    SearchFields(arrayElement);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.String) {
            Console.WriteLine(element);
        }

        return jsonDictionary;
    }

    private static object? SearchField(JsonElement element, string fieldName) {
        if (element.ValueKind == JsonValueKind.Object) {
            var objectEnumerator = element.EnumerateObject();
            foreach (var variable in objectEnumerator) {
                if (variable.Name.Equals(fieldName)) {
                    return variable.Value.ToString();
                }
                else {
                    if (variable.Value.ValueKind != JsonValueKind.Undefined) {
                        SearchField(variable.Value, fieldName);
                    }
                }
            }
            var property = element.EnumerateObject()
                .FirstOrDefault(x => x.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (property.Name.Equals(fieldName)) {
                return property.Value.ToString();
            }
            if (property.Value.ValueKind == JsonValueKind.Undefined) // ValueKind is undefined
            {
                return null;
            }
            else if (property.Value.ValueKind == JsonValueKind.Object ||
                     property.Value.ValueKind == JsonValueKind.Array) {
                return SearchField(property.Value, fieldName);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array) {
            foreach (var arrayElement in element.EnumerateArray()) {
                var result = SearchField(arrayElement, fieldName);
                if (result != null) {
                    return result?.ToString();
                }
            }
        }

        return null;
    }

    // 从计算公式中提取变量名称
    private static string[] ExtractVariableNames(string formula) {
        var variables = new HashSet<string>();
        foreach (var token in formula.Split(' ')) {
            if (token.Length > 0 && char.IsLetter(token[0])) {
                variables.Add(token);
            }
        }
        return variables.ToArray();
    }

    public static bool ValidateWeight(double weight) {
        string formula = "It > 30 && It < 50 && It != 33";
        try {
            // 解析并计算表达式
            var expression = DynamicExpressionParser
                .ParseLambda(new[] { Expression.Parameter(typeof(double), "it") }, typeof(bool), formula);
            // 编译并执行表达式
            return (bool)(expression.Compile().DynamicInvoke(weight) ?? false);
        }
        catch (Exception e) {
            return false;
        }
    }

    public static bool ValidateSorting(double length, double width, double height, double volume) {
        string formula = "Length > 10 and Width < 20 and Width > 10 and Height > 8 and Height < 50 and Volume > 105";
        try {
            // 解析并计算表达式
            ParameterExpression[] parameters = {
                Expression.Parameter(typeof(double), "Length"),
                Expression.Parameter(typeof(double), "Width"),
                Expression.Parameter(typeof(double), "Height"),
                Expression.Parameter(typeof(double), "Volume")
            };
            LambdaExpression expression = DynamicExpressionParser.ParseLambda(parameters, typeof(bool), formula);

            // 编译并执行表达式
            return (bool)(expression.Compile().DynamicInvoke(length, width, height, volume) ?? false);
        }
        catch (Exception e) {
            return false;
        }
    }
}

public class ExpressionEvaluator {
    private readonly Func<Dictionary<string, object>, object> _evaluator;

    public ExpressionEvaluator(LambdaExpression expression) {
        _evaluator = expression.Compile() as Func<Dictionary<string, object>, object>;
    }

    public T Evaluate<T>(Dictionary<string, object> arguments) {
        var result = _evaluator.Invoke(arguments.ToDictionary(kv => kv.Key, kv => (object)Convert.ChangeType(kv.Value, typeof(float))));
        return (T)Convert.ChangeType(result, typeof(T));
    }
}

public static class DynamicExpression {

    public static ExpressionEvaluator CompileLambda(string expression, params string[] parameterNames) {
        var parameterExpressions = parameterNames.Select(name => Expression.Parameter(typeof(float), name)).ToArray();
        var lambdaBody = DynamicExpressionParser.ParseLambda(parameterExpressions, null, expression).Body;
        var lambdaExpression = Expression.Lambda(lambdaBody, parameterExpressions);
        return new ExpressionEvaluator(lambdaExpression);
    }
}