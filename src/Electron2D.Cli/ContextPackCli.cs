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
using System.Text.RegularExpressions;
using Electron2D.ProjectSystem;

internal static partial class Electron2DCommandLine
{
    private static int RunContext(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (options.Values.Count != 1 || !string.Equals(options.Values[0], "build", StringComparison.OrdinalIgnoreCase))
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("context", options),
                    options,
                    "Unknown context command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d context build` is the implemented static context pack command.")),
                output,
                error);
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        if (options.Format is not (CliOutputFormat.Text or CliOutputFormat.Json))
        {
            return WriteResult(
                CliResult.Failure(
                    "context build",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Context pack build supports text or JSON output only.",
                    CreateCliDiagnostic("E2D-CLI-0002", "Use `--format text` or `--format json` for `e2d context build`."),
                    new JsonObject
                    {
                        ["mode"] = "context.build"
                    }),
                output,
                error);
        }

        try
        {
            var result = StaticContextPackBuilder.Build(projectRoot, FindRepositoryRoot(), context.NowUtc);
            return WriteResult(
                CliResult.Report(
                    "context build",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Static context pack created.",
                    result.Diagnostics,
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    result.Data),
                output,
                error);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or JsonException or InvalidOperationException)
        {
            return WriteResult(
                CliResult.Failure(
                    "context build",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Static context pack build failed.",
                    CreateCliDiagnostic("E2D-CLI-0002", exception.Message),
                    new JsonObject
                    {
                        ["mode"] = "context.build"
                    }),
                output,
                error);
        }
    }
}

internal static partial class StaticContextPackBuilder
{
    private const int MaxTextFileBytes = 64 * 1024;
    private const string ContextWarning = "Static context pack is a snapshot and can become stale after project changes or an active Editor session update.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] OutputFiles =
    [
        "context-manifest.json",
        "project-summary.json",
        "api-surface.json",
        "godot-differences.json",
        "scene-index.json",
        "resource-graph.json",
        "diagnostics.json",
        "conventions.md"
    ];

    private static readonly string[] CheckCommands =
    [
        "e2d validate --project <project> --format json",
        "e2d build --project <project> --format jsonl",
        "e2d test --project <project> --format json",
        "e2d doctor --project <project> --format json"
    ];

    private static readonly Regex ScriptClassRegex = new(
        @"(?:(?:namespace\s+(?<namespace>[A-Za-z_][A-Za-z0-9_.]*)\s*;)|(?:namespace\s+(?<blockNamespace>[A-Za-z_][A-Za-z0-9_.]*)\s*\{))|(?:\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<base>[A-Za-z_][A-Za-z0-9_.]*))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SecretLikeRegex = new(
        @"(?i)(secret|password|token|api[_-]?key|private[_-]?key|keystorepassword|certificatepassword)\s*[:=]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static StaticContextPackBuildResult Build(string projectRoot, string repositoryRoot, DateTimeOffset generatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        projectRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        repositoryRoot = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(projectRoot))
        {
            throw new DirectoryNotFoundException($"Project root was not found: {projectRoot}");
        }

        var settingsPath = Path.Combine(projectRoot, "project.e2d.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException($"Project settings were not found: {settingsPath}");
        }

        var contextRoot = Path.GetFullPath(Path.Combine(projectRoot, ".electron2d", "context"));
        EnsureChildPath(projectRoot, contextRoot);
        if (Directory.Exists(contextRoot))
        {
            Directory.Delete(contextRoot, recursive: true);
        }

        Directory.CreateDirectory(contextRoot);

        var skipped = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var safeFiles = EnumerateSafeProjectFiles(projectRoot, skipped).ToArray();
        var settings = LoadSettings(settingsPath);
        var scenes = BuildSceneIndex(projectRoot, safeFiles, skipped);
        var resources = BuildResourceGraph(projectRoot, safeFiles, scenes.SceneReferences, skipped);
        var customClasses = BuildCustomClasses(projectRoot, safeFiles, skipped);
        ScanForSecretLikeText(projectRoot, safeFiles, skipped);

        var documents = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["project-summary.json"] = BuildProjectSummary(settings, customClasses).ToJsonString(JsonOptions),
            ["api-surface.json"] = BuildApiSurface(repositoryRoot).ToJsonString(JsonOptions),
            ["godot-differences.json"] = BuildGodotDifferences().ToJsonString(JsonOptions),
            ["scene-index.json"] = scenes.Document.ToJsonString(JsonOptions),
            ["resource-graph.json"] = resources.ToJsonString(JsonOptions),
            ["diagnostics.json"] = BuildDiagnostics(skipped).ToJsonString(JsonOptions),
            ["conventions.md"] = BuildConventionsMarkdown()
        };

