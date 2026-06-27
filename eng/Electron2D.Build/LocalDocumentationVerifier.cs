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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class LocalDocumentationVerifier(
    string repositoryRoot,
    JsonDiagnosticSink diagnostics,
    ProcessRunner processRunner)
{
    private const string IndexRelativePath = "data/documentation/electron2d-local-docs-index.json";
    private const string ApiManifestRelativePath = "data/api/electron2d-api-manifest.json";
    private const string ExamplesRelativePath = "data/documentation/electron2d-doc-examples.json";
    private const string WikiGenerator = "eng/Electron2D.Build update wiki";

    private static readonly JsonSerializerOptions GeneratedIndexJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] RequiredAudiences =
    [
        "human",
        "ai",
        "cli",
        "ide",
        "wiki",
        "inspector",
        "generator"
    ];

    private static readonly string[] RequiredCommands =
    [
        "docs search",
        "docs type",
        "docs member",
        "docs example"
    ];

    private static readonly string[] RequiredSources =
    [
        "apiManifest",
        "documentation",
        "examples",
        "wiki"
    ];

    private static readonly string[] RequiredEntryIds =
    [
        "api-type:Electron2D.CharacterBody2D",
        "api-member:Electron2D.CharacterBody2D.MoveAndSlide",
        "doc:architecture.agent-native-workflow",
        "example:platformer-movement"
    ];

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var localDocumentationResult = await RunLocalDocumentationCommandAsync(cancellationToken).ConfigureAwait(false);
        var validationDiagnostics = VerifyIndex();
        foreach (var diagnostic in validationDiagnostics)
        {
            diagnostics.Write(diagnostic);
        }

        return localDocumentationResult != RepositoryBuildExitCodes.Success ||
            validationDiagnostics.Any(diagnostic => string.Equals(diagnostic.Severity, "error", StringComparison.Ordinal))
            ? RepositoryBuildExitCodes.Failed
            : RepositoryBuildExitCodes.Success;
    }

    private async Task<int> RunLocalDocumentationCommandAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGenerateIndex("verify docs", out var generatedIndex))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, IndexRelativePath, out var indexPath) ||
            !File.Exists(indexPath) ||
            !GeneratedIndexMatches(generatedIndex, indexPath, "verify docs"))
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify docs",
                "error",
                "E2D-BUILD-DOCS-LOCAL-CHECK-FAILED",
                "Local documentation index does not match the C# generated index.",
                Path: IndexRelativePath));
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify docs",
            "info",
            "E2D-BUILD-DOCS-LOCAL-CHECK-PASSED",
            "Local documentation index matches the C# generated index.",
            Path: IndexRelativePath));
        return RepositoryBuildExitCodes.Success;
    }

    public async Task<int> RunGeneratedIndexCommandAsync(bool check, CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var step = check ? "update docs --check" : "update docs";
        if (!TryGenerateIndex(step, out var generatedIndex))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, IndexRelativePath, out var indexPath))
        {
            diagnostics.Write(new BuildDiagnostic(
                "update",
                step,
                "error",
                "E2D-BUILD-DOCS-INDEX-PATH",
                $"Local documentation index path could not be resolved: {IndexRelativePath}.",
                Path: IndexRelativePath));
            return RepositoryBuildExitCodes.Failed;
        }

        if (check)
        {
            if (!File.Exists(indexPath) || !GeneratedIndexMatches(generatedIndex, indexPath, step))
            {
                diagnostics.Write(new BuildDiagnostic(
                    "update",
                    step,
                    "error",
                    "E2D-BUILD-DOCS-INDEX-CHECK-FAILED",
                    "Generated local documentation index is out of date or invalid.",
                    Path: IndexRelativePath));
                return RepositoryBuildExitCodes.Failed;
            }
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            File.WriteAllText(indexPath, generatedIndex, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        diagnostics.Write(new BuildDiagnostic(
            "update",
            step,
            "info",
            check ? "E2D-BUILD-DOCS-INDEX-CHECK-PASSED" : "E2D-BUILD-DOCS-INDEX-UPDATED",
            check
                ? "Generated local documentation index is synchronized with its sources."
                : "Generated local documentation index was updated from its sources.",
            Path: IndexRelativePath));
        return RepositoryBuildExitCodes.Success;
    }

    private bool TryGenerateIndex(string step, out string generatedIndex)
    {
        generatedIndex = string.Empty;
        try
        {
            if (!TryResolveRequiredFile(ApiManifestRelativePath, step, out var apiManifestPath) ||
                !TryResolveRequiredFile(ExamplesRelativePath, step, out var examplesPath))
            {
                return false;
            }

            var docsRoot = Path.Combine(repositoryRoot, "docs");
            if (!Directory.Exists(docsRoot))
            {
                diagnostics.Write(new BuildDiagnostic(
                    DiagnosticCommand(step),
                    step,
                    "error",
                    "E2D-BUILD-DOCS-INDEX-SOURCE-MISSING",
                    "Documentation source directory was not found: docs.",
                    Path: "docs"));
                return false;
            }

            using var apiManifest = JsonDocument.Parse(File.ReadAllText(apiManifestPath, Encoding.UTF8));
            using var examples = JsonDocument.Parse(File.ReadAllText(examplesPath, Encoding.UTF8));
            var documentationFiles = Directory
                .EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var documentationHashRecords = documentationFiles.Select(NewHashRecord).ToArray();

            var entries = new List<JsonObject>();
            foreach (var type in EnumerateArray(apiManifest.RootElement, "types")
                .OrderBy(type => RequiredString(type, "fullName"), StringComparer.Ordinal))
            {
                entries.Add(NewTypeEntry(type));

                var duplicateKeys = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var member in EnumerateArray(type, "members")
                    .OrderBy(member => RequiredString(member, "name"), StringComparer.Ordinal)
                    .ThenBy(member => RequiredString(member, "id"), StringComparer.Ordinal))
                {
                    var key = $"{RequiredString(member, "declaringType")}.{RequiredString(member, "name")}";
                    duplicateKeys.TryGetValue(key, out var duplicateIndex);
                    duplicateKeys[key] = duplicateIndex + 1;
                    entries.Add(NewMemberEntry(type, member, duplicateIndex));
                }
            }

            foreach (var documentationFile in documentationFiles)
            {
                entries.Add(NewDocumentationEntry(documentationFile));
            }

            foreach (var example in EnumerateArray(examples.RootElement, "examples")
                .OrderBy(example => RequiredString(example, "id"), StringComparer.Ordinal))
            {
                entries.Add(NewExampleEntry(example));
            }

            var root = new JsonObject
            {
                ["schemaVersion"] = 1,
                ["manifestVersion"] = "0.1.0-preview",
                ["generatedFrom"] = new JsonObject
                {
                    ["apiManifest"] = HashRecordJson(NewHashRecord(apiManifestPath)),
                    ["documentation"] = JsonArrayFrom(documentationHashRecords.Select(HashRecordJson)),
                    ["examples"] = HashRecordJson(NewHashRecord(examplesPath))
                },
                ["audiences"] = StringArray(["human", "ai", "cli", "ide", "wiki", "inspector", "generator"]),
                ["commands"] = new JsonArray
                {
                    NewCommand("docs search", "Searches local API, documentation and examples index."),
                    NewCommand("docs type", "Returns a public API type from the API manifest."),
                    NewCommand("docs member", "Returns a public API member from the API manifest."),
                    NewCommand("docs example", "Returns a local documentation example.")
                },
                ["sources"] = new JsonObject
                {
                    ["apiManifest"] = new JsonObject
                    {
                        ["path"] = ApiManifestRelativePath,
                        ["contract"] = "Public API metadata generated from compiled assembly, XML documentation and GitHub Wiki compatibility table."
                    },
                    ["documentation"] = new JsonObject
                    {
                        ["paths"] = StringArray(documentationHashRecords.Select(record => record.Path)),
                        ["contract"] = "Current implementation documentation and Agent-native cross-platform 2D game engine architecture notes."
                    },
                    ["examples"] = new JsonObject
                    {
                        ["path"] = ExamplesRelativePath,
                        ["contract"] = "Curated local examples for CLI and AI agents."
                    },
                    ["wiki"] = new JsonObject
                    {
                        ["generator"] = WikiGenerator,
                        ["compatibilityPage"] = ".github/wiki/API-Compatibility.md"
                    }
                },
                ["entries"] = JsonArrayFrom(entries
                    .OrderBy(entry => entry["id"]?.GetValue<string>() ?? string.Empty, StringComparer.Ordinal)
                    .ToArray())
            };

            generatedIndex = Normalize(JsonSerializer.Serialize(root, GeneratedIndexJsonOptions)).TrimEnd() + "\n";
            return CanParseJson(generatedIndex, step, "Generated local documentation index is not valid JSON.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-INDEX-GENERATE-FAILED",
                $"Generated local documentation index could not be created: {ex.Message}.",
                Path: IndexRelativePath));
            return false;
        }
    }

    private bool TryResolveRequiredFile(string relativePath, string step, out string fullPath)
    {
        if (RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, relativePath, out fullPath) &&
            File.Exists(fullPath))
        {
            return true;
        }

        diagnostics.Write(new BuildDiagnostic(
            DiagnosticCommand(step),
            step,
            "error",
            "E2D-BUILD-DOCS-INDEX-SOURCE-MISSING",
            $"Required documentation source file was not found: {relativePath}.",
            Path: relativePath));
        fullPath = string.Empty;
        return false;
    }

    private DocumentationHashRecord NewHashRecord(string fullPath)
    {
        return new DocumentationHashRecord(GetRelativeUnixPath(fullPath), ComputeNormalizedSha256(fullPath));
    }

    private static JsonObject HashRecordJson(DocumentationHashRecord record)
    {
        return new JsonObject
        {
            ["path"] = record.Path,
            ["sha256"] = record.Sha256
        };
    }

    private JsonObject NewDocumentationEntry(string fullPath)
    {
        var relativePath = GetRelativeUnixPath(fullPath);
        var text = File.ReadAllText(fullPath, Encoding.UTF8);
        var stem = relativePath.StartsWith("docs/", StringComparison.Ordinal)
            ? relativePath["docs/".Length..]
            : relativePath;
        if (stem.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            stem = stem[..^".md".Length];
        }

        var id = string.Equals(relativePath, "docs/architecture/agent-native-workflow.md", StringComparison.Ordinal)
            ? "doc:architecture.agent-native-workflow"
            : "doc:" + stem.Replace('/', '.');
        var title = MarkdownTitle(text, stem);
        var summary = MarkdownSummary(text);
        return new JsonObject
        {
            ["id"] = id,
            ["kind"] = "documentation",
            ["title"] = title,
            ["summary"] = summary,
            ["keywords"] = StringArray(SplitSearchWords([title, summary, relativePath])),
            ["sourcePath"] = relativePath,
            ["sourceId"] = id,
            ["audiences"] = StringArray(["human", "ai", "cli", "wiki"])
        };
    }

    private static JsonObject NewTypeEntry(JsonElement type)
    {
        var fullName = RequiredString(type, "fullName");
        var name = RequiredString(type, "name");
        var category = RequiredString(type, "category");
        var summary = RequiredString(type, "summary");
        return new JsonObject
        {
            ["id"] = "api-type:" + fullName,
            ["kind"] = "api-type",
            ["title"] = name,
            ["summary"] = summary,
            ["keywords"] = StringArray(SplitSearchWords([fullName, name, category, summary])),
            ["sourcePath"] = ApiManifestRelativePath,
            ["apiId"] = RequiredString(type, "id"),
            ["audiences"] = StringArray(["ai", "cli", "ide", "wiki", "inspector", "generator"])
        };
    }

    private static JsonObject NewMemberEntry(JsonElement type, JsonElement member, int duplicateIndex)
    {
        var declaringType = RequiredString(member, "declaringType");
        var memberName = RequiredString(member, "name");
        var baseId = $"api-member:{declaringType}.{memberName}";
        var title = $"{RequiredString(type, "name")}.{memberName}";
        var signature = RequiredString(member, "signature");
        var summary = RequiredString(member, "summary");
        return new JsonObject
        {
            ["id"] = duplicateIndex == 0 ? baseId : $"{baseId}#{duplicateIndex}",
            ["kind"] = "api-member",
            ["title"] = title,
            ["summary"] = summary,
            ["keywords"] = StringArray(SplitSearchWords([title, memberName, signature, summary, RequiredString(type, "category")])),
            ["sourcePath"] = ApiManifestRelativePath,
            ["apiId"] = RequiredString(member, "id"),
            ["audiences"] = StringArray(["ai", "cli", "ide", "wiki", "inspector", "generator"])
        };
    }

    private static JsonObject NewExampleEntry(JsonElement example)
    {
        var id = RequiredString(example, "id");
        var title = RequiredString(example, "title");
        var summary = RequiredString(example, "summary");
        var keywordSources = new List<string> { id, title, summary };
        keywordSources.AddRange(EnumerateStringArray(example, "keywords"));
        return new JsonObject
        {
            ["id"] = id,
            ["kind"] = "example",
            ["title"] = title,
            ["summary"] = summary,
            ["keywords"] = StringArray(SplitSearchWords(keywordSources)),
            ["sourcePath"] = ExamplesRelativePath,
            ["sourceId"] = id,
            ["apiIds"] = StringArray(EnumerateStringArray(example, "apiIds")),
            ["code"] = RequiredString(example, "code"),
            ["audiences"] = StringArray(["human", "ai", "cli", "ide"])
        };
    }

    private static JsonObject NewCommand(string name, string description)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["formats"] = StringArray(["text", "json"])
        };
    }

    private string GetRelativeUnixPath(string fullPath)
    {
        return Path.GetRelativePath(repositoryRoot, fullPath).Replace('\\', '/');
    }

    private static string MarkdownTitle(string text, string fallback)
    {
        var match = Regex.Match(text, @"(?m)^#\s+(.+)$", RegexOptions.CultureInvariant);
        return match.Success ? NormalizeWhitespace(match.Groups[1].Value) : fallback;
    }

    private static string MarkdownSummary(string text)
    {
        var withoutCodeBlocks = Regex.Replace(text, "(?s)```.*?```", " ", RegexOptions.CultureInvariant);
        foreach (var line in Regex.Split(withoutCodeBlocks, "\r?\n"))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.StartsWith('#') ||
                trimmed.StartsWith("<!--", StringComparison.Ordinal) ||
                trimmed.StartsWith('|'))
            {
                continue;
            }

            return NormalizeWhitespace(trimmed);
        }

        return string.Empty;
    }

    private static IEnumerable<string> SplitSearchWords(IEnumerable<string> values)
    {
        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            foreach (var word in SplitSearchWords(value))
            {
                words.Add(word);
            }
        }

        return words.OrderBy(word => word, StringComparer.Ordinal);
    }

    private static IEnumerable<string> SplitSearchWords(string value)
    {
        var words = new List<string>();
        foreach (Match match in Regex.Matches(value, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\\d+", RegexOptions.CultureInvariant))
        {
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                words.Add(match.Value.ToLowerInvariant());
            }
        }

        foreach (Match match in Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]+", RegexOptions.CultureInvariant))
        {
            if (!words.Contains(match.Value, StringComparer.Ordinal))
            {
                words.Add(match.Value);
            }
        }

        return words
            .Distinct(StringComparer.Ordinal)
            .OrderBy(word => word, StringComparer.Ordinal);
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array
            ? property.EnumerateArray()
            : [];
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static IEnumerable<string> EnumerateStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private static JsonArray StringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static JsonArray JsonArrayFrom(IEnumerable<JsonObject> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private sealed record DocumentationHashRecord(string Path, string Sha256);

    private bool GeneratedIndexMatches(string generatedIndex, string indexPath, string step)
    {
        try
        {
            var expected = ComparableJson(generatedIndex);
            var actual = ComparableJson(File.ReadAllText(indexPath, Encoding.UTF8));
            return string.Equals(expected, actual, StringComparison.Ordinal);
        }
        catch (JsonException ex)
        {
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-INDEX-INVALID-JSON",
                $"Local documentation index is not valid JSON: {ex.Message}.",
                Path: IndexRelativePath));
            return false;
        }
    }

    private bool CanParseJson(string text, string step, string message)
    {
        try
        {
            using var _ = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException ex)
        {
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-INDEX-INVALID-JSON",
                $"{message} {ex.Message}.",
                Path: IndexRelativePath));
            return false;
        }
    }

    private static string ComparableJson(string text)
    {
        using var document = JsonDocument.Parse(text);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static string DiagnosticCommand(string step)
    {
        return step.StartsWith("verify", StringComparison.Ordinal) ? "verify" : "update";
    }

    private IReadOnlyList<BuildDiagnostic> VerifyIndex()
    {
        var result = new List<BuildDiagnostic>();
        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, IndexRelativePath, out var indexPath) ||
            !File.Exists(indexPath))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-MISSING", "Local documentation index was not found.", IndexRelativePath));
            return result;
        }

        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, ApiManifestRelativePath, out var apiManifestPath) ||
            !File.Exists(apiManifestPath))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-API-MANIFEST-MISSING", "API manifest required by local documentation index was not found.", ApiManifestRelativePath));
            return result;
        }

        JsonDocument indexDocument;
        try
        {
            indexDocument = JsonDocument.Parse(File.ReadAllText(indexPath, Encoding.UTF8));
        }
        catch (JsonException ex)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-INVALID-JSON", $"Local documentation index is not valid JSON: {ex.Message}.", IndexRelativePath));
            return result;
        }

        using (indexDocument)
        {
            var root = indexDocument.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index root must be a JSON object.", IndexRelativePath));
                return result;
            }

            VerifyRootMetadata(root, result);
            VerifyGeneratedFrom(root, result);
            VerifyAudiences(root, result);
            VerifyCommands(root, result);
            VerifySources(root, result);
            VerifyEntries(root, LoadApiManifestIds(apiManifestPath, result), result);
        }

        if (result.Count == 0)
        {
            result.Add(new BuildDiagnostic(
                "verify",
                "verify docs",
                "info",
                "E2D-BUILD-DOCS-VERIFY-PASSED",
                "Local documentation index schema, generated source metadata, commands and API references are valid.",
                Path: IndexRelativePath));
        }

        return result;
    }

    private static void VerifyRootMetadata(JsonElement root, List<BuildDiagnostic> result)
    {
        if (!TryGetInt32(root, "schemaVersion", out var schemaVersion) || schemaVersion != 1)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index schemaVersion must be 1.", IndexRelativePath));
        }

        if (!TryGetString(root, "manifestVersion", out var manifestVersion) ||
            !string.Equals(manifestVersion, "0.1.0-preview", StringComparison.Ordinal))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index manifestVersion must be 0.1.0-preview.", IndexRelativePath));
        }
    }

    private void VerifyGeneratedFrom(JsonElement root, List<BuildDiagnostic> result)
    {
        if (!TryGetObject(root, "generatedFrom", out var generatedFrom))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index is missing generatedFrom metadata.", IndexRelativePath));
            return;
        }

        VerifyHashRecord(generatedFrom, "apiManifest", ApiManifestRelativePath, result);
        VerifyHashRecord(generatedFrom, "examples", ExamplesRelativePath, result);

        if (!generatedFrom.TryGetProperty("documentation", out var documentation) || documentation.ValueKind != JsonValueKind.Array)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index generatedFrom.documentation must be an array.", IndexRelativePath));
            return;
        }

        foreach (var record in documentation.EnumerateArray())
        {
            VerifyHashRecord(record, expectedPath: null, result);
        }
    }

    private void VerifyHashRecord(JsonElement parent, string propertyName, string expectedPath, List<BuildDiagnostic> result)
    {
        if (!parent.TryGetProperty(propertyName, out var record) || record.ValueKind != JsonValueKind.Object)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", $"Local documentation index generatedFrom.{propertyName} must be an object.", IndexRelativePath));
            return;
        }

        VerifyHashRecord(record, expectedPath, result);
    }

    private void VerifyHashRecord(JsonElement record, string? expectedPath, List<BuildDiagnostic> result)
    {
        if (!TryGetString(record, "path", out var relativePath) ||
            !TryGetString(record, "sha256", out var actualHash) ||
            string.IsNullOrWhiteSpace(actualHash))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index generatedFrom record must contain path and sha256.", IndexRelativePath));
            return;
        }

        if (expectedPath is not null && !string.Equals(relativePath, expectedPath, StringComparison.Ordinal))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCE", $"Local documentation index source path mismatch: expected {expectedPath}, got {relativePath}.", IndexRelativePath));
        }

        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, relativePath, out var fullPath) || !File.Exists(fullPath))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCE-MISSING", $"Local documentation index source file was not found: {relativePath}.", relativePath));
            return;
        }

        var expectedHash = ComputeNormalizedSha256(fullPath);
        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCE-HASH", $"Local documentation index source hash is stale for {relativePath}.", relativePath));
        }
    }

    private static void VerifyAudiences(JsonElement root, List<BuildDiagnostic> result)
    {
        if (!TryGetStringArray(root, "audiences", out var audiences))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-AUDIENCES", "Local documentation index is missing audiences metadata.", IndexRelativePath));
            return;
        }

        foreach (var audience in RequiredAudiences)
        {
            if (!audiences.Contains(audience, StringComparer.Ordinal))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-AUDIENCES", $"Local documentation index is missing audience: {audience}.", IndexRelativePath));
            }
        }
    }

    private static void VerifyCommands(JsonElement root, List<BuildDiagnostic> result)
    {
        if (!root.TryGetProperty("commands", out var commands) || commands.ValueKind != JsonValueKind.Array)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-COMMANDS", "Local documentation index is missing commands metadata.", IndexRelativePath));
            return;
        }

        var commandMap = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var command in commands.EnumerateArray())
        {
            if (TryGetString(command, "name", out var name))
            {
                commandMap[name] = command;
            }
        }

        foreach (var requiredCommand in RequiredCommands)
        {
            if (!commandMap.TryGetValue(requiredCommand, out var command))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-COMMANDS", $"Local documentation index is missing command metadata: {requiredCommand}.", IndexRelativePath));
                continue;
            }

            if (!TryGetString(command, "description", out _) ||
                !TryGetStringArray(command, "formats", out var formats) ||
                !formats.Contains("text", StringComparer.Ordinal) ||
                !formats.Contains("json", StringComparer.Ordinal))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-COMMANDS", $"Local documentation command metadata is incomplete: {requiredCommand}.", IndexRelativePath));
            }
        }
    }

    private void VerifySources(JsonElement root, List<BuildDiagnostic> result)
    {
        if (!TryGetObject(root, "sources", out var sources))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", "Local documentation index is missing sources metadata.", IndexRelativePath));
            return;
        }

        foreach (var source in RequiredSources)
        {
            if (!sources.TryGetProperty(source, out _))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", $"Local documentation index is missing source metadata: {source}.", IndexRelativePath));
            }
        }

        VerifySourcePath(sources, "apiManifest", "path", ApiManifestRelativePath, result);
        VerifySourcePath(sources, "examples", "path", ExamplesRelativePath, result);

        if (sources.TryGetProperty("documentation", out var documentation) &&
            documentation.ValueKind == JsonValueKind.Object &&
            documentation.TryGetProperty("paths", out var paths) &&
            paths.ValueKind == JsonValueKind.Array)
        {
            foreach (var path in paths.EnumerateArray())
            {
                if (path.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(path.GetString()))
                {
                    result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", "Local documentation source path must be a non-empty string.", IndexRelativePath));
                    continue;
                }

                VerifyExistingRelativePath(path.GetString()!, result);
            }
        }
        else
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", "Local documentation index sources.documentation.paths must be an array.", IndexRelativePath));
        }

        if (sources.TryGetProperty("wiki", out var wiki))
        {
            if (wiki.ValueKind != JsonValueKind.Object)
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", "Local documentation index sources.wiki must be an object.", IndexRelativePath));
                return;
            }

            if (!TryGetString(wiki, "generator", out var generator) ||
                !string.Equals(generator, WikiGenerator, StringComparison.Ordinal))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", $"Local documentation wiki source must reference {WikiGenerator}.", IndexRelativePath));
            }

            if (!TryGetString(wiki, "compatibilityPage", out var compatibilityPage) ||
                !string.Equals(compatibilityPage, ".github/wiki/API-Compatibility.md", StringComparison.Ordinal))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", "Local documentation wiki source must reference .github/wiki/API-Compatibility.md.", IndexRelativePath));
            }
        }
    }

    private void VerifySourcePath(JsonElement sources, string sourceName, string propertyName, string expectedPath, List<BuildDiagnostic> result)
    {
        if (!sources.TryGetProperty(sourceName, out var source) ||
            source.ValueKind != JsonValueKind.Object ||
            !TryGetString(source, propertyName, out var actualPath) ||
            !string.Equals(actualPath, expectedPath, StringComparison.Ordinal))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCES", $"Local documentation source '{sourceName}' must reference {expectedPath}.", IndexRelativePath));
            return;
        }

        VerifyExistingRelativePath(actualPath, result);
    }

    private void VerifyEntries(JsonElement root, HashSet<string> apiIds, List<BuildDiagnostic> result)
    {
        if (!root.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRIES", "Local documentation index is missing entries.", IndexRelativePath));
            return;
        }

        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        string? previousId = null;
        foreach (var entry in entries.EnumerateArray())
        {
            if (!TryGetString(entry, "id", out var id))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SCHEMA", "Local documentation entry is missing id.", IndexRelativePath));
                continue;
            }

            if (!entryIds.Add(id))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-DUPLICATE", $"Local documentation entry id is duplicated: {id}.", IndexRelativePath));
            }

            if (previousId is not null && string.CompareOrdinal(previousId, id) > 0)
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-ORDER", $"Local documentation entries are not sorted by id: {previousId} before {id}.", IndexRelativePath));
            }

            previousId = id;
            VerifyEntry(entry, id, apiIds, result);
        }

        foreach (var requiredEntryId in RequiredEntryIds)
        {
            if (!entryIds.Contains(requiredEntryId))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-MISSING", $"Local documentation index is missing required entry: {requiredEntryId}.", IndexRelativePath));
            }
        }
    }

    private void VerifyEntry(JsonElement entry, string id, HashSet<string> apiIds, List<BuildDiagnostic> result)
    {
        if (!TryGetString(entry, "kind", out var kind) ||
            kind is not ("api-type" or "api-member" or "documentation" or "example"))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SCHEMA", $"Local documentation entry has invalid kind: {id}.", IndexRelativePath));
            return;
        }

        if (!TryGetString(entry, "title", out _) ||
            !entry.TryGetProperty("summary", out var summary) ||
            summary.ValueKind != JsonValueKind.String ||
            !TryGetStringArray(entry, "keywords", out _) ||
            !TryGetStringArray(entry, "audiences", out _) ||
            !TryGetString(entry, "sourcePath", out var sourcePath))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SCHEMA", $"Local documentation entry has incomplete required metadata: {id}.", IndexRelativePath));
            return;
        }

        VerifyExistingRelativePath(sourcePath, result);

        if (kind is "api-type" or "api-member")
        {
            if (!TryGetString(entry, "apiId", out var apiId) || !apiIds.Contains(apiId))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-API-ID", $"Local documentation entry references missing API manifest id: {id}.", IndexRelativePath));
            }
        }
        else if (kind == "documentation")
        {
            if (!TryGetString(entry, "sourceId", out _))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SOURCE-ID", $"Documentation entry is missing sourceId: {id}.", IndexRelativePath));
            }
        }
        else if (kind == "example")
        {
            if (!string.Equals(sourcePath, ExamplesRelativePath, StringComparison.Ordinal) ||
                !TryGetString(entry, "sourceId", out _))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-EXAMPLE", $"Example entry must reference the local documentation examples source: {id}.", IndexRelativePath));
            }
        }
    }

    private void VerifyExistingRelativePath(string relativePath, List<BuildDiagnostic> result)
    {
        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, relativePath, out var fullPath) || !File.Exists(fullPath))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SOURCE-MISSING", $"Local documentation index references missing source file: {relativePath}.", relativePath));
        }
    }

    private static HashSet<string> LoadApiManifestIds(string apiManifestPath, List<BuildDiagnostic> result)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        JsonDocument apiManifest;
        try
        {
            apiManifest = JsonDocument.Parse(File.ReadAllText(apiManifestPath, Encoding.UTF8));
        }
        catch (JsonException ex)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-API-MANIFEST-INVALID-JSON", $"API manifest is not valid JSON: {ex.Message}.", ApiManifestRelativePath));
            return ids;
        }

        using (apiManifest)
        {
            if (!apiManifest.RootElement.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
            {
                result.Add(CreateError("E2D-BUILD-DOCS-API-MANIFEST-SCHEMA", "API manifest is missing types array.", ApiManifestRelativePath));
                return ids;
            }

            foreach (var type in types.EnumerateArray())
            {
                if (TryGetString(type, "id", out var typeId))
                {
                    ids.Add(typeId);
                }

                if (type.TryGetProperty("members", out var members) && members.ValueKind == JsonValueKind.Array)
                {
                    foreach (var member in members.EnumerateArray())
                    {
                        if (TryGetString(member, "id", out var memberId))
                        {
                            ids.Add(memberId);
                        }
                    }
                }
            }
        }

        return ids;
    }

    private static bool TryGetObject(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetInt32(JsonElement element, string propertyName, out int value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetStringArray(JsonElement element, string propertyName, out string[] values)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            values = [];
            return false;
        }

        var items = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                values = [];
                return false;
            }

            items.Add(item.GetString()!);
        }

        values = items.ToArray();
        return true;
    }

    private static string ComputeNormalizedSha256(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static BuildDiagnostic CreateError(string code, string message, string path)
    {
        return new BuildDiagnostic(
            "verify",
            "verify docs",
            "error",
            code,
            message,
            Path: path);
    }
}
