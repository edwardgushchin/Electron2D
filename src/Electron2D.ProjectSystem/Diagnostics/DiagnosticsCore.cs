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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum DiagnosticSeverity
{
    Error,
    Warning,
    Info,
    Hint
}

internal enum DiagnosticCategory
{
    Project,
    Scene,
    Resource,
    Import,
    Build,
    Runtime,
    Script,
    Export,
    Tooling,
    Diagnostics
}

internal enum DiagnosticFixActionKind
{
    ReplaceText,
    CreateFile,
    UpdateJsonProperty,
    DeleteJsonProperty
}

internal sealed class DiagnosticCodeDefinition
{
    public DiagnosticCodeDefinition(
        string code,
        DiagnosticSeverity severity,
        DiagnosticCategory category,
        string title,
        string documentationUri)
    {
        ValidateCode(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentationUri);

        Code = code;
        Severity = severity;
        Category = category;
        Title = title;
        DocumentationUri = documentationUri;
    }

    public string Code { get; }

    public DiagnosticSeverity Severity { get; }

    public DiagnosticCategory Category { get; }

    public string Title { get; }

    public string DocumentationUri { get; }

    internal static void ValidateCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var parts = code.Split('-');
        if (parts.Length < 3 ||
            parts[0] != "E2D" ||
            parts[^1].Length != 4 ||
            !parts[^1].All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Diagnostic code must use the E2D-<DOMAIN>-NNNN format.", nameof(code));
        }

        foreach (var part in parts.Skip(1).SkipLast(1))
        {
            if (part.Length == 0 ||
                !part.All(character =>
                    (char.IsAsciiLetter(character) && char.IsUpper(character)) ||
                    char.IsAsciiDigit(character)))
            {
                throw new ArgumentException("Diagnostic code domain parts must be uppercase ASCII letters or digits.", nameof(code));
            }
        }
    }
}

