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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

try
{
    var exitCode = Electron2DCommandLine.Run(args, Console.Out, Console.Error);
    return exitCode;
}
finally
{
    Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();
}

internal static partial class Electron2DCommandLine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        return Run(args, output, error, CliExecutionContext.Default());
    }

    public static int Run(string[] args, TextWriter output, TextWriter error, CliExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            if (TryRunExportedPlayer(args, output, error, out var exportedPlayerExitCode))
            {
                return exportedPlayerExitCode;
            }

            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteRootHelp(output);
                return 0;
            }

            var group = args[0].ToLowerInvariant();
            return group == "docs"
                ? RunDocs(args.Skip(1).ToArray(), output)
                : RunGeneralCommand(group, args.Skip(1).ToArray(), output, error, context);
        }
        catch (CliCommandException exception)
        {
            return WriteCliError(exception, output, error);
        }
        catch (CommandLineException exception)
        {
            error.WriteLine(exception.Message);
            return 1;
        }
        catch (JsonException exception)
        {
            error.WriteLine("Local documentation data could not be read as JSON.");
            error.WriteLine(exception.Message);
            error.WriteLine("Run `dotnet run --project eng/Electron2D.Build -- verify docs`.");
            return 1;
        }
        catch (IOException exception)
        {
            error.WriteLine(exception.Message);
            error.WriteLine("Run `dotnet run --project eng/Electron2D.Build -- verify docs`.");
            return 1;
        }
    }

    private static int RunDocs(string[] args, TextWriter output)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            WriteDocsHelp(output);
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var parsed = ParsedArguments.Parse(args.Skip(1));
        if (parsed.Help)
        {
            WriteDocsHelp(output);
            return 0;
        }

        var store = LocalDocumentationStore.Load(FindRepositoryRoot());
        return command switch
        {
            "search" => RunSearch(store, parsed, output),
            "type" => RunType(store, parsed, output),
            "member" => RunMember(store, parsed, output),
            "example" => RunExample(store, parsed, output),
            _ => throw new CommandLineException("Unknown docs command. Use `e2d docs --help`.")
        };
    }

    private static int RunSearch(LocalDocumentationStore store, ParsedArguments args, TextWriter output)
    {
        var query = args.RequireQuery("docs search");
        var results = store.Search(query)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => Value(result.Entry, "id"), StringComparer.Ordinal)
            .Take(10)
            .Select(result => result.Entry)
            .ToArray();
        if (args.Format == OutputFormat.Json)
        {
            var json = new JsonObject
            {
                ["command"] = "docs search",
                ["query"] = query,
                ["sourcePath"] = LocalDocumentationStore.IndexPath,
                ["results"] = CloneArray(results)
            };
            output.WriteLine(json.ToJsonString(JsonOptions));
            return 0;
        }

        foreach (var result in results)
        {
            output.WriteLine($"{Value(result, "kind")} {Value(result, "title")} - {Value(result, "summary")} ({Value(result, "sourcePath")})");
        }

        return 0;
    }

    private static int RunType(LocalDocumentationStore store, ParsedArguments args, TextWriter output)
    {
        var query = args.RequireSingleValue("docs type");
        var type = store.FindType(query);
        if (type is null)
        {
            throw new CommandLineException($"API type was not found in the manifest: {query}");
        }

        if (args.Format == OutputFormat.Json)
        {
            var json = new JsonObject
            {
                ["command"] = "docs type",
                ["sourcePath"] = LocalDocumentationStore.ApiManifestPath,
                ["type"] = Clone(type)
            };
            output.WriteLine(json.ToJsonString(JsonOptions));
            return 0;
        }

        output.WriteLine($"{Value(type, "fullName")}");
        output.WriteLine(Value(type, "summary"));
        output.WriteLine($"Source: {LocalDocumentationStore.ApiManifestPath}");
        var members = type["members"]?.AsArray();
        output.WriteLine($"Members: {members?.Count ?? 0}");
        return 0;
    }

    private static int RunMember(LocalDocumentationStore store, ParsedArguments args, TextWriter output)
    {
        var query = args.RequireSingleValue("docs member");
        var result = store.FindMember(query);
        if (result is null)
        {
            throw new CommandLineException($"API member was not found in the manifest: {query}");
        }

        if (args.Format == OutputFormat.Json)
        {
            var json = new JsonObject
            {
                ["command"] = "docs member",
                ["sourcePath"] = LocalDocumentationStore.ApiManifestPath,
                ["type"] = Value(result.Type, "fullName"),
                ["member"] = Clone(result.Member)
            };
            output.WriteLine(json.ToJsonString(JsonOptions));
            return 0;
        }

        output.WriteLine($"{Value(result.Member, "signature")}");
        output.WriteLine(Value(result.Member, "summary"));
        output.WriteLine($"Source: {LocalDocumentationStore.ApiManifestPath}");
        return 0;
    }

    private static int RunExample(LocalDocumentationStore store, ParsedArguments args, TextWriter output)
    {
        var query = args.RequireQuery("docs example");
        var example = store.FindExample(query);
        if (example is null)
        {
            throw new CommandLineException($"Documentation example was not found: {query}");
        }

        if (args.Format == OutputFormat.Json)
        {
            var json = new JsonObject
            {
                ["command"] = "docs example",
                ["query"] = query,
                ["sourcePath"] = LocalDocumentationStore.IndexPath,
                ["example"] = Clone(example)
            };
            output.WriteLine(json.ToJsonString(JsonOptions));
            return 0;
        }

        output.WriteLine(Value(example, "title"));
        output.WriteLine(Value(example, "summary"));
        output.WriteLine();
        output.WriteLine(Value(example, "code"));
        output.WriteLine($"Source: {Value(example, "sourcePath")}");
        return 0;
    }

    private static JsonArray CloneArray(IEnumerable<JsonObject> objects)
    {
        var array = new JsonArray();
        foreach (var item in objects)
        {
            array.Add(Clone(item));
        }

        return array;
    }

    private static JsonNode Clone(JsonNode node)
    {
        return JsonNode.Parse(node.ToJsonString()) ?? throw new CommandLineException("Local documentation JSON node could not be cloned.");
    }

    private static string Value(JsonObject obj, string property)
    {
        return obj[property]?.GetValue<string>() ?? string.Empty;
    }

    private static bool IsHelp(string value)
    {
        return string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteRootHelp(TextWriter output)
    {
        output.WriteLine("Usage: e2d <command> [options]");
        output.WriteLine();
        output.WriteLine("Common options: --project <path> --format text|json|jsonl|sarif --quiet --verbose");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  project    Project create, inspect and validate commands.");
        output.WriteLine("  scene      Scene inspection and mutation commands.");
        output.WriteLine("  resource   Resource inspection and dependency commands.");
        output.WriteLine("  workspace  Generic workspace transaction commands.");
        output.WriteLine("  import     Queue import jobs.");
        output.WriteLine("  build      Queue build jobs.");
        output.WriteLine("  run        Queue run jobs, headless runs or runtime debug inspection.");
        output.WriteLine("  test       Queue test jobs or run scene tests.");
        output.WriteLine("  export     Queue export jobs.");
        output.WriteLine("  validate   Validate a project and emit diagnostics.");
        output.WriteLine("  docs       Search local documentation and API manifest.");
        output.WriteLine("  api        API comparison commands.");
        output.WriteLine("  mcp        MCP server commands.");
        output.WriteLine("  tasks      Project Tasks report commands.");
        output.WriteLine("  context    Static context pack commands.");
        output.WriteLine("  doctor     Environment diagnostics.");
    }

    private static void WriteDocsHelp(TextWriter output)
    {
        output.WriteLine("Usage: e2d docs <search|type|member|example> [query] [--format text|json]");
        output.WriteLine();
        output.WriteLine("Common options: --project <path> --format text|json --quiet --verbose");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  search   Search local API, documentation and examples.");
        output.WriteLine("  type     Show a public API type from the API manifest.");
        output.WriteLine("  member   Show a public API member from the API manifest.");
        output.WriteLine("  example  Show a local documentation example.");
    }

    private static string FindRepositoryRoot()
    {
        var environmentRoot = Environment.GetEnvironmentVariable("ELECTRON2D_DOCS_ROOT");
        if (!string.IsNullOrWhiteSpace(environmentRoot) && IsRepositoryRoot(environmentRoot))
        {
            return Path.GetFullPath(environmentRoot);
        }

        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (IsRepositoryRoot(directory.FullName))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new CommandLineException("Electron2D local documentation root was not found. Run the command from the repository root or set ELECTRON2D_DOCS_ROOT.");
    }

    private static bool IsRepositoryRoot(string path)
    {
        return File.Exists(Path.Combine(path, "data", "api", "electron2d-api-manifest.json"))
            && Directory.Exists(Path.Combine(path, "docs", "documentation"));
    }
}

