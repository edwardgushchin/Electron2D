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

namespace Electron2D.ProjectSystem;

internal enum ProjectTextSchemaKind
{
    SceneFile,
    ResourceFile,
    ProjectSettings
}

internal static class ProjectTextFormatter
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string FormatText(string relativePath, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var classification = ProjectDocumentClassifier.Classify(normalizedPath, text);
        if (classification.IsGenerated || classification.IsBinary)
        {
            throw new InvalidOperationException("Generated and binary project files are not canonical source text.");
        }

        if (classification.ContentKind != ProjectDocumentContentKind.Json)
        {
            return text.ReplaceLineEndings("\n");
        }

        JsonObject root;
        try
        {
            root = JsonNode.Parse(text) as JsonObject ??
                throw new FormatException("Project text JSON root must be an object.");
        }
        catch (JsonException exception)
        {
            throw new FormatException("Project text JSON is malformed.", exception);
        }

        var canonical = CanonicalizeObject(root, GetTopLevelOrder(classification));
        return canonical.ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    private static IReadOnlyList<string> GetTopLevelOrder(ProjectDocumentClassification classification)
    {
        return classification.Kind switch
        {
            ProjectDocumentKind.Scene =>
            [
                "format",
                "version",
                "external",
                "internal",
                "nodes"
            ],
            ProjectDocumentKind.Resource =>
            [
                "format",
                "version",
                "uid",
                "type",
                "path",
                "external",
                "internal",
                "properties"
            ],
            ProjectDocumentKind.Settings =>
            [
                "format",
                "version",
                "engineVersion",
                "name",
                "mainScene",
                "rendererProfile",
                "physicsTickRate"
            ],
            _ => Array.Empty<string>()
        };
    }

    private static JsonNode? CanonicalizeNode(JsonNode? node)
    {
        return node switch
        {
            JsonObject jsonObject => CanonicalizeObject(jsonObject, GetObjectOrder(jsonObject)),
            JsonArray jsonArray => CanonicalizeArray(jsonArray),
            JsonValue jsonValue => JsonValue.Create(jsonValue.GetValue<object?>()),
            _ => null
        };
    }

    private static JsonObject CanonicalizeObject(JsonObject source, IReadOnlyList<string> preferredOrder)
    {
        var result = new JsonObject();
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in preferredOrder)
        {
            if (source.TryGetPropertyValue(key, out var value))
            {
                result[key] = CanonicalizeNode(value);
                used.Add(key);
            }
        }

        foreach (var key in source.Select(pair => pair.Key).Where(key => !used.Contains(key)).OrderBy(key => key, StringComparer.Ordinal))
        {
            result[key] = CanonicalizeNode(source[key]);
        }

        return result;
    }

    private static JsonArray CanonicalizeArray(JsonArray source)
    {
        var result = new JsonArray();
        foreach (var item in source)
        {
            result.Add(CanonicalizeNode(item));
        }

        return result;
    }

    private static IReadOnlyList<string> GetObjectOrder(JsonObject source)
    {
        if (source.ContainsKey("id") &&
            source.ContainsKey("type") &&
            source.ContainsKey("name") &&
            source.ContainsKey("parent") &&
            source.ContainsKey("owner") &&
            source.ContainsKey("groups") &&
            source.ContainsKey("properties"))
        {
            return
            [
                "id",
                "type",
                "name",
                "parent",
                "owner",
                "groups",
                "properties"
            ];
        }

        if (source.ContainsKey("id") &&
            source.ContainsKey("uid") &&
            source.ContainsKey("path") &&
            source.ContainsKey("type"))
        {
            return
            [
                "id",
                "uid",
                "path",
                "type"
            ];
        }

        if (source.ContainsKey("id") &&
            source.ContainsKey("type") &&
            source.ContainsKey("properties"))
        {
            return
            [
                "id",
                "type",
                "properties"
            ];
        }

        if (source.ContainsKey("type") && source.ContainsKey("value"))
        {
            return
            [
                "type",
                "value"
            ];
        }

        return Array.Empty<string>();
    }
}

internal sealed class ProjectTextMigrationResult
{
    public ProjectTextMigrationResult(string text, bool changed, IEnumerable<string> appliedMigrationIds)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(appliedMigrationIds);