internal static class DiagnosticCodeRegistry
{
    private static readonly DiagnosticCodeDefinition[] Definitions =
    [
        new(
            "E2D-AGENT-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Agent MCP handshake was rejected.",
            "docs/editor/agent-process-bootstrap.md#e2d-agent-0001"),
        new(
            "E2D-AGENT-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Agent MCP token expired.",
            "docs/editor/agent-process-bootstrap.md#e2d-agent-0002"),
        new(
            "E2D-AGENT-0003",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Agent process bootstrap failed.",
            "docs/editor/agent-process-bootstrap.md#e2d-agent-0003"),
        new(
            "E2D-DIAG-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Diagnostics,
            "Diagnostic record is invalid.",
            "docs/diagnostics/diagnostics-core.md#e2d-diag-0001"),
        new(
            "E2D-CLI-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "CLI command is not implemented in the current Preview scope.",
            "docs/diagnostics/diagnostics-core.md#e2d-cli-0001"),
        new(
            "E2D-CLI-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "CLI arguments are invalid.",
            "docs/diagnostics/diagnostics-core.md#e2d-cli-0002"),
        new(
            "E2D-CLI-0003",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "CLI route selection rejected the command.",
            "docs/diagnostics/diagnostics-core.md#e2d-cli-0003"),
        new(
            "E2D-CAPABILITY-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Editor capability parity is incomplete.",
            "docs/tooling/editor-capability-manifest.md#diagnostics"),
        new(
            "E2D-CAPABILITY-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Editor capability CLI binding policy is invalid.",
            "docs/tooling/editor-capability-manifest.md#diagnostics"),
        new(
            "E2D-CAPABILITY-0003",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Editor capability manifest shape is invalid.",
            "docs/tooling/editor-capability-manifest.md#diagnostics"),
        new(
            "E2D-CAPABILITY-0004",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Editor capability binding references an unpublished Tooling or MCP surface.",
            "docs/tooling/editor-capability-manifest.md#diagnostics"),
        new(
            "E2D-DOCTOR-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Project reproducibility baseline is missing, malformed or inconsistent.",
            "docs/project-system/reproducibility-lock-and-doctor.md#e2d-doctor-0001"),
        new(
            "E2D-MCP-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "MCP tool is not implemented in the current Preview scope.",
            "docs/diagnostics/diagnostics-core.md#e2d-mcp-0001"),
        new(
            "E2D-PROJECT-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Project document is malformed.",
            "docs/diagnostics/diagnostics-core.md#e2d-project-0001"),
        new(
            "E2D-PROJECT-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Project document version is unsupported.",
            "docs/diagnostics/diagnostics-core.md#e2d-project-0002"),
        new(
            "E2D-PROJECT-0003",
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Project,
            "Project document has a safe suggested fix.",
            "docs/diagnostics/diagnostics-core.md#e2d-project-0003"),
        new(
            "E2D-RUNTIME-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Runtime,
            "Runtime debug bridge rejected the request.",
            "docs/diagnostics/diagnostics-core.md#e2d-runtime-0001"),
        new(
            "E2D-SCRIPT-0003",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Script,
            "C# script language service semantic model failed.",
            "docs/scripting/editor-language-services.md#e2d-script-0003"),
        new(
            "E2D-TEST-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Scene test assertion failed.",
            "docs/diagnostics/diagnostics-core.md#e2d-test-0001"),
        new(
            "E2D-TEST-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Scene visual comparison failed.",
            "docs/diagnostics/diagnostics-core.md#e2d-test-0002"),
        new(
            "E2D-TOOLING-0001",
            DiagnosticSeverity.Info,
            DiagnosticCategory.Tooling,
            "Workspace job cancellation state changed.",
            "docs/diagnostics/diagnostics-core.md#e2d-tooling-0001"),
        new(
            "E2D-TOOLING-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Workspace transaction was rejected.",
            "docs/diagnostics/diagnostics-core.md#e2d-tooling-0002"),
        new(
            "E2D-TOOLING-0003",
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Tooling,
            "Editor session discovery selected fallback or rejected a mismatched descriptor.",
            "docs/diagnostics/diagnostics-core.md#e2d-tooling-0003"),
        new(
            "E2D-TOOLING-0004",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            "Editor session endpoint was rejected.",
            "docs/diagnostics/diagnostics-core.md#e2d-tooling-0004"),
        new(
            "E2D-TASK-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Project task acceptance or privileged field guard rejected the operation.",
            "docs/diagnostics/diagnostics-core.md#e2d-task-0002"),
        new(
            "E2D-TASK-0003",
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Project,
            "Project task dependency graph requires attention.",
            "docs/diagnostics/diagnostics-core.md#e2d-task-0003"),
        new(
            "E2D-TASK-0004",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Taskboard writer lock timed out.",
            "docs/diagnostics/diagnostics-core.md#e2d-task-0004"),
        new(
            "E2D-TASK-0005",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Taskboard writer coordination was cancelled.",
            "docs/diagnostics/diagnostics-core.md#e2d-task-0005"),
        new(
            "E2D-TASK-0006",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Taskboard optimistic revision check rejected the operation.",
            "docs/diagnostics/diagnostics-core.md#e2d-task-0006"),
        new(
            "E2D-TASK-0007",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Taskboard operation identity conflicts with an existing receipt.",
            "docs/diagnostics/diagnostics-core.md#e2d-task-0007")
    ];

    private static readonly IReadOnlyList<DiagnosticCodeDefinition> SortedDefinitions =
        Definitions.OrderBy(definition => definition.Code, StringComparer.Ordinal).ToArray();

    private static readonly IReadOnlyDictionary<string, DiagnosticCodeDefinition> ByCode =
        new ReadOnlyDictionary<string, DiagnosticCodeDefinition>(
            Definitions.ToDictionary(definition => definition.Code, StringComparer.Ordinal));

    static DiagnosticCodeRegistry()
    {
        if (Definitions.Length != ByCode.Count)
        {
            throw new InvalidOperationException("Diagnostic code registry contains duplicate codes.");
        }
    }

    public static IReadOnlyList<DiagnosticCodeDefinition> All => SortedDefinitions;

    public static DiagnosticCodeDefinition Get(string code)
    {
        return TryGet(code, out var definition)
            ? definition
            : throw new ArgumentException($"Diagnostic code '{code}' is not registered.", nameof(code));
    }

    public static bool TryGet(string code, out DiagnosticCodeDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return ByCode.TryGetValue(code, out definition!);
    }
}