internal sealed class LocalDocumentationStore
{
    public const string ApiManifestPath = "data/api/electron2d-api-manifest.json";
    public const string IndexPath = "data/documentation/electron2d-local-docs-index.json";
    public const string SqliteCachePath = "data/documentation/electron2d-local-docs-search.sqlite";

    private const int ManifestSchemaVersion = 2;
    private const int SqliteCacheSchemaVersion = 1;
    private const string SqliteEntriesTable = "entries";
    private const string SqliteFtsTable = "entries_fts";

    private static readonly ShardRequirement[] RequiredShards =
    [
        new("data/documentation/local-docs-index/api-types.ndjson", "api-type"),
        new("data/documentation/local-docs-index/api-members.ndjson", "api-member"),
        new("data/documentation/local-docs-index/documentation.ndjson", "documentation"),
        new("data/documentation/local-docs-index/examples.ndjson", "example")
    ];

    private readonly JsonObject apiManifest;
    private readonly JsonObject index;
    private readonly IReadOnlyList<JsonObject> entries;
    private readonly string? sqlitePath;

    private LocalDocumentationStore(
        string repositoryRoot,
        JsonObject apiManifest,
        JsonObject index,
        IReadOnlyList<JsonObject> entries,
        string? sqlitePath)
    {
        RepositoryRoot = repositoryRoot;
        this.apiManifest = apiManifest;
        this.index = index;
        this.entries = entries;
        this.sqlitePath = sqlitePath;
    }

