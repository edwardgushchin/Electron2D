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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum ProjectDocumentKind
{
    Scene,
    Resource,
    Settings,
    Code,
    Json,
    Text,
    Generated,
    BinaryAsset,
    EditorMetadata
}

internal enum ProjectDocumentContentKind
{
    Json,
    CSharp,
    Text,
    Binary
}

internal enum ProjectDocumentChangeKind
{
    PropertyChanged,
    Renamed,
    Moved,
    Deleted,
    Added
}

internal sealed class ProjectDocumentClassification
{
    public ProjectDocumentClassification(
        ProjectDocumentKind kind,
        ProjectDocumentContentKind contentKind,
        bool isGenerated = false,
        bool isBinary = false)
    {
        Kind = kind;
        ContentKind = contentKind;
        IsGenerated = isGenerated;
        IsBinary = isBinary;
    }

    public ProjectDocumentKind Kind { get; }

    public ProjectDocumentContentKind ContentKind { get; }

    public bool IsGenerated { get; }

    public bool IsBinary { get; }
}

internal static class ProjectDocumentClassifier
{
    public static ProjectDocumentClassification Classify(string relativePath, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var contentKind = DetermineTextContentKind(normalizedPath);
        if (IsGeneratedPath(normalizedPath))
        {
            return new ProjectDocumentClassification(ProjectDocumentKind.Generated, contentKind, isGenerated: true);
        }

        if (IsEditorMetadataPath(normalizedPath))
        {
            return new ProjectDocumentClassification(ProjectDocumentKind.EditorMetadata, contentKind);
        }

        var format = contentKind == ProjectDocumentContentKind.Json ? TryReadFormat(text) : null;
        if (format == "Electron2D.SceneFile" || normalizedPath.EndsWith(".scene.json", StringComparison.Ordinal))
        {
            return new ProjectDocumentClassification(ProjectDocumentKind.Scene, ProjectDocumentContentKind.Json);
        }

        if (format == "Electron2D.ResourceFile" || normalizedPath.EndsWith(".e2res", StringComparison.Ordinal))
        {
            return new ProjectDocumentClassification(ProjectDocumentKind.Resource, ProjectDocumentContentKind.Json);
        }

        if (IsSettingsPath(normalizedPath, format))
        {
            return new ProjectDocumentClassification(ProjectDocumentKind.Settings, ProjectDocumentContentKind.Json);
        }

        return contentKind switch
        {
            ProjectDocumentContentKind.CSharp => new ProjectDocumentClassification(ProjectDocumentKind.Code, contentKind),
            ProjectDocumentContentKind.Json => new ProjectDocumentClassification(ProjectDocumentKind.Json, contentKind),
            _ => new ProjectDocumentClassification(ProjectDocumentKind.Text, ProjectDocumentContentKind.Text)
        };
    }

    public static ProjectDocumentClassification ClassifyBinary(string relativePath, IReadOnlyList<byte> content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        if (IsGeneratedPath(normalizedPath))
        {
            return new ProjectDocumentClassification(
                ProjectDocumentKind.Generated,
                ProjectDocumentContentKind.Binary,
                isGenerated: true,
                isBinary: true);
        }

        return new ProjectDocumentClassification(
            ProjectDocumentKind.BinaryAsset,
            ProjectDocumentContentKind.Binary,
            isBinary: true);
    }

    private static ProjectDocumentContentKind DetermineTextContentKind(string normalizedPath)
    {
        if (normalizedPath.EndsWith(".cs", StringComparison.Ordinal))
        {
            return ProjectDocumentContentKind.CSharp;
        }

        if (normalizedPath.EndsWith(".json", StringComparison.Ordinal) ||
            normalizedPath.EndsWith(".e2res", StringComparison.Ordinal) ||
            normalizedPath.EndsWith(".e2task", StringComparison.Ordinal) ||
            normalizedPath.EndsWith(".e2tasks", StringComparison.Ordinal))
        {
            return ProjectDocumentContentKind.Json;
        }

        return ProjectDocumentContentKind.Text;
    }

