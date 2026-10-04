using System.Globalization;
using JayTom.Dws.Abstractions.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>读取旧版坐标字符串和当前坐标对象，保留当前配置的对象写入格式。</summary>
internal sealed class Point2DJsonConverter : JsonConverter<Point2D>
{
    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override Point2D ReadJson(JsonReader reader, Type objectType,
        Point2D existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String && reader.Value is string text)
        {
            var coordinates = text.Split(',');
            if (coordinates.Length == 2 &&
                int.TryParse(coordinates[0], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var x) &&
                int.TryParse(coordinates[1], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var y))
            {
                return new Point2D(x, y);
            }
        }
        else if (reader.TokenType == JsonToken.StartObject)
        {
            var value = JObject.Load(reader);
            return new Point2D(ReadCoordinate(value, "X"), ReadCoordinate(value, "Y"));
        }

        throw new JsonSerializationException($"无效的二维整数坐标，配置路径:{reader.Path}");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Point2D value, JsonSerializer serializer) =>
        throw new NotSupportedException("坐标使用默认对象格式序列化。");

    private static int ReadCoordinate(JObject value, string name)
    {
        if (!value.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var coordinate))
        {
            return 0;
        }
        if (coordinate.Type == JTokenType.Integer &&
            int.TryParse(coordinate.ToString(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        throw new JsonSerializationException($"无效的二维整数坐标分量:{name}");
    }
}
