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

internal static class ResourceFileTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false
    };

    public static string Serialize(ResourceFileDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return WriteDocument(document).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static ResourceFileDocument Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadDocument(ExpectObject(JsonNode.Parse(text), "Resource file"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Resource file JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Resource file JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Resource file JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteDocument(ResourceFileDocument document)
    {
        return new JsonObject
        {
            ["format"] = ResourceFileDocument.FormatName,
            ["version"] = ResourceFileDocument.CurrentVersion,
            ["uid"] = document.UidText,
            ["type"] = document.Type,
            ["path"] = document.Path,
            ["external"] = WriteExternalReferences(document.ExternalReferences),
            ["internal"] = WriteInternalResources(document.InternalResources),
            ["properties"] = WriteProperties(document.Properties)
        };
    }

    private static ResourceFileDocument ReadDocument(JsonObject root)
    {
        var format = ReadString(root, "format", "Resource file format");
        if (format != ResourceFileDocument.FormatName)
        {
            throw new FormatException($"Resource file format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Resource file version");
        if (version != ResourceFileDocument.CurrentVersion)
        {
            throw new FormatException($"Resource file version '{version}' is not supported.");
        }

        var uidText = ReadString(root, "uid", "Resource file uid");
        var uid = ResourceUid.TextToId(uidText);
        if (uid == ResourceUid.InvalidId)
        {
            throw new FormatException($"Resource file uid '{uidText}' is invalid.");
        }

        return new ResourceFileDocument(
            uid,
            ReadString(root, "type", "Resource file type"),
            ReadString(root, "path", "Resource file path"),
            ReadExternalReferences(ReadArray(root, "external", "Resource file external references")),
            ReadInternalResources(ReadArray(root, "internal", "Resource file internal resources")),
            ReadProperties(ExpectObject(ReadRequiredProperty(root, "properties", "Resource file properties"), "Resource file properties")));
    }

    private static JsonArray WriteExternalReferences(IEnumerable<ResourceFileExternalReference> references)
    {
        var result = new JsonArray();
        foreach (var reference in references.OrderBy(reference => reference.Id))
        {
            ValidateReferenceId(reference.Id, "External reference id");
            if (reference.Uid <= 0 || reference.Uid == ResourceUid.InvalidId)
            {
                throw new InvalidOperationException($"External reference '{reference.Id}' has an invalid UID.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(reference.Path);
            ArgumentException.ThrowIfNullOrWhiteSpace(reference.Type);

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

    private static JsonArray WriteInternalResources(IEnumerable<ResourceFileInternalResource> resources)
    {
        var result = new JsonArray();
        foreach (var resource in resources.OrderBy(resource => resource.Id))
        {
            ValidateReferenceId(resource.Id, "Internal resource id");
            ArgumentException.ThrowIfNullOrWhiteSpace(resource.Type);

            result.Add(new JsonObject
            {
                ["id"] = resource.Id,
                ["type"] = resource.Type,
                ["properties"] = WriteProperties(resource.Properties)
            });
        }

        return result;
    }

    private static JsonObject WriteProperties(IReadOnlyDictionary<string, Variant> properties)
    {
        var result = new JsonObject();
        foreach (var (key, value) in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            result[key] = JsonNode.Parse(VariantTextSerializer.Serialize(value));
        }

        return result;
    }

    private static IReadOnlyList<ResourceFileExternalReference> ReadExternalReferences(JsonArray references)
    {
        var result = new List<ResourceFileExternalReference>();
        foreach (var node in references)
        {
            var reference = ExpectObject(node, "External reference");
            var id = ReadInt32(reference, "id", "External reference id");
            ValidateReferenceId(id, "External reference id");

            var uidText = ReadString(reference, "uid", "External reference uid");
            var uid = ResourceUid.TextToId(uidText);
            if (uid == ResourceUid.InvalidId)
            {
                throw new FormatException($"External reference uid '{uidText}' is invalid.");
            }

            result.Add(new ResourceFileExternalReference(
                id,
                uid,
                ReadString(reference, "path", "External reference path"),
                ReadString(reference, "type", "External reference type")));
        }

        return result.OrderBy(reference => reference.Id).ToArray();
    }

    private static IReadOnlyList<ResourceFileInternalResource> ReadInternalResources(JsonArray resources)
    {
        var result = new List<ResourceFileInternalResource>();
        foreach (var node in resources)
        {
            var resource = ExpectObject(node, "Internal resource");
            var id = ReadInt32(resource, "id", "Internal resource id");
            ValidateReferenceId(id, "Internal resource id");

            result.Add(new ResourceFileInternalResource(
                id,
                ReadString(resource, "type", "Internal resource type"),
                ReadProperties(ExpectObject(
                    ReadRequiredProperty(resource, "properties", "Internal resource properties"),
                    "Internal resource properties"))));
        }

        return result.OrderBy(resource => resource.Id).ToArray();
    }

    private static IReadOnlyDictionary<string, Variant> ReadProperties(JsonObject properties)
    {
        var result = new Dictionary<string, Variant>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            result.Add(property.Key, VariantTextSerializer.Deserialize(
                ReadRequiredNode(property.Value, $"Resource property '{property.Key}'").ToJsonString(CompactOptions)));
        }

        return result;
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static JsonArray ReadArray(JsonObject value, string propertyName, string description)
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
        return ReadString(ReadRequiredProperty(value, propertyName, description), description);
    }

    private static string ReadString(JsonNode node, string description)
    {
        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return value;
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

    private static void ValidateReferenceId(int id, string description)
    {
        if (id <= 0)
        {
            throw new FormatException($"{description} must be positive.");
        }
    }
}
