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
using Microsoft.Data.Sqlite;

namespace Electron2D.Build;

internal sealed class LocalDocumentationVerifier(
    string repositoryRoot,
    JsonDiagnosticSink diagnostics)
{
    private const string IndexRelativePath = "data/documentation/electron2d-local-docs-index.json";
    private const string ApiManifestRelativePath = "data/api/electron2d-api-manifest.json";
    private const string ExamplesRelativePath = "data/documentation/electron2d-doc-examples.json";
    private const string SqliteCacheRelativePath = "data/documentation/electron2d-local-docs-search.sqlite";
    private const string ApiTypesShardRelativePath = "data/documentation/local-docs-index/api-types.ndjson";
    private const string ApiMembersShardRelativePath = "data/documentation/local-docs-index/api-members.ndjson";
    private const string DocumentationShardRelativePath = "data/documentation/local-docs-index/documentation.ndjson";
    private const string ExamplesShardRelativePath = "data/documentation/local-docs-index/examples.ndjson";
    private const string WikiGenerator = "eng/Electron2D.Build update wiki";
    private const int ManifestSchemaVersion = 2;
    private const int SqliteCacheSchemaVersion = 1;
    private const string SqliteEntriesTable = "entries";
    private const string SqliteFtsTable = "entries_fts";

    private static readonly DocumentationShardDefinition[] RequiredShards =
    [
        new(ApiTypesShardRelativePath, "api-type"),
        new(ApiMembersShardRelativePath, "api-member"),
        new(DocumentationShardRelativePath, "documentation"),
        new(ExamplesShardRelativePath, "example")
    ];

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
            !GeneratedManifestMatches(generatedIndex.ManifestJson, indexPath, "verify docs") ||
            !GeneratedShardsMatch(generatedIndex.Shards, "verify docs", emitDiagnostics: false))
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify docs",
                "error",
                "E2D-BUILD-DOCS-LOCAL-CHECK-FAILED",
                    "Local documentation manifest or shards do not match the C# generated local documentation index.",
                    Path: IndexRelativePath));
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify docs",
            "info",
            "E2D-BUILD-DOCS-LOCAL-CHECK-PASSED",
            "Local documentation manifest and shards match the C# generated local documentation index.",
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
            var manifestMatches = File.Exists(indexPath) && GeneratedManifestMatches(generatedIndex.ManifestJson, indexPath, step);
            var shardsMatch = GeneratedShardsMatch(generatedIndex.Shards, step, emitDiagnostics: true);
            var sqliteValid = manifestMatches && shardsMatch && TryBuildAndValidateTemporarySqliteCache(generatedIndex, step, emitSuccessDiagnostic: false);
            if (!manifestMatches || !shardsMatch || !sqliteValid)
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
            File.WriteAllText(indexPath, generatedIndex.ManifestJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            WriteGeneratedShards(generatedIndex.Shards);
            if (!RefreshSqliteCache(generatedIndex, step))
            {
                return RepositoryBuildExitCodes.Failed;
            }
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

    private bool TryGenerateIndex(string step, out GeneratedDocumentationIndex generatedIndex)
    {
        generatedIndex = GeneratedDocumentationIndex.Empty;
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
                .Where(path => !IsSavedAuditVerdictDocumentationPath(path))
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

            var sortedEntries = entries
                .OrderBy(entry => entry["id"]?.GetValue<string>() ?? string.Empty, StringComparer.Ordinal)
                .ToArray();
            var shards = CreateGeneratedShards(sortedEntries);
            var sourceDigest = ComputeSourceDigest(
                NewHashRecord(apiManifestPath),
                documentationHashRecords,
                NewHashRecord(examplesPath),
                shards);

            var root = new JsonObject
            {
                ["schemaVersion"] = ManifestSchemaVersion,
                ["manifestVersion"] = "0.1-preview",
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
                ["shards"] = JsonArrayFrom(shards.Select(ShardRecordJson)),
                ["sqliteCache"] = new JsonObject
                {
                    ["path"] = SqliteCacheRelativePath,
                    ["schemaVersion"] = SqliteCacheSchemaVersion,
                    ["sourceDigest"] = sourceDigest,
                    ["entriesTable"] = SqliteEntriesTable,
                    ["ftsTable"] = SqliteFtsTable,
                    ["contract"] = "Generated local SQLite search cache; manifest and NDJSON shards remain canonical."
                }
            };

            var manifestJson = Normalize(JsonSerializer.Serialize(root, GeneratedIndexJsonOptions)).TrimEnd() + "\n";
            if (!CanParseJson(manifestJson, step, "Generated local documentation index manifest is not valid JSON."))
            {
                return false;
            }

            generatedIndex = new GeneratedDocumentationIndex(manifestJson, shards, sourceDigest, sortedEntries);
            return true;
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

    private bool IsSavedAuditVerdictDocumentationPath(string fullPath)
    {
        return GetRelativeUnixPath(fullPath).StartsWith("docs/verdicts/", StringComparison.Ordinal);
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

    private static IReadOnlyList<GeneratedDocumentationShard> CreateGeneratedShards(IReadOnlyList<JsonObject> entries)
    {
        var shards = new List<GeneratedDocumentationShard>();
        foreach (var definition in RequiredShards)
        {
            var shardEntries = entries
                .Where(entry => string.Equals(entry["kind"]?.GetValue<string>(), definition.Kind, StringComparison.Ordinal))
                .OrderBy(entry => entry["id"]?.GetValue<string>() ?? string.Empty, StringComparer.Ordinal)
                .ToArray();
            var builder = new StringBuilder();
            foreach (var entry in shardEntries)
            {
                builder.Append(Normalize(entry.ToJsonString()));
                builder.Append('\n');
            }

            var content = builder.ToString();
            shards.Add(new GeneratedDocumentationShard(
                definition.Path,
                definition.Kind,
                content,
                shardEntries.Length,
                ComputeNormalizedTextSha256(content)));
        }

        return shards;
    }

    private static JsonObject ShardRecordJson(GeneratedDocumentationShard shard)
    {
        return new JsonObject
        {
            ["path"] = shard.Path,
            ["kind"] = shard.Kind,
            ["count"] = shard.Count,
            ["sha256"] = shard.Sha256
        };
    }

    private static string ComputeSourceDigest(
        DocumentationHashRecord apiManifest,
        IReadOnlyList<DocumentationHashRecord> documentationRecords,
        DocumentationHashRecord examples,
        IReadOnlyList<GeneratedDocumentationShard> shards)
    {
        var builder = new StringBuilder();
        AppendDigestRecord(builder, "apiManifest", apiManifest.Path, apiManifest.Sha256);
        foreach (var record in documentationRecords.OrderBy(record => record.Path, StringComparer.Ordinal))
        {
            AppendDigestRecord(builder, "documentation", record.Path, record.Sha256);
        }

        AppendDigestRecord(builder, "examples", examples.Path, examples.Sha256);
        foreach (var shard in shards.OrderBy(shard => shard.Path, StringComparer.Ordinal))
        {
            builder
                .Append("shard")
                .Append('\t')
                .Append(shard.Path)
                .Append('\t')
                .Append(shard.Kind)
                .Append('\t')
                .Append(shard.Count)
                .Append('\t')
                .Append(shard.Sha256)
                .Append('\n');
        }

        return ComputeNormalizedTextSha256(builder.ToString());
    }

    private static void AppendDigestRecord(StringBuilder builder, string group, string path, string sha256)
    {
        builder
            .Append(group)
            .Append('\t')
            .Append(path)
            .Append('\t')
            .Append(sha256)
            .Append('\n');
    }

    private sealed record DocumentationHashRecord(string Path, string Sha256);

    private sealed record DocumentationShardDefinition(string Path, string Kind);

    private sealed record GeneratedDocumentationShard(string Path, string Kind, string Content, int Count, string Sha256);

    private sealed record GeneratedDocumentationIndex(
        string ManifestJson,
        IReadOnlyList<GeneratedDocumentationShard> Shards,
        string SourceDigest,
        IReadOnlyList<JsonObject> Entries)
    {
        public static GeneratedDocumentationIndex Empty { get; } = new(string.Empty, [], string.Empty, []);
    }

    private bool GeneratedManifestMatches(string generatedIndex, string indexPath, string step)
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

    private bool GeneratedShardsMatch(IReadOnlyList<GeneratedDocumentationShard> generatedShards, string step, bool emitDiagnostics)
    {
        var matches = true;
        foreach (var shard in generatedShards)
        {
            if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, shard.Path, out var shardPath) ||
                !File.Exists(shardPath))
            {
                matches = false;
                if (emitDiagnostics)
                {
                    diagnostics.Write(new BuildDiagnostic(
                        DiagnosticCommand(step),
                        step,
                        "error",
                        "E2D-BUILD-DOCS-SHARD-CHECK-FAILED",
                        $"Generated local documentation shard is missing: {shard.Path}.",
                        Path: shard.Path));
                }

                continue;
            }

            var actual = File.ReadAllText(shardPath, Encoding.UTF8);
            if (!string.Equals(actual, shard.Content, StringComparison.Ordinal))
            {
                matches = false;
                if (emitDiagnostics)
                {
                    diagnostics.Write(new BuildDiagnostic(
                        DiagnosticCommand(step),
                        step,
                        "error",
                        "E2D-BUILD-DOCS-SHARD-CHECK-FAILED",
                        $"Generated local documentation shard is out of date: {shard.Path}.",
                        Path: shard.Path));
                }
            }
        }

        return matches;
    }

    private void WriteGeneratedShards(IReadOnlyList<GeneratedDocumentationShard> generatedShards)
    {
        foreach (var shard in generatedShards)
        {
            if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, shard.Path, out var shardPath))
            {
                throw new IOException($"Local documentation shard path could not be resolved: {shard.Path}.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(shardPath)!);
            File.WriteAllText(shardPath, shard.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private bool TryBuildAndValidateTemporarySqliteCache(
        GeneratedDocumentationIndex generatedIndex,
        string step,
        bool emitSuccessDiagnostic)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "Electron2D-local-docs-cache", Guid.NewGuid().ToString("N"));
        var tempPath = Path.Combine(tempRoot, "electron2d-local-docs-search.sqlite");
        try
        {
            Directory.CreateDirectory(tempRoot);
            BuildSqliteCache(generatedIndex, tempPath);
            if (!ValidateSqliteCache(generatedIndex, tempPath, step))
            {
                return false;
            }

            if (emitSuccessDiagnostic)
            {
                diagnostics.Write(new BuildDiagnostic(
                    DiagnosticCommand(step),
                    step,
                    "info",
                    "E2D-BUILD-DOCS-SQLITE-CACHE-PASSED",
                    "Temporary SQLite local documentation search cache is valid.",
                    Path: SqliteCacheRelativePath));
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException or InvalidOperationException)
        {
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                $"SQLite local documentation search cache could not be built: {ex.Message}.",
                Path: SqliteCacheRelativePath));
            return false;
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private bool RefreshSqliteCache(GeneratedDocumentationIndex generatedIndex, string step)
    {
        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, SqliteCacheRelativePath, out var cachePath))
        {
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-SQLITE-CACHE-PATH",
                $"SQLite local documentation search cache path could not be resolved: {SqliteCacheRelativePath}.",
                Path: SqliteCacheRelativePath));
            return false;
        }

        var directory = Path.GetDirectoryName(cachePath)!;
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(cachePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(directory);
            BuildSqliteCache(generatedIndex, tempPath);
            if (!ValidateSqliteCache(generatedIndex, tempPath, step))
            {
                TryDeleteFile(tempPath);
                return false;
            }

            File.Move(tempPath, cachePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException or InvalidOperationException)
        {
            TryDeleteFile(tempPath);
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                $"SQLite local documentation search cache could not be refreshed: {ex.Message}.",
                Path: SqliteCacheRelativePath));
            return false;
        }
    }

    private static void BuildSqliteCache(GeneratedDocumentationIndex generatedIndex, string sqlitePath)
    {
        TryDeleteFile(sqlitePath);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sqlitePath, Pooling = false }.ToString());
        connection.Open();
        ExecuteNonQuery(connection, "PRAGMA journal_mode=DELETE;");
        ExecuteNonQuery(connection, "CREATE TABLE metadata(key TEXT PRIMARY KEY, value TEXT NOT NULL);");
        ExecuteNonQuery(
            connection,
            """
            CREATE TABLE entries(
              rowid INTEGER PRIMARY KEY,
              id TEXT NOT NULL UNIQUE,
              kind TEXT NOT NULL,
              title TEXT NOT NULL,
              summary TEXT NOT NULL,
              source_path TEXT NOT NULL,
              api_id TEXT,
              source_id TEXT,
              payload_json TEXT NOT NULL
            );
            """);
        ExecuteNonQuery(connection, "CREATE VIRTUAL TABLE entries_fts USING fts5(id, title, summary, keywords);");

        using var transaction = connection.BeginTransaction();
        InsertMetadata(connection, transaction, "schemaVersion", SqliteCacheSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        InsertMetadata(connection, transaction, "manifestPath", IndexRelativePath);
        InsertMetadata(connection, transaction, "sourceDigest", generatedIndex.SourceDigest);
        InsertMetadata(connection, transaction, "manifestSha256", ComputeNormalizedTextSha256(generatedIndex.ManifestJson));
        foreach (var shard in generatedIndex.Shards)
        {
            InsertMetadata(connection, transaction, $"shard:{shard.Path}", $"{shard.Kind}|{shard.Count}|{shard.Sha256}");
        }

        var rowId = 1;
        foreach (var entry in generatedIndex.Entries)
        {
            InsertSqliteEntry(connection, transaction, rowId, entry);
            rowId++;
        }

        transaction.Commit();
    }

    private bool ValidateSqliteCache(GeneratedDocumentationIndex generatedIndex, string sqlitePath, string step)
    {
        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            connection.Open();

            var sourceDigest = ExecuteScalarString(connection, "SELECT value FROM metadata WHERE key = 'sourceDigest';");
            if (!string.Equals(sourceDigest, generatedIndex.SourceDigest, StringComparison.Ordinal))
            {
                diagnostics.Write(new BuildDiagnostic(
                    DiagnosticCommand(step),
                    step,
                    "error",
                    "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                    "SQLite local documentation search cache metadata sourceDigest is stale.",
                    Path: SqliteCacheRelativePath));
                return false;
            }

            var entryCount = ExecuteScalarInt64(connection, "SELECT COUNT(*) FROM entries;");
            if (entryCount != generatedIndex.Entries.Count)
            {
                diagnostics.Write(new BuildDiagnostic(
                    DiagnosticCommand(step),
                    step,
                    "error",
                    "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                    "SQLite local documentation search cache entry count does not match the generated shards.",
                    Path: SqliteCacheRelativePath));
                return false;
            }

            var matchCount = ExecuteScalarInt64(connection, "SELECT COUNT(*) FROM entries_fts WHERE entries_fts MATCH 'moveandslide';");
            if (matchCount <= 0)
            {
                diagnostics.Write(new BuildDiagnostic(
                    DiagnosticCommand(step),
                    step,
                    "error",
                    "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                    "SQLite local documentation search cache FTS surface did not find CharacterBody2D.MoveAndSlide.",
                    Path: SqliteCacheRelativePath));
                return false;
            }

            return true;
        }
        catch (SqliteException ex)
        {
            diagnostics.Write(new BuildDiagnostic(
                DiagnosticCommand(step),
                step,
                "error",
                "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                $"SQLite local documentation search cache validation failed: {ex.Message}.",
                Path: SqliteCacheRelativePath));
            return false;
        }
    }

    private static void InsertMetadata(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO metadata(key, value) VALUES ($key, $value);";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private static void InsertSqliteEntry(SqliteConnection connection, SqliteTransaction transaction, int rowId, JsonObject entry)
    {
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO entries(rowid, id, kind, title, summary, source_path, api_id, source_id, payload_json)
                VALUES ($rowid, $id, $kind, $title, $summary, $sourcePath, $apiId, $sourceId, $payloadJson);
                """;
            command.Parameters.AddWithValue("$rowid", rowId);
            command.Parameters.AddWithValue("$id", RequiredNodeString(entry, "id"));
            command.Parameters.AddWithValue("$kind", RequiredNodeString(entry, "kind"));
            command.Parameters.AddWithValue("$title", RequiredNodeString(entry, "title"));
            command.Parameters.AddWithValue("$summary", RequiredNodeString(entry, "summary"));
            command.Parameters.AddWithValue("$sourcePath", RequiredNodeString(entry, "sourcePath"));
            command.Parameters.AddWithValue("$apiId", OptionalNodeString(entry, "apiId") is { } apiId ? apiId : DBNull.Value);
            command.Parameters.AddWithValue("$sourceId", OptionalNodeString(entry, "sourceId") is { } sourceId ? sourceId : DBNull.Value);
            command.Parameters.AddWithValue("$payloadJson", entry.ToJsonString());
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO entries_fts(rowid, id, title, summary, keywords) VALUES ($rowid, $id, $title, $summary, $keywords);";
            command.Parameters.AddWithValue("$rowid", rowId);
            command.Parameters.AddWithValue("$id", RequiredNodeString(entry, "id"));
            command.Parameters.AddWithValue("$title", RequiredNodeString(entry, "title"));
            command.Parameters.AddWithValue("$summary", RequiredNodeString(entry, "summary"));
            command.Parameters.AddWithValue("$keywords", KeywordsText(entry));
            command.ExecuteNonQuery();
        }
    }

    private static string KeywordsText(JsonObject entry)
    {
        if (entry["keywords"] is not JsonArray keywords)
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            keywords
                .Select(keyword => keyword?.GetValue<string>())
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword)));
    }

    private static string RequiredNodeString(JsonObject entry, string propertyName)
    {
        return entry[propertyName]?.GetValue<string>() ?? string.Empty;
    }

    private static string? OptionalNodeString(JsonObject entry, string propertyName)
    {
        return entry[propertyName]?.GetValue<string>();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static string ExecuteScalarString(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return command.ExecuteScalar()?.ToString() ?? string.Empty;
    }

    private static long ExecuteScalarInt64(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
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
            var entries = VerifyShards(root, LoadApiManifestIds(apiManifestPath, result), result);
            VerifySqliteCacheMetadata(root, result);
            if (result.Count == 0)
            {
                var generatedIndex = new GeneratedDocumentationIndex(
                    File.ReadAllText(indexPath, Encoding.UTF8),
                    LoadManifestShards(root),
                    ReadSqliteCacheSourceDigest(root),
                    entries);
                if (!TryBuildAndValidateTemporarySqliteCache(generatedIndex, "verify docs", emitSuccessDiagnostic: true))
                {
                    result.Add(CreateError(
                        "E2D-BUILD-DOCS-SQLITE-CACHE-FAILED",
                        "Temporary SQLite local documentation search cache validation failed.",
                        SqliteCacheRelativePath));
                }
            }
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
        if (!TryGetInt32(root, "schemaVersion", out var schemaVersion) || schemaVersion != ManifestSchemaVersion)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index schemaVersion must be 2.", IndexRelativePath));
        }

        if (!TryGetString(root, "manifestVersion", out var manifestVersion) ||
            !string.Equals(manifestVersion, "0.1-preview", StringComparison.Ordinal))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index manifestVersion must be 0.1-preview.", IndexRelativePath));
        }

        if (root.TryGetProperty("entries", out _))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SCHEMA", "Local documentation index schemaVersion 2 must not contain root entries.", IndexRelativePath));
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

    private List<JsonObject> VerifyShards(JsonElement root, HashSet<string> apiIds, List<BuildDiagnostic> result)
    {
        var entries = new List<JsonObject>();
        if (!root.TryGetProperty("shards", out var shards) || shards.ValueKind != JsonValueKind.Array)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SHARDS", "Local documentation index is missing shards metadata.", IndexRelativePath));
            return entries;
        }

        var shardMap = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var shard in shards.EnumerateArray())
        {
            if (TryGetString(shard, "path", out var path))
            {
                shardMap[path] = shard;
            }
        }

        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in RequiredShards)
        {
            if (!shardMap.TryGetValue(definition.Path, out var shard))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SHARDS", $"Local documentation index is missing shard metadata: {definition.Path}.", IndexRelativePath));
                continue;
            }

            VerifyShard(definition, shard, apiIds, entryIds, entries, result);
        }

        foreach (var requiredEntryId in RequiredEntryIds)
        {
            if (!entryIds.Contains(requiredEntryId))
            {
                result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-MISSING", $"Local documentation index is missing required entry: {requiredEntryId}.", IndexRelativePath));
            }
        }

        return entries;
    }

    private void VerifyShard(
        DocumentationShardDefinition definition,
        JsonElement shard,
        HashSet<string> apiIds,
        HashSet<string> entryIds,
        List<JsonObject> entries,
        List<BuildDiagnostic> result)
    {
        if (!TryGetString(shard, "kind", out var kind) ||
            !string.Equals(kind, definition.Kind, StringComparison.Ordinal) ||
            !TryGetInt32(shard, "count", out var expectedCount) ||
            expectedCount < 0 ||
            !TryGetString(shard, "sha256", out var expectedSha256))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-INDEX-SHARDS", $"Local documentation shard metadata is incomplete: {definition.Path}.", IndexRelativePath));
            return;
        }

        if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, definition.Path, out var shardPath) ||
            !File.Exists(shardPath))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SHARD-MISSING", $"Local documentation shard file was not found: {definition.Path}.", definition.Path));
            return;
        }

        var bytes = File.ReadAllBytes(shardPath);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SHARD-ENCODING", $"Local documentation shard must be UTF-8 without BOM: {definition.Path}.", definition.Path));
        }

        var text = Encoding.UTF8.GetString(bytes);
        if (text.Contains('\r'))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SHARD-LINE-ENDINGS", $"Local documentation shard must use LF line endings: {definition.Path}.", definition.Path));
        }

        if (text.Length > 0 && !text.EndsWith('\n'))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SHARD-LINE-ENDINGS", $"Local documentation shard must end with LF: {definition.Path}.", definition.Path));
        }

        var actualSha256 = ComputeNormalizedSha256(shardPath);
        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SHARD-HASH", $"Local documentation shard hash is stale: {definition.Path}.", definition.Path));
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != expectedCount)
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SHARD-COUNT", $"Local documentation shard count is stale: {definition.Path}.", definition.Path));
        }

        string? previousId = null;
        foreach (var line in lines)
        {
            JsonDocument entryDocument;
            try
            {
                entryDocument = JsonDocument.Parse(line);
            }
            catch (JsonException ex)
            {
                result.Add(CreateError("E2D-BUILD-DOCS-SHARD-INVALID-JSON", $"Local documentation shard contains invalid JSON: {ex.Message}.", definition.Path));
                continue;
            }

            using (entryDocument)
            {
                var entry = entryDocument.RootElement;
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SCHEMA", $"Local documentation shard entry root must be an object: {definition.Path}.", definition.Path));
                    continue;
                }

                if (!TryGetString(entry, "id", out var id))
                {
                    result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SCHEMA", $"Local documentation entry is missing id: {definition.Path}.", definition.Path));
                    continue;
                }

                if (!entryIds.Add(id))
                {
                    result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-DUPLICATE", $"Local documentation entry id is duplicated: {id}.", definition.Path));
                }

                if (previousId is not null && string.CompareOrdinal(previousId, id) > 0)
                {
                    result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-ORDER", $"Local documentation shard entries are not sorted by id: {previousId} before {id}.", definition.Path));
                }

                previousId = id;
                VerifyEntry(entry, id, apiIds, result);
                if (TryGetString(entry, "kind", out var entryKind) &&
                    !string.Equals(entryKind, definition.Kind, StringComparison.Ordinal))
                {
                    result.Add(CreateError("E2D-BUILD-DOCS-INDEX-ENTRY-SCHEMA", $"Local documentation shard entry has wrong kind: {id}.", definition.Path));
                }

                entries.Add(JsonNode.Parse(line)?.AsObject() ?? new JsonObject());
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

    private static void VerifySqliteCacheMetadata(JsonElement root, List<BuildDiagnostic> result)
    {
        if (!TryGetObject(root, "sqliteCache", out var cache))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SQLITE-CACHE-METADATA", "Local documentation index is missing sqliteCache metadata.", IndexRelativePath));
            return;
        }

        if (!TryGetString(cache, "path", out var path) ||
            !string.Equals(path, SqliteCacheRelativePath, StringComparison.Ordinal) ||
            !TryGetInt32(cache, "schemaVersion", out var schemaVersion) ||
            schemaVersion != SqliteCacheSchemaVersion ||
            !TryGetString(cache, "entriesTable", out var entriesTable) ||
            !string.Equals(entriesTable, SqliteEntriesTable, StringComparison.Ordinal) ||
            !TryGetString(cache, "ftsTable", out var ftsTable) ||
            !string.Equals(ftsTable, SqliteFtsTable, StringComparison.Ordinal) ||
            !TryGetString(cache, "sourceDigest", out var sourceDigest))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SQLITE-CACHE-METADATA", "Local documentation sqliteCache metadata is incomplete.", IndexRelativePath));
            return;
        }

        var expectedDigest = ComputeSourceDigestFromManifest(root);
        if (!string.IsNullOrWhiteSpace(expectedDigest) &&
            !string.Equals(sourceDigest, expectedDigest, StringComparison.OrdinalIgnoreCase))
        {
            result.Add(CreateError("E2D-BUILD-DOCS-SQLITE-CACHE-METADATA", "Local documentation sqliteCache sourceDigest is stale.", IndexRelativePath));
        }
    }

    private static string ReadSqliteCacheSourceDigest(JsonElement root)
    {
        return root.TryGetProperty("sqliteCache", out var cache) && TryGetString(cache, "sourceDigest", out var sourceDigest)
            ? sourceDigest
            : string.Empty;
    }

    private IReadOnlyList<GeneratedDocumentationShard> LoadManifestShards(JsonElement root)
    {
        var shards = new List<GeneratedDocumentationShard>();
        if (!root.TryGetProperty("shards", out var shardArray) || shardArray.ValueKind != JsonValueKind.Array)
        {
            return shards;
        }

        foreach (var shard in shardArray.EnumerateArray())
        {
            if (!TryGetString(shard, "path", out var path) ||
                !TryGetString(shard, "kind", out var kind) ||
                !TryGetInt32(shard, "count", out var count) ||
                !TryGetString(shard, "sha256", out var sha256) ||
                !RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, path, out var fullPath) ||
                !File.Exists(fullPath))
            {
                continue;
            }

            shards.Add(new GeneratedDocumentationShard(path, kind, File.ReadAllText(fullPath, Encoding.UTF8), count, sha256));
        }

        return shards;
    }

    private static string ComputeSourceDigestFromManifest(JsonElement root)
    {
        if (!TryGetObject(root, "generatedFrom", out var generatedFrom) ||
            !TryReadHashRecord(generatedFrom, "apiManifest", out var apiManifest) ||
            !TryReadHashRecord(generatedFrom, "examples", out var examples) ||
            !generatedFrom.TryGetProperty("documentation", out var documentation) ||
            documentation.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("shards", out var shards) ||
            shards.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var documentationRecords = new List<DocumentationHashRecord>();
        foreach (var record in documentation.EnumerateArray())
        {
            if (TryReadHashRecord(record, out var documentationRecord))
            {
                documentationRecords.Add(documentationRecord);
            }
        }

        var generatedShards = new List<GeneratedDocumentationShard>();
        foreach (var shard in shards.EnumerateArray())
        {
            if (TryGetString(shard, "path", out var path) &&
                TryGetString(shard, "kind", out var kind) &&
                TryGetInt32(shard, "count", out var count) &&
                TryGetString(shard, "sha256", out var sha256))
            {
                generatedShards.Add(new GeneratedDocumentationShard(path, kind, string.Empty, count, sha256));
            }
        }

        return ComputeSourceDigest(apiManifest, documentationRecords, examples, generatedShards);
    }

    private static bool TryReadHashRecord(JsonElement parent, string propertyName, out DocumentationHashRecord record)
    {
        if (parent.TryGetProperty(propertyName, out var property))
        {
            return TryReadHashRecord(property, out record);
        }

        record = new DocumentationHashRecord(string.Empty, string.Empty);
        return false;
    }

    private static bool TryReadHashRecord(JsonElement element, out DocumentationHashRecord record)
    {
        if (TryGetString(element, "path", out var path) &&
            TryGetString(element, "sha256", out var sha256))
        {
            record = new DocumentationHashRecord(path, sha256);
            return true;
        }

        record = new DocumentationHashRecord(string.Empty, string.Empty);
        return false;
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
        return ComputeNormalizedTextSha256(text);
    }

    private static string ComputeNormalizedTextSha256(string text)
    {
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
