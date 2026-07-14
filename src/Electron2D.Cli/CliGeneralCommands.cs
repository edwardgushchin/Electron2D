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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        "tasks",
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
        "export",
        "tasks"
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
        if (string.Equals(group, "tasks", StringComparison.Ordinal) && !options.Help)
        {
            options = options.WithTaskInput(context.Input);
        }
        options = options.WithCancellationToken(context.CancellationToken);

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
            "api" => RunApi(options, output, error),
            "mcp" => RunMcp(options, output, error, context),
            "tasks" => RunTasks(options, output, error, context),
            "context" => RunContext(options, output, error, context),
            "doctor" => RunDoctor(options, output, error),
            "workspace" => RunWorkspace(options, output, error, context),
            "validate" => RunValidate(options, output, error),
            "run" when IsRuntimeDebugCommand(options) => RunRuntimeDebug(options, output, error),
            "run" when IsProjectRuntimeRun(options) => RunProjectRuntime(options, output, error),
            "run" when HeadlessRuntimeAutomation.HasRuntimeOptions(options) => RunHeadlessRuntime(options, output, error, context),
            "test" when HasSceneTestSuite(options, NormalizeProjectRoot(options.ProjectRoot)) => RunSceneTests(options, output, error, context),
            "export" when IsWindowsExportCommand(options) => RunWindowsExport(options, output, error),
            "export" when IsWebExportCommand(options) => RunWebExport(options, output, error, context),
            "export" when IsAndroidExportCommand(options) => RunAndroidExport(options, output, error, context),
            "export" when IsIosExportCommand(options) => RunIosExport(options, output, error, context),
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
        if (options.Values.Count == 2 && string.Equals(options.Values[0], "create", StringComparison.OrdinalIgnoreCase))
        {
            var projectsRoot = options.GetOption("--output") ?? options.ProjectRoot;
            var rendererProfile = options.GetOption("--renderer-profile") ?? "Automatic";
            var templateRoot = Path.Combine(FindRepositoryRoot(), "data", "templates", "electron2d-empty");
            var created = ProjectTemplateCreator.Create(new ProjectTemplateCreateOptions(
                templateRoot,
                options.Values[1],
                projectsRoot,
                rendererProfile,
                InitializeGit: true));
            var result = CliResult.Report(
                "project create",
                options,
                created.ProjectPath,
                CliRoute.Headless,
                "Project created.",
                created.Diagnostics,
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: new JsonObject
                {
                    ["projectName"] = created.ProjectName,
                    ["projectPath"] = created.ProjectPath,
                    ["projectSettingsPath"] = created.ProjectSettingsPath,
                    ["mainScenePath"] = created.MainScenePath,
                    ["rendererProfile"] = created.RendererProfile,
                    ["gitInitialized"] = created.GitInitialized,
                    ["taskBoardPath"] = created.TaskBoardPath,
                    ["starterSkillCount"] = created.StarterSkillCount,
                    ["agentInstructionsPath"] = created.AgentInstructionsPath
                });
            return WriteResult(result, output, error);
        }

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
                CreateCliDiagnostic("E2D-CLI-0001", "Use `e2d project create <name> --output <projects-root>` or `e2d project validate`.")),
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

    private static int RunApi(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count == 0 || !string.Equals(options.Values[0], "compare-godot", StringComparison.OrdinalIgnoreCase))
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("api", options),
                    options,
                    "Unknown API command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d api compare-godot <type>` is the implemented API verifier command.")),
                output,
                error);
        }

        if (options.Values.Count != 2)
        {
            return WriteResult(
                CliResult.Failure(
                    "api compare-godot",
                    options,
                    NormalizeProjectRoot(options.ProjectRoot),
                    CliRoute.None,
                    "API comparison requires a single type name.",
                    CreateCliDiagnostic("E2D-CLI-0002", "Usage: e2d api compare-godot <type> --format json."),
                    BuildApiCompareData(
                        type: null,
                        profileDecision: null,
                        query: options.Values.Count > 1 ? options.Values[1] : null,
                        resultStatus: "invalid_query",
                        manifest: null)),
                output,
                error);
        }

        var query = options.Values[1];
        var manifest = LoadApiManifest();
        var manualProfile = LoadManualApiProfile();
        var profileDecision = FindManualApiProfileType(manualProfile, query);
        var type = profileDecision is null
            ? null
            : FindApiManifestType(manifest, Value(profileDecision, "fullName"));
        if (profileDecision is null)
        {
            return WriteResult(
                CliResult.Failure(
                    "api compare-godot",
                    options,
                    NormalizeProjectRoot(options.ProjectRoot),
                    CliRoute.None,
                    $"API type was not found in the manual public API profile: {query}.",
                    CreateCliDiagnostic("E2D-CLI-0002", $"API type was not found in the manual public API profile: {query}."),
                    BuildApiCompareData(type: null, profileDecision: null, query, "type_not_found", manifest)),
                output,
                error);
        }

        var decision = Value(profileDecision, "decision");
        var outOfProfile = !string.Equals(decision, "approved", StringComparison.Ordinal);
        var resultStatus = outOfProfile ? decision : "profile_approved";
        var data = BuildApiCompareData(type, profileDecision, query, resultStatus, manifest);
        if (outOfProfile)
        {
            var fullName = Value(profileDecision, "fullName");
            return WriteResult(
                CliResult.Failure(
                    "api compare-godot",
                    options,
                    NormalizeProjectRoot(options.ProjectRoot),
                    CliRoute.None,
                    $"API type has manual profile decision '{decision}' and is outside the current public API surface.",
                    CreateCliDiagnostic("E2D-CLI-0002", $"API type '{fullName}' has manual profile decision '{decision}' and is outside the current public API surface."),
                    data),
                output,
                error);
        }

        return WriteResult(
            CliResult.Success(
                "api compare-godot",
                options,
                NormalizeProjectRoot(options.ProjectRoot),
                CliRoute.None,
                type is null
                    ? "API type is approved by the Electron2D public API profile but is not exported by the current runtime surface."
                    : "API type is approved by the Electron2D public API profile.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data),
            output,
            error);
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

    private static bool IsWebExportCommand(CliOptions options)
    {
        return options.Values.Count > 0 &&
            (string.Equals(options.Values[0], "plan-web", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "build-web", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "run-web", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAndroidExportCommand(CliOptions options)
    {
        return options.Values.Count > 0 &&
            (string.Equals(options.Values[0], "plan-android", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "build-android", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "run-android", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsIosExportCommand(CliOptions options)
    {
        return options.Values.Count > 0 &&
            (string.Equals(options.Values[0], "plan-ios", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "build-ios", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "run-ios", StringComparison.OrdinalIgnoreCase));
    }

    private static int RunWebExport(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        return options.Values[0].ToLowerInvariant() switch
        {
            "plan-web" => RunWebExportPlan(options, output, error),
            "build-web" => RunWebExportBuild(options, output, error),
            "run-web" => RunWebExportSmoke(options, output, error, context),
            _ => WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "Use `e2d export plan-web`, `e2d export build-web` or `e2d export run-web`.")),
                output,
                error)
        };
    }

    private static int RunAndroidExport(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        return options.Values[0].ToLowerInvariant() switch
        {
            "plan-android" => RunAndroidExportPlan(options, output, error),
            "build-android" => RunAndroidExportBuild(options, output, error),
            "run-android" => RunAndroidExportSmoke(options, output, error, context),
            _ => WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "Use `e2d export plan-android`, `e2d export build-android` or `e2d export run-android`.")),
                output,
                error)
        };
    }

    private static int RunIosExport(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        return options.Values[0].ToLowerInvariant() switch
        {
            "plan-ios" => RunIosExportPlan(options, output, error),
            "build-ios" => RunIosExportBuild(options, output, error),
            "run-ios" => RunIosExportSmoke(options, output, error, context),
            _ => WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "Use `e2d export plan-ios`, `e2d export build-ios` or `e2d export run-ios`.")),
                output,
                error)
        };
    }

    private static int RunWebExportPlan(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export plan-web` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateWebExportPlanContext("export plan-web", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        return WriteResult(
            CliResult.Success(
                "export plan-web",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "WebAssembly browser export plan created.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildWebExportPlanData(planContext.Plan)),
            output,
            error);
    }

    private static int RunWebExportBuild(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export build-web` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateWebExportPlanContext("export build-web", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        var skipPublish = ReadBooleanOption(options, "--skip-publish", defaultValue: false, "export build-web");
        if (!skipPublish)
        {
            var environment = DetectWebExportToolchainEnvironment();
            var validation = Electron2D.Electron2DExportToolchainValidator.Validate(planContext.Preset, environment);
            if (!validation.Succeeded)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export build-web",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "WebAssembly browser export build failed before publish.",
                        MapExportDiagnostics(validation.Diagnostics),
                        BuildWebExportFailureData("export.web.build", "toolchain_failed")),
                    output,
                    error);
            }

            var publishDiagnostics = RunDotnetPublish(planContext.Plan);
            if (publishDiagnostics.Count > 0)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export build-web",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "WebAssembly browser export publish failed.",
                        publishDiagnostics,
                        BuildWebExportFailureData("export.web.build", "publish_failed")),
                    output,
                    error);
            }
        }

        var packageResult = Electron2D.Electron2DWebAssemblyPackageBuilder.Build(
            planContext.Plan,
            planContext.ProjectRoot,
            planContext.Settings);
        if (!packageResult.Succeeded)
        {
            return WriteResult(
                CliResult.Failure(
                    "export build-web",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "WebAssembly browser package build failed.",
                    MapExportDiagnostics(packageResult.Diagnostics),
                    BuildWebExportFailureData("export.web.build", "package_failed")),
                output,
                error);
        }

        return WriteResult(
            CliResult.Success(
                "export build-web",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "WebAssembly browser package created.",
                changedFiles: ToProjectRelativeWebFiles(planContext.ProjectRoot, planContext.Plan.WebRootDirectory, packageResult.Files),
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildWebExportBuildData(planContext.Plan, packageResult, skipPublish)),
            output,
            error);
    }

    private static int RunWebExportSmoke(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export run-web` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateWebExportPlanContext("export run-web", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        var launchUrl = new Uri(options.GetOption("--url") ?? "http://127.0.0.1:8080/index.html", UriKind.Absolute);
        var smokeOutput = ResolveProjectChildPath(
            planContext.ProjectRoot,
            options.GetOption("--smoke-output") ?? Path.Combine(".electron2d", "export-smoke", "web-smoke.json"));
        var smokeResult = Electron2D.Electron2DWebAssemblySmokeRunner.Run(
            planContext.Plan,
            smokeOutput,
            launchUrl,
            context.NowUtc);

        if (!smokeResult.Succeeded)
        {
            return WriteResult(
                CliResult.Failure(
                    "export run-web",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "WebAssembly browser smoke failed.",
                    MapExportDiagnostics(smokeResult.Diagnostics),
                    BuildWebExportRunData(planContext.Plan, smokeResult)),
                output,
                error);
        }

        return WriteResult(
            CliResult.Success(
                "export run-web",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "WebAssembly browser smoke artifact created.",
                changedFiles: [Path.GetRelativePath(planContext.ProjectRoot, smokeOutput).Replace('\\', '/')],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildWebExportRunData(planContext.Plan, smokeResult)),
            output,
            error);
    }

    private static int RunAndroidExportPlan(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export plan-android` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateAndroidExportPlanContext("export plan-android", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        return WriteResult(
            CliResult.Success(
                "export plan-android",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "Android arm64 export plan created.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildAndroidExportPlanData(planContext.Plan)),
            output,
            error);
    }

    private static int RunIosExportPlan(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export plan-ios` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateIosExportPlanContext("export plan-ios", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        return WriteResult(
            CliResult.Success(
                "export plan-ios",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "iOS arm64 export plan created.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildIosExportPlanData(planContext.Plan)),
            output,
            error);
    }

    private static int RunIosExportBuild(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export build-ios` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateIosExportPlanContext("export build-ios", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        var packageResult = Electron2D.Electron2DIosXcodeProjectBuilder.Build(
            planContext.Plan,
            planContext.ProjectRoot,
            planContext.Settings);
        if (!packageResult.Succeeded)
        {
            return WriteResult(
                CliResult.Failure(
                    "export build-ios",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "iOS Xcode project staging failed.",
                    MapExportDiagnostics(packageResult.Diagnostics),
                    BuildIosExportFailureData("export.ios.build", "package_failed", planContext.Plan)),
                output,
                error);
        }

        var skipPublish = ReadBooleanOption(options, "--skip-publish", defaultValue: false, "export build-ios");
        if (!skipPublish)
        {
            var validation = Electron2D.Electron2DExportToolchainValidator.Validate(
                planContext.Preset,
                DetectIosExportToolchainEnvironment(planContext.Preset));
            if (!validation.Succeeded)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export build-ios",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "iOS export build failed before publish.",
                        MapExportDiagnostics(validation.Diagnostics),
                        BuildIosExportFailureData("export.ios.build", "toolchain_failed", planContext.Plan)),
                    output,
                    error);
            }

            return WriteResult(
                CliResult.Failure(
                    "export build-ios",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "iOS publish is not available in this preview CLI route.",
                    [CreateCliDiagnostic("E2D-CLI-0002", "iOS publish requires a macOS/Xcode verifier path; use --skip-publish true for deterministic staging.")],
                    BuildIosExportFailureData("export.ios.build", "publish_unavailable", planContext.Plan)),
                output,
                error);
        }

        return WriteResult(
            CliResult.Success(
                "export build-ios",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "iOS Xcode project staging created.",
                changedFiles: ToProjectRelativeIosFiles(planContext.ProjectRoot, planContext.Plan.StagingDirectory, packageResult.Files),
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildIosExportBuildData(planContext.Plan, packageResult, publishSkipped: true)),
            output,
            error);
    }

    private static int RunIosExportSmoke(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export run-ios` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateIosExportPlanContext("export run-ios", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        var packageResult = Electron2D.Electron2DIosXcodeProjectBuilder.Build(
            planContext.Plan,
            planContext.ProjectRoot,
            planContext.Settings);
        if (!packageResult.Succeeded)
        {
            return WriteResult(
                CliResult.Failure(
                    "export run-ios",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "iOS export staging failed before simulator/device smoke.",
                    MapExportDiagnostics(packageResult.Diagnostics),
                    BuildIosExportFailureData("export.ios.run", "package_failed", planContext.Plan)),
                output,
                error);
        }

        var smokeOutput = ResolveProjectChildPath(
            planContext.ProjectRoot,
            options.GetOption("--smoke-output") ?? Path.Combine(".electron2d", "export-smoke", "ios-smoke.json"));
        var smokeResult = Electron2D.Electron2DIosDeviceSmokeRunner.Run(
            planContext.Plan,
            smokeOutput,
            Electron2D.Electron2DIosDeviceSmokeObservation.Blocked("No iOS simulator or device evidence is available for iOS export smoke."),
            context.NowUtc);

        return WriteResult(
            CliResult.Failure(
                "export run-ios",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "iOS simulator/device smoke is blocked.",
                MapExportDiagnostics(smokeResult.Diagnostics),
                BuildIosExportRunData(planContext.Plan, smokeResult)),
            output,
            error);
    }

    private static int RunAndroidExportBuild(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export build-android` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateAndroidExportPlanContext("export build-android", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        var packageResult = Electron2D.Electron2DAndroidPackageBuilder.Build(
            planContext.Plan,
            planContext.ProjectRoot,
            planContext.Settings);
        if (!packageResult.Succeeded)
        {
            return WriteResult(
                CliResult.Failure(
                    "export build-android",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "Android package staging failed.",
                    MapExportDiagnostics(packageResult.Diagnostics),
                    BuildAndroidExportFailureData("export.android.build", "package_failed", planContext.Plan)),
                output,
                error);
        }

        var skipPublish = ReadBooleanOption(options, "--skip-publish", defaultValue: false, "export build-android");
        if (!skipPublish)
        {
            var environment = DetectAndroidExportToolchainEnvironment();
            environment.SigningIdentityAvailable = !planContext.Preset.Signing.Required ||
                !string.IsNullOrWhiteSpace(planContext.Preset.Signing.Identity);
            environment.SigningCredentialReferenceAvailable = !planContext.Preset.Signing.Required ||
                !string.IsNullOrWhiteSpace(planContext.Preset.Signing.CredentialReference);
            var validation = Electron2D.Electron2DExportToolchainValidator.Validate(planContext.Preset, environment);
            if (!validation.Succeeded)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export build-android",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "Android export build failed before publish.",
                        MapExportDiagnostics(validation.Diagnostics),
                        BuildAndroidExportFailureData("export.android.build", "toolchain_failed", planContext.Plan)),
                    output,
                    error);
            }

            var buildDiagnostics = RunDotnetAndroidBuild(planContext.Plan, environment);
            if (buildDiagnostics.Count > 0)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export build-android",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "Android export build failed.",
                        buildDiagnostics,
                        BuildAndroidExportFailureData("export.android.build", "build_failed", planContext.Plan)),
                    output,
                    error);
            }
        }

        return WriteResult(
            CliResult.Success(
                "export build-android",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "Android package staging created.",
                changedFiles: ToProjectRelativeAndroidFiles(planContext.ProjectRoot, planContext.Plan.StagingDirectory, packageResult.Files),
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildAndroidExportBuildData(planContext.Plan, packageResult, skipPublish)),
            output,
            error);
    }

    private static int RunAndroidExportSmoke(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export run-android` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateAndroidExportPlanContext("export run-android", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        var smokeOutput = ResolveProjectChildPath(
            planContext.ProjectRoot,
            options.GetOption("--smoke-output") ?? Path.Combine(".electron2d", "export-smoke", "android-smoke.json"));
        var adbPath = ResolveAndroidAdbPath(options, FindAndroidSdkPath());
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            return WriteBlockedAndroidSmoke(
                options,
                output,
                error,
                context,
                planContext,
                smokeOutput,
                "No Android adb executable is available for Android export smoke.");
        }

        var preferredAdbSerial = options.GetOption("--adb-serial") ?? string.Empty;
        var device = DiscoverAndroidDevice(adbPath, preferredAdbSerial);
        if (string.IsNullOrWhiteSpace(device.Serial))
        {
            var blockReason = string.IsNullOrWhiteSpace(preferredAdbSerial)
                ? "No connected Android device or emulator is available for Android export smoke."
                : $"Android adb serial '{preferredAdbSerial}' is not connected, authorized or in device state.";
            return WriteBlockedAndroidSmoke(
                options,
                output,
                error,
                context,
                planContext,
                smokeOutput,
                blockReason);
        }

        var apkPath = FindAndroidApk(planContext.Plan);
        var smokePlan = CreateAndroidSmokePlan(planContext.Plan, device.Abi);
        if (string.IsNullOrWhiteSpace(apkPath))
        {
            var packageResult = Electron2D.Electron2DAndroidPackageBuilder.Build(
                smokePlan,
                planContext.ProjectRoot,
                planContext.Settings);
            if (!packageResult.Succeeded)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export run-android",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "Android export staging failed before device smoke.",
                        MapExportDiagnostics(packageResult.Diagnostics),
                        BuildAndroidExportFailureData("export.android.run", "package_failed", smokePlan)),
                    output,
                    error);
            }

            var environment = DetectAndroidExportToolchainEnvironment();
            var validation = Electron2D.Electron2DExportToolchainValidator.Validate(planContext.Preset, environment);
            if (!validation.Succeeded)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export run-android",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "Android export smoke failed before build.",
                        MapExportDiagnostics(validation.Diagnostics),
                        BuildAndroidExportFailureData("export.android.run", "toolchain_failed", smokePlan)),
                    output,
                    error);
            }

            var buildDiagnostics = RunDotnetAndroidBuild(smokePlan, environment);
            if (buildDiagnostics.Count > 0)
            {
                return WriteResult(
                    CliResult.Failure(
                        "export run-android",
                        options,
                        planContext.ProjectRoot,
                        CliRoute.None,
                        "Android export smoke build failed.",
                        buildDiagnostics,
                        BuildAndroidExportFailureData("export.android.run", "build_failed", smokePlan)),
                    output,
                    error);
            }

            apkPath = FindAndroidApk(smokePlan);
        }

        if (string.IsNullOrWhiteSpace(apkPath))
        {
            return WriteResult(
                CliResult.Failure(
                    "export run-android",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "Android export smoke failed because no debug APK was found.",
                    [CreateCliDiagnostic("E2D-CLI-0002", "E2D-EXPORT-ANDROID-0012: debug APK was not found in the Android export output.")],
                    BuildAndroidExportFailureData("export.android.run", "apk_missing", smokePlan)),
                output,
                error);
        }

        var observation = RunAndroidDeviceSmoke(adbPath, device, apkPath, planContext.Settings);
        var smokeResult = Electron2D.Electron2DAndroidDeviceSmokeRunner.Run(
            smokePlan,
            smokeOutput,
            observation,
            context.NowUtc);

        return smokeResult.Succeeded
            ? WriteResult(
                CliResult.Success(
                    "export run-android",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "Android device smoke passed.",
                    changedFiles: [Path.GetRelativePath(planContext.ProjectRoot, smokeResult.ArtifactPath).Replace('\\', '/')],
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    data: BuildAndroidExportRunData(smokePlan, smokeResult)),
                output,
                error)
            : WriteResult(
                CliResult.Failure(
                    "export run-android",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    smokeResult.Status == "blocked" ? "Android device smoke is blocked." : "Android device smoke failed.",
                    MapExportDiagnostics(smokeResult.Diagnostics),
                    BuildAndroidExportRunData(smokePlan, smokeResult)),
                output,
                error);
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
        var formatHelp = string.Equals(group, "tasks", StringComparison.Ordinal)
            ? "--format text|json|markdown"
            : string.Equals(group, "context", StringComparison.Ordinal)
                ? "--format text|json"
            : "--format text|json|jsonl|sarif";
        output.WriteLine($"Common options: --project <path> {formatHelp} --quiet --verbose");
        if (MutatingOrJobGroups.Contains(group, StringComparer.Ordinal))
        {
            output.WriteLine("Mutation/job options: --dry-run --headless");
        }

        output.WriteLine();
        output.WriteLine("Commands:");
        var commands = group switch
        {
            "project" => "  create|validate       Create an AI-ready project or validate a project without requiring GUI runtime.",
            "mcp" => "  serve                 Emit local MCP resources and tools manifest.",
            "doctor" => "  <default>             Inspect reproducibility lock and local environment without opening workspace.",
            "workspace" => "  transaction           Apply a generic workspace text transaction.",
            "run" => "  debug                 Inspect runtime state, or queue/run headless with runtime options.",
            "import" or "build" or "export" => "  <default>             Queue a job and emit JSON or JSONL status.",
            "test" => "  <default>             Queue a job, or run scene tests with --format json and a scene-test manifest.",
            "validate" => "  <default>             Validate a project and emit text, JSON or SARIF diagnostics.",
            "docs" => "  search|type|member|example",
            "api" => "  compare-godot <type>  Compare one API type against the approved Electron2D 0.1-preview 2D profile.",
            "tasks" => "  init|board|list|get|create|update|move|set-status\n  submit|accept|request-changes|cancel|reopen\n  archive|unarchive|delete\n  comment|context|criterion|parent|dependency|group|attachment|tag\n  verify|normalize|migrate|export  Manage the canonical `.taskboard`.",
            "context" => "  build                 Write a compact static context pack to `.electron2d/context/`.",
            _ => "  Reserved for a later Preview task."
        };
        output.WriteLine(commands);
    }

    private static JsonObject LoadApiManifest()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiManifestPath = Path.Combine(repositoryRoot, LocalDocumentationStore.ApiManifestPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(apiManifestPath))
        {
            throw new FileNotFoundException($"API manifest was not found: {apiManifestPath}");
        }

        return JsonNode.Parse(File.ReadAllText(apiManifestPath))?.AsObject()
            ?? throw new CommandLineException("API manifest root must be a JSON object.");
    }

    private static JsonObject LoadManualApiProfile()
    {
        var repositoryRoot = FindRepositoryRoot();
        var profilePath = Path.Combine(repositoryRoot, LocalDocumentationStore.PublicApiProfilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException($"Manual public API profile was not found: {profilePath}");
        }

        return JsonNode.Parse(File.ReadAllText(profilePath))?.AsObject()
            ?? throw new CommandLineException("Manual public API profile root must be a JSON object.");
    }

    private static JsonObject? FindApiManifestType(JsonObject manifest, string fullName)
    {
        var types = manifest["types"]?.AsArray()
            ?? throw new CommandLineException("API manifest is missing `types`.");
        return types
            .OfType<JsonObject>()
            .SingleOrDefault(type => string.Equals(Value(type, "fullName"), fullName, StringComparison.Ordinal));
    }

    private static JsonObject? FindManualApiProfileType(JsonObject profile, string query)
    {
        var normalized = NormalizeApiTypeQuery(query);
        var types = profile["types"]?.AsArray()
            ?? throw new CommandLineException("Manual public API profile is missing `types`.");
        var candidates = types
            .OfType<JsonObject>()
            .Select(type =>
            {
                var fullName = Value(type, "fullName");
                var shortName = fullName.StartsWith("Electron2D.", StringComparison.Ordinal)
                    ? fullName["Electron2D.".Length..]
                    : fullName;
                return new { Type = type, ShortName = shortName };
            })
            .ToArray();
        var exactMatches = candidates
            .Where(candidate => string.Equals(candidate.ShortName, normalized, StringComparison.Ordinal))
            .Select(candidate => candidate.Type)
            .ToArray();
        if (exactMatches.Length > 0)
        {
            return exactMatches.Length == 1 ? exactMatches[0] : null;
        }

        var caseInsensitiveMatches = candidates
            .Where(candidate => string.Equals(candidate.ShortName, normalized, StringComparison.OrdinalIgnoreCase))
            .Select(candidate => candidate.Type)
            .ToArray();
        return caseInsensitiveMatches.Length == 1 ? caseInsensitiveMatches[0] : null;
    }

    private static JsonObject BuildApiCompareData(
        JsonObject? type,
        JsonObject? profileDecision,
        string? query,
        string resultStatus,
        JsonObject? manifest)
    {
        return new JsonObject
        {
            ["mode"] = "api.compareGodot",
            ["sourcePath"] = LocalDocumentationStore.ApiManifestPath,
            ["profileSourcePath"] = LocalDocumentationStore.PublicApiProfilePath,
            ["query"] = query,
            ["type"] = profileDecision is null ? null : BuildApiTypeSummary(type, profileDecision),
            ["result"] = new JsonObject
            {
                ["status"] = resultStatus
            },
            ["parityEvidence"] = manifest is null
                ? new JsonObject
                {
                    ["status"] = "not_available",
                    ["reason"] = "API manifest was not loaded."
                }
                : CloneObject(manifest["strictParityEvidence"], "API manifest is missing `strictParityEvidence`.")
        };
    }

    private static JsonObject BuildApiTypeSummary(JsonObject? type, JsonObject profileDecision)
    {
        var manifestProfile = type?["profile"]?.AsObject();
        var decision = Value(profileDecision, "decision");
        var approved = string.Equals(decision, "approved", StringComparison.Ordinal);
        return new JsonObject
        {
            ["fullName"] = Value(profileDecision, "fullName"),
            ["id"] = type is null ? null : Value(type, "id"),
            ["availability"] = new JsonObject
            {
                ["exported"] = type is not null,
                ["status"] = type is null ? "not_exported" : "exported"
            },
            ["profile"] = new JsonObject
            {
                ["decision"] = decision,
                ["status"] = manifestProfile is null ? (approved ? "approved_not_exported" : decision) : Value(manifestProfile, "status"),
                ["parity"] = manifestProfile is null ? "not_verified" : Value(manifestProfile, "parity"),
                ["outOfProfile"] = !approved,
                ["editorOnly"] = profileDecision["editorOnly"]?.GetValue<bool>() ?? false,
                ["godotReference"] = Value(profileDecision, "godotReference"),
                ["rationale"] = Value(profileDecision, "rationale")
            }
        };
    }

    private static JsonObject CloneObject(JsonNode? node, string errorMessage)
    {
        if (node is not JsonObject)
        {
            throw new CommandLineException(errorMessage);
        }

        return JsonNode.Parse(node.ToJsonString())?.AsObject()
            ?? throw new CommandLineException(errorMessage);
    }

    private static string NormalizeApiTypeQuery(string query)
    {
        var value = query.Trim();
        const string prefix = "Electron2D.";
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..]
            : value;
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

    private static bool TryCreateWebExportPlanContext(
        string command,
        CliOptions options,
        out WebExportPlanContext planContext,
        out CliResult? failure)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var settingsPath = Electron2D.ProjectFileLocator.ResolveProjectFilePath(projectRoot);
        var settingsResult = Electron2D.Electron2DSettingsStore.LoadProject(settingsPath);
        if (!settingsResult.Succeeded || settingsResult.Settings is null)
        {
            planContext = WebExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "WebAssembly browser export planning failed.",
                settingsResult.Diagnostics.Select(diagnostic => CreateCliDiagnostic(
                    "E2D-CLI-0002",
                    $"{diagnostic.Code}: {diagnostic.Message}")).ToArray(),
                BuildWebExportFailureData(ModeForWebCommand(command), "settings_load_failed"));
            return false;
        }

        var projectFilePath = ResolveProjectFilePath(projectRoot, options.GetOption("--project-file"));
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = options.GetOption("--preset-name") ?? "web-release",
            Target = Electron2D.Electron2DExportTarget.WebAssemblyBrowser,
            Configuration = ReadExportConfiguration(options.GetOption("--configuration") ?? "Release", options, command),
            RuntimeIdentifier = "browser-wasm",
            SelfContained = true,
            RendererProfile = ReadRendererProfile(options.GetOption("--renderer-profile") ?? settingsResult.Settings.RendererProfile.ToString(), options, command),
            OutputDirectory = ResolveProjectChildPath(projectRoot, options.GetOption("--output") ?? Path.Combine("exports", "web")),
            IncludeDebugSymbols = ReadBooleanOption(options, "--debug-symbols", defaultValue: false, command)
        };
        var planResult = Electron2D.Electron2DWebAssemblyExportPlanner.CreatePlan(preset, projectFilePath, settingsResult.Settings, settingsPath);
        if (!planResult.Succeeded || planResult.Plan is null)
        {
            planContext = WebExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "WebAssembly browser export planning failed.",
                MapExportDiagnostics(planResult.Diagnostics),
                BuildWebExportFailureData(ModeForWebCommand(command), "plan_failed"));
            return false;
        }

        planContext = new WebExportPlanContext(projectRoot, settingsResult.Settings, preset, planResult.Plan);
        failure = null;
        return true;
    }

    private static bool TryCreateAndroidExportPlanContext(
        string command,
        CliOptions options,
        out AndroidExportPlanContext planContext,
        out CliResult? failure)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var settingsPath = Electron2D.ProjectFileLocator.ResolveProjectFilePath(projectRoot);
        var settingsResult = Electron2D.Electron2DSettingsStore.LoadProject(settingsPath);
        if (!settingsResult.Succeeded || settingsResult.Settings is null)
        {
            planContext = AndroidExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Android arm64 export planning failed.",
                settingsResult.Diagnostics.Select(diagnostic => CreateCliDiagnostic(
                    "E2D-CLI-0002",
                    $"{diagnostic.Code}: {diagnostic.Message}")).ToArray(),
                BuildAndroidExportFailureData(
                    ModeForAndroidCommand(command),
                    "settings_load_failed",
                    plan: null));
            return false;
        }

        var configuration = ReadExportConfiguration(options.GetOption("--configuration") ?? "Debug", options, command);
        var signingRequired = ReadBooleanOption(
            options,
            "--signing-required",
            defaultValue: configuration == Electron2D.Electron2DExportConfiguration.Release,
            command);
        var outputDirectory = ResolveProjectChildPath(
            projectRoot,
            options.GetOption("--output") ?? Path.Combine("exports", "android", configuration.ToString().ToLowerInvariant()));
        var projectFilePath = ResolveProjectFilePath(projectRoot, options.GetOption("--project-file"));
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = options.GetOption("--preset-name") ??
                (configuration == Electron2D.Electron2DExportConfiguration.Release ? "android-release" : "android-debug"),
            Target = Electron2D.Electron2DExportTarget.AndroidArm64,
            Configuration = configuration,
            RuntimeIdentifier = "android-arm64",
            SelfContained = true,
            RendererProfile = ReadRendererProfile(options.GetOption("--renderer-profile") ?? settingsResult.Settings.RendererProfile.ToString(), options, command),
            OutputDirectory = outputDirectory,
            IncludeDebugSymbols = ReadBooleanOption(options, "--debug-symbols", defaultValue: configuration == Electron2D.Electron2DExportConfiguration.Debug, command),
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = signingRequired,
                Identity = options.GetOption("--signing-identity") ?? string.Empty,
                CredentialReference = options.GetOption("--signing-credential-reference") ?? string.Empty
            }
        };

        var planResult = Electron2D.Electron2DAndroidExportPlanner.CreatePlan(preset, projectFilePath, settingsResult.Settings);
        if (!planResult.Succeeded || planResult.Plan is null)
        {
            planContext = AndroidExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Android arm64 export planning failed.",
                MapExportDiagnostics(planResult.Diagnostics),
                BuildAndroidExportFailureData(ModeForAndroidCommand(command), "plan_failed", plan: null));
            return false;
        }

        planContext = new AndroidExportPlanContext(projectRoot, settingsResult.Settings, preset, planResult.Plan);
        failure = null;
        return true;
    }

    private static bool TryCreateIosExportPlanContext(
        string command,
        CliOptions options,
        out IosExportPlanContext planContext,
        out CliResult? failure)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var settingsPath = Electron2D.ProjectFileLocator.ResolveProjectFilePath(projectRoot);
        var settingsResult = Electron2D.Electron2DSettingsStore.LoadProject(settingsPath);
        if (!settingsResult.Succeeded || settingsResult.Settings is null)
        {
            planContext = IosExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "iOS arm64 export planning failed.",
                settingsResult.Diagnostics.Select(diagnostic => CreateCliDiagnostic(
                    "E2D-CLI-0002",
                    $"{diagnostic.Code}: {diagnostic.Message}")).ToArray(),
                BuildIosExportFailureData(
                    ModeForIosCommand(command),
                    "settings_load_failed",
                    plan: null));
            return false;
        }

        var configuration = ReadExportConfiguration(options.GetOption("--configuration") ?? "Debug", options, command);
        var signingRequired = ReadBooleanOption(
            options,
            "--signing-required",
            defaultValue: configuration == Electron2D.Electron2DExportConfiguration.Release,
            command);
        var outputDirectory = ResolveProjectChildPath(
            projectRoot,
            options.GetOption("--output") ?? Path.Combine("exports", "ios", configuration.ToString().ToLowerInvariant()));
        var projectFilePath = ResolveProjectFilePath(projectRoot, options.GetOption("--project-file"));
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = options.GetOption("--preset-name") ??
                (configuration == Electron2D.Electron2DExportConfiguration.Release ? "ios-release" : "ios-debug"),
            Target = Electron2D.Electron2DExportTarget.IosArm64,
            Configuration = configuration,
            RuntimeIdentifier = "ios-arm64",
            SelfContained = true,
            RendererProfile = ReadRendererProfile(options.GetOption("--renderer-profile") ?? settingsResult.Settings.RendererProfile.ToString(), options, command),
            OutputDirectory = outputDirectory,
            IncludeDebugSymbols = ReadBooleanOption(options, "--debug-symbols", defaultValue: configuration == Electron2D.Electron2DExportConfiguration.Debug, command),
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = signingRequired,
                Identity = options.GetOption("--signing-identity") ?? string.Empty,
                CredentialReference = options.GetOption("--signing-credential-reference") ?? string.Empty
            }
        };

        var planResult = Electron2D.Electron2DIosExportPlanner.CreatePlan(preset, projectFilePath, settingsResult.Settings);
        if (!planResult.Succeeded || planResult.Plan is null)
        {
            planContext = IosExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "iOS arm64 export planning failed.",
                MapExportDiagnostics(planResult.Diagnostics),
                BuildIosExportFailureData(ModeForIosCommand(command), "plan_failed", plan: null));
            return false;
        }

        planContext = new IosExportPlanContext(projectRoot, settingsResult.Settings, preset, planResult.Plan);
        failure = null;
        return true;
    }

    private static string ResolveProjectChildPath(string projectRoot, string path)
    {
        var fullPath = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(projectRoot, path));
        if (!Path.IsPathRooted(path))
        {
            WorkspaceSnapshotMaterializer.EnsureChildPath(projectRoot, fullPath);
        }

        return fullPath;
    }

    private static string ResolveProjectFilePath(string projectRoot, string? projectFilePath)
    {
        if (!string.IsNullOrWhiteSpace(projectFilePath))
        {
            return Path.IsPathRooted(projectFilePath)
                ? Path.GetFullPath(projectFilePath)
                : Path.GetFullPath(Path.Combine(projectRoot, projectFilePath));
        }

        var projectFiles = Directory.Exists(projectRoot)
            ? Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];
        return projectFiles.Length > 0
            ? projectFiles[0]
            : Path.Combine(projectRoot, "Electron2D.Game.csproj");
    }

    private static Electron2D.Electron2DExportConfiguration ReadExportConfiguration(
        string value,
        CliOptions options,
        string command)
    {
        if (Enum.TryParse<Electron2D.Electron2DExportConfiguration>(value, ignoreCase: false, out var result) &&
            Enum.IsDefined(result))
        {
            return result;
        }

        throw new CliCommandException(
            command,
            options,
            "Web export configuration must be Debug or Release.",
            CreateCliDiagnostic("E2D-CLI-0002", "Web export configuration must be Debug or Release."));
    }

    private static Electron2D.Electron2DRendererProfileSetting ReadRendererProfile(
        string value,
        CliOptions options,
        string command)
    {
        if (Enum.TryParse<Electron2D.Electron2DRendererProfileSetting>(value, ignoreCase: false, out var result) &&
            Enum.IsDefined(result))
        {
            return result;
        }

        throw new CliCommandException(
            command,
            options,
            "Web export renderer profile must be Automatic, Compatibility or Standard.",
            CreateCliDiagnostic("E2D-CLI-0002", "Web export renderer profile must be Automatic, Compatibility or Standard."));
    }

    private static bool ReadBooleanOption(CliOptions options, string optionName, bool defaultValue, string command)
    {
        var value = options.GetOption(optionName);
        if (value is null)
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        throw new CliCommandException(
            command,
            options,
            $"{optionName} must be true or false.",
            CreateCliDiagnostic("E2D-CLI-0002", $"{optionName} must be true or false."));
    }

    private static Electron2D.Electron2DExportToolchainEnvironment DetectWebExportToolchainEnvironment()
    {
        return new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = ReadDotnetSdkVersion() is not null,
            WebAssemblyBuildToolsAvailable = IsWebAssemblyBuildToolsAvailable()
        };
    }

    private static bool IsWebAssemblyBuildToolsAvailable()
    {
        var sdkVersion = ReadDotnetSdkVersion();
        var sdkMajor = sdkVersion?.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var workloadList = ReadCommandOutput("dotnet", "workload list");
        if (workloadList is null)
        {
            return false;
        }

        return workloadList.Contains("wasm-tools ", StringComparison.Ordinal) ||
            (!string.IsNullOrWhiteSpace(sdkMajor) &&
            workloadList.Contains($"wasm-tools-net{sdkMajor}", StringComparison.Ordinal));
    }

    private static IReadOnlyList<StructuredDiagnostic> RunDotnetPublish(Electron2D.Electron2DWebAssemblyExportPlan plan)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in plan.PublishArguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return [CreateCliDiagnostic("E2D-CLI-0002", "E2D-EXPORT-WEB-0009: dotnet publish could not start.")];
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                return [];
            }

            var message = string.IsNullOrWhiteSpace(stderr)
                ? stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "dotnet publish failed."
                : stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "dotnet publish failed.";
            return [CreateCliDiagnostic("E2D-CLI-0002", $"E2D-EXPORT-WEB-0009: {message}")];
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return [CreateCliDiagnostic("E2D-CLI-0002", $"E2D-EXPORT-WEB-0009: dotnet publish could not run: {exception.Message}")];
        }
    }

    private static Electron2D.Electron2DExportToolchainEnvironment DetectAndroidExportToolchainEnvironment()
    {
        var androidSdkPath = FindAndroidSdkPath();
        return new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = ReadDotnetSdkVersion() is not null,
            AndroidSdkPath = androidSdkPath,
            AndroidNdkPath = FindAndroidNdkPath(androidSdkPath),
            JavaSdkPath = FindJavaSdkPath()
        };
    }

    private static Electron2D.Electron2DExportToolchainEnvironment DetectIosExportToolchainEnvironment(
        Electron2D.Electron2DExportPreset preset)
    {
        return new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = ReadDotnetSdkVersion() is not null,
            XcodePath = FindXcodePath(),
            SigningIdentityAvailable = !preset.Signing.Required || !string.IsNullOrWhiteSpace(preset.Signing.Identity),
            SigningCredentialReferenceAvailable = !preset.Signing.Required || !string.IsNullOrWhiteSpace(preset.Signing.CredentialReference)
        };
    }

    private static string FindXcodePath()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return string.Empty;
        }

        var developerDir = Environment.GetEnvironmentVariable("DEVELOPER_DIR");
        if (!string.IsNullOrWhiteSpace(developerDir) && Directory.Exists(developerDir))
        {
            return Path.GetFullPath(developerDir);
        }

        var xcodeRoot = Environment.GetEnvironmentVariable("XCODE_ROOT");
        if (!string.IsNullOrWhiteSpace(xcodeRoot) && Directory.Exists(xcodeRoot))
        {
            return Path.GetFullPath(xcodeRoot);
        }

        var xcodeVersion = ReadCommandFirstLine("xcodebuild", "-version");
        return xcodeVersion is null ? string.Empty : "/Applications/Xcode.app";
    }

    private static string FindAndroidSdkPath()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Android",
                "Sdk")
        };

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => Path.GetFullPath(candidate!))
            .FirstOrDefault(Directory.Exists) ?? string.Empty;
    }

    private static string FindAndroidNdkPath(string androidSdkPath)
    {
        var env = Environment.GetEnvironmentVariable("ANDROID_NDK_HOME");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return Path.GetFullPath(env);
        }

        var ndkRoot = string.IsNullOrWhiteSpace(androidSdkPath) ? string.Empty : Path.Combine(androidSdkPath, "ndk");
        if (!Directory.Exists(ndkRoot))
        {
            return string.Empty;
        }

        return Directory.EnumerateDirectories(ndkRoot)
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string FindJavaSdkPath()
    {
        var candidates = EnumerateJavaSdkCandidates();

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => Path.GetFullPath(candidate!))
            .FirstOrDefault(path => Directory.Exists(path) &&
                File.Exists(Path.Combine(path, "bin", "java.exe")) &&
                IsJavaSdk17OrNewer(path)) ?? string.Empty;
    }

    private static IEnumerable<string?> EnumerateJavaSdkCandidates()
    {
        yield return Environment.GetEnvironmentVariable("JAVA_HOME");

        foreach (var root in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            foreach (var vendorRoot in new[]
            {
                Path.Combine(root, "Java"),
                Path.Combine(root, "Eclipse Adoptium"),
                Path.Combine(root, "Microsoft")
            })
            {
                if (!Directory.Exists(vendorRoot))
                {
                    continue;
                }

                foreach (var candidate in Directory.EnumerateDirectories(vendorRoot, "*jdk*", SearchOption.TopDirectoryOnly))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static bool IsJavaSdk17OrNewer(string javaSdkPath)
    {
        var releasePath = Path.Combine(javaSdkPath, "release");
        if (!File.Exists(releasePath))
        {
            return false;
        }

        try
        {
            var versionLine = File.ReadLines(releasePath)
                .FirstOrDefault(line => line.StartsWith("JAVA_VERSION=", StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(versionLine))
            {
                return false;
            }

            var version = versionLine.Split('=', count: 2)[1].Trim().Trim('"');
            var majorText = version.StartsWith("1.", StringComparison.Ordinal)
                ? version.Split('.', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()
                : version.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return int.TryParse(majorText, out var major) && major >= 17;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static IReadOnlyList<StructuredDiagnostic> RunDotnetAndroidBuild(
        Electron2D.Electron2DAndroidExportPlan plan,
        Electron2D.Electron2DExportToolchainEnvironment environment)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in plan.BuildArguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!string.IsNullOrWhiteSpace(environment.AndroidSdkPath))
            {
                process.StartInfo.Environment["ANDROID_HOME"] = environment.AndroidSdkPath;
                process.StartInfo.Environment["ANDROID_SDK_ROOT"] = environment.AndroidSdkPath;
                process.StartInfo.ArgumentList.Add("-p:AndroidSdkDirectory=" + environment.AndroidSdkPath);
            }

            if (!string.IsNullOrWhiteSpace(environment.AndroidNdkPath))
            {
                process.StartInfo.ArgumentList.Add("-p:AndroidNdkDirectory=" + environment.AndroidNdkPath);
            }

            if (!string.IsNullOrWhiteSpace(environment.JavaSdkPath))
            {
                process.StartInfo.Environment["JAVA_HOME"] = environment.JavaSdkPath;
                var javaBin = Path.Combine(environment.JavaSdkPath, "bin");
                var existingPath = process.StartInfo.Environment.TryGetValue("Path", out var pathValue) ||
                    process.StartInfo.Environment.TryGetValue("PATH", out pathValue)
                    ? pathValue
                    : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                var javaFirstPath = string.IsNullOrWhiteSpace(existingPath)
                    ? javaBin
                    : javaBin + Path.PathSeparator + existingPath;
                process.StartInfo.Environment["Path"] = javaFirstPath;
                process.StartInfo.Environment["PATH"] = javaFirstPath;
                process.StartInfo.ArgumentList.Add("-p:JavaSdkDirectory=" + environment.JavaSdkPath);
            }

            if (!process.Start())
            {
                return [CreateCliDiagnostic("E2D-CLI-0002", "E2D-EXPORT-ANDROID-0010: dotnet Android build could not start.")];
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                return [];
            }

            var message = ExtractDotnetFailureMessage(stdout, stderr, process.ExitCode, "dotnet Android build failed.");
            return [CreateCliDiagnostic("E2D-CLI-0002", $"E2D-EXPORT-ANDROID-0010: {message}")];
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return [CreateCliDiagnostic("E2D-CLI-0002", $"E2D-EXPORT-ANDROID-0010: dotnet Android build could not run: {exception.Message}")];
        }
    }

    private static int WriteBlockedAndroidSmoke(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context,
        AndroidExportPlanContext planContext,
        string smokeOutput,
        string reason)
    {
        var smokeResult = Electron2D.Electron2DAndroidDeviceSmokeRunner.Run(
            planContext.Plan,
            smokeOutput,
            Electron2D.Electron2DAndroidDeviceSmokeObservation.Blocked(reason),
            context.NowUtc);

        return WriteResult(
            CliResult.Failure(
                "export run-android",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "Android device smoke is blocked.",
                MapExportDiagnostics(smokeResult.Diagnostics),
                BuildAndroidExportRunData(planContext.Plan, smokeResult)),
            output,
            error);
    }

    private static string ResolveAndroidAdbPath(CliOptions options, string androidSdkPath)
    {
        var explicitPath = options.GetOption("--adb-path");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var fullPath = Path.GetFullPath(explicitPath);
            return File.Exists(fullPath) ? fullPath : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(androidSdkPath))
        {
            var platformTools = Path.Combine(androidSdkPath, "platform-tools");
            foreach (var name in new[] { "adb.exe", "adb.cmd", "adb.bat" })
            {
                var candidate = Path.Combine(platformTools, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return ReadCommandFirstLine("adb", "version") is null ? string.Empty : "adb";
    }

    private static AndroidDeviceInfo DiscoverAndroidDevice(string adbPath, string preferredSerial)
    {
        var devices = RunProcess(adbPath, ["devices", "-l"], timeoutMilliseconds: 10000);
        if (devices.ExitCode != 0)
        {
            return AndroidDeviceInfo.None;
        }

        var availableSerials = devices.Stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2 && string.Equals(parts[1], "device", StringComparison.Ordinal))
            .Select(parts => parts[0])
            .ToArray();
        var requestedSerial = preferredSerial.Trim();
        var serial = string.IsNullOrWhiteSpace(requestedSerial)
            ? availableSerials.FirstOrDefault() ?? string.Empty
            : availableSerials.FirstOrDefault(
                candidate => string.Equals(candidate, requestedSerial, StringComparison.Ordinal)) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(serial))
        {
            return AndroidDeviceInfo.None;
        }

        var abi = RunAdb(adbPath, serial, ["shell", "getprop", "ro.product.cpu.abi"], timeoutMilliseconds: 10000)
            .Stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault() ?? string.Empty;
        return new AndroidDeviceInfo(serial, abi);
    }

    private static string FindAndroidApk(Electron2D.Electron2DAndroidExportPlan plan)
    {
        if (!Directory.Exists(plan.OutputDirectory))
        {
            return string.Empty;
        }

        return Directory.EnumerateFiles(plan.OutputDirectory, "*.apk", SearchOption.AllDirectories)
            .OrderByDescending(path => path.EndsWith("-Signed.apk", StringComparison.OrdinalIgnoreCase))
            .ThenBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;
    }

    private static Electron2D.Electron2DAndroidExportPlan CreateAndroidSmokePlan(
        Electron2D.Electron2DAndroidExportPlan plan,
        string deviceAbi)
    {
        if (!string.Equals(deviceAbi, "x86_64", StringComparison.OrdinalIgnoreCase))
        {
            return plan;
        }

        var buildArguments = plan.BuildArguments
            .Select(argument => argument switch
            {
                "-p:RuntimeIdentifier=android-arm64" => "-p:RuntimeIdentifier=android-x64",
                "-p:RuntimeIdentifiers=android-arm64" => "-p:RuntimeIdentifiers=android-x64",
                _ => argument
            })
            .Append("-p:AndroidSupportedAbis=x86_64")
            .ToArray();

        return new Electron2D.Electron2DAndroidExportPlan
        {
            ProjectFilePath = plan.ProjectFilePath,
            Configuration = plan.Configuration,
            RuntimeIdentifier = "android-x64",
            Abi = "x86_64",
            PackageFormat = plan.PackageFormat,
            DotnetCommand = plan.DotnetCommand,
            SelfContained = plan.SelfContained,
            OutputDirectory = plan.OutputDirectory,
            StagingDirectory = plan.StagingDirectory,
            ArtifactsDirectory = plan.ArtifactsDirectory,
            SmokeDirectory = plan.SmokeDirectory,
            AndroidProjectFilePath = plan.AndroidProjectFilePath,
            MainActivityPath = plan.MainActivityPath,
            ManifestPath = plan.ManifestPath,
            ExportMetadataPath = plan.ExportMetadataPath,
            ProjectAssetsDirectory = plan.ProjectAssetsDirectory,
            BuildArguments = buildArguments,
            RendererProfile = plan.RendererProfile,
            GraphicsBackend = plan.GraphicsBackend,
            MobileGraphicsProfile = plan.MobileGraphicsProfile,
            FallbackPolicy = plan.FallbackPolicy,
            RequiredFiles = plan.RequiredFiles.ToArray(),
            MobilePolicies = plan.MobilePolicies.ToArray(),
            SmokeCriteria = plan.SmokeCriteria.ToArray(),
            IncludeDebugSymbols = plan.IncludeDebugSymbols,
            SigningRequired = plan.SigningRequired,
            SigningIdentity = plan.SigningIdentity,
            SigningCredentialReference = plan.SigningCredentialReference,
            Orientation = plan.Orientation
        };
    }

    private static Electron2D.Electron2DAndroidDeviceSmokeObservation RunAndroidDeviceSmoke(
        string adbPath,
        AndroidDeviceInfo device,
        string apkPath,
        Electron2D.Electron2DProjectSettings settings)
    {
        var packageId = CreateAndroidApplicationId(settings);
        WakeAndroidDevice(adbPath, device.Serial);
        _ = RunAdb(adbPath, device.Serial, ["shell", "am", "force-stop", packageId], timeoutMilliseconds: 10000);
        _ = RunAdb(adbPath, device.Serial, ["logcat", "-c"], timeoutMilliseconds: 10000);
        var install = RunAdb(adbPath, device.Serial, ["install", "-r", "-t", apkPath], timeoutMilliseconds: 120000);
        var activityComponent = ResolveAndroidActivityComponent(adbPath, device.Serial, packageId);
        var launch = StartAndroidActivity(adbPath, device.Serial, activityComponent);
        Thread.Sleep(2500);
        TapAndroidScreenCenter(adbPath, device.Serial);
        Thread.Sleep(250);
        var touch = RunAdb(
            adbPath,
            device.Serial,
            [
                "shell",
                "monkey",
                "-p",
                packageId,
                "--pct-touch",
                "100",
                "--pct-motion",
                "0",
                "--pct-nav",
                "0",
                "--pct-majornav",
                "0",
                "--pct-syskeys",
                "0",
                "--pct-appswitch",
                "0",
                "--pct-anyevent",
                "0",
                "1"
            ],
            timeoutMilliseconds: 30000);
        Thread.Sleep(500);
        var pause = RunAdb(
            adbPath,
            device.Serial,
            ["shell", "am", "start", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.HOME"],
            timeoutMilliseconds: 10000);
        Thread.Sleep(750);
        var resume = StartAndroidActivity(adbPath, device.Serial, activityComponent);
        Thread.Sleep(2500);
        var firstLogcat = ReadElectron2DLogcat(adbPath, device.Serial);
        var shutdown = RunAdb(adbPath, device.Serial, ["shell", "am", "force-stop", packageId], timeoutMilliseconds: 10000);
        Thread.Sleep(250);
        var finalLogcat = ReadElectron2DLogcat(adbPath, device.Serial);
        var logs = firstLogcat.Stdout + "\n" + finalLogcat.Stdout;

        var criteria = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["install"] = install.ExitCode == 0 && install.Stdout.Contains("Success", StringComparison.OrdinalIgnoreCase),
            ["launch"] = launch.ExitCode == 0 && ContainsMarker(logs, "E2D_SMOKE_LAUNCH_READY"),
            ["render"] = ContainsMarker(logs, "E2D_SMOKE_RENDER_READY"),
            ["input"] = ContainsMarker(logs, "E2D_SMOKE_TOUCH_READY"),
            ["pauseResume"] = ContainsMarker(logs, "E2D_SMOKE_PAUSE_READY") &&
                ContainsMarker(logs, "E2D_SMOKE_RESUME_READY"),
            ["orientation"] = ContainsMarker(logs, "E2D_SMOKE_ORIENTATION_READY"),
            ["safeArea"] = ContainsMarker(logs, "E2D_SMOKE_SAFE_AREA_READY"),
            ["audio"] = ContainsMarker(logs, "E2D_SMOKE_AUDIO_READY"),
            ["resources"] = ContainsMarker(logs, "E2D_SMOKE_RESOURCES_READY"),
            ["filesystem"] = ContainsMarker(logs, "E2D_SMOKE_FILESYSTEM_READY"),
            ["logoOnBlack"] = ContainsMarker(logs, "E2D_SMOKE_LOGO_BLACK_READY"),
            ["rendererFallback"] = ContainsMarker(logs, "E2D_SMOKE_RENDERER_FALLBACK_READY"),
            ["shutdown"] = shutdown.ExitCode == 0 || ContainsMarker(logs, "E2D_SMOKE_SHUTDOWN_READY")
        };

        return Electron2D.Electron2DAndroidDeviceSmokeObservation.Observed(device.Serial, criteria);
    }

    private static void TapAndroidScreenCenter(string adbPath, string serial)
    {
        var (width, height) = ReadAndroidWindowSize(adbPath, serial);
        var points = new List<(int X, int Y)>
        {
            (Math.Max(width, 1) / 2, Math.Max(height, 1) / 2)
        };
        var landscapeWidth = Math.Max(width, height);
        var landscapeHeight = Math.Min(width, height);
        points.Add((landscapeWidth / 2, landscapeHeight / 2));

        foreach (var (x, y) in points.Distinct())
        {
            _ = RunAdb(adbPath, serial, ["shell", "input", "tap", x.ToString(), y.ToString()], timeoutMilliseconds: 10000);
        }
    }

    private static (int Width, int Height) ReadAndroidWindowSize(string adbPath, string serial)
    {
        var result = RunAdb(adbPath, serial, ["shell", "wm", "size"], timeoutMilliseconds: 10000);
        foreach (var line in result.Stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Reverse())
        {
            var separator = line.LastIndexOf(':');
            var size = separator >= 0 ? line[(separator + 1)..].Trim() : line.Trim();
            var parts = size.Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var width) &&
                int.TryParse(parts[1], out var height) &&
                width > 0 &&
                height > 0)
            {
                return (width, height);
            }
        }

        return (1080, 1920);
    }

    private static string ResolveAndroidActivityComponent(string adbPath, string serial, string packageId)
    {
        var result = RunAdb(
            adbPath,
            serial,
            ["shell", "cmd", "package", "resolve-activity", "--brief", packageId],
            timeoutMilliseconds: 10000);
        var component = result.Stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .LastOrDefault(line => line.Contains('/', StringComparison.Ordinal));
        return string.IsNullOrWhiteSpace(component)
            ? packageId + "/crc644abc767ad8be2900.MainActivity"
            : component;
    }

    private static ProcessRunResult StartAndroidActivity(string adbPath, string serial, string activityComponent)
    {
        return RunAdb(adbPath, serial, ["shell", "am", "start", "-n", activityComponent], timeoutMilliseconds: 30000);
    }

    private static void WakeAndroidDevice(string adbPath, string serial)
    {
        _ = RunAdb(adbPath, serial, ["shell", "input", "keyevent", "KEYCODE_WAKEUP"], timeoutMilliseconds: 10000);
        _ = RunAdb(adbPath, serial, ["shell", "wm", "dismiss-keyguard"], timeoutMilliseconds: 10000);
        Thread.Sleep(500);
    }

    private static ProcessRunResult ReadElectron2DLogcat(string adbPath, string serial)
    {
        return RunAdb(adbPath, serial, ["logcat", "-d", "-s", "Electron2D:I", "*:S"], timeoutMilliseconds: 10000);
    }

    private static bool ContainsMarker(string logs, string marker)
    {
        return logs.Contains(marker, StringComparison.Ordinal);
    }

    private static ProcessRunResult RunAdb(
        string adbPath,
        string serial,
        IReadOnlyList<string> arguments,
        int timeoutMilliseconds)
    {
        return RunProcess(adbPath, ["-s", serial, .. arguments], timeoutMilliseconds);
    }

    private static ProcessRunResult RunProcess(
        string fileName,
        IReadOnlyList<string> arguments,
        int timeoutMilliseconds)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return new ProcessRunResult(-1, string.Empty, "process did not start");
            }

            if (!process.WaitForExit(timeoutMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                return new ProcessRunResult(-1, process.StandardOutput.ReadToEnd(), "process timed out");
            }

            return new ProcessRunResult(process.ExitCode, process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd());
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new ProcessRunResult(-1, string.Empty, exception.Message);
        }
    }

    private static string CreateAndroidApplicationId(Electron2D.Electron2DProjectSettings settings)
    {
        var suffix = new string(settings.Name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return "dev.electron2d." + (suffix.Length == 0 ? "game" : suffix);
    }

    private sealed record AndroidDeviceInfo(string Serial, string Abi)
    {
        public static AndroidDeviceInfo None { get; } = new(string.Empty, string.Empty);
    }

    private sealed record ProcessRunResult(int ExitCode, string Stdout, string Stderr);

    private static string ExtractDotnetFailureMessage(string stdout, string stderr, int exitCode, string fallback)
    {
        var lines = stdout
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Concat(stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var errorLine = lines.FirstOrDefault(line =>
            line.Contains(": error ", StringComparison.OrdinalIgnoreCase) ||
            line.Contains(" error ", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("error ", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(errorLine))
        {
            return $"exit code {exitCode}: {errorLine}";
        }

        var usefulLine = lines.LastOrDefault(line =>
            !line.Contains("Прошло времени", StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("Elapsed", StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(usefulLine)
            ? $"exit code {exitCode}: {fallback}"
            : $"exit code {exitCode}: {usefulLine}";
    }

    private static JsonObject BuildWebExportFailureData(string mode, string status)
    {
        return new JsonObject
        {
            ["mode"] = mode,
            ["target"] = "WebAssemblyBrowser",
            ["runtimeIdentifier"] = "browser-wasm",
            ["result"] = new JsonObject
            {
                ["status"] = status
            }
        };
    }

    private static JsonObject BuildWebExportPlanData(Electron2D.Electron2DWebAssemblyExportPlan plan)
    {
        return new JsonObject
        {
            ["mode"] = "export.web.plan",
            ["target"] = "WebAssemblyBrowser",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = "planned"
            },
            ["plan"] = new JsonObject
            {
                ["projectFilePath"] = plan.ProjectFilePath,
                ["configuration"] = plan.Configuration.ToString(),
                ["runtimeIdentifier"] = plan.RuntimeIdentifier,
                ["selfContained"] = plan.SelfContained,
                ["outputDirectory"] = plan.OutputDirectory,
                ["webRootDirectory"] = plan.WebRootDirectory,
                ["frameworkDirectory"] = plan.FrameworkDirectory,
                ["assetsDirectory"] = plan.AssetsDirectory,
                ["indexHtmlPath"] = plan.IndexHtmlPath,
                ["loaderScriptPath"] = plan.LoaderScriptPath,
                ["webManifestPath"] = plan.WebManifestPath,
                ["publishArguments"] = WriteStringArray(plan.PublishArguments),
                ["rendererProfile"] = plan.RendererProfile.ToString(),
                ["graphicsBackend"] = plan.GraphicsBackend,
                ["requiredFiles"] = WriteStringArray(plan.RequiredFiles),
                ["browserPolicies"] = WriteStringArray(plan.BrowserPolicies),
                ["smokeCriteria"] = WriteStringArray(plan.SmokeCriteria),
                ["includeDebugSymbols"] = plan.IncludeDebugSymbols,
                ["signingRequired"] = plan.SigningRequired,
                ["audioPolicy"] = plan.AudioPolicy,
                ["filesystemPolicy"] = plan.FilesystemPolicy
            }
        };
    }

    private static JsonObject BuildWebExportBuildData(
        Electron2D.Electron2DWebAssemblyExportPlan plan,
        Electron2D.Electron2DWebAssemblyPackageBuildResult packageResult,
        bool publishSkipped)
    {
        return new JsonObject
        {
            ["mode"] = "export.web.build",
            ["target"] = "WebAssemblyBrowser",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = "packaged",
                ["publishSkipped"] = publishSkipped
            },
            ["plan"] = BuildWebExportPlanData(plan)["plan"]?.DeepClone(),
            ["package"] = new JsonObject
            {
                ["webRootDirectory"] = plan.WebRootDirectory,
                ["files"] = WriteStringArray(packageResult.Files),
                ["launchUrl"] = "http://127.0.0.1:8080/index.html",
                ["serveCommand"] = $"python -m http.server 8080 --directory \"{plan.WebRootDirectory}\"",
                ["smokeCommand"] = "e2d export run-web --project <project-root> --output <output-directory> --format json"
            }
        };
    }

    private static JsonObject BuildWebExportRunData(
        Electron2D.Electron2DWebAssemblyExportPlan plan,
        Electron2D.Electron2DWebAssemblySmokeResult smokeResult)
    {
        return new JsonObject
        {
            ["mode"] = "export.web.run",
            ["target"] = "WebAssemblyBrowser",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = smokeResult.Succeeded ? "smoke-passed" : "smoke-failed"
            },
            ["smoke"] = new JsonObject
            {
                ["artifactPath"] = smokeResult.ArtifactPath,
                ["launchUrl"] = smokeResult.LaunchUrl.ToString(),
                ["status"] = smokeResult.Status,
                ["criteria"] = WriteSmokeCriteria(smokeResult.Criteria)
            }
        };
    }

    private static JsonObject BuildAndroidExportFailureData(
        string mode,
        string status,
        Electron2D.Electron2DAndroidExportPlan? plan)
    {
        return new JsonObject
        {
            ["mode"] = mode,
            ["target"] = "AndroidArm64",
            ["runtimeIdentifier"] = "android-arm64",
            ["result"] = new JsonObject
            {
                ["status"] = status
            },
            ["plan"] = plan is null ? null : BuildAndroidExportPlanData(plan)["plan"]?.DeepClone()
        };
    }

    private static JsonObject BuildAndroidExportPlanData(Electron2D.Electron2DAndroidExportPlan plan)
    {
        return new JsonObject
        {
            ["mode"] = "export.android.plan",
            ["target"] = "AndroidArm64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = "planned"
            },
            ["plan"] = new JsonObject
            {
                ["projectFilePath"] = plan.ProjectFilePath,
                ["configuration"] = plan.Configuration.ToString(),
                ["runtimeIdentifier"] = plan.RuntimeIdentifier,
                ["abi"] = plan.Abi,
                ["packageFormat"] = plan.PackageFormat,
                ["dotnetCommand"] = plan.DotnetCommand,
                ["selfContained"] = plan.SelfContained,
                ["outputDirectory"] = plan.OutputDirectory,
                ["stagingDirectory"] = plan.StagingDirectory,
                ["artifactsDirectory"] = plan.ArtifactsDirectory,
                ["smokeDirectory"] = plan.SmokeDirectory,
                ["androidProjectFilePath"] = plan.AndroidProjectFilePath,
                ["mainActivityPath"] = plan.MainActivityPath,
                ["manifestPath"] = plan.ManifestPath,
                ["exportMetadataPath"] = plan.ExportMetadataPath,
                ["projectAssetsDirectory"] = plan.ProjectAssetsDirectory,
                ["buildArguments"] = WriteStringArray(plan.BuildArguments),
                ["rendererProfile"] = plan.RendererProfile.ToString(),
                ["graphicsBackend"] = plan.GraphicsBackend,
                ["mobileGraphicsProfile"] = plan.MobileGraphicsProfile,
                ["fallbackPolicy"] = plan.FallbackPolicy,
                ["requiredFiles"] = WriteStringArray(plan.RequiredFiles),
                ["mobilePolicies"] = WriteStringArray(plan.MobilePolicies),
                ["smokeCriteria"] = WriteStringArray(plan.SmokeCriteria),
                ["includeDebugSymbols"] = plan.IncludeDebugSymbols,
                ["signingRequired"] = plan.SigningRequired,
                ["signingIdentity"] = plan.SigningIdentity,
                ["signingCredentialReference"] = plan.SigningCredentialReference,
                ["orientation"] = plan.Orientation
            }
        };
    }

    private static JsonObject BuildAndroidExportBuildData(
        Electron2D.Electron2DAndroidExportPlan plan,
        Electron2D.Electron2DAndroidPackageBuildResult packageResult,
        bool publishSkipped)
    {
        return new JsonObject
        {
            ["mode"] = "export.android.build",
            ["target"] = "AndroidArm64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = publishSkipped ? "staged" : "built",
                ["publishSkipped"] = publishSkipped
            },
            ["plan"] = BuildAndroidExportPlanData(plan)["plan"]?.DeepClone(),
            ["package"] = new JsonObject
            {
                ["stagingDirectory"] = plan.StagingDirectory,
                ["artifactsDirectory"] = plan.ArtifactsDirectory,
                ["files"] = WriteStringArray(packageResult.Files),
                ["smokeCommand"] = "e2d export run-android --project <project-root> --output <output-directory> --format json"
            }
        };
    }

    private static JsonObject BuildAndroidExportRunData(
        Electron2D.Electron2DAndroidExportPlan plan,
        Electron2D.Electron2DAndroidDeviceSmokeResult smokeResult)
    {
        return new JsonObject
        {
            ["mode"] = "export.android.run",
            ["target"] = "AndroidArm64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = smokeResult.Status switch
                {
                    "passed" => "smoke-passed",
                    "blocked" => "smoke-blocked",
                    _ => "smoke-failed"
                }
            },
            ["smoke"] = new JsonObject
            {
                ["artifactPath"] = smokeResult.ArtifactPath,
                ["deviceSerial"] = smokeResult.DeviceSerial,
                ["status"] = smokeResult.Status,
                ["criteria"] = WriteSmokeCriteria(smokeResult.Criteria)
            }
        };
    }

    private static JsonObject BuildIosExportFailureData(
        string mode,
        string status,
        Electron2D.Electron2DIosExportPlan? plan)
    {
        return new JsonObject
        {
            ["mode"] = mode,
            ["target"] = "IosArm64",
            ["runtimeIdentifier"] = "ios-arm64",
            ["result"] = new JsonObject
            {
                ["status"] = status
            },
            ["plan"] = plan is null ? null : BuildIosExportPlanData(plan)["plan"]?.DeepClone()
        };
    }

    private static JsonObject BuildIosExportPlanData(Electron2D.Electron2DIosExportPlan plan)
    {
        return new JsonObject
        {
            ["mode"] = "export.ios.plan",
            ["target"] = "IosArm64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = "planned"
            },
            ["plan"] = new JsonObject
            {
                ["projectFilePath"] = plan.ProjectFilePath,
                ["configuration"] = plan.Configuration.ToString(),
                ["runtimeIdentifier"] = plan.RuntimeIdentifier,
                ["architecture"] = plan.Architecture,
                ["targetFramework"] = plan.TargetFramework,
                ["selfContained"] = plan.SelfContained,
                ["outputDirectory"] = plan.OutputDirectory,
                ["stagingDirectory"] = plan.StagingDirectory,
                ["artifactsDirectory"] = plan.ArtifactsDirectory,
                ["smokeDirectory"] = plan.SmokeDirectory,
                ["iosProjectFilePath"] = plan.IosProjectFilePath,
                ["appDelegatePath"] = plan.AppDelegatePath,
                ["infoPlistPath"] = plan.InfoPlistPath,
                ["entitlementsPath"] = plan.EntitlementsPath,
                ["exportMetadataPath"] = plan.ExportMetadataPath,
                ["xcodeProjectDirectory"] = plan.XcodeProjectDirectory,
                ["xcodeProjectFilePath"] = plan.XcodeProjectFilePath,
                ["appBundlePath"] = plan.AppBundlePath,
                ["projectAssetsDirectory"] = plan.ProjectAssetsDirectory,
                ["appName"] = plan.AppName,
                ["executableName"] = plan.ExecutableName,
                ["bundleIdentifier"] = plan.BundleIdentifier,
                ["publishArguments"] = WriteStringArray(plan.PublishArguments),
                ["xcodeBuildArguments"] = WriteStringArray(plan.XcodeBuildArguments),
                ["rendererProfile"] = plan.RendererProfile.ToString(),
                ["graphicsBackend"] = plan.GraphicsBackend,
                ["requiredFiles"] = WriteStringArray(plan.RequiredFiles),
                ["mobilePolicies"] = WriteStringArray(plan.MobilePolicies),
                ["smokeCriteria"] = WriteStringArray(plan.SmokeCriteria),
                ["includeDebugSymbols"] = plan.IncludeDebugSymbols,
                ["signingRequired"] = plan.SigningRequired,
                ["signingIdentity"] = plan.SigningIdentity,
                ["signingCredentialReference"] = plan.SigningCredentialReference
            }
        };
    }

    private static JsonObject BuildIosExportBuildData(
        Electron2D.Electron2DIosExportPlan plan,
        Electron2D.Electron2DIosXcodeProjectBuildResult packageResult,
        bool publishSkipped)
    {
        return new JsonObject
        {
            ["mode"] = "export.ios.build",
            ["target"] = "IosArm64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = publishSkipped ? "staged" : "built",
                ["publishSkipped"] = publishSkipped
            },
            ["plan"] = BuildIosExportPlanData(plan)["plan"]?.DeepClone(),
            ["package"] = new JsonObject
            {
                ["stagingDirectory"] = plan.StagingDirectory,
                ["artifactsDirectory"] = plan.ArtifactsDirectory,
                ["files"] = WriteStringArray(packageResult.Files),
                ["smokeCommand"] = "e2d export run-ios --project <project-root> --output <output-directory> --format json"
            }
        };
    }

    private static JsonObject BuildIosExportRunData(
        Electron2D.Electron2DIosExportPlan plan,
        Electron2D.Electron2DIosDeviceSmokeResult smokeResult)
    {
        return new JsonObject
        {
            ["mode"] = "export.ios.run",
            ["target"] = "IosArm64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = smokeResult.Status switch
                {
                    "passed" => "smoke-passed",
                    "blocked" => "smoke-blocked",
                    _ => "smoke-failed"
                }
            },
            ["smoke"] = new JsonObject
            {
                ["artifactPath"] = smokeResult.ArtifactPath,
                ["deviceIdentifier"] = smokeResult.DeviceIdentifier,
                ["status"] = smokeResult.Status,
                ["criteria"] = WriteSmokeCriteria(smokeResult.Criteria)
            }
        };
    }

    private static JsonObject WriteSmokeCriteria(IReadOnlyDictionary<string, bool> criteria)
    {
        var result = new JsonObject();
        foreach (var item in criteria.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            result[item.Key] = new JsonObject
            {
                ["passed"] = item.Value
            };
        }

        return result;
    }

    private static IReadOnlyList<StructuredDiagnostic> MapExportDiagnostics(
        IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return diagnostics
            .Select(diagnostic => CreateCliDiagnostic("E2D-CLI-0002", $"{diagnostic.Code}: {diagnostic.Message}"))
            .ToArray();
    }

    private static string[] ToProjectRelativeWebFiles(string projectRoot, string webRootDirectory, IEnumerable<string> packageFiles)
    {
        return packageFiles
            .Select(file => Path.GetRelativePath(projectRoot, Path.Combine(webRootDirectory, file)).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] ToProjectRelativeAndroidFiles(string projectRoot, string stagingDirectory, IEnumerable<string> packageFiles)
    {
        return packageFiles
            .Select(file => Path.GetRelativePath(projectRoot, Path.Combine(stagingDirectory, file)).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] ToProjectRelativeIosFiles(string projectRoot, string stagingDirectory, IEnumerable<string> packageFiles)
    {
        return packageFiles
            .Select(file => Path.GetRelativePath(projectRoot, Path.Combine(stagingDirectory, file)).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ModeForWebCommand(string command)
    {
        return command switch
        {
            "export build-web" => "export.web.build",
            "export run-web" => "export.web.run",
            _ => "export.web.plan"
        };
    }

    private static string ModeForAndroidCommand(string command)
    {
        return command switch
        {
            "export build-android" => "export.android.build",
            "export run-android" => "export.android.run",
            _ => "export.android.plan"
        };
    }

    private static string ModeForIosCommand(string command)
    {
        return command switch
        {
            "export build-ios" => "export.ios.build",
            "export run-ios" => "export.ios.run",
            _ => "export.ios.plan"
        };
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

        var presets = ReadExportPresetArray(projectRoot);
        if (presets is null)
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

    private static JsonArray? ReadExportPresetArray(string projectRoot)
    {
        var exportPresetsPath = Path.Combine(projectRoot, "export_presets.e2export.json");
        if (File.Exists(exportPresetsPath))
        {
            return TryReadJsonObject(exportPresetsPath)?["presets"] as JsonArray;
        }

        if (!Electron2D.ProjectFileLocator.TryResolveProjectFilePath(projectRoot, out var projectFilePath))
        {
            return null;
        }

        return TryReadJsonObject(projectFilePath)?["exportPresets"] is JsonObject exportPresets
            ? exportPresets["presets"] as JsonArray
            : null;
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

    private static string? ReadCommandOutput(string fileName, string arguments)
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

            if (!process.WaitForExit(5000))
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
                ? process.StandardOutput.ReadToEnd()
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

    private sealed class WebExportPlanContext
    {
        public static readonly WebExportPlanContext Empty = new(
            string.Empty,
            new Electron2D.Electron2DProjectSettings(),
            new Electron2D.Electron2DExportPreset(),
            new Electron2D.Electron2DWebAssemblyExportPlan());

        public WebExportPlanContext(
            string projectRoot,
            Electron2D.Electron2DProjectSettings settings,
            Electron2D.Electron2DExportPreset preset,
            Electron2D.Electron2DWebAssemblyExportPlan plan)
        {
            ProjectRoot = projectRoot;
            Settings = settings;
            Preset = preset;
            Plan = plan;
        }

        public string ProjectRoot { get; }

        public Electron2D.Electron2DProjectSettings Settings { get; }

        public Electron2D.Electron2DExportPreset Preset { get; }

        public Electron2D.Electron2DWebAssemblyExportPlan Plan { get; }
    }

    private sealed class AndroidExportPlanContext
    {
        public static readonly AndroidExportPlanContext Empty = new(
            string.Empty,
            new Electron2D.Electron2DProjectSettings(),
            new Electron2D.Electron2DExportPreset(),
            new Electron2D.Electron2DAndroidExportPlan());

        public AndroidExportPlanContext(
            string projectRoot,
            Electron2D.Electron2DProjectSettings settings,
            Electron2D.Electron2DExportPreset preset,
            Electron2D.Electron2DAndroidExportPlan plan)
        {
            ProjectRoot = projectRoot;
            Settings = settings;
            Preset = preset;
            Plan = plan;
        }

        public string ProjectRoot { get; }

        public Electron2D.Electron2DProjectSettings Settings { get; }

        public Electron2D.Electron2DExportPreset Preset { get; }

        public Electron2D.Electron2DAndroidExportPlan Plan { get; }
    }

    private sealed class IosExportPlanContext
    {
        public static readonly IosExportPlanContext Empty = new(
            string.Empty,
            new Electron2D.Electron2DProjectSettings(),
            new Electron2D.Electron2DExportPreset(),
            new Electron2D.Electron2DIosExportPlan());

        public IosExportPlanContext(
            string projectRoot,
            Electron2D.Electron2DProjectSettings settings,
            Electron2D.Electron2DExportPreset preset,
            Electron2D.Electron2DIosExportPlan plan)
        {
            ProjectRoot = projectRoot;
            Settings = settings;
            Preset = preset;
            Plan = plan;
        }

        public string ProjectRoot { get; }

        public Electron2D.Electron2DProjectSettings Settings { get; }

        public Electron2D.Electron2DExportPreset Preset { get; }

        public Electron2D.Electron2DIosExportPlan Plan { get; }
    }
}

internal sealed class CliExecutionContext
{
    private const string CodexDesktopOriginator = "Codex Desktop";

    private CliExecutionContext(
        DateTimeOffset nowUtc,
        EditorSessionRegistry? sessionRegistry,
        TextReader input,
        string? humanCapability,
        string humanActorId,
        string taskActorId,
        string taskActorKind,
        CancellationToken cancellationToken)
    {
        NowUtc = nowUtc;
        SessionRegistry = sessionRegistry;
        Input = input;
        HumanCapability = humanCapability;
        HumanActorId = humanActorId;
        TaskActorId = taskActorId;
        TaskActorKind = taskActorKind;
        CancellationToken = cancellationToken;
    }

    public DateTimeOffset NowUtc { get; }

    public EditorSessionRegistry? SessionRegistry { get; }

    public TextReader Input { get; }

    public string? HumanCapability { get; }

    public string HumanActorId { get; }

    public string TaskActorId { get; }

    public string TaskActorKind { get; }

    public CancellationToken CancellationToken { get; }

    public static CliExecutionContext Default(CancellationToken cancellationToken = default)
    {
        var isCodexDesktop = string.Equals(
            Environment.GetEnvironmentVariable("CODEX_INTERNAL_ORIGINATOR_OVERRIDE"),
            CodexDesktopOriginator,
            StringComparison.OrdinalIgnoreCase);
        return new CliExecutionContext(
            DateTimeOffset.UtcNow,
            sessionRegistry: null,
            Console.In,
            Environment.GetEnvironmentVariable("E2D_TASKBOARD_HUMAN_CAPABILITY"),
            $"vscode:{Environment.UserName}",
            isCodexDesktop ? "Codex" : "cli",
            isCodexDesktop ? "Agent" : "Cli",
            cancellationToken);
    }

    public static CliExecutionContext ForTests(
        DateTimeOffset nowUtc,
        EditorSessionRegistry? sessionRegistry = null,
        TextReader? input = null,
        string? humanCapability = null,
        string humanActorId = "test-human",
        string taskActorId = "cli",
        string taskActorKind = "Cli",
        CancellationToken cancellationToken = default)
    {
        return new CliExecutionContext(
            nowUtc,
            sessionRegistry,
            input ?? TextReader.Null,
            humanCapability,
            humanActorId,
            taskActorId,
            taskActorKind,
            cancellationToken);
    }
}

internal enum CliOutputFormat
{
    Text,
    Json,
    Jsonl,
    Sarif,
    Markdown
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
    private const int MaximumTaskInputBytes = 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> TaskInputFields =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["title"] = "--title",
            ["description"] = "--description",
            ["priority"] = "--priority",
            ["deadline"] = "--deadline",
            ["clearDeadline"] = "--clear-deadline",
            ["name"] = "--name",
            ["color"] = "--color",
            ["tag"] = "--tag",
            ["tagId"] = "--tag",
            ["assignTo"] = "--assign-to",
            ["assignee"] = "--assignee",
            ["status"] = "--status",
            ["criterion"] = "--criterion",
            ["criterionId"] = "--criterion",
            ["reason"] = "--reason",
            ["expectedRevision"] = "--expected-revision",
            ["expectedTaskRevision"] = "--expected-revision",
            ["expectedParentRevision"] = "--expected-parent-revision",
            ["expectedBoardRevision"] = "--expected-board-revision",
            ["includeArchived"] = "--include-archived",
            ["group"] = "--group",
            ["groupId"] = "--group",
            ["rank"] = "--rank",
            ["parent"] = "--parent",
            ["parentGroupId"] = "--parent",
            ["parentTaskId"] = "--parent",
            ["dependsOn"] = "--depends-on",
            ["kind"] = "--kind",
            ["text"] = "--text",
            ["file"] = "--file",
            ["attachment"] = "--attachment",
            ["attachmentId"] = "--attachment",
            ["derivative"] = "--derivative",
            ["derivativeId"] = "--derivative",
            ["confirm"] = "--confirm",
            ["reportSha"] = "--report-sha",
            ["apply"] = "--apply",
            ["finalize"] = "--finalize",
            ["operationId"] = "--operation-id",
            ["lockTimeoutMs"] = "--lock-timeout-ms",
            ["lockBackoffMs"] = "--lock-backoff-ms"
        });

    private static readonly IReadOnlySet<string> StructuredTaskInputFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "executionContract",
            "acceptanceCriteria",
            "links",
            "tagIds",
            "tagUpdates",
            "parentTaskUid",
            "relations"
        };

    private readonly IReadOnlyDictionary<string, string> options;
    private readonly JsonObject? structuredTaskInput;
    private readonly CancellationToken cancellationToken;

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
        IReadOnlyDictionary<string, string> options,
        JsonObject? structuredTaskInput = null,
        CancellationToken cancellationToken = default)
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
        this.structuredTaskInput = structuredTaskInput;
        this.cancellationToken = cancellationToken;
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
                    "markdown" when string.Equals(group, "tasks", StringComparison.Ordinal) => CliOutputFormat.Markdown,
                    _ => throw new CliCommandException(
                        group,
                        Placeholder(group),
                        string.Equals(group, "tasks", StringComparison.Ordinal)
                            ? "Unsupported output format. Use text or markdown."
                            : "Unsupported output format. Use text, json, jsonl or sarif.",
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
            new ReadOnlyDictionary<string, string>(options),
            structuredTaskInput: null);
    }

    public string? GetOption(string name)
    {
        return options.TryGetValue(name, out var value) ? value : null;
    }

    public CliOptions WithCancellationToken(CancellationToken value)
    {
        return new CliOptions(
            Group,
            Format,
            ProjectRoot,
            Quiet,
            Verbose,
            DryRun,
            Headless,
            Help,
            Values,
            options,
            structuredTaskInput,
            value);
    }

    public CliOptions WithTaskInput(TextReader standardInput)
    {
        var inputSource = GetOption("--input");
        if (inputSource is null)
        {
            return this;
        }

        try
        {
            var inputBytes = ReadTaskInput(inputSource, standardInput);
            var input = JsonNode.Parse(inputBytes) as JsonObject;
            if (input is null)
            {
                throw TaskInputError("Task input must be a JSON object.");
            }

            var merged = new Dictionary<string, string>(options, StringComparer.OrdinalIgnoreCase);
            var structured = new JsonObject();
            foreach (var property in input)
            {
                if (TaskInputFields.TryGetValue(property.Key, out var optionName))
                {
                    if (merged.ContainsKey(optionName))
                    {
                        throw TaskInputError($"Task input field '{property.Key}' conflicts with option '{optionName}'.");
                    }

                    merged[optionName] = ReadTaskInputScalar(property.Key, property.Value);
                    continue;
                }

                if (!StructuredTaskInputFields.Contains(property.Key))
                {
                    throw TaskInputError($"Task input field '{property.Key}' is not allowed.");
                }

                structured[property.Key] = property.Value?.DeepClone() ??
                    throw TaskInputError($"Task input field '{property.Key}' cannot be null.");
            }

            return new CliOptions(
                Group,
                Format,
                ProjectRoot,
                Quiet,
                Verbose,
                DryRun,
                Headless,
                Help,
                Values,
                new ReadOnlyDictionary<string, string>(merged),
                structured);
        }
        catch (CliCommandException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            throw TaskInputError($"Task input could not be read: {exception.Message}");
        }
    }

    public string RequireOption(string name, string message)
    {
        return GetOption(name) ?? throw new CliCommandException(
            string.Join(' ', new[] { Group }.Concat(Values)),
            this,
            message,
            Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", message));
    }

    public JsonObject? GetStructuredTaskInput()
    {
        return structuredTaskInput?.DeepClone().AsObject();
    }

    public TaskBoardWriteOptions CreateTaskBoardWriteOptions(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        var timeoutMilliseconds = ReadBoundedWriterOption("--lock-timeout-ms", defaultValue: 10_000, maximum: 10_000);
        var backoffMilliseconds = ReadBoundedWriterOption("--lock-backoff-ms", defaultValue: 25, maximum: 250);
        TaskBoardOperationIdentity? operation = null;
        if (GetOption("--operation-id") is { } operationId)
        {
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "--format", "--project", "--quiet", "--verbose", "--input",
                "--operation-id", "--lock-timeout-ms", "--lock-backoff-ms"
            };
            var canonicalOptions = new JsonObject();
            foreach (var option in options.Where(item => !excluded.Contains(item.Key)).OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                canonicalOptions[option.Key] = option.Value;
            }

            var payload = new JsonObject
            {
                ["command"] = command,
                ["values"] = new JsonArray(Values.Select(value => (JsonNode)value).ToArray()),
                ["options"] = canonicalOptions,
                ["structuredInput"] = structuredTaskInput?.DeepClone()
            };
            var bytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
            operation = new TaskBoardOperationIdentity(
                operationId,
                command,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }

        return new TaskBoardWriteOptions(
            TimeSpan.FromMilliseconds(timeoutMilliseconds),
            TimeSpan.FromMilliseconds(backoffMilliseconds),
            TimeSpan.FromMilliseconds(Math.Max(backoffMilliseconds, Math.Min(250, backoffMilliseconds * 8))),
            cancellationToken,
            Operation: operation);
    }

    private int ReadBoundedWriterOption(string name, int defaultValue, int maximum)
    {
        if (GetOption(name) is not { } value)
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1 || parsed > maximum)
        {
            throw new CliCommandException(
                string.Join(' ', new[] { Group }.Concat(Values)),
                this,
                $"{name} must be an integer between 1 and {maximum}.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"{name} must be an integer between 1 and {maximum}."));
        }

        return parsed;
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

    private static byte[] ReadTaskInput(string inputSource, TextReader standardInput)
    {
        if (string.Equals(inputSource, "-", StringComparison.Ordinal))
        {
            var buffer = new char[4096];
            var builder = new StringBuilder();
            var utf8ByteCount = 0;
            while (true)
            {
                var count = standardInput.Read(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }

                utf8ByteCount += Encoding.UTF8.GetByteCount(buffer, 0, count);
                if (utf8ByteCount > MaximumTaskInputBytes)
                {
                    throw new InvalidOperationException($"Task input exceeds {MaximumTaskInputBytes} bytes.");
                }

                builder.Append(buffer, 0, count);
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        var fullPath = Path.GetFullPath(inputSource);
        var file = new FileInfo(fullPath);
        if (!file.Exists)
        {
            throw new IOException($"Task input file '{inputSource}' was not found.");
        }

        if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("Task input file must not be a reparse point.");
        }

        if (file.Length > MaximumTaskInputBytes)
        {
            throw new IOException($"Task input exceeds {MaximumTaskInputBytes} bytes.");
        }

        return File.ReadAllBytes(fullPath);
    }

    private string ReadTaskInputScalar(string propertyName, JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            throw TaskInputError($"Task input field '{propertyName}' must be a scalar value.");
        }

        if (value.TryGetValue<string>(out var text))
        {
            return text;
        }

        if (value.TryGetValue<bool>(out var boolean))
        {
            return boolean ? bool.TrueString.ToLowerInvariant() : bool.FalseString.ToLowerInvariant();
        }

        if (value.TryGetValue<long>(out var integer))
        {
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        if (value.TryGetValue<decimal>(out var number))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        throw TaskInputError($"Task input field '{propertyName}' must be a string, number or boolean.");
    }

    private CliCommandException TaskInputError(string message)
    {
        return new CliCommandException(
            string.Join(' ', new[] { Group }.Concat(Values)),
            this,
            message,
            Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", message));
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
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()),
            structuredTaskInput: null);
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

    public static CliResult Failure(
        string command,
        CliOptions options,
        string projectRoot,
        CliRoute route,
        string message,
        StructuredDiagnostic diagnostic,
        JsonObject data)
    {
        return new CliResult(
            command,
            options,
            succeeded: false,
            exitCode: 1,
            projectRoot,
            route,
            message,
            [diagnostic],
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data);
    }

    public static CliResult Failure(
        string command,
        CliOptions options,
        string projectRoot,
        CliRoute route,
        string message,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        JsonObject data)
    {
        return new CliResult(
            command,
            options,
            succeeded: false,
            exitCode: 1,
            projectRoot,
            route,
            message,
            diagnostics,
            changedFiles: [],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data);
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