    private static bool IsGeneratedPath(string normalizedPath)
    {
        return normalizedPath.StartsWith(".electron2d/import-cache/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/workspaces/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/context/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/session/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("bin/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("obj/", StringComparison.Ordinal) ||
            normalizedPath.Contains("/generated/", StringComparison.Ordinal);
    }

    private static bool IsEditorMetadataPath(string normalizedPath)
    {
        return normalizedPath.StartsWith(".electron2d/tasks/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/user/", StringComparison.Ordinal);
    }

    private static bool IsSettingsPath(string normalizedPath, string? format)
    {
        if (format is not null && format.Contains("Settings", StringComparison.Ordinal))
        {
            return true;
        }

        return normalizedPath.Equals("project.e2project.json", StringComparison.Ordinal) ||
            normalizedPath.Equals("project-settings.json", StringComparison.Ordinal) ||
            normalizedPath.Equals("project_settings.json", StringComparison.Ordinal) ||
            normalizedPath.Equals("input-map.json", StringComparison.Ordinal) ||
            normalizedPath.Equals("input_map.json", StringComparison.Ordinal) ||
            normalizedPath.Equals("export_presets.json", StringComparison.Ordinal);
    }

    private static string? TryReadFormat(string text)
    {
        try
        {
            return JsonNode.Parse(text) is JsonObject root &&
                root.TryGetPropertyValue("format", out var formatNode) &&
                formatNode is JsonValue formatValue &&
                formatValue.TryGetValue<string>(out var format)
                    ? format
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal readonly record struct ProjectDocumentRevision
{
    public ProjectDocumentRevision(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Project document revision must be non-negative.");
        }

        Value = value;
    }

    public long Value { get; }

    public ProjectDocumentRevision Next()
    {
        return new ProjectDocumentRevision(checked(Value + 1));
    }
}

internal readonly record struct ProjectDocumentRevisionState
{
    public ProjectDocumentRevisionState(ProjectDocumentRevision persistedRevision, ProjectDocumentRevision inMemoryRevision)
    {
        PersistedRevision = persistedRevision;
        InMemoryRevision = inMemoryRevision;
    }

    public ProjectDocumentRevision PersistedRevision { get; }

    public ProjectDocumentRevision InMemoryRevision { get; }

    public static ProjectDocumentRevisionState Clean(long revision)
    {
        var documentRevision = new ProjectDocumentRevision(revision);
        return new ProjectDocumentRevisionState(documentRevision, documentRevision);
    }
}

internal sealed class ProjectDocumentIdentity
{
    public ProjectDocumentIdentity(string documentId, string normalizedPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedPath);

        DocumentId = documentId;
        NormalizedPath = normalizedPath;
    }

    public string DocumentId { get; }

    public string NormalizedPath { get; }
}

internal readonly record struct ProjectDocumentObjectUid
{
    public ProjectDocumentObjectUid(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}

internal sealed class ProjectDocumentObject
{
    public ProjectDocumentObject(
        ProjectDocumentObjectUid uid,
        ProjectDocumentObjectUid? parentObjectUid,
        string type,
        string name,
        int order,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Uid = uid;
        ParentObjectUid = parentObjectUid;
        Type = type;
        Name = name ?? string.Empty;
        Order = order;
        Properties = CopyProperties(properties);
    }

    public ProjectDocumentObjectUid Uid { get; }

    public ProjectDocumentObjectUid? ParentObjectUid { get; }

    public string Type { get; }

    public string Name { get; }

    public int Order { get; }

    public IReadOnlyDictionary<string, string> Properties { get; }

    private static IReadOnlyDictionary<string, string> CopyProperties(IReadOnlyDictionary<string, string>? properties)
    {
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        if (properties is not null)
        {
            foreach (var (key, value) in properties)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                copy.Add(key, value);
            }
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}

internal sealed class ProjectDocumentSnapshot
{
    public ProjectDocumentSnapshot(
        ProjectDocumentIdentity identity,
        ProjectDocumentClassification classification,
        ProjectDocumentRevision persistedRevision,
        ProjectDocumentRevision inMemoryRevision,
        IEnumerable<ProjectDocumentObject> objects)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(classification);
        ArgumentNullException.ThrowIfNull(objects);

        Identity = identity;
        Classification = classification;
        PersistedRevision = persistedRevision;
        InMemoryRevision = inMemoryRevision;
        Objects = objects
            .OrderBy(projectObject => projectObject.Uid.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public ProjectDocumentIdentity Identity { get; }

    public ProjectDocumentClassification Classification { get; }

    public ProjectDocumentRevision PersistedRevision { get; }

    public ProjectDocumentRevision InMemoryRevision { get; }

    public IReadOnlyList<ProjectDocumentObject> Objects { get; }

    public bool IsDirty => PersistedRevision != InMemoryRevision;

    public ProjectDocumentSnapshot WithInMemoryRevision(ProjectDocumentRevision revision)
    {
        return new ProjectDocumentSnapshot(Identity, Classification, PersistedRevision, revision, Objects);
    }

    public ProjectDocumentSnapshot MarkPersisted()
    {
        return new ProjectDocumentSnapshot(Identity, Classification, InMemoryRevision, InMemoryRevision, Objects);
    }
}

internal static class ProjectDocumentParser
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false
    };

    public static ProjectDocumentSnapshot ParseText(
        string relativePath,
        string text,
        ProjectDocumentRevisionState revisionState)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var classification = ProjectDocumentClassifier.Classify(normalizedPath, text);
        if (classification.IsBinary)
        {
            throw new FormatException("Binary project document cannot be parsed as text.");
        }

        return classification.ContentKind == ProjectDocumentContentKind.Json
            ? ParseJsonDocument(normalizedPath, text, classification, revisionState)
            : CreateTextSnapshot(normalizedPath, text, classification, revisionState);
    }

    public static ProjectDocumentSnapshot ParseBinary(
        string relativePath,
        IReadOnlyList<byte> content,
        ProjectDocumentRevisionState revisionState)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var classification = ProjectDocumentClassifier.ClassifyBinary(normalizedPath, content);
        var identity = CreateIdentity(normalizedPath, classification, persistentUid: null);
        var root = new ProjectDocumentObject(
            new ProjectDocumentObjectUid("binary:root"),
            parentObjectUid: null,
            type: "BinaryAsset",
            name: ProjectDocumentPaths.GetFileName(normalizedPath),
            order: 0,
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["length"] = content.Count.ToString(CultureInfo.InvariantCulture)
            });

        return new ProjectDocumentSnapshot(
            identity,
            classification,
            revisionState.PersistedRevision,
            revisionState.InMemoryRevision,
            [root]);
    }