    public string RepositoryRoot { get; }

    private JsonArray Types => apiManifest["types"]?.AsArray()
        ?? throw new CommandLineException("API manifest is missing `types`.");

    public static LocalDocumentationStore Load(string repositoryRoot)
    {
        var apiManifestPath = Path.Combine(repositoryRoot, ApiManifestPath.Replace('/', Path.DirectorySeparatorChar));
        var indexPath = Path.Combine(repositoryRoot, IndexPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(apiManifestPath))
        {
            throw new FileNotFoundException($"API manifest was not found: {apiManifestPath}");
        }

        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException($"Local documentation index was not found: {indexPath}");
        }

        var apiManifest = JsonNode.Parse(File.ReadAllText(apiManifestPath))?.AsObject()
            ?? throw new CommandLineException("API manifest root must be a JSON object.");
        var index = JsonNode.Parse(File.ReadAllText(indexPath))?.AsObject()
            ?? throw new CommandLineException("Local documentation index root must be a JSON object.");

        var shardRecords = ReadShardRecords(repositoryRoot, index);
        var cachePath = Path.Combine(repositoryRoot, SqliteCachePath.Replace('/', Path.DirectorySeparatorChar));
        var entries = TryLoadEntriesFromSqlite(cachePath, index, shardRecords, out var sqliteEntries)
            ? sqliteEntries
            : LoadEntriesFromShards(repositoryRoot, shardRecords);
        var sqlitePath = ReferenceEquals(entries, sqliteEntries) ? cachePath : null;

