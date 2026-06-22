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
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.Mcp;
using Electron2D.ProjectSystem;
using Electron2D.Testing;
using Electron2D.Tooling;

internal static partial class Electron2DCommandLine
{
    private static readonly JsonSerializerOptions CliJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] RequiredGroups =
    [
        "project",
        "scene",
        "resource",
        "workspace",
        "import",
        "build",
        "run",
        "test",
        "export",
        "validate",
        "docs",
        "api",
        "mcp",
        "context",
        "doctor"
    ];

    private static readonly string[] MutatingOrJobGroups =
    [
        "project",
        "scene",
        "resource",
        "workspace",
        "import",
        "build",
        "run",
        "test",
        "export"
    ];

    private const string DefaultSceneTestManifestPath = "tests/electron2d.scene-tests.json";

    private static int RunGeneralCommand(
        string group,
        string[] args,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var options = CliOptions.Parse(group, args);
        if (options.Help)
        {
            WriteGroupHelp(group, output);
            return 0;
        }

        if (!RequiredGroups.Contains(group, StringComparer.Ordinal))
        {
            return WriteResult(
                CliResult.Blocked(
                    string.Join(' ', new[] { group }.Concat(options.Values)),
                    options,
                    "Unknown command group.",
                    CreateCliDiagnostic("E2D-CLI-0001", $"Command group '{group}' is not implemented.")),
                output,
                error);
        }

        return group switch
        {
            "project" => RunProject(options, output, error),
            "mcp" => RunMcp(options, output, error, context),
            "doctor" => RunDoctor(options, output, error),
            "workspace" => RunWorkspace(options, output, error, context),
            "validate" => RunValidate(options, output, error),
            "run" when IsRuntimeDebugCommand(options) => RunRuntimeDebug(options, output, error),
            "run" when HeadlessRuntimeAutomation.HasRuntimeOptions(options) => RunHeadlessRuntime(options, output, error, context),
            "test" when HasSceneTestSuite(options, NormalizeProjectRoot(options.ProjectRoot)) => RunSceneTests(options, output, error, context),
            "import" or "build" or "run" or "test" or "export" => RunJob(group, options, output, error, context),
            _ => WriteResult(
                CliResult.Blocked(
                    BuildCommandName(group, options),
                    options,
                    $"Command group '{group}' is reserved for a later Preview task.",
                    CreateCliDiagnostic("E2D-CLI-0001", $"Command group '{group}' is not implemented in the current Preview scope.")),
                output,
                error)
        };
    }

    private static int RunProject(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count == 1 && string.Equals(options.Values[0], "validate", StringComparison.OrdinalIgnoreCase))
        {
            var result = CliResult.Success(
                "project validate",
                options,
                NormalizeProjectRoot(options.ProjectRoot),
                CliRoute.Headless,
                "Project validation command completed.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: new JsonObject
                {
                    ["validationMode"] = "previewStub"
                });
            return WriteResult(result, output, error);
        }

        return WriteResult(
            CliResult.Blocked(
                BuildCommandName("project", options),
                options,
                "Unknown project command.",
                CreateCliDiagnostic("E2D-CLI-0001", "Only `e2d project validate` is implemented in the current Preview CLI scope.")),
            output,
            error);
    }

    private static int RunValidate(CliOptions options, TextWriter output, TextWriter error)
    {
        var result = CliResult.Success(
            "validate",
            options,
            NormalizeProjectRoot(options.ProjectRoot),
            CliRoute.Headless,
            "Project validation command completed.",
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data: new JsonObject
            {
                ["validationMode"] = "previewStub"
            });
        return WriteResult(result, output, error);
    }

    private static int RunDoctor(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 0)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("doctor", options),
                    options,
                    "Unknown doctor command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d doctor` does not take a subcommand in the current Preview scope.")),
                output,
                error);
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var verification = ProjectReproducibilityLockVerifier.Verify(projectRoot);
        var data = BuildDoctorData(projectRoot, verification);
        var result = CliResult.Report(
            "doctor",
            options,
            projectRoot,
            CliRoute.None,
            "Environment diagnostics completed.",
            verification.Diagnostics,
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data);
        return WriteResult(result, output, error);
    }

    private static int RunMcp(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (options.Values.Count != 1 || !string.Equals(options.Values[0], "serve", StringComparison.OrdinalIgnoreCase))
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("mcp", options),
                    options,
                    "Unknown MCP command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d mcp serve` is the implemented local MCP manifest command.")),
                output,
                error);
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        using var session = McpServerSession.Open(
            projectRoot,
            options.Headless ? null : context.SessionRegistry,
            context.NowUtc);
        var result = CliResult.Success(
            "mcp serve",
            options,
            projectRoot,
            ToCliRoute(session.Route),
            "MCP manifest emitted.",
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data: session.Manifest());
        return WriteResult(result, output, error);
    }

    private static int RunWorkspace(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (options.Values.Count != 1 || !string.Equals(options.Values[0], "transaction", StringComparison.OrdinalIgnoreCase))
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("workspace", options),
                    options,
                    "Unknown workspace command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d workspace transaction` is the implemented generic mutation command.")),
                output,
                error);
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var relativePath = options.RequireOption("--path", "workspace transaction requires --path <relative-file>.");
        var text = options.RequireOption("--text", "workspace transaction requires --text <text>.");
        var expectedRevision = options.RequireInt64("--expected-revision", "workspace transaction requires --expected-revision <n>.");
        var operationId = $"cli-workspace-transaction-{Guid.NewGuid():N}";
        var undoGroupId = options.DryRun ? null : $"undo-{operationId}";
        using var route = OpenWorkspaceForMutation(
            options,
            context,
            projectRoot,
            relativePath,
            new ProjectDocumentRevision(expectedRevision));

        var mode = route.Route == CliRoute.ActiveEditor
            ? ToolingApplyMode.WorkspaceOnly
            : ToolingApplyMode.HeadlessCommit;
        var result = route.Tooling.Project.ApplyTextEdit(new ToolingTextEditRequest(
            operationId,
            "workspace.transaction",
            mode,
            relativePath,
            new ProjectDocumentRevision(expectedRevision),
            text,
            undoGroupId,
            options.DryRun),
            new OperationContext(
                "cli",
                PrincipalKind.Cli,
                "cli-session",
                [OperationCapability.TaskWrite],
                "e2d"));

        var envelope = CliResult.FromOperation(
            "workspace transaction",
            options,
            projectRoot,
            route.Route,
            result,
            route.Diagnostics);
        return WriteResult(envelope, output, error);
    }

    private static int RunHeadlessRuntime(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var request = HeadlessRuntimeAutomation.Parse(options, projectRoot);
        var buildConfigurationHash = options.GetOption("--input-build-configuration-hash") ?? "sha256:default";
        using var route = OpenWorkspaceForJob(options, context, projectRoot);
        HeadlessRuntimeAutomation.OpenRuntimeInputsIfNeeded(route.Workspace, projectRoot, request);
        var operationId = HeadlessRuntimeAutomation.CreateOperationId(request, route.Workspace, buildConfigurationHash);
        var job = route.Tooling.Runtime.Queue(new ToolingJobRequest(operationId, buildConfigurationHash));
        var result = HeadlessRuntimeAutomation.Run(
            options,
            projectRoot,
            route.Route,
            request,
            job,
            buildConfigurationHash,
            route.Diagnostics);
        return WriteResult(result, output, error);
    }

    private static int RunSceneTests(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var manifestPath = options.GetOption("--manifest") ?? DefaultSceneTestManifestPath;
        var outputDirectory = ResolveSceneTestOutputDirectory(projectRoot, options.GetOption("--output") ?? ".electron2d/test-artifacts/latest");
        var buildConfigurationHash = options.GetOption("--input-build-configuration-hash") ?? "sha256:default";
        var result = SceneTestRunner.Run(new SceneTestRunRequest(
            projectRoot,
            manifestPath,
            outputDirectory,
            buildConfigurationHash,
            context.NowUtc));

        return WriteResult(CliResult.FromSceneTests(options, projectRoot, result), output, error);
    }

    private static int RunJob(
        string command,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var buildConfigurationHash = options.GetOption("--input-build-configuration-hash") ?? "sha256:default";
        using var route = OpenWorkspaceForJob(options, context, projectRoot);
        var operationId = $"cli-{command}-{Guid.NewGuid():N}";
        var request = new ToolingJobRequest(operationId, buildConfigurationHash);
        var result = command switch
        {
            "import" => route.Tooling.Import.Queue(request),
            "build" => route.Tooling.Build.Queue(request),
            "run" => route.Tooling.Runtime.Queue(request),
            "test" => route.Tooling.Tests.Queue(request),
            "export" => route.Tooling.Export.Queue(request),
            _ => throw new InvalidOperationException($"Unsupported job command '{command}'.")
        };

        if (options.Format == CliOutputFormat.Jsonl)
        {
            output.WriteLine(WriteJobEvent(command, route.Route, result, buildConfigurationHash).ToJsonString());
            return 0;
        }

        return WriteResult(
            CliResult.FromJob(command, options, projectRoot, route.Route, result, buildConfigurationHash, route.Diagnostics),
            output,
            error);
    }

    private static CliWorkspaceRoute OpenWorkspaceForMutation(
        CliOptions options,
        CliExecutionContext context,
        string projectRoot,
        string relativePath,
        ProjectDocumentRevision expectedRevision)
    {
        if (!options.Headless && context.SessionRegistry is not null)
        {
            var active = context.SessionRegistry.Connect(
                EditorSessionAdapterKind.Cli,
                projectRoot,
                "cli",
                context.NowUtc);
            if (active.State == EditorSessionConnectionState.ActiveEditor)
            {
                return new CliWorkspaceRoute(CliRoute.ActiveEditor, active.Workspace, active.Tooling, active.Diagnostics, active);
            }

            active.Dispose();
        }

        var workspace = ProjectWorkspace.CreateHeadless(projectRoot, "cli");
        OpenDocumentIfNeeded(workspace, projectRoot, relativePath, expectedRevision);
        return new CliWorkspaceRoute(CliRoute.Headless, workspace, new ProjectToolingHost(workspace), [], owned: workspace);
    }

    private static CliWorkspaceRoute OpenWorkspaceForJob(
        CliOptions options,
        CliExecutionContext context,
        string projectRoot)
    {
        if (!options.Headless && context.SessionRegistry is not null)
        {
            var active = context.SessionRegistry.Connect(
                EditorSessionAdapterKind.Cli,
                projectRoot,
                "cli",
                context.NowUtc);
            if (active.State == EditorSessionConnectionState.ActiveEditor)
            {
                return new CliWorkspaceRoute(CliRoute.ActiveEditor, active.Workspace, active.Tooling, active.Diagnostics, active);
            }

            active.Dispose();
        }

        var workspace = ProjectWorkspace.CreateHeadless(projectRoot, "cli");
        OpenProjectDocuments(workspace, projectRoot);
        return new CliWorkspaceRoute(CliRoute.Headless, workspace, new ProjectToolingHost(workspace), [], owned: workspace);
    }

    private static void OpenProjectDocuments(ProjectWorkspace workspace, string projectRoot)
    {
        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.scene.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(projectRoot, file).Replace(Path.DirectorySeparatorChar, '/');
            OpenDocumentIfNeeded(workspace, projectRoot, relativePath, new ProjectDocumentRevision(1));
        }
    }

    private static void OpenDocumentIfNeeded(
        ProjectWorkspace workspace,
        string projectRoot,
        string relativePath,
        ProjectDocumentRevision persistedRevision)
    {
        if (workspace.Documents.Documents.Any(document => string.Equals(document.Path, relativePath, StringComparison.Ordinal)))
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        workspace.CommandBus.OpenTextDocument(
            relativePath,
            File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty,
            persistedRevision.Value,
            ProjectWorkspaceOperationContext.ForTest($"cli-open-{Guid.NewGuid():N}"));
    }

    private static int WriteResult(CliResult result, TextWriter output, TextWriter error)
    {
        if (result.Options.Format == CliOutputFormat.Jsonl)
        {
            output.WriteLine(result.ToJson().ToJsonString());
            return result.ExitCode;
        }

        if (result.Options.Format == CliOutputFormat.Sarif)
        {
            output.WriteLine(DiagnosticSarifSerializer.WriteRun("Electron2D", "https://electron2d.dev", result.Diagnostics).ToJsonString(CliJsonOptions));
            return result.ExitCode;
        }

        if (result.Options.Format == CliOutputFormat.Json)
        {
            output.WriteLine(result.ToJson().ToJsonString(CliJsonOptions));
            return result.ExitCode;
        }

        if (!result.Options.Quiet)
        {
            var writer = result.Succeeded ? output : error;
            writer.WriteLine(result.Message);
            foreach (var diagnostic in result.Diagnostics)
            {
                writer.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }
        }

        return result.ExitCode;
    }

    private static int WriteCliError(CliCommandException exception, TextWriter output, TextWriter error)
    {
        return WriteResult(
            CliResult.Blocked(
                exception.Command,
                exception.Options,
                exception.Message,
                exception.Diagnostic),
            output,
            error);
    }

    private static void WriteGroupHelp(string group, TextWriter output)
    {
        output.WriteLine($"Usage: e2d {group} [command] [options]");
        output.WriteLine();
        output.WriteLine("Common options: --project <path> --format text|json|jsonl|sarif --quiet --verbose");
        if (MutatingOrJobGroups.Contains(group, StringComparer.Ordinal))
        {
            output.WriteLine("Mutation/job options: --dry-run --headless");
        }

        output.WriteLine();
        output.WriteLine("Commands:");
        var commands = group switch
        {
            "project" => "  validate              Validate a project without requiring GUI runtime.",
            "mcp" => "  serve                 Emit local MCP resources and tools manifest.",
            "doctor" => "  <default>             Inspect reproducibility lock and local environment without opening workspace.",
            "workspace" => "  transaction           Apply a generic workspace text transaction.",
            "run" => "  debug                 Inspect runtime state, or queue/run headless with runtime options.",
            "import" or "build" or "export" => "  <default>             Queue a job and emit JSON or JSONL status.",
            "test" => "  <default>             Queue a job, or run scene tests with --format json and a scene-test manifest.",
            "validate" => "  <default>             Validate a project and emit text, JSON or SARIF diagnostics.",
            "docs" => "  search|type|member|example",
            _ => "  Reserved for a later Preview task."
        };
        output.WriteLine(commands);
    }

    private static JsonObject WriteJobEvent(
        string command,
        CliRoute route,
        ToolingJobResult result,
        string buildConfigurationHash)
    {
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["command"] = command,
            ["event"] = "operation.queued",
            ["route"] = RouteName(route),
            ["operationId"] = result.OperationId,
            ["jobId"] = result.JobId,
            ["jobKind"] = result.JobKind.ToString(),
            ["jobState"] = result.JobState.ToString(),
            ["inputSnapshotId"] = result.InputSnapshotId,
            ["inputWorkspaceRevision"] = result.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = result.InputContentRevision.Value,
            ["inputDocumentRevisions"] = WriteRevisions(result.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = buildConfigurationHash,
            ["stale"] = false,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics),
            ["artifacts"] = new JsonArray()
        };
    }

    private static bool HasSceneTestSuite(CliOptions options, string projectRoot)
    {
        if (options.Format != CliOutputFormat.Json)
        {
            return false;
        }

        if (options.GetOption("--manifest") is not null)
        {
            return true;
        }

        return File.Exists(Path.Combine(projectRoot, DefaultSceneTestManifestPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string ResolveSceneTestOutputDirectory(string projectRoot, string outputDirectory)
    {
        var fullPath = Path.IsPathRooted(outputDirectory)
            ? Path.GetFullPath(outputDirectory)
            : Path.GetFullPath(Path.Combine(projectRoot, outputDirectory));
        if (!Path.IsPathRooted(outputDirectory))
        {
            WorkspaceSnapshotMaterializer.EnsureChildPath(projectRoot, fullPath);
        }

        return fullPath;
    }

    internal static JsonArray WriteDiagnostics(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        return DiagnosticJsonSerializer.ToJsonArray(diagnostics);
    }

    internal static JsonObject WriteRevisions(IReadOnlyDictionary<string, ProjectDocumentRevision> revisions)
    {
        var root = new JsonObject();
        foreach (var pair in revisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[pair.Key] = pair.Value.Value;
        }

        return root;
    }

    internal static StructuredDiagnostic CreateCliDiagnostic(string code, string message)
    {
        var definition = DiagnosticCodeRegistry.Get(code);
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static string BuildCommandName(string group, CliOptions options)
    {
        return string.Join(' ', new[] { group }.Concat(options.Values));
    }

    private static JsonObject BuildDoctorData(
        string projectRoot,
        ProjectReproducibilityLockVerificationResult verification)
    {
        var lockJson = TryReadJsonObject(Path.Combine(projectRoot, ProjectReproducibilityLockVerifier.LockFileName));
        var checks = new JsonArray
        {
            BuildDotnetSdkCheck(lockJson),
            BuildElectron2DCheck(verification),
            BuildLockArrayCheck("nativeRuntime", "nativeRuntime", lockJson),
            BuildEnvironmentVariableCheck("androidSdk", ["ANDROID_SDK_ROOT", "ANDROID_HOME"]),
            BuildEnvironmentVariableCheck("androidNdk", ["ANDROID_NDK_ROOT", "ANDROID_NDK_HOME"]),
            BuildXcodeCheck(),
            BuildExportTemplatesCheck(lockJson),
            BuildGraphicsCapabilitiesCheck(lockJson),
            BuildSigningCheck(projectRoot, lockJson)
        };
        var summaryStatus = SummarizeDoctorStatus(checks);

        return new JsonObject
        {
            ["mode"] = "doctor.environment",
            ["summary"] = new JsonObject
            {
                ["status"] = summaryStatus,
                ["blocked"] = string.Equals(summaryStatus, "blocked", StringComparison.Ordinal)
            },
            ["checks"] = checks
        };
    }

    private static JsonObject BuildDotnetSdkCheck(JsonObject? lockJson)
    {
        var expectedSdk = ReadLockString(lockJson, "dotnet", "sdkVersion");
        var actualSdk = ReadDotnetSdkVersion();
        if (actualSdk is null)
        {
            return DoctorCheck(
                "dotnetSdk",
                "blocked",
                "dotnet SDK was not found on PATH.",
                new JsonObject
                {
                    ["expectedSdkVersion"] = expectedSdk
                });
        }

        if (!string.IsNullOrWhiteSpace(expectedSdk) &&
            !string.Equals(actualSdk, expectedSdk, StringComparison.Ordinal))
        {
            return DoctorCheck(
                "dotnetSdk",
                "warning",
                "Installed dotnet SDK differs from the project reproducibility lock.",
                new JsonObject
                {
                    ["expectedSdkVersion"] = expectedSdk,
                    ["actualSdkVersion"] = actualSdk
                });
        }

        return DoctorCheck(
            "dotnetSdk",
            "ok",
            "dotnet SDK is available.",
            new JsonObject
            {
                ["expectedSdkVersion"] = expectedSdk,
                ["actualSdkVersion"] = actualSdk
            });
    }

    private static JsonObject BuildElectron2DCheck(ProjectReproducibilityLockVerificationResult verification)
    {
        return verification.Succeeded
            ? DoctorCheck("electron2d", "ok", "Electron2D package version matches the reproducibility lock.")
            : DoctorCheck("electron2d", "blocked", "Electron2D reproducibility files are missing, malformed or inconsistent.");
    }

    private static JsonObject BuildLockArrayCheck(string id, string lockSection, JsonObject? lockJson)
    {
        var packages = ReadLockArray(lockJson, lockSection, "packages");
        if (packages is null || packages.Count == 0)
        {
            return DoctorCheck(id, "blocked", $"The {lockSection}.packages lock section is missing or empty.");
        }

        return DoctorCheck(
            id,
            "ok",
            $"The {lockSection}.packages lock section declares {packages.Count} package(s).",
            new JsonObject
            {
                ["packageCount"] = packages.Count
            });
    }

    private static JsonObject BuildEnvironmentVariableCheck(string id, IReadOnlyList<string> variableNames)
    {
        foreach (var variableName in variableNames)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName)))
            {
                return DoctorCheck(
                    id,
                    "ok",
                    $"{variableName} is set.",
                    new JsonObject
                    {
                        ["source"] = variableName
                    });
            }
        }

        return DoctorCheck(
            id,
            "missing",
            string.Join(" or ", variableNames) + " is not set.",
            new JsonObject
            {
                ["sources"] = WriteStringArray(variableNames)
            });
    }

    private static JsonObject BuildXcodeCheck()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return DoctorCheck("xcode", "missing", "Xcode is only available on macOS hosts.");
        }

        var xcodeVersion = ReadCommandFirstLine("xcodebuild", "-version");
        return xcodeVersion is null
            ? DoctorCheck("xcode", "missing", "xcodebuild was not found.")
            : DoctorCheck(
                "xcode",
                "ok",
                "xcodebuild is available.",
                new JsonObject
                {
                    ["version"] = xcodeVersion
                });
    }

    private static JsonObject BuildExportTemplatesCheck(JsonObject? lockJson)
    {
        var version = ReadLockString(lockJson, "exportTemplates", "version");
        return string.IsNullOrWhiteSpace(version)
            ? DoctorCheck("exportTemplates", "blocked", "exportTemplates.version is missing from the reproducibility lock.")
            : DoctorCheck(
                "exportTemplates",
                "ok",
                "Export template version is declared in the reproducibility lock.",
                new JsonObject
                {
                    ["version"] = version
                });
    }

    private static JsonObject BuildGraphicsCapabilitiesCheck(JsonObject? lockJson)
    {
        var rendererProfile = ReadLockString(lockJson, "project", "rendererProfile");
        return string.IsNullOrWhiteSpace(rendererProfile)
            ? DoctorCheck("graphicsCapabilities", "blocked", "project.rendererProfile is missing from the reproducibility lock.")
            : DoctorCheck(
                "graphicsCapabilities",
                "ok",
                "Renderer profile is declared in the reproducibility lock.",
                new JsonObject
                {
                    ["rendererProfile"] = rendererProfile
                });
    }

    private static JsonObject BuildSigningCheck(string projectRoot, JsonObject? lockJson)
    {
        var signingMode = ReadLockString(lockJson, "signing", "mode");
        if (!string.Equals(signingMode, "referencesOnly", StringComparison.Ordinal))
        {
            return DoctorCheck("signing", "blocked", "signing.mode must be referencesOnly.");
        }

        var exportPresetsPath = Path.Combine(projectRoot, "export_presets.e2export.json");
        if (!File.Exists(exportPresetsPath))
        {
            return DoctorCheck(
                "signing",
                "ok",
                "No export presets were found; signing policy uses references only.",
                new JsonObject
                {
                    ["presetCount"] = 0,
                    ["credentialReferences"] = new JsonArray()
                });
        }

        var presets = TryReadJsonObject(exportPresetsPath)?["presets"] as JsonArray;
        if (presets is null)
        {
            return DoctorCheck("signing", "blocked", "export_presets.e2export.json could not be read as a preset list.");
        }

        var requiredSigningCount = 0;
        var missingRequiredReferences = 0;
        var references = new JsonArray();
        foreach (var preset in presets.OfType<JsonObject>())
        {
            var signing = preset["signing"] as JsonObject;
            if (signing is null)
            {
                continue;
            }

            var required = signing["required"] is JsonValue requiredValue &&
                requiredValue.TryGetValue<bool>(out var requiredFlag) &&
                requiredFlag;
            var credentialReference = signing["credentialReference"] is JsonValue referenceValue &&
                referenceValue.TryGetValue<string>(out var reference)
                    ? reference ?? string.Empty
                    : string.Empty;

            if (required)
            {
                requiredSigningCount++;
                if (string.IsNullOrWhiteSpace(credentialReference))
                {
                    missingRequiredReferences++;
                }
            }

            if (!string.IsNullOrWhiteSpace(credentialReference))
            {
                references.Add(SanitizeSigningReference(credentialReference));
            }
        }

        var status = missingRequiredReferences == 0 ? "ok" : "warning";
        return DoctorCheck(
            "signing",
            status,
            missingRequiredReferences == 0
                ? "Signing presets use external references only."
                : "Some required signing presets do not declare a credential reference.",
            new JsonObject
            {
                ["presetCount"] = presets.Count,
                ["requiredSigningCount"] = requiredSigningCount,
                ["missingRequiredReferences"] = missingRequiredReferences,
                ["credentialReferences"] = references
            });
    }

    private static JsonObject DoctorCheck(string id, string status, string message, JsonObject? details = null)
    {
        return new JsonObject
        {
            ["id"] = id,
            ["status"] = status,
            ["message"] = message,
            ["details"] = details ?? new JsonObject()
        };
    }

    private static string SummarizeDoctorStatus(JsonArray checks)
    {
        var statuses = checks
            .OfType<JsonObject>()
            .Select(check => check["status"]?.GetValue<string>() ?? "blocked")
            .ToArray();
        if (statuses.Contains("blocked", StringComparer.Ordinal))
        {
            return "blocked";
        }

        return statuses.Any(status => status is "warning" or "missing")
            ? "warning"
            : "ok";
    }

    private static JsonObject? TryReadJsonObject(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                : null;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ReadLockString(JsonObject? lockJson, string section, string property)
    {
        return lockJson?[section] is JsonObject sectionObject &&
            sectionObject[property] is JsonValue value &&
            value.TryGetValue<string>(out var text)
                ? text
                : null;
    }

    private static JsonArray? ReadLockArray(JsonObject? lockJson, string section, string property)
    {
        return lockJson?[section] is JsonObject sectionObject
            ? sectionObject[property] as JsonArray
            : null;
    }

    private static string? ReadDotnetSdkVersion()
    {
        return ReadCommandFirstLine("dotnet", "--version");
    }

    private static string? ReadCommandFirstLine(string fileName, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (!process.Start())
            {
                return null;
            }

            if (!process.WaitForExit(3000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                return null;
            }

            return process.ExitCode == 0
                ? process.StandardOutput.ReadLine()
                : null;
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string SanitizeSigningReference(string credentialReference)
    {
        return credentialReference.StartsWith("env:", StringComparison.Ordinal)
            ? credentialReference
            : "external-reference";
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var result = new JsonArray();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static string NormalizeProjectRoot(string projectRoot)
    {
        return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    internal static string RouteName(CliRoute route)
    {
        return route switch
        {
            CliRoute.None => "none",
            CliRoute.ActiveEditor => "activeEditor",
            CliRoute.Headless => "headless",
            CliRoute.Blocked => "blocked",
            _ => "none"
        };
    }

    private static CliRoute ToCliRoute(McpRoute route)
    {
        return route switch
        {
            McpRoute.ActiveEditor => CliRoute.ActiveEditor,
            McpRoute.Headless => CliRoute.Headless,
            McpRoute.Blocked => CliRoute.Blocked,
            _ => CliRoute.Blocked
        };
    }
}

internal sealed class CliExecutionContext
{
    private CliExecutionContext(DateTimeOffset nowUtc, EditorSessionRegistry? sessionRegistry)
    {
        NowUtc = nowUtc;
        SessionRegistry = sessionRegistry;
    }

    public DateTimeOffset NowUtc { get; }

    public EditorSessionRegistry? SessionRegistry { get; }

    public static CliExecutionContext Default()
    {
        return new CliExecutionContext(DateTimeOffset.UtcNow, sessionRegistry: null);
    }

    public static CliExecutionContext ForTests(DateTimeOffset nowUtc, EditorSessionRegistry? sessionRegistry = null)
    {
        return new CliExecutionContext(nowUtc, sessionRegistry);
    }
}

internal enum CliOutputFormat
{
    Text,
    Json,
    Jsonl,
    Sarif
}

internal enum CliRoute
{
    None,
    ActiveEditor,
    Headless,
    Blocked
}

internal sealed class CliOptions
{
    private readonly IReadOnlyDictionary<string, string> options;

    private CliOptions(
        string group,
        CliOutputFormat format,
        string projectRoot,
        bool quiet,
        bool verbose,
        bool dryRun,
        bool headless,
        bool help,
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, string> options)
    {
        Group = group;
        Format = format;
        ProjectRoot = projectRoot;
        Quiet = quiet;
        Verbose = verbose;
        DryRun = dryRun;
        Headless = headless;
        Help = help;
        Values = values;
        this.options = options;
    }

    public string Group { get; }

    public CliOutputFormat Format { get; }

    public string ProjectRoot { get; }

    public bool Quiet { get; }

    public bool Verbose { get; }

    public bool DryRun { get; }

    public bool Headless { get; }

    public bool Help { get; }

    public IReadOnlyList<string> Values { get; }

    public static CliOptions Parse(string group, IEnumerable<string> arguments)
    {
        var values = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var format = CliOutputFormat.Text;
        var projectRoot = Environment.CurrentDirectory;
        var quiet = false;
        var verbose = false;
        var dryRun = false;
        var headless = false;
        var help = false;
        var items = arguments.ToArray();
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            if (item is "--help" or "-h" or "help")
            {
                help = true;
                continue;
            }

            if (item is "--quiet")
            {
                quiet = true;
                continue;
            }

            if (item is "--verbose")
            {
                verbose = true;
                continue;
            }

            if (item is "--dry-run")
            {
                dryRun = true;
                continue;
            }

            if (item is "--headless")
            {
                headless = true;
                continue;
            }

            if (item is "--format")
            {
                var value = RequireValue(items, ref index, "--format");
                format = value.ToLowerInvariant() switch
                {
                    "text" => CliOutputFormat.Text,
                    "json" => CliOutputFormat.Json,
                    "jsonl" => CliOutputFormat.Jsonl,
                    "sarif" => CliOutputFormat.Sarif,
                    _ => throw new CliCommandException(
                        group,
                        Placeholder(group),
                        "Unsupported output format. Use text, json, jsonl or sarif.",
                        Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", "Unsupported output format."))
                };
                continue;
            }

            if (item is "--project")
            {
                projectRoot = RequireValue(items, ref index, "--project");
                continue;
            }

            if (item.StartsWith("--", StringComparison.Ordinal))
            {
                options[item] = RequireValue(items, ref index, item);
                continue;
            }

            values.Add(item);
        }

        return new CliOptions(
            group,
            format,
            projectRoot,
            quiet,
            verbose,
            dryRun,
            headless,
            help,
            values,
            new ReadOnlyDictionary<string, string>(options));
    }

    public string? GetOption(string name)
    {
        return options.TryGetValue(name, out var value) ? value : null;
    }

    public string RequireOption(string name, string message)
    {
        return GetOption(name) ?? throw new CliCommandException(
            string.Join(' ', new[] { Group }.Concat(Values)),
            this,
            message,
            Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", message));
    }

    public long RequireInt64(string name, string message)
    {
        var value = RequireOption(name, message);
        return long.TryParse(value, out var parsed)
            ? parsed
            : throw new CliCommandException(
                string.Join(' ', new[] { Group }.Concat(Values)),
                this,
                $"{name} must be an integer.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"{name} must be an integer."));
    }

    private static string RequireValue(string[] items, ref int index, string option)
    {
        if (index + 1 >= items.Length)
        {
            throw new CommandLineException($"Missing value for {option}.");
        }

        return items[++index];
    }

    private static CliOptions Placeholder(string group)
    {
        return new CliOptions(
            group,
            CliOutputFormat.Json,
            Environment.CurrentDirectory,
            quiet: false,
            verbose: false,
            dryRun: false,
            headless: false,
            help: false,
            [],
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
    }
}

internal sealed class CliResult
{
    private CliResult(
        string command,
        CliOptions options,
        bool succeeded,
        int exitCode,
        string projectRoot,
        CliRoute route,
        string message,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> dirtyDocuments,
        JsonObject? operation,
        JsonObject? job,
        JsonObject data)
    {
        Command = command;
        Options = options;
        Succeeded = succeeded;
        ExitCode = exitCode;
        ProjectRoot = projectRoot;
        Route = route;
        Message = message;
        Diagnostics = diagnostics.ToArray();
        ChangedFiles = changedFiles.ToArray();
        DirtyDocuments = dirtyDocuments.ToArray();
        Operation = operation;
        Job = job;
        Data = data;
    }

    public string Command { get; }

    public CliOptions Options { get; }

    public bool Succeeded { get; }

    public int ExitCode { get; }

    public string ProjectRoot { get; }

    public CliRoute Route { get; }

    public string Message { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public IReadOnlyList<string> ChangedFiles { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }

    public JsonObject? Operation { get; }

    public JsonObject? Job { get; }

    public JsonObject Data { get; }

    public static CliResult Success(
        string command,
        CliOptions options,
        string projectRoot,
        CliRoute route,
        string message,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> dirtyDocuments,
        JsonObject? operation,
        JsonObject? job,
        JsonObject data)
    {
        return new CliResult(
            command,
            options,
            succeeded: true,
            exitCode: 0,
            projectRoot,
            route,
            message,
            diagnostics: [],
            changedFiles,
            dirtyDocuments,
            operation,
            job,
            data);
    }

    public static CliResult Blocked(
        string command,
        CliOptions options,
        string message,
        StructuredDiagnostic diagnostic)
    {
        return new CliResult(
            command,
            options,
            succeeded: false,
            exitCode: 1,
            options.ProjectRoot,
            CliRoute.Blocked,
            message,
            [diagnostic],
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data: new JsonObject());
    }

    public static CliResult Report(
        string command,
        CliOptions options,
        string projectRoot,
        CliRoute route,
        string message,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> dirtyDocuments,
        JsonObject? operation,
        JsonObject? job,
        JsonObject data)
    {
        return new CliResult(
            command,
            options,
            succeeded: true,
            exitCode: 0,
            projectRoot,
            route,
            message,
            diagnostics,
            changedFiles,
            dirtyDocuments,
            operation,
            job,
            data);
    }

    public static CliResult FromOperation(
        string command,
        CliOptions options,
        string projectRoot,
        CliRoute route,
        ToolingOperationResult result,
        IReadOnlyList<StructuredDiagnostic> routeDiagnostics)
    {
        var diagnostics = routeDiagnostics.Concat(result.Diagnostics).ToArray();
        return new CliResult(
            command,
            options,
            result.Succeeded,
            result.Succeeded ? 0 : 1,
            projectRoot,
            route,
            result.Succeeded ? "Workspace transaction applied." : "Workspace transaction failed.",
            diagnostics,
            result.ChangedFiles,
            result.DirtyDocuments,
            new JsonObject
            {
                ["operationId"] = result.OperationId,
                ["operationKind"] = result.OperationKind,
                ["workspaceRevision"] = result.WorkspaceRevision.Value,
                ["contentRevision"] = result.ContentRevision.Value,
                ["documentRevisions"] = Electron2DCommandLine.WriteRevisions(result.DocumentRevisions),
                ["persistenceState"] = result.PersistenceState.ToString(),
                ["undoGroupId"] = result.UndoGroupId
            },
            job: null,
            data: new JsonObject());
    }

    public static CliResult FromJob(
        string command,
        CliOptions options,
        string projectRoot,
        CliRoute route,
        ToolingJobResult result,
        string buildConfigurationHash,
        IReadOnlyList<StructuredDiagnostic> routeDiagnostics)
    {
        return new CliResult(
            command,
            options,
            result.Succeeded,
            result.Succeeded ? 0 : 1,
            projectRoot,
            route,
            "Workspace job queued.",
            routeDiagnostics.Concat(result.Diagnostics).ToArray(),
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: new JsonObject
            {
                ["operationId"] = result.OperationId,
                ["jobId"] = result.JobId,
                ["jobKind"] = result.JobKind.ToString(),
                ["jobState"] = result.JobState.ToString(),
                ["inputSnapshotId"] = result.InputSnapshotId,
                ["inputWorkspaceRevision"] = result.InputWorkspaceRevision.Value,
                ["inputContentRevision"] = result.InputContentRevision.Value,
                ["inputDocumentRevisions"] = Electron2DCommandLine.WriteRevisions(result.InputDocumentRevisions),
                ["inputBuildConfigurationHash"] = buildConfigurationHash,
                ["stale"] = false
            },
            data: new JsonObject());
    }

    public static CliResult FromSceneTests(
        CliOptions options,
        string projectRoot,
        SceneTestRunResult result)
    {
        var data = JsonNode.Parse(result.ToJson().ToJsonString())!.AsObject();
        data["mode"] = "test.scene";
        return new CliResult(
            "test",
            options,
            result.Succeeded,
            result.Succeeded ? 0 : 1,
            projectRoot,
            CliRoute.Headless,
            result.Succeeded ? "Scene tests completed." : "Scene tests failed.",
            result.Diagnostics,
            result.Artifacts.Values.ToArray(),
            dirtyDocuments: [],
            operation: null,
            job: null,
            data);
    }

    public JsonObject ToJson()
    {
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["command"] = Command,
            ["succeeded"] = Succeeded,
            ["exitCode"] = ExitCode,
            ["projectRoot"] = ProjectRoot,
            ["route"] = Electron2DCommandLine.RouteName(Route),
            ["dryRun"] = Options.DryRun,
            ["message"] = Message,
            ["diagnostics"] = Electron2DCommandLine.WriteDiagnostics(Diagnostics),
            ["changedFiles"] = WriteStringArray(ChangedFiles),
            ["dirtyDocuments"] = WriteStringArray(DirtyDocuments),
            ["operation"] = Operation is null ? null : JsonNode.Parse(Operation.ToJsonString()),
            ["job"] = Job is null ? null : JsonNode.Parse(Job.ToJsonString()),
            ["data"] = JsonNode.Parse(Data.ToJsonString())
        };
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values.OrderBy(value => value, StringComparer.Ordinal))
        {
            array.Add(value);
        }

        return array;
    }
}

internal sealed class CliCommandException : Exception
{
    public CliCommandException(string command, CliOptions options, string message, StructuredDiagnostic diagnostic)
        : base(message)
    {
        Command = command;
        Options = options;
        Diagnostic = diagnostic;
    }

    public string Command { get; }

    public CliOptions Options { get; }

    public StructuredDiagnostic Diagnostic { get; }
}

internal sealed class CliWorkspaceRoute : IDisposable
{
    private readonly IDisposable owned;
    private bool disposed;

    public CliWorkspaceRoute(
        CliRoute route,
        ProjectWorkspace workspace,
        ProjectToolingHost tooling,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IDisposable owned)
    {
        Route = route;
        Workspace = workspace;
        Tooling = tooling;
        Diagnostics = diagnostics.ToArray();
        this.owned = owned;
    }

    public CliRoute Route { get; }

    public ProjectWorkspace Workspace { get; }

    public ProjectToolingHost Tooling { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        owned.Dispose();
    }
}