        foreach (var (fileName, text) in documents.OrderBy(pair => Array.IndexOf(OutputFiles, pair.Key)))
        {
            File.WriteAllText(Path.Combine(contextRoot, fileName), text.ReplaceLineEndings("\n"));
        }

        var fileSummaries = BuildFileSummaries(contextRoot);
        var totalBytes = fileSummaries.Sum(file => file.SizeBytes);
        var manifest = BuildManifest(projectRoot, generatedAtUtc, fileSummaries, totalBytes);
        File.WriteAllText(Path.Combine(contextRoot, "context-manifest.json"), manifest.ToJsonString(JsonOptions).ReplaceLineEndings("\n"));

        fileSummaries = BuildFileSummaries(contextRoot);
        totalBytes = fileSummaries.Sum(file => file.SizeBytes);
        manifest = BuildManifest(projectRoot, generatedAtUtc, fileSummaries, totalBytes);
        File.WriteAllText(Path.Combine(contextRoot, "context-manifest.json"), manifest.ToJsonString(JsonOptions).ReplaceLineEndings("\n"));
        fileSummaries = BuildFileSummaries(contextRoot);
        totalBytes = fileSummaries.Sum(file => file.SizeBytes);
        var changedFiles = OutputFiles
            .Select(file => ".electron2d/context/" + file)
            .ToArray();
        var data = new JsonObject
        {
            ["mode"] = "context.build",
            ["outputPath"] = ".electron2d/context",
            ["files"] = WriteFileNames(OutputFiles),
            ["totalBytes"] = totalBytes,
            ["snapshotWarning"] = ContextWarning
        };