    private static ProjectDocumentSnapshot ParseJsonDocument(
        string normalizedPath,
        string text,
        ProjectDocumentClassification classification,
        ProjectDocumentRevisionState revisionState)
    {
        JsonObject root;
        try
        {
            root = JsonNode.Parse(text) as JsonObject ??
                throw new FormatException("Project JSON document root must be an object.");
        }
        catch (JsonException exception)
        {
            throw new FormatException("Project JSON document text is malformed.", exception);
        }

        var identity = CreateIdentity(normalizedPath, classification, ReadOptionalString(root, "uid"));
        var objects = classification.Kind switch
        {
            ProjectDocumentKind.Scene => ReadSceneObjects(root),
            ProjectDocumentKind.Resource => ReadResourceObjects(root),
            ProjectDocumentKind.Settings => ReadRootJsonObject("settings:root", "SettingsDocument", root),
            ProjectDocumentKind.EditorMetadata => ReadRootJsonObject("settings:root", "EditorMetadataDocument", root),
            ProjectDocumentKind.Generated => ReadRootJsonObject("json:root", "GeneratedJsonDocument", root),
            _ => ReadRootJsonObject("json:root", "JsonDocument", root)
        };

        return new ProjectDocumentSnapshot(
            identity,
            classification,
            revisionState.PersistedRevision,
            revisionState.InMemoryRevision,
            objects);
    }

    private static ProjectDocumentSnapshot CreateTextSnapshot(
        string normalizedPath,
        string text,
        ProjectDocumentClassification classification,
        ProjectDocumentRevisionState revisionState)
    {
        var identity = CreateIdentity(normalizedPath, classification, persistentUid: null);
        var uid = classification.Kind == ProjectDocumentKind.Code ? "text:root" : "text:root";
        var root = new ProjectDocumentObject(
            new ProjectDocumentObjectUid(uid),
            parentObjectUid: null,
            type: classification.Kind == ProjectDocumentKind.Code ? "CodeDocument" : "TextDocument",
            name: ProjectDocumentPaths.GetFileName(normalizedPath),
            order: 0,
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["length"] = text.Length.ToString(CultureInfo.InvariantCulture)
            });