internal sealed class DiagnosticLocation
{
    public DiagnosticLocation(
        string? file = null,
        int? line = null,
        int? column = null,
        string? sceneUid = null,
        string? nodePath = null,
        string? resourceUid = null)
    {
        File = file is null ? null : DiagnosticProjectPathRules.NormalizeProjectPath(file);
        Line = ValidatePositiveOrNull(line, nameof(line));
        Column = ValidatePositiveOrNull(column, nameof(column));
        if (Column is not null && Line is null)
        {
            throw new ArgumentException("Diagnostic location column requires line.", nameof(column));
        }

        SceneUid = NormalizeOptionalIdentifier(sceneUid, nameof(sceneUid));
        NodePath = NormalizeOptionalIdentifier(nodePath, nameof(nodePath));
        ResourceUid = NormalizeOptionalIdentifier(resourceUid, nameof(resourceUid));
    }

    public string? File { get; }

    public int? Line { get; }

    public int? Column { get; }

    public string? SceneUid { get; }

    public string? NodePath { get; }

    public string? ResourceUid { get; }

    private static int? ValidatePositiveOrNull(int? value, string parameterName)
    {
        if (value is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Diagnostic location positions are 1-based.");
        }

        return value;
    }

    private static string? NormalizeOptionalIdentifier(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Diagnostic location identifier must not be empty.", parameterName);
        }

        return value;
    }
}

internal sealed class DiagnosticRelatedLocation
{
    public DiagnosticRelatedLocation(DiagnosticLocation location, string message)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Location = location;
        Message = message;
    }

    public DiagnosticLocation Location { get; }

    public string Message { get; }
}

internal sealed class DiagnosticFixAction
{
    private DiagnosticFixAction(
        DiagnosticFixActionKind kind,
        string path,
        int? startLine = null,
        int? startColumn = null,
        int? endLine = null,
        int? endColumn = null,
        string? expectedText = null,
        string? newText = null,
        string? jsonPointer = null,
        string? expectedValue = null,
        string? newValue = null,
        string? content = null)
    {
        Kind = kind;
        Path = DiagnosticProjectPathRules.NormalizeProjectPath(path);
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        ExpectedText = expectedText;
        NewText = newText;
        JsonPointer = jsonPointer;
        ExpectedValue = expectedValue;
        NewValue = newValue;
        Content = content;
    }

    public DiagnosticFixActionKind Kind { get; }

    public string Path { get; }

    public int? StartLine { get; }

    public int? StartColumn { get; }

    public int? EndLine { get; }

    public int? EndColumn { get; }

    public string? ExpectedText { get; }

    public string? NewText { get; }

    public string? JsonPointer { get; }

    public string? ExpectedValue { get; }

    public string? NewValue { get; }

    public string? Content { get; }

    public static DiagnosticFixAction ReplaceText(
        string path,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string? expectedText,
        string newText)
    {
        ArgumentNullException.ThrowIfNull(newText);
        ValidateTextRange(startLine, startColumn, endLine, endColumn);
        return new DiagnosticFixAction(
            DiagnosticFixActionKind.ReplaceText,
            path,
            startLine,
            startColumn,
            endLine,
            endColumn,
            expectedText,
            newText);
    }

    public static DiagnosticFixAction CreateFile(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new DiagnosticFixAction(
            DiagnosticFixActionKind.CreateFile,
            path,
            content: content);
    }

    public static DiagnosticFixAction UpdateJsonProperty(
        string path,
        string jsonPointer,
        string? expectedValue,
        string newValue)
    {
        ArgumentNullException.ThrowIfNull(newValue);
        ValidateJsonPointer(jsonPointer);
        return new DiagnosticFixAction(
            DiagnosticFixActionKind.UpdateJsonProperty,
            path,
            jsonPointer: jsonPointer,
            expectedValue: expectedValue,
            newValue: newValue);
    }

    public static DiagnosticFixAction DeleteJsonProperty(
        string path,
        string jsonPointer,
        string? expectedValue)
    {
        ValidateJsonPointer(jsonPointer);
        return new DiagnosticFixAction(
            DiagnosticFixActionKind.DeleteJsonProperty,
            path,
            jsonPointer: jsonPointer,
            expectedValue: expectedValue);
    }

    private static void ValidateTextRange(int startLine, int startColumn, int endLine, int endColumn)
    {
        if (startLine <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startLine), startLine, "Text edit line is 1-based.");
        }

        if (startColumn <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startColumn), startColumn, "Text edit column is 1-based.");
        }

        if (endLine <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endLine), endLine, "Text edit line is 1-based.");
        }

        if (endColumn <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endColumn), endColumn, "Text edit column is 1-based.");
        }

        if (endLine < startLine || (endLine == startLine && endColumn < startColumn))
        {
            throw new ArgumentException("Text edit end position must be after or equal to start position.");
        }
    }

    private static void ValidateJsonPointer(string jsonPointer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPointer);
        if (!jsonPointer.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException("JSON Pointer must start with '/'.", nameof(jsonPointer));
        }
    }
}

