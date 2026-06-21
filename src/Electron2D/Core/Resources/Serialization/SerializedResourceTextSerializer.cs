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

internal static class SerializedResourceTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(SerializedResourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return WriteDocument(document).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static SerializedResourceDocument Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadDocument(ExpectObject(JsonNode.Parse(text), "Serialized resource"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Serialized resource JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Serialized resource JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Serialized resource JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteDocument(SerializedResourceDocument document)
    {
        return new JsonObject
        {
            ["format"] = SerializedResourceDocument.FormatName,
            ["version"] = SerializedResourceDocument.CurrentVersion,
            ["uid"] = document.UidText,
            ["type"] = document.Type,
            ["path"] = document.Path,
            ["external"] = SerializationJson.WriteExternalReferences(document.ExternalReferences),
            ["internal"] = WriteInternalResources(document.InternalResources),
            ["properties"] = SerializedPropertyValueTextSerializer.WriteProperties(document.Properties)
        };
    }

    private static SerializedResourceDocument ReadDocument(JsonObject root)
    {
        SerializationJson.ValidateFormatAndVersion(
            root,
            SerializedResourceDocument.FormatName,
            SerializedResourceDocument.CurrentVersion,
            "Serialized resource");

        var uidText = SerializationJson.ReadString(root, "uid", "Serialized resource uid");
        var uid = ResourceUid.TextToId(uidText);
        if (uid == ResourceUid.InvalidId)
        {
            throw new FormatException($"Serialized resource uid '{uidText}' is invalid.");
        }

        return new SerializedResourceDocument(
            uid,
            SerializationJson.ReadString(root, "type", "Serialized resource type"),
            SerializationJson.ReadString(root, "path", "Serialized resource path"),
            SerializationJson.ReadExternalReferences(SerializationJson.ReadArray(root, "external", "Serialized resource external references")),
            ReadInternalResources(SerializationJson.ReadArray(root, "internal", "Serialized resource internal resources")),
            SerializedPropertyValueTextSerializer.ReadProperties(ExpectObject(
                SerializationJson.ReadRequiredProperty(root, "properties", "Serialized resource properties"),
                "Serialized resource properties")));
    }

    internal static JsonArray WriteInternalResources(IEnumerable<SerializedResourceEntry> resources)
    {
        var result = new JsonArray();
        foreach (var resource in resources.OrderBy(resource => resource.Id))
        {
            result.Add(new JsonObject
            {
                ["id"] = resource.Id,
                ["type"] = resource.Type,
                ["properties"] = SerializedPropertyValueTextSerializer.WriteProperties(resource.Properties)
            });
        }

        return result;
    }

    internal static IReadOnlyList<SerializedResourceEntry> ReadInternalResources(JsonArray resources)
    {
        var result = new List<SerializedResourceEntry>();
        foreach (var node in resources)
        {
            var resource = ExpectObject(node, "Serialized internal resource");
            result.Add(new SerializedResourceEntry(
                SerializationJson.ReadInt32(resource, "id", "Serialized internal resource id"),
                SerializationJson.ReadString(resource, "type", "Serialized internal resource type"),
                SerializedPropertyValueTextSerializer.ReadProperties(ExpectObject(
                    SerializationJson.ReadRequiredProperty(resource, "properties", "Serialized internal resource properties"),
                    "Serialized internal resource properties"))));
        }

        return result.OrderBy(resource => resource.Id).ToArray();
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }
}
