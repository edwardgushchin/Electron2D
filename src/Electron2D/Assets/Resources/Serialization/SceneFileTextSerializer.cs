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

internal static class SceneFileTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(SceneFileDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return WriteDocument(document).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static SceneFileDocument Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadDocument(ExpectObject(JsonNode.Parse(text), "Scene file"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Scene file JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Scene file JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Scene file JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteDocument(SceneFileDocument document)
    {
        return new JsonObject
        {
            ["format"] = SceneFileDocument.FormatName,
            ["version"] = SceneFileDocument.CurrentVersion,
            ["external"] = SerializationJson.WriteExternalReferences(document.ExternalReferences),
            ["internal"] = SerializedResourceTextSerializer.WriteInternalResources(document.InternalResources),
            ["nodes"] = WriteNodes(document.Nodes)
        };
    }

    private static SceneFileDocument ReadDocument(JsonObject root)
    {
        SerializationJson.ValidateFormatAndVersion(
            root,
            SceneFileDocument.FormatName,
            SceneFileDocument.CurrentVersion,
            "Scene file");

        return new SceneFileDocument(
            SerializationJson.ReadExternalReferences(SerializationJson.ReadArray(root, "external", "Scene file external references")),
            SerializedResourceTextSerializer.ReadInternalResources(SerializationJson.ReadArray(root, "internal", "Scene file internal resources")),
            ReadNodes(SerializationJson.ReadArray(root, "nodes", "Scene file nodes")));
    }

    private static JsonArray WriteNodes(IEnumerable<SceneFileNode> nodes)
    {
        var result = new JsonArray();
        foreach (var node in nodes.OrderBy(node => node.Id))
        {
            result.Add((JsonNode)new JsonObject
            {
                ["id"] = node.Id,
                ["type"] = node.Type,
                ["name"] = node.Name,
                ["parent"] = node.ParentId,
                ["owner"] = node.OwnerId,
                ["groups"] = WriteGroups(node.PersistentGroups),
                ["properties"] = SerializedPropertyValueTextSerializer.WriteProperties(node.Properties)
            });
        }

        return result;
    }

    private static IReadOnlyList<SceneFileNode> ReadNodes(JsonArray nodes)
    {
        var result = new List<SceneFileNode>();
        foreach (var nodeValue in nodes)
        {
            var node = ExpectObject(nodeValue, "Scene node");
            result.Add(new SceneFileNode(
                SerializationJson.ReadInt32(node, "id", "Scene node id"),
                SerializationJson.ReadString(node, "type", "Scene node type"),
                SerializationJson.ReadOptionalString(node, "name"),
                SerializationJson.ReadNullableInt32(node, "parent", "Scene node parent"),
                SerializationJson.ReadNullableInt32(node, "owner", "Scene node owner"),
                ReadGroups(SerializationJson.ReadArray(node, "groups", "Scene node persistent groups")),
                SerializedPropertyValueTextSerializer.ReadProperties(ExpectObject(
                    SerializationJson.ReadRequiredProperty(node, "properties", "Scene node properties"),
                    "Scene node properties"))));
        }

        return result.OrderBy(node => node.Id).ToArray();
    }

    private static JsonArray WriteGroups(IEnumerable<string> groups)
    {
        var result = new JsonArray();
        foreach (var group in groups.OrderBy(group => group, StringComparer.Ordinal))
        {
            result.Add((JsonNode?)JsonValue.Create(group));
        }

        return result;
    }

    private static IReadOnlyList<string> ReadGroups(JsonArray groups)
    {
        var result = new List<string>();
        foreach (var group in groups)
        {
            if (group is not JsonValue jsonValue ||
                !jsonValue.TryGetValue<string>(out var text) ||
                string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Scene node persistent group must be a non-empty JSON string.");
            }

            result.Add(text);
        }

        return result;
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }
}