internal sealed class DiagnosticSuggestedFix
{
    public DiagnosticSuggestedFix(string title, IEnumerable<DiagnosticFixAction> actions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(actions);

        Title = title;
        Actions = actions.ToArray();
        if (Actions.Count == 0)
        {
            throw new ArgumentException("Suggested fix must contain at least one action.", nameof(actions));
        }
    }

    public string Title { get; }

    public IReadOnlyList<DiagnosticFixAction> Actions { get; }
}

internal sealed class StructuredDiagnostic
{
    private StructuredDiagnostic(
        string code,
        DiagnosticSeverity severity,
        DiagnosticCategory category,
        string message,
        DiagnosticLocation? location,
        IEnumerable<DiagnosticRelatedLocation> relatedLocations,
        IEnumerable<DiagnosticSuggestedFix> suggestedFixes,
        string documentationUri)
    {
        Code = code;
        Severity = severity;
        Category = category;
        Message = message;
        Location = location;
        RelatedLocations = relatedLocations.ToArray();
        SuggestedFixes = suggestedFixes.ToArray();
        DocumentationUri = documentationUri;
    }

    public string Code { get; }

    public DiagnosticSeverity Severity { get; }

    public DiagnosticCategory Category { get; }

    public string Message { get; }

    public DiagnosticLocation? Location { get; }

    public IReadOnlyList<DiagnosticRelatedLocation> RelatedLocations { get; }

    public IReadOnlyList<DiagnosticSuggestedFix> SuggestedFixes { get; }

    public string DocumentationUri { get; }

    public static StructuredDiagnostic Create(
        string code,
        DiagnosticSeverity severity,
        DiagnosticCategory category,
        string message,
        DiagnosticLocation? location,
        IEnumerable<DiagnosticRelatedLocation> relatedLocations,
        IEnumerable<DiagnosticSuggestedFix> suggestedFixes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(relatedLocations);
        ArgumentNullException.ThrowIfNull(suggestedFixes);

        var definition = DiagnosticCodeRegistry.Get(code);
        if (definition.Severity != severity)
        {
            throw new InvalidOperationException(
                $"Diagnostic code '{code}' has registry severity '{definition.Severity}' but '{severity}' was requested.");
        }

        if (definition.Category != category)
        {
            throw new InvalidOperationException(
                $"Diagnostic code '{code}' has registry category '{definition.Category}' but '{category}' was requested.");
        }

        return new StructuredDiagnostic(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location,
            relatedLocations,
            suggestedFixes,
            definition.DocumentationUri);
    }
}

