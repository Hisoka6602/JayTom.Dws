using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using JayTom.Dws.Plugin.Speech;
using System.Text.RegularExpressions;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

internal class Program {

    private static void Main(string[] args) {
        try {
            var s = "{\"code\":1,\"msg\":\"请求成功\",\"version\":null,\"data\":[{\"waybillNo\":\"JT5264019990398\",\"terminalDispatchCode\":\"432 R851-00 307\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"R851-00\",\"thirdlyDispatchCode\":\"307\",\"customerCode\":null,\"interceptor\":2,\"orderType\":2,\"pickNetworkCode\":\"\",\"destinationCode\":\"\",\"extendJson\":\"\",\"codeList\":null},{\"waybillNo\":\"JT5264019990398\",\"terminalDispatchCode\":\"432 R851-00 307\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"R851-00\",\"thirdlyDispatchCode\":\"307\",\"customerCode\":null,\"interceptor\":2,\"orderType\":2,\"pickNetworkCode\":\"\",\"destinationCode\":\"\",\"extendJson\":\"\",\"codeList\":null},{\"waybillNo\":\"JT5264019990398\",\"terminalDispatchCode\":\"902,H346-00,005\",\"firstDispatchCode\":\"902\",\"secondDispatchCode\":\"H346-00\",\"thirdlyDispatchCode\":\"005\",\"customerCode\":null,\"interceptor\":1,\"orderType\":1,\"pickNetworkCode\":\"4579114\",\"destinationCode\":\"510100\",\"extendJson\":\"{\\\"stationCode\\\":\\\"5O\\\"}\",\"codeList\":null}],\"succ\":true,\"fail\":false}";
            var resultContent = Regex.Unescape(s);

            var replace = Regex.Replace(s, @"[\u0000-\u001f\b]", "");
            var reader = new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(replace));
            var tryParseValue = JsonDocument.TryParseValue(ref reader, out var document);
            if (tryParseValue && document is not null) {
                var fieldValue = FindFieldValue(document.RootElement, "thirdlyDispatchCode");
                if (fieldValue.HasValue) {
                    var equals = fieldValue.Value.ToString()?.Equals("307");
                    Console.WriteLine(equals);
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
        Console.ReadLine();
    }

    private static JsonElement? FindFieldValue(JsonElement root, string fieldName) {
        try {
            var stack = new Stack<JsonElement>();
            stack.Push(root);

            while (stack.Count > 0) {
                var element = stack.Pop();

                switch (element.ValueKind) {
                    case JsonValueKind.Object when element.TryGetProperty(fieldName, out var field):
                        return field;

                    case JsonValueKind.Object: {
                            foreach (var property in element.EnumerateObject()) {
                                stack.Push(property.Value);
                            }

                            break;
                        }
                    case JsonValueKind.Array: {
                            foreach (var arrayElement in element.EnumerateArray()) {
                                stack.Push(arrayElement);
                            }

                            break;
                        }
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
        }

        return null;
    }
}