        Text = text;
        Changed = changed;
        AppliedMigrationIds = appliedMigrationIds.ToArray();
    }

    public string Text { get; }

    public bool Changed { get; }

    public IReadOnlyList<string> AppliedMigrationIds { get; }
}

internal static class ProjectTextMigrationPipeline
{
    public static ProjectTextMigrationResult MigrateText(string relativePath, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var classification = ProjectDocumentClassifier.Classify(normalizedPath, text);
        if (classification.ContentKind != ProjectDocumentContentKind.Json)
        {
            return new ProjectTextMigrationResult(text.ReplaceLineEndings("\n"), changed: false, []);
        }

        var root = JsonNode.Parse(text) as JsonObject ??
            throw new FormatException("Project text JSON root must be an object.");
        var version = ReadOptionalVersion(root);
        if (version > 1)
        {
            throw new FormatException($"Project text version '{version}' is newer than supported version '1'.");
        }

        var applied = new List<string>();
        if (version is null or 0)
        {
            root["version"] = 1;
            applied.Add($"project-text-format/{GetMigrationFormatName(classification)}-v0-to-v1");
        }

        var migratedText = ProjectTextFormatter.FormatText(normalizedPath, root.ToJsonString());
        return new ProjectTextMigrationResult(migratedText, applied.Count > 0, applied);
    }

    private static string GetMigrationFormatName(ProjectDocumentClassification classification)
    {
        return classification.Kind switch
        {
            ProjectDocumentKind.Scene => "scene-file",
            ProjectDocumentKind.Resource => "resource-file",
            ProjectDocumentKind.Settings => "project-settings",
            _ => "generic-json"
        };
    }

    private static int? ReadOptionalVersion(JsonObject root)
    {
        if (!root.TryGetPropertyValue("version", out var node) || node is null)
        {
            return null;
        }

        return node is JsonValue value && value.TryGetValue<int>(out var version)
            ? version
            : throw new FormatException("Project text version must be a JSON integer.");
    }
}

internal sealed class ProjectTextValidationResult
{
    public ProjectTextValidationResult(IEnumerable<ProjectTextValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors.ToArray();
    }

    public bool Succeeded => Errors.Count == 0;

    public IReadOnlyList<ProjectTextValidationError> Errors { get; }
}

internal sealed class ProjectTextValidationError
{
    public ProjectTextValidationError(string code, string jsonPath, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        JsonPath = jsonPath;
        Message = message;
    }

    public string Code { get; }

    public string JsonPath { get; }

    public string Message { get; }
}

internal static class ProjectTextValidator
{
    private static readonly string[] SecretFieldFragments =
    [
        "password",
        "token",
        "secret",
        "key",
        "credential"
    ];

    private static readonly string[] EditorStateFields =
    [
        "selection",
        "expanded",
        "scroll",
        "viewport",
        "inspector",
        "dockLayout"
    ];

    public static ProjectTextValidationResult ValidateText(string relativePath, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var errors = new List<ProjectTextValidationError>();
        var classification = ProjectDocumentClassifier.Classify(normalizedPath, text);
        if (classification.IsGenerated)
        {
            errors.Add(new ProjectTextValidationError(
                "E2D-TEXT-GENERATED-SOURCE",
                "$",
                "Generated files are not canonical source documents."));
            return new ProjectTextValidationResult(errors);
        }

        if (classification.ContentKind != ProjectDocumentContentKind.Json)
        {
            return new ProjectTextValidationResult(errors);
        }

        JsonObject root;
        try
        {
            root = JsonNode.Parse(text) as JsonObject ??
                throw new FormatException("Project text JSON root must be an object.");
        }
        catch (JsonException exception)
        {
            errors.Add(new ProjectTextValidationError("E2D-TEXT-MALFORMED-JSON", "$", exception.Message));
            return new ProjectTextValidationResult(errors);
        }

        ValidateFormatAndVersion(root, classification, errors);
        ValidateNode(root, "$", errors, currentPropertyName: null);

        return new ProjectTextValidationResult(errors);
    }