internal static class DiagnosticJsonSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(StructuredDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return ToJson(diagnostic).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static StructuredDiagnostic Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var root = JsonNode.Parse(text) as JsonObject ??
            throw new FormatException("Diagnostic JSON root must be an object.");
        return ReadDiagnostic(root);
    }

    public static JsonObject ToJson(StructuredDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return WriteDiagnostic(diagnostic);
    }

    public static JsonArray ToJsonArray(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var result = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            result.Add(ToJson(diagnostic));
        }

        return result;
    }

    private static JsonObject WriteDiagnostic(StructuredDiagnostic diagnostic)
    {
        return new JsonObject
        {
            ["code"] = diagnostic.Code,
            ["severity"] = diagnostic.Severity.ToString(),
            ["category"] = diagnostic.Category.ToString(),
            ["message"] = diagnostic.Message,
            ["location"] = diagnostic.Location is null ? null : WriteLocation(diagnostic.Location),
            ["relatedLocations"] = WriteRelatedLocations(diagnostic.RelatedLocations),
            ["suggestedFixes"] = WriteSuggestedFixes(diagnostic.SuggestedFixes),
            ["documentationUri"] = diagnostic.DocumentationUri
        };
    }

    private static JsonObject WriteLocation(DiagnosticLocation location)
    {
        return new JsonObject
        {
            ["file"] = location.File,
            ["line"] = location.Line,
            ["column"] = location.Column,
            ["sceneUid"] = location.SceneUid,
            ["nodePath"] = location.NodePath,
            ["resourceUid"] = location.ResourceUid
        };
    }

    private static JsonArray WriteRelatedLocations(IEnumerable<DiagnosticRelatedLocation> relatedLocations)
    {
        var result = new JsonArray();
        foreach (var relatedLocation in relatedLocations)
        {
            result.Add((JsonNode)new JsonObject
            {
                ["location"] = WriteLocation(relatedLocation.Location),
                ["message"] = relatedLocation.Message
            });
        }

        return result;
    }

    private static JsonArray WriteSuggestedFixes(IEnumerable<DiagnosticSuggestedFix> fixes)
    {
        var result = new JsonArray();
        foreach (var fix in fixes)
        {
            result.Add((JsonNode)new JsonObject
            {
                ["title"] = fix.Title,
                ["actions"] = WriteActions(fix.Actions)
            });
        }

        return result;
    }

    private static JsonArray WriteActions(IEnumerable<DiagnosticFixAction> actions)
    {
        var result = new JsonArray();
        foreach (var action in actions)
        {
            result.Add((JsonNode)new JsonObject
            {
                ["kind"] = action.Kind.ToString(),
                ["path"] = action.Path,
                ["startLine"] = action.StartLine,
                ["startColumn"] = action.StartColumn,
                ["endLine"] = action.EndLine,
                ["endColumn"] = action.EndColumn,
                ["expectedText"] = action.ExpectedText,
                ["newText"] = action.NewText,
                ["jsonPointer"] = action.JsonPointer,
                ["expectedValue"] = action.ExpectedValue,
                ["newValue"] = action.NewValue,
                ["content"] = action.Content
            });
        }

        return result;
    }

    private static StructuredDiagnostic ReadDiagnostic(JsonObject root)
    {
        var documentationUri = ReadString(root, "documentationUri", "Diagnostic documentation URI");
        var diagnostic = StructuredDiagnostic.Create(
            ReadString(root, "code", "Diagnostic code"),
            Enum.Parse<DiagnosticSeverity>(ReadString(root, "severity", "Diagnostic severity")),
            Enum.Parse<DiagnosticCategory>(ReadString(root, "category", "Diagnostic category")),
            ReadString(root, "message", "Diagnostic message"),
            ReadOptionalLocation(root, "location"),
            ReadRelatedLocations(ReadArray(root, "relatedLocations", "Diagnostic related locations")),
            ReadSuggestedFixes(ReadArray(root, "suggestedFixes", "Diagnostic suggested fixes")));

        if (!string.Equals(documentationUri, diagnostic.DocumentationUri, StringComparison.Ordinal))
        {
            throw new FormatException("Diagnostic documentation URI must match the registry definition.");
        }

        return diagnostic;
    }

    private static DiagnosticLocation? ReadOptionalLocation(JsonObject root, string name)
    {
        if (!root.TryGetPropertyValue(name, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return ReadLocation(node as JsonObject ??
            throw new FormatException("Diagnostic location must be a JSON object or null."));
    }

    private static DiagnosticLocation ReadLocation(JsonObject root)
    {
        return new DiagnosticLocation(
            ReadOptionalString(root, "file"),
            ReadOptionalInt32(root, "line"),
            ReadOptionalInt32(root, "column"),
            ReadOptionalString(root, "sceneUid"),
            ReadOptionalString(root, "nodePath"),
            ReadOptionalString(root, "resourceUid"));
    }

    private static IReadOnlyList<DiagnosticRelatedLocation> ReadRelatedLocations(JsonArray array)
    {
        var result = new List<DiagnosticRelatedLocation>(array.Count);
        foreach (var node in array)
        {
            var item = node as JsonObject ??
                throw new FormatException("Related location must be a JSON object.");
            var locationNode = item.TryGetPropertyValue("location", out var locationValue) && locationValue is JsonObject location
                ? location
                : throw new FormatException("Related location must contain location object.");
            result.Add(new DiagnosticRelatedLocation(
                ReadLocation(location),
                ReadString(item, "message", "Related location message")));
        }

        return result;
    }

    private static IReadOnlyList<DiagnosticSuggestedFix> ReadSuggestedFixes(JsonArray array)
    {
        var result = new List<DiagnosticSuggestedFix>(array.Count);
        foreach (var node in array)
        {
            var item = node as JsonObject ??
                throw new FormatException("Suggested fix must be a JSON object.");
            result.Add(new DiagnosticSuggestedFix(
                ReadString(item, "title", "Suggested fix title"),
                ReadActions(ReadArray(item, "actions", "Suggested fix actions"))));
        }

        return result;
    }

    private static IReadOnlyList<DiagnosticFixAction> ReadActions(JsonArray array)
    {
        var result = new List<DiagnosticFixAction>(array.Count);
        foreach (var node in array)
        {
            var item = node as JsonObject ??
                throw new FormatException("Suggested fix action must be a JSON object.");
            var kind = Enum.Parse<DiagnosticFixActionKind>(ReadString(item, "kind", "Suggested fix action kind"));
            var path = ReadString(item, "path", "Suggested fix action path");
            var action = kind switch
            {
                DiagnosticFixActionKind.ReplaceText => DiagnosticFixAction.ReplaceText(
                    path,
                    ReadRequiredInt32(item, "startLine", "Text replacement start line"),
                    ReadRequiredInt32(item, "startColumn", "Text replacement start column"),
                    ReadRequiredInt32(item, "endLine", "Text replacement end line"),
                    ReadRequiredInt32(item, "endColumn", "Text replacement end column"),
                    ReadOptionalString(item, "expectedText"),
                    ReadString(item, "newText", "Text replacement new text")),
                DiagnosticFixActionKind.CreateFile => DiagnosticFixAction.CreateFile(
                    path,
                    ReadString(item, "content", "Create file content")),
                DiagnosticFixActionKind.UpdateJsonProperty => DiagnosticFixAction.UpdateJsonProperty(
                    path,
                    ReadString(item, "jsonPointer", "JSON property pointer"),
                    ReadOptionalString(item, "expectedValue"),
                    ReadString(item, "newValue", "JSON property new value")),
                DiagnosticFixActionKind.DeleteJsonProperty => DiagnosticFixAction.DeleteJsonProperty(
                    path,
                    ReadString(item, "jsonPointer", "JSON property pointer"),
                    ReadOptionalString(item, "expectedValue")),
                _ => throw new FormatException($"Suggested fix action kind '{kind}' is not supported.")
            };

            result.Add(action);
        }

        return result;
    }

    private static JsonArray ReadArray(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node) && node is JsonArray array
            ? array
            : throw new FormatException($"{description} must be a JSON array.");
    }

    private static string ReadString(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var value) &&
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new FormatException($"{description} must be a non-empty JSON string.");
    }

    private static string? ReadOptionalString(JsonObject root, string name)
    {
        if (!root.TryGetPropertyValue(name, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value) ? value : null;
    }

    private static int ReadRequiredInt32(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<int>(out var value)
                ? value
                : throw new FormatException($"{description} must be a JSON integer.");
    }

    private static int? ReadOptionalInt32(JsonObject root, string name)
    {
        if (!root.TryGetPropertyValue(name, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return node is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var value)
            ? value
            : throw new FormatException($"{name} must be a JSON integer or null.");
    }
}

internal static class DiagnosticProjectPathRules
{
    public static string NormalizeProjectPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!path.StartsWith("res://", StringComparison.Ordinal) &&
            (IsAbsolutePath(path) || Uri.TryCreate(path, UriKind.Absolute, out _)))
        {
            throw new ArgumentException("Diagnostic project path must be project-relative.", nameof(path));
        }

        var normalized = ProjectDocumentPaths.NormalizeRelativePath(path);
        if (IsGeneratedOrCachePath(normalized))
        {
            throw new ArgumentException("Diagnostic suggested fix path must not target generated or cache files.", nameof(path));
        }

        return normalized;
    }

    private static bool IsAbsolutePath(string path)
    {
        return path.StartsWith("/", StringComparison.Ordinal) ||
            path.StartsWith("\\\\", StringComparison.Ordinal) ||
            (path.Length >= 3 &&
                char.IsAsciiLetter(path[0]) &&
                path[1] == ':' &&
                (path[2] == '\\' || path[2] == '/'));
    }

    private static bool IsGeneratedOrCachePath(string normalizedPath)
    {
        return normalizedPath.StartsWith(".electron2d/import-cache/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/workspaces/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/context/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/session/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("bin/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("obj/", StringComparison.Ordinal) ||
            normalizedPath.Contains("/generated/", StringComparison.Ordinal);
    }
}