        return new StaticContextPackBuildResult(
            changedFiles,
            [],
            data);
    }

    private static Electron2D.Electron2DProjectSettings LoadSettings(string settingsPath)
    {
        var result = Electron2D.Electron2DSettingsStore.LoadProject(settingsPath);
        if (!result.Succeeded || result.Settings is null)
        {
            var message = !result.Diagnostics.Any()
                ? "Project settings could not be loaded."
                : string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message));
            throw new FormatException(message);
        }

        return result.Settings;
    }

    private static JsonObject BuildProjectSummary(
        Electron2D.Electron2DProjectSettings settings,
        IReadOnlyList<JsonObject> customClasses)
    {
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["snapshotWarning"] = ContextWarning,
            ["project"] = new JsonObject
            {
                ["name"] = settings.Name,
                ["version"] = settings.ProjectVersion
            },
            ["engineVersion"] = settings.EngineVersion,
            ["dotnetVersion"] = Environment.Version.ToString(),
            ["mainScene"] = settings.MainScene,
            ["rendererProfile"] = settings.RendererProfile.ToString(),
            ["physicsTicksPerSecond"] = settings.PhysicsTicksPerSecond,
            ["display"] = new JsonObject
            {
                ["windowWidth"] = settings.Display.WindowSize.X,
                ["windowHeight"] = settings.Display.WindowSize.Y,
                ["fullscreen"] = settings.Display.Fullscreen,
                ["dpiScale"] = settings.Display.DpiScale,
                ["orientation"] = settings.Display.Orientation.ToString()
            },
            ["inputMap"] = new JsonObject
            {
                ["actions"] = WriteInputActions(settings.InputActions)
            },
            ["customClasses"] = CloneObjects(customClasses),
            ["checkCommands"] = WriteFileNames(CheckCommands)
        };
    }

    private static JsonArray WriteInputActions(IEnumerable<Electron2D.InputMapActionSnapshot> actions)
    {
        var result = new JsonArray();
        foreach (var action in actions.OrderBy(action => action.Name, StringComparer.Ordinal))
        {
            result.Add(new JsonObject
            {
                ["name"] = action.Name,
                ["deadzone"] = action.Deadzone,
                ["events"] = WriteInputEvents(action.Events)
            });
        }

        return result;
    }

    private static JsonArray WriteInputEvents(IEnumerable<Electron2D.InputEvent> events)
    {
        var result = new JsonArray();
        foreach (var inputEvent in events)
        {
            result.Add(inputEvent switch
            {
                Electron2D.InputEventKey key => new JsonObject
                {
                    ["kind"] = "key",
                    ["keycode"] = key.Keycode.ToString()
                },
                Electron2D.InputEventMouseButton mouse => new JsonObject
                {
                    ["kind"] = "mouseButton",
                    ["button"] = mouse.ButtonIndex.ToString()
                },
                Electron2D.InputEventJoypadButton button => new JsonObject
                {
                    ["kind"] = "joypadButton",
                    ["button"] = button.ButtonIndex.ToString()
                },
                Electron2D.InputEventJoypadMotion motion => new JsonObject
                {
                    ["kind"] = "joypadMotion",
                    ["axis"] = motion.Axis.ToString(),
                    ["axisValue"] = motion.AxisValue
                },
                _ => new JsonObject
                {
                    ["kind"] = inputEvent.GetType().Name
                }
            });
        }

        return result;
    }

    private static SceneIndexBuildResult BuildSceneIndex(
        string projectRoot,
        IEnumerable<ProjectFile> safeFiles,
        IDictionary<string, int> skipped)
    {
        var sceneReferences = new List<JsonObject>();
        var scenes = new JsonArray();
        foreach (var file in safeFiles
            .Where(file => file.RelativePath.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            try
            {
                var document = Electron2D.SceneFileTextSerializer.Deserialize(File.ReadAllText(file.FullPath));
                var externalReferences = WriteExternalReferences(document.ExternalReferences);
                foreach (var reference in document.ExternalReferences)
                {
                    sceneReferences.Add(new JsonObject
                    {
                        ["source"] = file.RelativePath,
                        ["target"] = reference.Path,
                        ["type"] = reference.Type,
                        ["uid"] = reference.UidText
                    });
                }

                scenes.Add(new JsonObject
                {
                    ["path"] = file.RelativePath,
                    ["rootNodeCount"] = document.Nodes.Count(node => node.ParentId is null),
                    ["nodeCount"] = document.Nodes.Count,
                    ["nodes"] = WriteSceneNodes(document.Nodes),
                    ["externalReferences"] = externalReferences
                });
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or JsonException)
            {
                AddSkipped(skipped, "scene-parse-diagnostic");
            }
        }

        return new SceneIndexBuildResult(
            new JsonObject
            {
                ["schemaVersion"] = 1,
                ["scenes"] = scenes
            },
            sceneReferences);
    }

    private static JsonArray WriteSceneNodes(IEnumerable<Electron2D.SceneFileNode> nodes)
    {
        var result = new JsonArray();
        foreach (var node in nodes.OrderBy(node => node.Id))
        {
            result.Add(new JsonObject
            {
                ["id"] = node.Id,
                ["name"] = node.Name,
                ["type"] = node.Type,
                ["parent"] = node.ParentId,
                ["groups"] = WriteFileNames(node.PersistentGroups)
            });
        }

        return result;
    }

    private static JsonArray WriteExternalReferences(IEnumerable<Electron2D.ResourceFileExternalReference> references)
    {
        var result = new JsonArray();
        foreach (var reference in references.OrderBy(reference => reference.Id))
        {
            result.Add(new JsonObject
            {
                ["id"] = reference.Id,
                ["uid"] = reference.UidText,
                ["path"] = reference.Path,
                ["type"] = reference.Type
            });
        }

        return result;
    }

    private static JsonObject BuildResourceGraph(
        string projectRoot,
        IEnumerable<ProjectFile> safeFiles,
        IReadOnlyList<JsonObject> sceneReferences,
        IDictionary<string, int> skipped)
    {
        var resources = new JsonArray();
        foreach (var file in safeFiles
            .Where(file => file.RelativePath.EndsWith(".e2res", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            try
            {
                var document = Electron2D.ResourceFileTextSerializer.Deserialize(File.ReadAllText(file.FullPath));
                resources.Add(new JsonObject
                {
                    ["path"] = file.RelativePath,
                    ["resourcePath"] = document.Path,
                    ["uid"] = document.UidText,
                    ["type"] = document.Type,
                    ["externalReferences"] = WriteExternalReferences(document.ExternalReferences)
                });
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or JsonException)
            {
                AddSkipped(skipped, "resource-parse-diagnostic");
            }
        }

        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["resources"] = resources,
            ["sceneReferences"] = CloneObjects(sceneReferences)
        };
    }

    private static IReadOnlyList<JsonObject> BuildCustomClasses(
        string projectRoot,
        IEnumerable<ProjectFile> safeFiles,
        IDictionary<string, int> skipped)
    {
        var result = new List<JsonObject>();
        foreach (var file in safeFiles
            .Where(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            if (new FileInfo(file.FullPath).Length > MaxTextFileBytes)
            {
                AddSkipped(skipped, "large-script");
                continue;
            }

            try
            {
                var text = File.ReadAllText(file.FullPath);
                var currentNamespace = string.Empty;
                foreach (Match match in ScriptClassRegex.Matches(text))
                {
                    if (match.Groups["namespace"].Success)
                    {
                        currentNamespace = match.Groups["namespace"].Value;
                        continue;
                    }

                    if (match.Groups["blockNamespace"].Success)
                    {
                        currentNamespace = match.Groups["blockNamespace"].Value;
                        continue;
                    }

                    if (!match.Groups["name"].Success)
                    {
                        continue;
                    }

                    var name = match.Groups["name"].Value;
                    var type = string.IsNullOrWhiteSpace(currentNamespace) ? name : currentNamespace + "." + name;
                    result.Add(new JsonObject
                    {
                        ["type"] = type,
                        ["baseType"] = match.Groups["base"].Value,
                        ["path"] = file.RelativePath
                    });
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                AddSkipped(skipped, "script-read-diagnostic");
            }
        }

        return result
            .OrderBy(item => item["type"]?.GetValue<string>(), StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonObject BuildApiSurface(string repositoryRoot)
    {
        var manifestPath = Path.Combine(repositoryRoot, LocalDocumentationStore.ApiManifestPath.Replace('/', Path.DirectorySeparatorChar));
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject()
            ?? throw new FormatException("API manifest root must be a JSON object.");
        var types = manifest["types"]?.AsArray() ?? new JsonArray();
        var typeSummaries = new JsonArray();
        foreach (var type in types.OfType<JsonObject>()
            .OrderBy(type => ReadString(type, "fullName"), StringComparer.Ordinal)
            .Take(64))
        {
            typeSummaries.Add(new JsonObject
            {
                ["fullName"] = ReadString(type, "fullName"),
                ["kind"] = ReadString(type, "kind"),
                ["category"] = ReadString(type, "category"),
                ["status"] = type["profile"]?["status"]?.GetValue<string>() ?? string.Empty,
                ["outOfProfile"] = type["profile"]?["outOfProfile"]?.GetValue<bool>() ?? true
            });
        }

        var statusSummary = manifest["statusSummary"]?.AsObject();
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["manifestPath"] = LocalDocumentationStore.ApiManifestPath,
            ["manifestVersion"] = manifest["manifestVersion"]?.GetValue<string>() ?? string.Empty,
            ["engineVersion"] = manifest["engineVersion"]?.GetValue<string>() ?? string.Empty,
            ["profileName"] = manifest["profileName"]?.GetValue<string>() ?? string.Empty,
            ["typeCount"] = types.Count,
            ["statusSummary"] = statusSummary is null ? new JsonObject() : JsonNode.Parse(statusSummary.ToJsonString()),
            ["types"] = typeSummaries
        };
    }

    private static JsonObject BuildGodotDifferences()
    {
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["profile"] = "Electron2D 0.1-preview",
            ["status"] = "strict-profile-summary",
            ["compareCommand"] = "e2d api compare-godot <type> --format json",
            ["compatibilityDocumentation"] = "docs/release-management/api-compatibility.md"
        };
    }

    private static JsonObject BuildDiagnostics(IReadOnlyDictionary<string, int> skipped)
    {
        var skippedArray = new JsonArray();
        foreach (var (category, count) in skipped.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            skippedArray.Add(new JsonObject
            {
                ["category"] = category,
                ["count"] = count
            });
        }

        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["summary"] = new JsonObject
            {
                ["status"] = "completed",
                ["snapshotWarning"] = ContextWarning
            },
            ["skipped"] = skippedArray,
            ["diagnostics"] = new JsonArray()
        };
    }

    private static JsonObject BuildManifest(
        string projectRoot,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<ContextPackFileSummary> files,
        long totalBytes)
    {
        var fileArray = new JsonArray();
        foreach (var file in files.OrderBy(file => Array.IndexOf(OutputFiles, file.Name)))
        {
            fileArray.Add(new JsonObject
            {
                ["path"] = ".electron2d/context/" + file.Name,
                ["sizeBytes"] = file.SizeBytes
            });
        }

        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["generatedAtUtc"] = generatedAtUtc.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["projectRoot"] = projectRoot,
            ["snapshotWarning"] = ContextWarning,
            ["totalBytes"] = totalBytes,
            ["files"] = fileArray
        };
    }

    private static string BuildConventionsMarkdown()
    {
        return """
        # Electron2D Context Pack

        This directory is generated by `e2d context build`.

        - Treat this directory as a snapshot. Rebuild it after project settings, scene, resource, script or task changes.
        - Do not edit generated `.electron2d` working directories by hand.
        - Canonical project tasks stay in `.electron2d/tasks/*.e2task` and `.electron2d/tasks/board.e2tasks`.
        - Read the current project `AGENTS.md` before changing files.
        - Run `e2d validate --project <project> --format json` before reporting project state.
        """;
    }

    private static IReadOnlyList<ContextPackFileSummary> BuildFileSummaries(string contextRoot)
    {
        return OutputFiles
            .Where(file => File.Exists(Path.Combine(contextRoot, file)))
            .Select(file => new ContextPackFileSummary(file, new FileInfo(Path.Combine(contextRoot, file)).Length))
            .ToArray();
    }

    private static IEnumerable<ProjectFile> EnumerateSafeProjectFiles(string projectRoot, IDictionary<string, int> skipped)
    {
        var pending = new Stack<DirectoryInfo>();
        pending.Push(new DirectoryInfo(projectRoot));
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in directory.EnumerateDirectories())
            {
                var relativePath = ToProjectPath(projectRoot, child.FullName);
                if (IsExcludedDirectory(relativePath, out var category))
                {
                    AddSkipped(skipped, category);
                    continue;
                }

                pending.Push(child);
            }

            foreach (var file in directory.EnumerateFiles())
            {
                var relativePath = ToProjectPath(projectRoot, file.FullName);
                if (IsExcludedFile(relativePath, file, out var category))
                {
                    AddSkipped(skipped, category);
                    continue;
                }

                yield return new ProjectFile(relativePath, file.FullName);
            }
        }
    }

    private static bool IsExcludedDirectory(string relativePath, out string category)
    {
        var normalized = NormalizeProjectPath(relativePath);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            category = string.Empty;
            return false;
        }

        if (segments[0] is ".git")
        {
            category = "git";
            return true;
        }

        if (segments[0] is "bin" or "obj" or "node_modules" or "dev-diary" or "completed-tasks")
        {
            category = "generated-or-local-workflow";
            return true;
        }

        if (segments[0] == ".electron2d" &&
            segments.Length > 1 &&
            segments[1] is "context" or "import-cache" or "workspaces" or "session" or "user" or "export-smoke")
        {
            category = "electron2d-generated";
            return true;
        }

        category = string.Empty;
        return false;
    }

    private static bool IsExcludedFile(string relativePath, FileInfo file, out string category)
    {
        var normalized = NormalizeProjectPath(relativePath);
        var fileName = Path.GetFileName(normalized);
        if (fileName is "TASKS.md" ||
            fileName.StartsWith("CHANGELOG", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("RELEASE-NOTES", StringComparison.OrdinalIgnoreCase))
        {
            category = "local-workflow";
            return true;
        }

        if (IsSecretLikePath(normalized))
        {
            category = "secret-like-path";
            return true;
        }

        if (normalized.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ||
            file.Length > MaxTextFileBytes)
        {
            category = "large-or-log";
            return true;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".wav" or ".ogg" or ".mp3" or ".zip" or ".dll" or ".exe" or ".pdb" or ".so" or ".dylib" or ".apk" or ".aab" or ".keystore" or ".p12" or ".pem")
        {
            category = "binary";
            return true;
        }

        category = string.Empty;
        return false;
    }

    private static bool IsSecretLikePath(string normalizedPath)
    {
        var fileName = Path.GetFileName(normalizedPath);
        return fileName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("privatekey", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("private_key", StringComparison.OrdinalIgnoreCase);
    }

    private static void ScanForSecretLikeText(
        string projectRoot,
        IEnumerable<ProjectFile> safeFiles,
        IDictionary<string, int> skipped)
    {
        foreach (var file in safeFiles)
        {
            var extension = Path.GetExtension(file.RelativePath);
            if (extension is not (".json" or ".cs" or ".md" or ".txt" or ".e2res" or ".e2task" or ".e2tasks"))
            {
                continue;
            }

            try
            {
                var text = File.ReadAllText(file.FullPath);
                if (SecretLikeRegex.IsMatch(text))
                {
                    AddSkipped(skipped, "secret-like-text");
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                AddSkipped(skipped, "secret-scan-diagnostic");
            }
        }
    }

    private static void EnsureChildPath(string root, string child)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedChild = Path.GetFullPath(child);
        var rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;
        if (!normalizedChild.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path is outside the project root: {normalizedChild}");
        }
    }

    private static string ToProjectPath(string projectRoot, string fullPath)
    {
        return NormalizeProjectPath(Path.GetRelativePath(projectRoot, fullPath));
    }

    private static string NormalizeProjectPath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static string ReadString(JsonObject obj, string propertyName)
    {
        return obj[propertyName]?.GetValue<string>() ?? string.Empty;
    }

    private static JsonArray WriteFileNames(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static JsonArray CloneObjects(IEnumerable<JsonObject> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(JsonNode.Parse(value.ToJsonString()));
        }

        return array;
    }

    private static void AddSkipped(IDictionary<string, int> skipped, string category)
    {
        skipped[category] = skipped.TryGetValue(category, out var count) ? count + 1 : 1;
    }
}

internal sealed record StaticContextPackBuildResult(
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<StructuredDiagnostic> Diagnostics,
    JsonObject Data);

internal sealed record ProjectFile(string RelativePath, string FullPath);

internal sealed record SceneIndexBuildResult(JsonObject Document, IReadOnlyList<JsonObject> SceneReferences);

internal sealed record ContextPackFileSummary(string Name, long SizeBytes);