    private static void ValidateFormatAndVersion(
        JsonObject root,
        ProjectDocumentClassification classification,
        List<ProjectTextValidationError> errors)
    {
        if (root.TryGetPropertyValue("format", out var formatNode) &&
            formatNode is JsonValue formatValue &&
            formatValue.TryGetValue<string>(out var format) &&
            format.StartsWith("Electron2D.", StringComparison.Ordinal) &&
            classification.Kind is not (ProjectDocumentKind.Scene or ProjectDocumentKind.Resource or ProjectDocumentKind.Settings))
        {
            errors.Add(new ProjectTextValidationError(
                "E2D-TEXT-UNKNOWN-FORMAT",
                "$.format",
                $"Project text format '{format}' is not supported."));
        }

        if (!root.TryGetPropertyValue("version", out var versionNode) ||
            versionNode is not JsonValue versionValue ||
            !versionValue.TryGetValue<int>(out var version) ||
            version != 1)
        {
            errors.Add(new ProjectTextValidationError(
                "E2D-TEXT-VERSION",
                "$.version",
                "Project text schema version must be 1."));
        }
    }

    private static void ValidateNode(
        JsonNode? node,
        string jsonPath,
        List<ProjectTextValidationError> errors,
        string? currentPropertyName)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var (key, child) in jsonObject)
                {
                    ValidateFieldName(key, $"{jsonPath}.{key}", child, errors);
                    ValidateNode(child, $"{jsonPath}.{key}", errors, key);
                }

                break;
            case JsonArray jsonArray:
                for (var i = 0; i < jsonArray.Count; i++)
                {
                    ValidateNode(jsonArray[i], $"{jsonPath}[{i}]", errors, currentPropertyName);
                }

                break;
            case JsonValue jsonValue:
                if (jsonValue.TryGetValue<string>(out var value) && IsForbiddenAbsolutePath(value))
                {
                    errors.Add(new ProjectTextValidationError(
                        "E2D-TEXT-ABSOLUTE-PATH",
                        jsonPath,
                        $"Project source field '{currentPropertyName ?? jsonPath}' must not contain an absolute path."));
                }

                break;
        }
    }

    private static void ValidateFieldName(
        string propertyName,
        string jsonPath,
        JsonNode? value,
        List<ProjectTextValidationError> errors)
    {
        if (EditorStateFields.Any(field => string.Equals(field, propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(new ProjectTextValidationError(
                "E2D-TEXT-EDITOR-STATE",
                jsonPath,
                $"Project source field '{propertyName}' must not contain Editor UI state."));
        }

        if (!SecretFieldFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var secretValueIsReference = value is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var text) &&
            text.StartsWith("env:", StringComparison.Ordinal);
        if (!secretValueIsReference)
        {
            errors.Add(new ProjectTextValidationError(
                "E2D-TEXT-SECRET-FIELD",
                jsonPath,
                $"Project source field '{propertyName}' must not store secrets directly."));
        }
    }

    private static bool IsForbiddenAbsolutePath(string value)
    {
        if (value.StartsWith("res://", StringComparison.Ordinal) ||
            value.StartsWith("uid://", StringComparison.Ordinal) ||
            value.StartsWith("env:", StringComparison.Ordinal))
        {
            return false;
        }

        return value.StartsWith("/", StringComparison.Ordinal) ||
            value.StartsWith("\\\\", StringComparison.Ordinal) ||
            (value.Length >= 3 &&
                char.IsAsciiLetter(value[0]) &&
                value[1] == ':' &&
                (value[2] == '\\' || value[2] == '/'));
    }
}

internal static class ProjectTextSchemaRegistry
{
    public static string GetSchemaText(ProjectTextSchemaKind kind)
    {
        var fileName = kind switch
        {
            ProjectTextSchemaKind.SceneFile => "scene-file.schema.json",
            ProjectTextSchemaKind.ResourceFile => "resource-file.schema.json",
            ProjectTextSchemaKind.ProjectSettings => "project-settings.schema.json",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported project text schema kind.")
        };

        var root = FindRepositoryRoot();
        var schemaPath = Path.Combine(root, "data", "schemas", "project-system", fileName);
        return File.ReadAllText(schemaPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
