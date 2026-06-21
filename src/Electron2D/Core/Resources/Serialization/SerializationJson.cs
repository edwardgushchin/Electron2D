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
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class SerializationJson
{
    public static void ValidateFormatAndVersion(JsonObject root, string expectedFormat, int expectedVersion, string description)
    {
        var format = ReadString(root, "format", $"{description} format");
        if (format != expectedFormat)
        {
            throw new FormatException($"{description} format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "version", $"{description} version");
        if (version != expectedVersion)
        {
            throw new FormatException($"{description} version '{version}' is not supported.");
        }
    }

    public static JsonArray WriteExternalReferences(IEnumerable<ResourceFileExternalReference> references)
    {
        var result = new JsonArray();
        foreach (var reference in references.OrderBy(reference => reference.Id))
        {
            result.Add(new JsonObject
            {
                ["id"] = reference.Id,
                ["uid"] = reference.UidText,
                ["path"] = reference.Path,
                ["type"] = reference.Type
            });
        }

        return result;
    }

    public static IReadOnlyList<ResourceFileExternalReference> ReadExternalReferences(JsonArray references)
    {
        var result = new List<ResourceFileExternalReference>();
        foreach (var node in references)
        {
            var reference = ExpectObject(node, "External resource reference");
            var uidText = ReadString(reference, "uid", "External resource reference uid");
            var uid = ResourceUid.TextToId(uidText);
            if (uid == ResourceUid.InvalidId)
            {
                throw new FormatException($"External resource reference uid '{uidText}' is invalid.");
            }

            result.Add(new ResourceFileExternalReference(
                ReadInt32(reference, "id", "External resource reference id"),
                uid,
                ReadString(reference, "path", "External resource reference path"),
                ReadString(reference, "type", "External resource reference type")));
        }

        return result.OrderBy(reference => reference.Id).ToArray();
    }

    public static JsonArray ReadArray(JsonObject value, string propertyName, string description)
    {
        return ReadRequiredProperty(value, propertyName, description) as JsonArray ??
            throw new FormatException($"{description} must be a JSON array.");
    }

    public static JsonNode ReadRequiredProperty(JsonObject value, string propertyName, string description)
    {
        return value.TryGetPropertyValue(propertyName, out var node)
            ? ReadRequiredNode(node, description)
            : throw new FormatException($"{description} is missing.");
    }

    public static string ReadString(JsonObject value, string propertyName, string description)
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

    public static string ReadOptionalString(JsonObject value, string propertyName)
    {
        if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return string.Empty;
        }

        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var result))
        {
            throw new FormatException($"{propertyName} must be a JSON string.");
        }

        return result ?? string.Empty;
    }

    public static int ReadInt32(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }

    public static int? ReadNullableInt32(JsonObject value, string propertyName, string description)
    {
        if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return null;
        }

        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
        {
            throw new FormatException($"{description} must be null or a JSON integer.");
        }

        return result;
    }

    private static JsonNode ReadRequiredNode(JsonNode? node, string description)
    {
        return node ?? throw new FormatException($"{description} is missing.");
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }
}
