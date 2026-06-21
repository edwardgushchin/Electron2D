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

internal static class ResourceImportManifestTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(ResourceImportManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return WriteManifest(manifest).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static ResourceImportManifest Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadManifest(ExpectObject(JsonNode.Parse(text), "Import cache manifest"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Import cache manifest JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Import cache manifest JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Import cache manifest JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteManifest(ResourceImportManifest manifest)
    {
        var entries = new JsonArray();
        foreach (var entry in manifest.Entries.OrderBy(entry => entry.SourcePath, StringComparer.Ordinal))
        {
            entries.Add((JsonNode)WriteEntry(entry));
        }

        return new JsonObject
        {
            ["format"] = ResourceImportManifest.FormatName,
            ["version"] = ResourceImportManifest.CurrentVersion,
            ["entries"] = entries
        };
    }

    private static JsonObject WriteEntry(ResourceImportManifestEntry entry)
    {
        return new JsonObject
        {
            ["source"] = entry.SourcePath,
            ["uid"] = entry.UidText,
            ["type"] = entry.Type,
            ["importer"] = entry.Importer,
            ["sourceHash"] = entry.SourceHash,
            ["cacheFiles"] = WriteStringArray(entry.CacheFiles),
            ["dependencies"] = WriteDependencies(entry.Dependencies)
        };
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var result = new JsonArray();
        foreach (var value in values.OrderBy(value => value, StringComparer.Ordinal))
        {
            result.Add((JsonNode?)JsonValue.Create(value));
        }

        return result;
    }

    private static JsonArray WriteDependencies(IEnumerable<ResourceImportManifestDependency> dependencies)
    {
        var result = new JsonArray();
        foreach (var dependency in dependencies.OrderBy(dependency => dependency.Path, StringComparer.Ordinal))
        {
            result.Add((JsonNode)new JsonObject
            {
                ["path"] = dependency.Path,
                ["hash"] = dependency.ContentHash
            });
        }

        return result;
    }

    private static ResourceImportManifest ReadManifest(JsonObject root)
    {
        var format = ReadString(root, "format", "Import cache manifest format");
        if (format != ResourceImportManifest.FormatName)
        {
            throw new FormatException($"Import cache manifest format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Import cache manifest version");
        if (version != ResourceImportManifest.CurrentVersion)
        {
            throw new FormatException($"Import cache manifest version '{version}' is not supported.");
        }

        var entries = new List<ResourceImportManifestEntry>();
        foreach (var node in ReadArray(root, "entries", "Import cache manifest entries"))
        {
            var entry = ExpectObject(node, "Import cache manifest entry");
            var uidText = ReadString(entry, "uid", "Import cache manifest entry uid");
            var uid = ResourceUid.TextToId(uidText);
            if (uid == ResourceUid.InvalidId)
            {
                throw new FormatException($"Import cache manifest entry uid '{uidText}' is invalid.");
            }

            entries.Add(new ResourceImportManifestEntry(
                ReadString(entry, "source", "Import cache manifest entry source"),
                uid,
                ReadString(entry, "type", "Import cache manifest entry type"),
                ReadString(entry, "importer", "Import cache manifest entry importer"),
                ReadString(entry, "sourceHash", "Import cache manifest entry source hash"),
                ReadStringArray(ReadArray(entry, "cacheFiles", "Import cache manifest entry cache files")),
                ReadDependencies(ReadArray(entry, "dependencies", "Import cache manifest entry dependencies"))));
        }

        return new ResourceImportManifest(entries);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonArray values)
    {
        var result = new List<string>();
        foreach (var node in values)
        {
            result.Add(ReadString(ReadRequiredNode(node, "String array item"), "String array item"));
        }

        return result;
    }

    private static IReadOnlyList<ResourceImportManifestDependency> ReadDependencies(JsonArray dependencies)
    {
        var result = new List<ResourceImportManifestDependency>();
        foreach (var node in dependencies)
        {
            var dependency = ExpectObject(node, "Import cache manifest dependency");
            result.Add(new ResourceImportManifestDependency(
                ReadString(dependency, "path", "Import cache manifest dependency path"),
                ReadString(dependency, "hash", "Import cache manifest dependency hash")));
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
}
