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

var exitCode = Electron2DCommandLine.Run(args, Console.Out, Console.Error);
return exitCode;

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
            error.WriteLine("Run `powershell -ExecutionPolicy Bypass -File tools\\Verify-LocalDocumentation.ps1`.");
            return 1;
        }
        catch (IOException exception)
        {
            error.WriteLine(exception.Message);
            error.WriteLine("Run `powershell -ExecutionPolicy Bypass -File tools\\Verify-LocalDocumentation.ps1`.");
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

    private readonly JsonObject apiManifest;
    private readonly JsonObject index;

    private LocalDocumentationStore(string repositoryRoot, JsonObject apiManifest, JsonObject index)
    {
        RepositoryRoot = repositoryRoot;
        this.apiManifest = apiManifest;
        this.index = index;
    }

    public string RepositoryRoot { get; }

    private JsonArray Types => apiManifest["types"]?.AsArray()
        ?? throw new CommandLineException("API manifest is missing `types`.");

    private JsonArray Entries => index["entries"]?.AsArray()
        ?? throw new CommandLineException("Local documentation index is missing `entries`.");

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

        return new LocalDocumentationStore(repositoryRoot, apiManifest, index);
    }

    public IEnumerable<SearchResult> Search(string query)
    {
        var tokens = Tokenize(query).ToArray();
        if (tokens.Length == 0)
        {
            throw new CommandLineException("Search query must not be empty.");
        }

        foreach (var node in Entries)
        {
            if (node is not JsonObject entry)
            {
                continue;
            }

            var score = Score(entry, tokens);
            if (score > 0)
            {
                yield return new SearchResult(entry, score);
            }
        }
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

    private static int Score(JsonObject entry, IReadOnlyCollection<string> tokens)
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
        foreach (var token in tokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
            {
                score += token.Length <= 3 ? 1 : 3;
            }
        }

        return score;
    }

    private static IEnumerable<string> Tokenize(string value)
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

    private static string Value(JsonObject obj, string property)
    {
        return obj[property]?.GetValue<string>() ?? string.Empty;
    }
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