        return new LocalDocumentationStore(repositoryRoot, apiManifest, index, entries, sqlitePath);
    }

    public IEnumerable<SearchResult> Search(string query)
    {
        var searchTerms = SplitSearchTerms(query).ToArray();
        if (searchTerms.Length == 0)
        {
            throw new CommandLineException("Search query must not be empty.");
        }

        if (sqlitePath is not null && TrySearchSqlite(searchTerms, out var sqliteResults))
        {
            return ScoreEntries(sqliteResults, searchTerms);
        }

        return ScoreEntries(entries, searchTerms);
    }

    public JsonObject? FindType(string query)
    {
        var normalized = NormalizeTypeQuery(query);
        return Types
            .OfType<JsonObject>()
            .FirstOrDefault(type =>
                string.Equals(Value(type, "fullName"), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Value(type, "name"), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Value(type, "fullName"), "Electron2D." + normalized, StringComparison.OrdinalIgnoreCase));
    }

    public ApiMemberResult? FindMember(string query)
    {
        var trimmed = query.Trim();
        var separator = trimmed.LastIndexOf('.');
        string? typeQuery = null;
        var memberQuery = trimmed;
        if (separator > 0)
        {
            typeQuery = trimmed[..separator];
            memberQuery = trimmed[(separator + 1)..];
        }

        var candidateTypes = typeQuery is null
            ? Types.OfType<JsonObject>()
            : Types.OfType<JsonObject>().Where(type =>
                string.Equals(Value(type, "name"), NormalizeTypeQuery(typeQuery), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Value(type, "fullName"), NormalizeTypeQuery(typeQuery), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Value(type, "fullName"), "Electron2D." + NormalizeTypeQuery(typeQuery), StringComparison.OrdinalIgnoreCase));

        foreach (var type in candidateTypes)
        {
            var members = type["members"]?.AsArray();
            if (members is null)
            {
                continue;
            }

            foreach (var memberNode in members)
            {
                if (memberNode is JsonObject member &&
                    string.Equals(Value(member, "name"), memberQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiMemberResult(type, member);
                }
            }
        }

        return null;
    }

    public JsonObject? FindExample(string query)
    {
        var result = Search(query)
            .Where(result => string.Equals(Value(result.Entry, "kind"), "example", StringComparison.Ordinal))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => Value(result.Entry, "id"), StringComparer.Ordinal)
            .FirstOrDefault();
        return result?.Entry;
    }

    private static IReadOnlyList<ShardRecord> ReadShardRecords(string repositoryRoot, JsonObject index)
    {
        if (index["schemaVersion"]?.GetValue<int>() != ManifestSchemaVersion)
        {
            throw new CommandLineException("Local documentation index schemaVersion must be 2.");
        }

        if (index.ContainsKey("entries"))
        {
            throw new CommandLineException("Local documentation index schemaVersion 2 must not contain root `entries`.");
        }

        var shards = index["shards"]?.AsArray()
            ?? throw new CommandLineException("Local documentation index is missing `shards`.");
        var shardMap = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var node in shards)
        {
            if (node is JsonObject shard && !string.IsNullOrWhiteSpace(Value(shard, "path")))
            {
                shardMap[Value(shard, "path")] = shard;
            }
        }

        var records = new List<ShardRecord>();
        foreach (var required in RequiredShards)
        {
            if (!shardMap.TryGetValue(required.Path, out var shard))
            {
                throw new CommandLineException($"Local documentation index is missing shard metadata: {required.Path}");
            }

            if (!string.Equals(Value(shard, "kind"), required.Kind, StringComparison.Ordinal) ||
                shard["count"] is null ||
                !int.TryParse(shard["count"]!.ToJsonString(), out var count) ||
                count < 0 ||
                string.IsNullOrWhiteSpace(Value(shard, "sha256")))
            {
                throw new CommandLineException($"Local documentation shard metadata is incomplete: {required.Path}");
            }

            var fullPath = Path.Combine(repositoryRoot, required.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Local documentation shard was not found: {fullPath}");
            }

            var actualHash = ComputeNormalizedSha256(fullPath);
            var expectedHash = Value(shard, "sha256");
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new CommandLineException($"Local documentation shard is stale: {required.Path}");
            }

            records.Add(new ShardRecord(required.Path, required.Kind, count, expectedHash));
        }

        return records;
    }

    private static IReadOnlyList<JsonObject> LoadEntriesFromShards(string repositoryRoot, IReadOnlyList<ShardRecord> shardRecords)
    {
        var entries = new List<JsonObject>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var shard in shardRecords)
        {
            var fullPath = Path.Combine(repositoryRoot, shard.Path.Replace('/', Path.DirectorySeparatorChar));
            var text = File.ReadAllText(fullPath, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != shard.Count)
            {
                throw new CommandLineException($"Local documentation shard count is stale: {shard.Path}");
            }

            string? previousId = null;
            foreach (var line in lines)
            {
                var entry = JsonNode.Parse(line)?.AsObject()
                    ?? throw new CommandLineException($"Local documentation shard entry root must be a JSON object: {shard.Path}");
                var id = Value(entry, "id");
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new CommandLineException($"Local documentation shard entry is missing id: {shard.Path}");
                }

                if (!string.Equals(Value(entry, "kind"), shard.Kind, StringComparison.Ordinal))
                {
                    throw new CommandLineException($"Local documentation shard entry has wrong kind: {id}");
                }

                if (previousId is not null && string.CompareOrdinal(previousId, id) > 0)
                {
                    throw new CommandLineException($"Local documentation shard entries are not sorted by id: {shard.Path}");
                }

                if (!ids.Add(id))
                {
                    throw new CommandLineException($"Local documentation shard entry id is duplicated: {id}");
                }

                previousId = id;
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static bool TryLoadEntriesFromSqlite(
        string cachePath,
        JsonObject index,
        IReadOnlyList<ShardRecord> shardRecords,
        out IReadOnlyList<JsonObject> entries)
    {
        entries = [];
        if (!File.Exists(cachePath) ||
            index["sqliteCache"] is not JsonObject cache)
        {
            return false;
        }

        var sourceDigest = Value(cache, "sourceDigest");
        if (string.IsNullOrWhiteSpace(sourceDigest))
        {
            return false;
        }

        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = cachePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            connection.Open();
            var cacheSchemaVersion = ExecuteScalarString(connection, "SELECT value FROM metadata WHERE key = 'schemaVersion';");
            var cacheSourceDigest = ExecuteScalarString(connection, "SELECT value FROM metadata WHERE key = 'sourceDigest';");
            if (!string.Equals(cacheSchemaVersion, SqliteCacheSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal) ||
                !string.Equals(cacheSourceDigest, sourceDigest, StringComparison.Ordinal))
            {
                return false;
            }

            var expectedCount = shardRecords.Sum(shard => shard.Count);
            if (ExecuteScalarInt64(connection, "SELECT COUNT(*) FROM entries;") != expectedCount)
            {
                return false;
            }

            if (ExecuteScalarInt64(connection, "SELECT COUNT(*) FROM entries_fts WHERE entries_fts MATCH 'moveandslide';") <= 0)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT payload_json FROM entries ORDER BY id;";
            using var reader = command.ExecuteReader();
            var loadedEntries = new List<JsonObject>();
            while (reader.Read())
            {
                loadedEntries.Add(JsonNode.Parse(reader.GetString(0))?.AsObject()
                    ?? throw new CommandLineException("SQLite local documentation entry payload must be a JSON object."));
            }

            entries = loadedEntries;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or SqliteException or CommandLineException)
        {
            entries = [];
            return false;
        }
    }

    private bool TrySearchSqlite(IReadOnlyList<string> searchTerms, out IReadOnlyList<JsonObject> results)
    {
        results = [];
        if (sqlitePath is null)
        {
            return false;
        }

        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT e.payload_json
                FROM entries_fts
                JOIN entries e ON e.rowid = entries_fts.rowid
                WHERE entries_fts MATCH $query
                ORDER BY e.id
                LIMIT 50;
                """;
            command.Parameters.AddWithValue("$query", BuildFtsQuery(searchTerms));

            using var reader = command.ExecuteReader();
            var matches = new List<JsonObject>();
            while (reader.Read())
            {
                matches.Add(JsonNode.Parse(reader.GetString(0))?.AsObject()
                    ?? throw new CommandLineException("SQLite local documentation entry payload must be a JSON object."));
            }

            results = matches;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or SqliteException or CommandLineException or IOException or UnauthorizedAccessException)
        {
            results = [];
            return false;
        }
    }

    private static IEnumerable<SearchResult> ScoreEntries(IEnumerable<JsonObject> entries, IReadOnlyCollection<string> searchTerms)
    {
        var results = new List<SearchResult>();
        foreach (var entry in entries)
        {
            var score = Score(entry, searchTerms);
            if (score > 0)
            {
                results.Add(new SearchResult(entry, score));
            }
        }

        return results;
    }

    private static int Score(JsonObject entry, IReadOnlyCollection<string> searchTerms)
    {
        var haystack = new StringBuilder();
        haystack.Append(Value(entry, "id")).Append(' ');
        haystack.Append(Value(entry, "kind")).Append(' ');
        haystack.Append(Value(entry, "title")).Append(' ');
        haystack.Append(Value(entry, "summary")).Append(' ');
        if (entry["keywords"] is JsonArray keywords)
        {
            foreach (var keyword in keywords)
            {
                haystack.Append(keyword?.GetValue<string>()).Append(' ');
            }
        }

        var text = haystack.ToString().ToLowerInvariant();
        var score = 0;
        foreach (var term in searchTerms)
        {
            if (text.Contains(term, StringComparison.Ordinal))
            {
                score += term.Length <= 3 ? 1 : 3;
            }
        }

        return score;
    }

    private static IEnumerable<string> SplitSearchTerms(string value)
    {
        foreach (Match match in Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]+"))
        {
            yield return match.Value;
        }
    }

    private static string NormalizeTypeQuery(string query)
    {
        var value = query.Trim();
        const string prefix = "Electron2D.";
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..]
            : value;
    }

    private static string BuildFtsQuery(IEnumerable<string> searchTerms)
    {
        var ftsTerms = searchTerms.Where(term => term.Length > 3).ToArray();
        if (ftsTerms.Length == 0)
        {
            ftsTerms = searchTerms.ToArray();
        }

        return string.Join(" OR ", ftsTerms.Select(term => "\"" + term.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""));
    }

    private static string ComputeNormalizedSha256(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
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

    private static string Value(JsonObject obj, string property)
    {
        return obj[property]?.GetValue<string>() ?? string.Empty;
    }

    private sealed record ShardRequirement(string Path, string Kind);

    private sealed record ShardRecord(string Path, string Kind, int Count, string Sha256);
}

internal sealed record SearchResult(JsonObject Entry, int Score);

internal sealed record ApiMemberResult(JsonObject Type, JsonObject Member);

internal sealed class ParsedArguments
{
    private ParsedArguments(OutputFormat format, bool help, IReadOnlyList<string> values)
    {
        Format = format;
        Help = help;
        Values = values;
    }

    public OutputFormat Format { get; }

    public bool Help { get; }

    public IReadOnlyList<string> Values { get; }

    public static ParsedArguments Parse(IEnumerable<string> arguments)
    {
        var format = OutputFormat.Text;
        var help = false;
        var values = new List<string>();
        var items = arguments.ToArray();
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (string.Equals(item, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item, "-h", StringComparison.OrdinalIgnoreCase))
            {
                help = true;
                continue;
            }

            if (string.Equals(item, "--format", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= items.Length)
                {
                    throw new CommandLineException("Missing value for --format.");
                }

                format = items[++i].ToLowerInvariant() switch
                {
                    "text" => OutputFormat.Text,
                    "json" => OutputFormat.Json,
                    _ => throw new CommandLineException("Unsupported docs output format. Use text or json.")
                };
                continue;
            }

            values.Add(item);
        }

        return new ParsedArguments(format, help, values);
    }

    public string RequireSingleValue(string commandName)
    {
        if (Values.Count != 1)
        {
            throw new CommandLineException($"Usage: e2d {commandName} <name> [--format text|json]");
        }

        return Values[0];
    }

    public string RequireQuery(string commandName)
    {
        if (Values.Count == 0)
        {
            throw new CommandLineException($"Usage: e2d {commandName} <query> [--format text|json]");
        }

        return string.Join(' ', Values);
    }
}

internal enum OutputFormat
{
    Text,
    Json
}

internal sealed class CommandLineException : Exception
{
    public CommandLineException(string message)
        : base(message)
    {
    }
}