        return new ProjectDocumentSnapshot(
            identity,
            classification,
            revisionState.PersistedRevision,
            revisionState.InMemoryRevision,
            [root]);
    }

    private static ProjectDocumentIdentity CreateIdentity(
        string normalizedPath,
        ProjectDocumentClassification classification,
        string? persistentUid)
    {
        var kind = classification.Kind switch
        {
            ProjectDocumentKind.Scene => "scene",
            ProjectDocumentKind.Resource => "resource",
            ProjectDocumentKind.Settings => "settings",
            ProjectDocumentKind.Code => "code",
            ProjectDocumentKind.Json => "json",
            ProjectDocumentKind.Generated => "generated",
            ProjectDocumentKind.BinaryAsset => "binary-asset",
            ProjectDocumentKind.EditorMetadata => "editor-metadata",
            _ => "text"
        };

        var identityKey = classification.Kind == ProjectDocumentKind.Resource &&
            !string.IsNullOrWhiteSpace(persistentUid)
                ? persistentUid
                : ProjectDocumentPaths.ToResourcePath(normalizedPath);

        return new ProjectDocumentIdentity($"document://{kind}/{identityKey}", normalizedPath);
    }

    private static IReadOnlyList<ProjectDocumentObject> ReadSceneObjects(JsonObject root)
    {
        var nodes = ReadArray(root, "nodes", "Scene nodes");
        var result = new List<ProjectDocumentObject>(nodes.Count);
        var order = 0;
        foreach (var node in nodes)
        {
            var nodeObject = ExpectObject(node, "Scene node");
            var id = ReadInt32(nodeObject, "id", "Scene node id");
            var parentId = ReadNullableInt32(nodeObject, "parent", "Scene node parent");
            var uid = new ProjectDocumentObjectUid($"scene-node:{id.ToString(CultureInfo.InvariantCulture)}");
            var parentUid = parentId is null
                ? (ProjectDocumentObjectUid?)null
                : new ProjectDocumentObjectUid($"scene-node:{parentId.Value.ToString(CultureInfo.InvariantCulture)}");

            result.Add(new ProjectDocumentObject(
                uid,
                parentUid,
                ReadString(nodeObject, "type", "Scene node type"),
                ReadOptionalString(nodeObject, "name") ?? string.Empty,
                order++,
                ReadProperties(ExpectObject(ReadRequiredProperty(nodeObject, "properties", "Scene node properties"), "Scene node properties"))));
        }

        return result;
    }

    private static IReadOnlyList<ProjectDocumentObject> ReadResourceObjects(JsonObject root)
    {
        var result = new List<ProjectDocumentObject>
        {
            new(
                new ProjectDocumentObjectUid("resource:main"),
                parentObjectUid: null,
                ReadString(root, "type", "Resource file type"),
                ReadString(root, "path", "Resource file path"),
                order: 0,
                ReadProperties(ExpectObject(ReadRequiredProperty(root, "properties", "Resource file properties"), "Resource file properties")))
        };

        foreach (var resource in ReadArray(root, "internal", "Resource file internal resources"))
        {
            var resourceObject = ExpectObject(resource, "Internal resource");
            var id = ReadInt32(resourceObject, "id", "Internal resource id");
            result.Add(new ProjectDocumentObject(
                new ProjectDocumentObjectUid($"resource-internal:{id.ToString(CultureInfo.InvariantCulture)}"),
                new ProjectDocumentObjectUid("resource:main"),
                ReadString(resourceObject, "type", "Internal resource type"),
                $"internal:{id.ToString(CultureInfo.InvariantCulture)}",
                order: id,
                ReadProperties(ExpectObject(ReadRequiredProperty(resourceObject, "properties", "Internal resource properties"), "Internal resource properties"))));
        }

        foreach (var reference in ReadArray(root, "external", "Resource file external references"))
        {
            var referenceObject = ExpectObject(reference, "External reference");
            var id = ReadInt32(referenceObject, "id", "External reference id");
            result.Add(new ProjectDocumentObject(
                new ProjectDocumentObjectUid($"resource-external:{id.ToString(CultureInfo.InvariantCulture)}"),
                new ProjectDocumentObjectUid("resource:main"),
                ReadString(referenceObject, "type", "External reference type"),
                ReadString(referenceObject, "path", "External reference path"),
                order: id,
                properties: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["uid"] = ReadString(referenceObject, "uid", "External reference uid")
                }));
        }

        return result;
    }

    private static IReadOnlyList<ProjectDocumentObject> ReadRootJsonObject(string uid, string type, JsonObject root)
    {
        return
        [
            new ProjectDocumentObject(
                new ProjectDocumentObjectUid(uid),
                parentObjectUid: null,
                type,
                name: type,
                order: 0,
                ReadProperties(root))
        ];
    }

    private static IReadOnlyDictionary<string, string> ReadProperties(JsonObject properties)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result[key] = value?.ToJsonString(CompactOptions) ?? "null";
        }

        return result;
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static JsonNode ReadRequiredProperty(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node)
            ? node ?? throw new FormatException($"{description} must not be null.")
            : throw new FormatException($"{description} is required.");
    }

    private static JsonArray ReadArray(JsonObject root, string name, string description)
    {
        return ReadRequiredProperty(root, name, description) as JsonArray ??
            throw new FormatException($"{description} must be a JSON array.");
    }

    private static string ReadString(JsonObject root, string name, string description)
    {
        var node = ReadRequiredProperty(root, name, description);
        return node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value) &&
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new FormatException($"{description} must be a non-empty JSON string.");
    }

    private static string? ReadOptionalString(JsonObject root, string name)
    {
        if (!root.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        return node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value) ? value : null;
    }

    private static int ReadInt32(JsonObject root, string name, string description)
    {
        var node = ReadRequiredProperty(root, name, description);
        return node is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var value)
            ? value
            : throw new FormatException($"{description} must be a JSON integer.");
    }

    private static int? ReadNullableInt32(JsonObject root, string name, string description)
    {
        if (!root.TryGetPropertyValue(name, out var node))
        {
            throw new FormatException($"{description} is required.");
        }

        if (node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return node is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var value)
            ? value
            : throw new FormatException($"{description} must be a JSON integer or null.");
    }
}

internal static class ProjectDocumentStructuralSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(ProjectDocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var root = new JsonObject
        {
            ["format"] = "Electron2D.ProjectDocumentSnapshot",
            ["version"] = 1,
            ["documentId"] = snapshot.Identity.DocumentId,
            ["path"] = snapshot.Identity.NormalizedPath,
            ["kind"] = snapshot.Classification.Kind.ToString(),
            ["contentKind"] = snapshot.Classification.ContentKind.ToString(),
            ["isGenerated"] = snapshot.Classification.IsGenerated,
            ["isBinary"] = snapshot.Classification.IsBinary,
            ["persistedRevision"] = snapshot.PersistedRevision.Value,
            ["inMemoryRevision"] = snapshot.InMemoryRevision.Value,
            ["objects"] = WriteObjects(snapshot.Objects)
        };

        return root.ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static ProjectDocumentSnapshot Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var root = JsonNode.Parse(text) as JsonObject ??
            throw new FormatException("Project document snapshot must be a JSON object.");
        var format = ReadString(root, "format", "Snapshot format");
        if (format != "Electron2D.ProjectDocumentSnapshot")
        {
            throw new FormatException($"Snapshot format '{format}' is not supported.");
        }

        var version = ReadInt64(root, "version", "Snapshot version");
        if (version != 1)
        {
            throw new FormatException($"Snapshot version '{version}' is not supported.");
        }

        var classification = new ProjectDocumentClassification(
            Enum.Parse<ProjectDocumentKind>(ReadString(root, "kind", "Snapshot kind")),
            Enum.Parse<ProjectDocumentContentKind>(ReadString(root, "contentKind", "Snapshot content kind")),
            ReadBool(root, "isGenerated", "Snapshot generated flag"),
            ReadBool(root, "isBinary", "Snapshot binary flag"));

        return new ProjectDocumentSnapshot(
            new ProjectDocumentIdentity(
                ReadString(root, "documentId", "Snapshot document id"),
                ReadString(root, "path", "Snapshot path")),
            classification,
            new ProjectDocumentRevision(ReadInt64(root, "persistedRevision", "Snapshot persisted revision")),
            new ProjectDocumentRevision(ReadInt64(root, "inMemoryRevision", "Snapshot in-memory revision")),
            ReadObjects(ReadArray(root, "objects", "Snapshot objects")));
    }

    private static JsonArray WriteObjects(IEnumerable<ProjectDocumentObject> objects)
    {
        var result = new JsonArray();
        foreach (var projectObject in objects.OrderBy(projectObject => projectObject.Uid.Value, StringComparer.Ordinal))
        {
            result.Add((JsonNode)new JsonObject
            {
                ["uid"] = projectObject.Uid.Value,
                ["parentUid"] = projectObject.ParentObjectUid?.Value,
                ["type"] = projectObject.Type,
                ["name"] = projectObject.Name,
                ["order"] = projectObject.Order,
                ["properties"] = WriteProperties(projectObject.Properties)
            });
        }

        return result;
    }

    private static JsonObject WriteProperties(IReadOnlyDictionary<string, string> properties)
    {
        var result = new JsonObject();
        foreach (var (key, value) in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result[key] = value;
        }

        return result;
    }

    private static IReadOnlyList<ProjectDocumentObject> ReadObjects(JsonArray objects)
    {
        var result = new List<ProjectDocumentObject>(objects.Count);
        foreach (var node in objects)
        {
            var projectObject = node as JsonObject ??
                throw new FormatException("Snapshot object must be a JSON object.");
            var parentUid = ReadOptionalString(projectObject, "parentUid");
            result.Add(new ProjectDocumentObject(
                new ProjectDocumentObjectUid(ReadString(projectObject, "uid", "Snapshot object uid")),
                parentUid is null ? null : new ProjectDocumentObjectUid(parentUid),
                ReadString(projectObject, "type", "Snapshot object type"),
                ReadOptionalString(projectObject, "name") ?? string.Empty,
                (int)ReadInt64(projectObject, "order", "Snapshot object order"),
                ReadProperties(ReadArrayObject(projectObject, "properties", "Snapshot object properties"))));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> ReadProperties(JsonObject properties)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in properties)
        {
            result.Add(key, value?.GetValue<string>() ?? "null");
        }

        return result;
    }

    private static JsonArray ReadArray(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node) && node is JsonArray array
            ? array
            : throw new FormatException($"{description} must be a JSON array.");
    }

    private static JsonObject ReadArrayObject(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node) && node is JsonObject value
            ? value
            : throw new FormatException($"{description} must be a JSON object.");
    }

    private static string ReadString(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a non-empty JSON string.");
    }

    private static string? ReadOptionalString(JsonObject root, string name)
    {
        if (!root.TryGetPropertyValue(name, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value) ? value : null;
    }

    private static long ReadInt64(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<long>(out var value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a JSON integer.");
    }

    private static bool ReadBool(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<bool>(out var value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a JSON bool.");
    }
}

internal sealed class ProjectDocumentDiff
{
    public ProjectDocumentDiff(IEnumerable<ProjectDocumentChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        Changes = changes.ToArray();
    }

    public IReadOnlyList<ProjectDocumentChange> Changes { get; }
}

internal sealed class ProjectDocumentChange
{
    public ProjectDocumentChange(
        ProjectDocumentChangeKind kind,
        ProjectDocumentObjectUid objectUid,
        string? propertyPath = null,
        string? oldValue = null,
        string? newValue = null)
    {
        Kind = kind;
        ObjectUid = objectUid;
        PropertyPath = propertyPath;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public ProjectDocumentChangeKind Kind { get; }

    public ProjectDocumentObjectUid ObjectUid { get; }

    public string? PropertyPath { get; }

    public string? OldValue { get; }

    public string? NewValue { get; }
}

internal static class ProjectDocumentStructuralDiff
{
    public static ProjectDocumentDiff Compare(ProjectDocumentSnapshot before, ProjectDocumentSnapshot after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (!string.Equals(before.Identity.DocumentId, after.Identity.DocumentId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Project document snapshots must have the same document identity.");
        }

        var changes = new List<ProjectDocumentChange>();
        var beforeByUid = before.Objects.ToDictionary(projectObject => projectObject.Uid.Value, StringComparer.Ordinal);
        var afterByUid = after.Objects.ToDictionary(projectObject => projectObject.Uid.Value, StringComparer.Ordinal);

        foreach (var beforeObject in before.Objects.OrderBy(projectObject => projectObject.Uid.Value, StringComparer.Ordinal))
        {
            if (!afterByUid.TryGetValue(beforeObject.Uid.Value, out var afterObject))
            {
                changes.Add(new ProjectDocumentChange(ProjectDocumentChangeKind.Deleted, beforeObject.Uid));
                continue;
            }

            CompareObject(beforeObject, afterObject, changes);
        }

        foreach (var afterObject in after.Objects.OrderBy(projectObject => projectObject.Uid.Value, StringComparer.Ordinal))
        {
            if (!beforeByUid.ContainsKey(afterObject.Uid.Value))
            {
                changes.Add(new ProjectDocumentChange(ProjectDocumentChangeKind.Added, afterObject.Uid));
            }
        }

        return new ProjectDocumentDiff(changes);
    }

    public static bool AreNonOverlappingPropertyChanges(ProjectDocumentChange left, ProjectDocumentChange right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Kind != ProjectDocumentChangeKind.PropertyChanged ||
            right.Kind != ProjectDocumentChangeKind.PropertyChanged)
        {
            return false;
        }

        return !string.Equals(left.ObjectUid.Value, right.ObjectUid.Value, StringComparison.Ordinal) ||
            !string.Equals(left.PropertyPath, right.PropertyPath, StringComparison.Ordinal);
    }

    private static void CompareObject(
        ProjectDocumentObject before,
        ProjectDocumentObject after,
        List<ProjectDocumentChange> changes)
    {
        if (!string.Equals(before.Name, after.Name, StringComparison.Ordinal))
        {
            changes.Add(new ProjectDocumentChange(
                ProjectDocumentChangeKind.Renamed,
                before.Uid,
                oldValue: before.Name,
                newValue: after.Name));
        }

        var beforeParent = before.ParentObjectUid?.Value;
        var afterParent = after.ParentObjectUid?.Value;
        if (!string.Equals(beforeParent, afterParent, StringComparison.Ordinal))
        {
            changes.Add(new ProjectDocumentChange(
                ProjectDocumentChangeKind.Moved,
                before.Uid,
                oldValue: beforeParent,
                newValue: afterParent));
        }
        else if (before.Order != after.Order)
        {
            changes.Add(new ProjectDocumentChange(
                ProjectDocumentChangeKind.Moved,
                before.Uid,
                oldValue: before.Order.ToString(CultureInfo.InvariantCulture),
                newValue: after.Order.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var propertyName in before.Properties.Keys
            .Concat(after.Properties.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal))
        {
            before.Properties.TryGetValue(propertyName, out var beforeValue);
            after.Properties.TryGetValue(propertyName, out var afterValue);
            if (!string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
            {
                changes.Add(new ProjectDocumentChange(
                    ProjectDocumentChangeKind.PropertyChanged,
                    before.Uid,
                    propertyName,
                    beforeValue,
                    afterValue));
            }
        }
    }
}

internal static class ProjectDocumentPaths
{
    public static string NormalizeRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var path = relativePath.Replace('\\', '/');
        if (path.StartsWith("res://", StringComparison.Ordinal))
        {
            path = path["res://".Length..];
        }

        path = path.Trim('/');
        var parts = new List<string>();
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                throw new ArgumentException("Project document path must not contain '..'.", nameof(relativePath));
            }

            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException("Project document path must not be empty.", nameof(relativePath));
        }

        return string.Join('/', parts);
    }

    public static string ToResourcePath(string normalizedPath)
    {
        return normalizedPath.StartsWith("res://", StringComparison.Ordinal)
            ? normalizedPath
            : $"res://{normalizedPath}";
    }

    public static string GetFileName(string normalizedPath)
    {
        var index = normalizedPath.LastIndexOf('/');
        return index >= 0 ? normalizedPath[(index + 1)..] : normalizedPath;
    }
}
