using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electron2D;

internal static class ResourceJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        opts.Converters.Add(new Vector2JsonConverter());
        return opts;
    }

    public static bool TryParseFilterMode(string? value, out FilterMode mode)
    {
        mode = FilterMode.Inherit;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Нормализуем: lower + убираем пробелы/дефисы/подчёркивания.
        var s = value.Trim().ToLowerInvariant();
        s = s.Replace(" ", string.Empty)
             .Replace("-", string.Empty)
             .Replace("_", string.Empty);

        mode = s switch
        {
            "inherit" or "default" => FilterMode.Inherit,
            "nearest" or "point" => FilterMode.Nearest,
            "linear" or "bilinear" => FilterMode.Linear,
            "pixelart" or "pixel" => FilterMode.Pixelart,
            _ => FilterMode.Inherit
        };

        return s is "inherit" or "default" or "nearest" or "point" or "linear" or "bilinear" or "pixelart" or "pixel";
    }

    private sealed class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                var x = ReadFloat(ref reader);
                reader.Read();
                var y = ReadFloat(ref reader);

                reader.Read();
                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException("Vector2 array must have exactly 2 elements.");

                return new Vector2(x, y);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float x = 0f, y = 0f;
                var hasX = false;
                var hasY = false;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException("Vector2 object must contain properties.");

                    var prop = reader.GetString();
                    reader.Read();

                    if (prop is null)
                    {
                        reader.Skip();
                        continue;
                    }

                    if (prop.Equals("x", StringComparison.OrdinalIgnoreCase))
                    {
                        x = ReadFloat(ref reader);
                        hasX = true;
                    }
                    else if (prop.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        y = ReadFloat(ref reader);
                        hasY = true;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                if (!hasX || !hasY)
                    throw new JsonException("Vector2 object must contain both 'x' and 'y'.");

                return new Vector2(x, y);
            }

            throw new JsonException($"Unsupported token for Vector2: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }

        private static float ReadFloat(ref Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetSingle(),
                JsonTokenType.String => float.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                    ? v
                    : throw new JsonException("Invalid float string."),
                _ => throw new JsonException("Expected a number.")
            };
        }
    }
}
