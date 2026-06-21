/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class SerializedPropertyValueTextSerializer
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false
    };

    public static JsonObject Write(SerializedPropertyValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Kind switch
        {
            SerializedPropertyValueKind.Variant => new JsonObject
            {
                ["kind"] = "Variant",
                ["value"] = JsonNode.Parse(VariantTextSerializer.Serialize(value.VariantValue))
            },
            SerializedPropertyValueKind.Enum => new JsonObject
            {
                ["kind"] = "Enum",
                ["type"] = value.EnumType,
                ["name"] = value.EnumName,
                ["value"] = value.EnumValue
            },
            SerializedPropertyValueKind.Nullable => new JsonObject
            {
                ["kind"] = "Nullable",
                ["type"] = value.NullableType,
                ["value"] = value.NullableValue is null ? null : Write(value.NullableValue)
            },
            SerializedPropertyValueKind.Resource => new JsonObject
            {
                ["kind"] = "Resource",
                ["scope"] = value.ReferenceScope.ToString(),
                ["id"] = value.ReferenceId
            },
            SerializedPropertyValueKind.Array => new JsonObject
            {
                ["kind"] = "Array",
                ["items"] = WriteArray(value.Items)
            },
            SerializedPropertyValueKind.Dictionary => new JsonObject
            {
                ["kind"] = "Dictionary",
                ["entries"] = WriteDictionary(value.DictionaryEntries)
            },
            _ => throw new InvalidOperationException($"Serialized property kind '{value.Kind}' is not supported.")
        };
    }

    public static SerializedPropertyValue Read(JsonNode? node)
    {
        var value = ExpectObject(node, "Serialized property value");
        var kind = ReadString(value, "kind", "Serialized property kind");
        return kind switch
        {
            "Variant" => SerializedPropertyValue.FromVariant(VariantTextSerializer.Deserialize(
                ReadRequiredProperty(value, "value", "Serialized property Variant value").ToJsonString(CompactOptions))),
            "Enum" => ReadEnum(value),
            "Nullable" => SerializedPropertyValue.FromNullable(
                ReadString(value, "type", "Serialized nullable type"),
                ReadOptionalNullableValue(value)),
            "Resource" => ReadResource(value),
            "Array" => SerializedPropertyValue.FromArray(ReadArray(
                ReadJsonArray(value, "items", "Serialized property array items"))),
            "Dictionary" => SerializedPropertyValue.FromDictionary(ReadDictionary(
                ReadJsonArray(value, "entries", "Serialized property dictionary entries"))),
            _ => throw new FormatException($"Serialized property kind '{kind}' is not supported.")
        };
    }

    public static JsonObject WriteProperties(IReadOnlyDictionary<string, SerializedPropertyValue> properties)
    {
        var result = new JsonObject();
        foreach (var (key, value) in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            result[key] = Write(value);
        }

        return result;
    }

    public static IReadOnlyDictionary<string, SerializedPropertyValue> ReadProperties(JsonObject properties)
    {
        var result = new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            result.Add(property.Key, Read(ReadRequiredNode(property.Value, $"Serialized property '{property.Key}'")));
        }

        return result;
    }

    private static JsonArray WriteArray(IEnumerable<SerializedPropertyValue> items)
    {
        var result = new JsonArray();
        foreach (var item in items)
        {
            result.Add(Write(item));
        }

        return result;
    }

    private static JsonArray WriteDictionary(IEnumerable<SerializedPropertyDictionaryEntry> entries)
    {
        var result = new JsonArray();
        foreach (var entry in entries.OrderBy(entry => Write(entry.Key).ToJsonString(CompactOptions), StringComparer.Ordinal))
        {
            result.Add(new JsonObject
            {
                ["key"] = Write(entry.Key),
                ["value"] = Write(entry.Value)
            });
        }

        return result;
    }

    private static SerializedPropertyValue ReadEnum(JsonObject value)
    {
        return SerializedPropertyValue.FromEnum(
            ReadString(value, "type", "Serialized enum type"),
            ReadString(value, "name", "Serialized enum name"),
            ReadInt64(value, "value", "Serialized enum value"));
    }

    private static SerializedPropertyValue ReadResource(JsonObject value)
    {
        var scopeName = ReadString(value, "scope", "Serialized resource reference scope");
        if (!Enum.TryParse<SerializedResourceReferenceScope>(scopeName, ignoreCase: false, out var scope))
        {
            throw new FormatException($"Serialized resource reference scope '{scopeName}' is not supported.");
        }

        var id = ReadInt32(value, "id", "Serialized resource reference id");
        return scope == SerializedResourceReferenceScope.External
            ? SerializedPropertyValue.ExternalResource(id)
            : SerializedPropertyValue.InternalResource(id);
    }

    private static IReadOnlyList<SerializedPropertyValue> ReadArray(JsonArray items)
    {
        return items.Select(item => Read(item)).ToArray();
    }

    private static IReadOnlyList<SerializedPropertyDictionaryEntry> ReadDictionary(JsonArray entries)
    {
        var result = new List<SerializedPropertyDictionaryEntry>();
        foreach (var node in entries)
        {
            var entry = ExpectObject(node, "Serialized dictionary entry");
            result.Add(new SerializedPropertyDictionaryEntry(
                Read(ReadRequiredProperty(entry, "key", "Serialized dictionary entry key")),
                Read(ReadRequiredProperty(entry, "value", "Serialized dictionary entry value"))));
        }

        return result;
    }

    private static SerializedPropertyValue? ReadOptionalNullableValue(JsonObject value)
    {
        return value.TryGetPropertyValue("value", out var node)
            ? node is null ? null : Read(node)
            : throw new FormatException("Serialized nullable value is missing.");
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static JsonArray ReadJsonArray(JsonObject value, string propertyName, string description)
    {
        return ReadRequiredProperty(value, propertyName, description) as JsonArray ??
            throw new FormatException($"{description} must be a JSON array.");
    }

    private static JsonNode ReadRequiredProperty(JsonObject value, string propertyName, string description)
    {
        return value.TryGetPropertyValue(propertyName, out var node)
            ? ReadRequiredNode(node, description)
            : throw new FormatException($"{description} is missing.");
    }

    private static JsonNode ReadRequiredNode(JsonNode? node, string description)
    {
        return node ?? throw new FormatException($"{description} is missing.");
    }

    private static string ReadString(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var result) ||
            string.IsNullOrWhiteSpace(result))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return result;
    }

    private static int ReadInt32(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }

    private static long ReadInt64(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<long>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }
}
