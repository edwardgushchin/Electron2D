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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class LocalDocumentationCliTests
{
    private static readonly TimeSpan CliCommandTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CliStreamReadTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void CliProjectIsPartOfSolutionAndNamedE2D()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj");

        Assert.True(File.Exists(projectPath), "Electron2D.Cli project file must exist.");
        Assert.Contains("Electron2D.Cli", File.ReadAllText(Path.Combine(root, "src", "Electron2D.sln")));

        var project = XDocument.Load(projectPath);
        var assemblyName = project.Descendants("AssemblyName").SingleOrDefault()?.Value;
        var outputType = project.Descendants("OutputType").SingleOrDefault()?.Value;

        Assert.Equal("e2d", assemblyName);
        Assert.Equal("Exe", outputType);
    }

    [Fact]
    public async Task DocsTypeAndMemberCommandsReturnManifestBackedJson()
    {
        var type = await RunCliJsonAsync("docs", "type", "CharacterBody2D", "--format", "json");
        var typeEntry = type.RootElement.GetProperty("type");

        Assert.Equal("Electron2D.CharacterBody2D", typeEntry.GetProperty("fullName").GetString());
        Assert.Equal("electron2d://api/type/Electron2D.CharacterBody2D", typeEntry.GetProperty("id").GetString());
        Assert.Equal("data/api/electron2d-api-manifest.json", type.RootElement.GetProperty("sourcePath").GetString());
        Assert.True(typeEntry.GetProperty("members").GetArrayLength() > 0);

        var member = await RunCliJsonAsync("docs", "member", "CharacterBody2D.MoveAndSlide", "--format", "json");
        var memberEntry = member.RootElement.GetProperty("member");

        Assert.Equal("Electron2D.CharacterBody2D", memberEntry.GetProperty("declaringType").GetString());
        Assert.Equal("MoveAndSlide", memberEntry.GetProperty("name").GetString());
        Assert.Equal("Method", memberEntry.GetProperty("kind").GetString());
        Assert.Contains("MoveAndSlide", memberEntry.GetProperty("signature").GetString(), StringComparison.Ordinal);
        Assert.Equal("data/api/electron2d-api-manifest.json", member.RootElement.GetProperty("sourcePath").GetString());
    }

    [Fact]
    public async Task DocsSearchAndExampleCommandsUseLocalDocumentationIndex()
    {
        var search = await RunCliJsonAsync("docs", "search", "move and slide", "--format", "json");
        var results = search.RootElement.GetProperty("results").EnumerateArray().ToArray();

        Assert.Contains(results, result =>
            result.GetProperty("kind").GetString() == "api-member" &&
            result.GetProperty("title").GetString() == "CharacterBody2D.MoveAndSlide" &&
            result.GetProperty("sourcePath").GetString() == "data/api/electron2d-api-manifest.json");

        var example = await RunCliJsonAsync("docs", "example", "platformer movement", "--format", "json");
        var exampleEntry = example.RootElement.GetProperty("example");
        var apiIds = exampleEntry.GetProperty("apiIds").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal("example:platformer-movement", exampleEntry.GetProperty("id").GetString());
        Assert.Contains("CharacterBody2D", exampleEntry.GetProperty("code").GetString(), StringComparison.Ordinal);
        Assert.Contains("electron2d://api/type/Electron2D.CharacterBody2D", apiIds);
        Assert.Equal("data/documentation/electron2d-doc-examples.json", exampleEntry.GetProperty("sourcePath").GetString());
    }

    [Fact]
    public async Task DocsSearchAndExampleFallBackToShardsWhenSqliteCacheIsAbsent()
    {
        using var workspace = CreateShardDocumentationFixture("cli-docs-fallback");
        var cachePath = Path.Combine(workspace.Root, "data", "documentation", "electron2d-local-docs-search.sqlite");
        Assert.False(File.Exists(cachePath));

        var search = await RunCliJsonAsyncWithDocsRoot(workspace.Root, "docs", "search", "move and slide", "--format", "json");
        var results = search.RootElement.GetProperty("results").EnumerateArray().ToArray();

        Assert.Contains(results, result =>
            result.GetProperty("kind").GetString() == "api-member" &&
            result.GetProperty("title").GetString() == "CharacterBody2D.MoveAndSlide" &&
            result.GetProperty("sourcePath").GetString() == "data/api/electron2d-api-manifest.json");

        var example = await RunCliJsonAsyncWithDocsRoot(workspace.Root, "docs", "example", "platformer movement", "--format", "json");
        var exampleEntry = example.RootElement.GetProperty("example");

        Assert.Equal("example:platformer-movement", exampleEntry.GetProperty("id").GetString());
        Assert.Contains("CharacterBody2D", exampleEntry.GetProperty("code").GetString(), StringComparison.Ordinal);
        Assert.Equal("data/documentation/electron2d-doc-examples.json", exampleEntry.GetProperty("sourcePath").GetString());
        Assert.False(File.Exists(cachePath));
    }

    private static Task<JsonDocument> RunCliJsonAsync(params string[] arguments)
    {
        return RunCliJsonAsyncCore(docsRoot: null, arguments);
    }

    private static Task<JsonDocument> RunCliJsonAsyncWithDocsRoot(string docsRoot, params string[] arguments)
    {
        return RunCliJsonAsyncCore(docsRoot, arguments);
    }

    private static async Task<JsonDocument> RunCliJsonAsyncCore(string? docsRoot, params string[] arguments)
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (!string.IsNullOrWhiteSpace(docsRoot))
        {
            startInfo.Environment["ELECTRON2D_DOCS_ROOT"] = docsRoot;
        }

        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        var (output, error) = await WaitForCliProcessAsync(process, outputTask, errorTask);

        Assert.True(
            process.ExitCode == 0,
            $"e2d command failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

        return JsonDocument.Parse(output);
    }

    private static async Task<(string Output, string Error)> WaitForCliProcessAsync(
        Process process,
        Task<string> outputTask,
        Task<string> errorTask)
    {
        using var timeout = new CancellationTokenSource(CliCommandTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            var timedOutOutput = await ReadProcessStreamSafelyAsync(outputTask);
            var timedOutError = await ReadProcessStreamSafelyAsync(errorTask);
            throw new TimeoutException(
                $"e2d command timed out after {CliCommandTimeout}.{Environment.NewLine}stdout:{Environment.NewLine}{timedOutOutput}{Environment.NewLine}stderr:{Environment.NewLine}{timedOutError}");
        }

        return (
            await ReadProcessStreamAsync(outputTask, "stdout"),
            await ReadProcessStreamAsync(errorTask, "stderr"));
    }

    private static async Task<string> ReadProcessStreamAsync(Task<string> streamTask, string streamName)
    {
        var completed = await Task.WhenAny(streamTask, Task.Delay(CliStreamReadTimeout));
        if (!ReferenceEquals(completed, streamTask))
        {
            throw new TimeoutException($"e2d command exited, but {streamName} stream did not close within {CliStreamReadTimeout}.");
        }

        return await streamTask;
    }

    private static async Task<string> ReadProcessStreamSafelyAsync(Task<string> streamTask)
    {
        try
        {
            var completed = await Task.WhenAny(streamTask, Task.Delay(CliStreamReadTimeout));
            return ReferenceEquals(completed, streamTask)
                ? await streamTask
                : "<stream did not close>";
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (ObjectDisposedException)
        {
            return string.Empty;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static TemporaryDirectory CreateShardDocumentationFixture(string name)
    {
        var workspace = TemporaryDirectory.Create(name);
        WriteText(
            workspace.Root,
            "data/api/electron2d-api-manifest.json",
            """
            {
              "types": [
                {
                  "id": "electron2d://api/type/Electron2D.CharacterBody2D",
                  "fullName": "Electron2D.CharacterBody2D",
                  "name": "CharacterBody2D",
                  "category": "Physics",
                  "summary": "Provides a 2D character body moved directly by user code.",
                  "members": [
                    {
                      "id": "electron2d://api/member/Electron2D.CharacterBody2D.MoveAndSlide",
                      "declaringType": "Electron2D.CharacterBody2D",
                      "name": "MoveAndSlide",
                      "kind": "Method",
                      "signature": "public bool MoveAndSlide()",
                      "summary": "Moves the body and slides along collisions."
                    }
                  ]
                }
              ]
            }
            """);
        WriteText(
            workspace.Root,
            "data/documentation/electron2d-doc-examples.json",
            """
            {
              "examples": [
                {
                  "id": "example:platformer-movement",
                  "title": "Platformer movement",
                  "summary": "Shows platformer movement with CharacterBody2D.",
                  "keywords": [
                    "platformer",
                    "movement"
                  ],
                  "apiIds": [
                    "electron2d://api/type/Electron2D.CharacterBody2D",
                    "electron2d://api/member/Electron2D.CharacterBody2D.MoveAndSlide"
                  ],
                  "code": "var body = new CharacterBody2D();\nbody.MoveAndSlide();\n"
                }
              ]
            }
            """);
        WriteText(
            workspace.Root,
            "docs/architecture/agent-native-workflow.md",
            """
            # Agent workflow

            Agent workflow documentation.
            """);
        WriteText(workspace.Root, "docs/documentation/local-documentation-pipeline.md", "# Local documentation pipeline\n");

        var apiTypes = WriteShard(
            workspace.Root,
            "data/documentation/local-docs-index/api-types.ndjson",
            """
            {"id":"api-type:Electron2D.CharacterBody2D","kind":"api-type","title":"CharacterBody2D","summary":"Provides a 2D character body moved directly by user code.","keywords":["body","character","characterbody2d","physics"],"sourcePath":"data/api/electron2d-api-manifest.json","apiId":"electron2d://api/type/Electron2D.CharacterBody2D","audiences":["ai","cli","ide","wiki","inspector","generator"]}
            """);
        var apiMembers = WriteShard(
            workspace.Root,
            "data/documentation/local-docs-index/api-members.ndjson",
            """
            {"id":"api-member:Electron2D.CharacterBody2D.MoveAndSlide","kind":"api-member","title":"CharacterBody2D.MoveAndSlide","summary":"Moves the body and slides along collisions.","keywords":["and","body","characterbody2d","move","moveandslide","slide"],"sourcePath":"data/api/electron2d-api-manifest.json","apiId":"electron2d://api/member/Electron2D.CharacterBody2D.MoveAndSlide","audiences":["ai","cli","ide","wiki","inspector","generator"]}
            """);
        var documentation = WriteShard(
            workspace.Root,
            "data/documentation/local-docs-index/documentation.ndjson",
            """
            {"id":"doc:architecture.agent-native-workflow","kind":"documentation","title":"Agent workflow","summary":"Agent workflow documentation.","keywords":["agent","workflow"],"sourcePath":"docs/architecture/agent-native-workflow.md","sourceId":"doc:architecture.agent-native-workflow","audiences":["human","ai","cli","wiki"]}
            """);
        var examples = WriteShard(
            workspace.Root,
            "data/documentation/local-docs-index/examples.ndjson",
            """
            {"id":"example:platformer-movement","kind":"example","title":"Platformer movement","summary":"Shows platformer movement with CharacterBody2D.","keywords":["movement","platformer"],"sourcePath":"data/documentation/electron2d-doc-examples.json","sourceId":"example:platformer-movement","apiIds":["electron2d://api/type/Electron2D.CharacterBody2D","electron2d://api/member/Electron2D.CharacterBody2D.MoveAndSlide"],"code":"var body = new CharacterBody2D();\nbody.MoveAndSlide();\n","audiences":["human","ai","cli","ide"]}
            """);
        var sourceDigest = ComputeTextSha256(string.Concat(apiTypes.Sha256, apiMembers.Sha256, documentation.Sha256, examples.Sha256));
        WriteText(
            workspace.Root,
            "data/documentation/electron2d-local-docs-index.json",
            $$"""
            {
              "schemaVersion": 2,
              "manifestVersion": "0.1-preview",
              "generatedFrom": {
                "apiManifest": {
                  "path": "data/api/electron2d-api-manifest.json",
                  "sha256": "{{ComputeNormalizedSha256(Path.Combine(workspace.Root, "data", "api", "electron2d-api-manifest.json"))}}"
                },
                "documentation": [
                  {
                    "path": "docs/architecture/agent-native-workflow.md",
                    "sha256": "{{ComputeNormalizedSha256(Path.Combine(workspace.Root, "docs", "architecture", "agent-native-workflow.md"))}}"
                  },
                  {
                    "path": "docs/documentation/local-documentation-pipeline.md",
                    "sha256": "{{ComputeNormalizedSha256(Path.Combine(workspace.Root, "docs", "documentation", "local-documentation-pipeline.md"))}}"
                  }
                ],
                "examples": {
                  "path": "data/documentation/electron2d-doc-examples.json",
                  "sha256": "{{ComputeNormalizedSha256(Path.Combine(workspace.Root, "data", "documentation", "electron2d-doc-examples.json"))}}"
                }
              },
              "audiences": [
                "human",
                "ai",
                "cli",
                "ide",
                "wiki",
                "inspector",
                "generator"
              ],
              "commands": [
                {
                  "name": "docs search",
                  "description": "Searches local API, documentation and examples index.",
                  "formats": [
                    "text",
                    "json"
                  ]
                },
                {
                  "name": "docs type",
                  "description": "Returns a public API type from the API manifest.",
                  "formats": [
                    "text",
                    "json"
                  ]
                },
                {
                  "name": "docs member",
                  "description": "Returns a public API member from the API manifest.",
                  "formats": [
                    "text",
                    "json"
                  ]
                },
                {
                  "name": "docs example",
                  "description": "Returns a local documentation example.",
                  "formats": [
                    "text",
                    "json"
                  ]
                }
              ],
              "sources": {
                "apiManifest": {
                  "path": "data/api/electron2d-api-manifest.json"
                },
                "documentation": {
                  "paths": [
                    "docs/architecture/agent-native-workflow.md",
                    "docs/documentation/local-documentation-pipeline.md"
                  ]
                },
                "examples": {
                  "path": "data/documentation/electron2d-doc-examples.json"
                },
                "wiki": {
                  "generator": "eng/Electron2D.Build update wiki",
                  "compatibilityPage": ".github/wiki/API-Compatibility.md"
                }
              },
              "shards": [
                {
                  "path": "{{apiTypes.Path}}",
                  "kind": "{{apiTypes.Kind}}",
                  "count": {{apiTypes.Count}},
                  "sha256": "{{apiTypes.Sha256}}"
                },
                {
                  "path": "{{apiMembers.Path}}",
                  "kind": "{{apiMembers.Kind}}",
                  "count": {{apiMembers.Count}},
                  "sha256": "{{apiMembers.Sha256}}"
                },
                {
                  "path": "{{documentation.Path}}",
                  "kind": "{{documentation.Kind}}",
                  "count": {{documentation.Count}},
                  "sha256": "{{documentation.Sha256}}"
                },
                {
                  "path": "{{examples.Path}}",
                  "kind": "{{examples.Kind}}",
                  "count": {{examples.Count}},
                  "sha256": "{{examples.Sha256}}"
                }
              ],
              "sqliteCache": {
                "path": "data/documentation/electron2d-local-docs-search.sqlite",
                "schemaVersion": 1,
                "sourceDigest": "{{sourceDigest}}",
                "entriesTable": "entries",
                "ftsTable": "entries_fts"
              }
            }
            """);

        return workspace;
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

    private static ShardInfo WriteShard(string root, string relativePath, string content)
    {
        var normalized = content.TrimEnd().Replace("\r\n", "\n").Replace('\r', '\n') + "\n";
        WriteText(root, relativePath, normalized);
        var kind = JsonDocument.Parse(normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]).RootElement.GetProperty("kind").GetString()
            ?? throw new InvalidOperationException("Shard fixture entry is missing kind.");
        return new ShardInfo(relativePath, kind, 1, ComputeNormalizedSha256(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))));
    }

    private static void WriteText(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content.Replace("\r\n", "\n").Replace('\r', '\n'), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ComputeNormalizedSha256(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');
        return ComputeTextSha256(text);
    }

    private static string ComputeTextSha256(string text)
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed record ShardInfo(string Path, string Kind, int Count, string Sha256);

    private sealed record TemporaryDirectory(string Root) : IDisposable
    {
        public static TemporaryDirectory Create(string name)
        {
            var root = Path.Combine(Path.GetTempPath(), "Electron2D-LocalDocumentationCliTests", name, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TemporaryDirectory(root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
