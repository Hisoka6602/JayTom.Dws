using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using JayTom.Dws.Plugin.Speech;
using System.Text.RegularExpressions;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

internal class Program {

    private static void Main(string[] args) {
        try {
            var s = "{\"result\":[{\"code\":0,\"command\":\"sorter.dest_request\",\"error\":\"\",\"params\":{\"bcrName\":\"sorter\",\"chuteCode\":\"5\",\"theoryWeight\":0,\"weight\":192,\"errorCode\":0,\"optionalChuteCodes\":\"\",\"barCode\":\"4PXTEST240227000006\"}}],\"requestId\":1710834606}";
            var resultContent = Regex.Unescape(s);

            var replace = Regex.Replace(s, @"[\u0000-\u001f\b]", "");
            var reader = new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(replace));
            var tryParseValue = JsonDocument.TryParseValue(ref reader, out var document);
            if (tryParseValue && document is not null) {
                var fieldValue = FindFieldValue(document.RootElement, "chuteCode");
                if (fieldValue.HasValue) {
                    var equals = fieldValue.Value.ToString()?.Equals("5");
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