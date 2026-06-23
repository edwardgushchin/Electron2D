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
            "api" => RunApi(options, output, error),
            "mcp" => RunMcp(options, output, error, context),
            "doctor" => RunDoctor(options, output, error),
            "workspace" => RunWorkspace(options, output, error, context),
            "validate" => RunValidate(options, output, error),
            "run" when IsRuntimeDebugCommand(options) => RunRuntimeDebug(options, output, error),
            "run" when HeadlessRuntimeAutomation.HasRuntimeOptions(options) => RunHeadlessRuntime(options, output, error, context),
            "test" when HasSceneTestSuite(options, NormalizeProjectRoot(options.ProjectRoot)) => RunSceneTests(options, output, error, context),
            "export" when IsWebExportCommand(options) => RunWebExport(options, output, error, context),
            "export" when IsAndroidExportCommand(options) => RunAndroidExport(options, output, error, context),
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
                    BuildApiCompareData(type: null, options.Values.Count > 1 ? options.Values[1] : null, "invalid_query", null)),
                output,
                error);
        }

        var query = options.Values[1];
        var manifest = LoadApiManifest();
        var type = FindApiManifestType(manifest, query);
        if (type is null)
        {
            return WriteResult(
                CliResult.Failure(
                    "api compare-godot",
                    options,
                    NormalizeProjectRoot(options.ProjectRoot),
                    CliRoute.None,
                    $"API type was not found in the manifest: {query}.",
                    CreateCliDiagnostic("E2D-CLI-0002", $"API type was not found in the manifest: {query}."),
                    BuildApiCompareData(type: null, query, "type_not_found", manifest)),
                output,
                error);
        }

        var profile = type["profile"]?.AsObject()
            ?? throw new CommandLineException($"API manifest type '{Value(type, "fullName")}' is missing `profile`.");
        var outOfProfile = profile["outOfProfile"]?.GetValue<bool>() ?? true;
        var resultStatus = outOfProfile ? "out_of_profile" : "parity_verified";
        var data = BuildApiCompareData(type, query, resultStatus, manifest);
        if (outOfProfile)
        {
            var fullName = Value(type, "fullName");
            return WriteResult(
                CliResult.Failure(
                    "api compare-godot",
                    options,
                    NormalizeProjectRoot(options.ProjectRoot),
                    CliRoute.None,
                    "API type is outside the Electron2D 0.1.0 2D profile.",
                    CreateCliDiagnostic("E2D-CLI-0002", $"API type '{fullName}' is outside the Electron2D 0.1.0 2D profile."),
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
                "API parity verified.",
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
        var smokeResult = Electron2D.Electron2DAndroidDeviceSmokeRunner.Run(
            planContext.Plan,
            smokeOutput,
            Electron2D.Electron2DAndroidDeviceSmokeObservation.Blocked("No connected Android device or emulator is available for Android export smoke."),
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
            "project" => "  create|validate       Create an AI-ready project or validate a project without requiring GUI runtime.",
            "mcp" => "  serve                 Emit local MCP resources and tools manifest.",
            "doctor" => "  <default>             Inspect reproducibility lock and local environment without opening workspace.",
            "workspace" => "  transaction           Apply a generic workspace text transaction.",
            "run" => "  debug                 Inspect runtime state, or queue/run headless with runtime options.",
            "import" or "build" or "export" => "  <default>             Queue a job and emit JSON or JSONL status.",
            "test" => "  <default>             Queue a job, or run scene tests with --format json and a scene-test manifest.",
            "validate" => "  <default>             Validate a project and emit text, JSON or SARIF diagnostics.",
            "docs" => "  search|type|member|example",
            "api" => "  compare-godot <type>  Compare one API type against the approved Electron2D 0.1.0 2D profile.",
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

    private static JsonObject? FindApiManifestType(JsonObject manifest, string query)
    {
        var normalized = NormalizeApiTypeQuery(query);
        var types = manifest["types"]?.AsArray()
            ?? throw new CommandLineException("API manifest is missing `types`.");
        return types
            .OfType<JsonObject>()
            .FirstOrDefault(type =>
                string.Equals(Value(type, "fullName"), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Value(type, "name"), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Value(type, "fullName"), "Electron2D." + normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static JsonObject BuildApiCompareData(JsonObject? type, string? query, string resultStatus, JsonObject? manifest)
    {
        return new JsonObject
        {
            ["mode"] = "api.compareGodot",
            ["sourcePath"] = LocalDocumentationStore.ApiManifestPath,
            ["query"] = query,
            ["type"] = type is null ? null : BuildApiTypeSummary(type),
            ["result"] = new JsonObject
            {
                ["status"] = resultStatus
            },
            ["strictParity"] = manifest is null
                ? new JsonObject()
                : CloneObject(manifest["strictParitySummary"], "API manifest is missing `strictParitySummary`.")
        };
    }

    private static JsonObject BuildApiTypeSummary(JsonObject type)
    {
        var profile = type["profile"]?.AsObject()
            ?? throw new CommandLineException($"API manifest type '{Value(type, "fullName")}' is missing `profile`.");
        return new JsonObject
        {
            ["fullName"] = Value(type, "fullName"),
            ["id"] = Value(type, "id"),
            ["profile"] = new JsonObject
            {
                ["status"] = Value(profile, "status"),
                ["parity"] = Value(profile, "parity"),
                ["outOfProfile"] = profile["outOfProfile"]?.GetValue<bool>() ?? true
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
        var settingsPath = Path.Combine(projectRoot, "project.e2d.json");
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
        var planResult = Electron2D.Electron2DWebAssemblyExportPlanner.CreatePlan(preset, projectFilePath, settingsResult.Settings);
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
        var settingsPath = Path.Combine(projectRoot, "project.e2d.json");
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

    private static string FindAndroidSdkPath()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            @"G:\Android\Sdk",
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
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("JAVA_HOME"),
            @"G:\Dev\jdk17"
        };

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => Path.GetFullPath(candidate!))
            .FirstOrDefault(path => Directory.Exists(path) &&
                File.Exists(Path.Combine(path, "bin", "java.exe")) &&
                IsJavaSdk17OrNewer(path)) ?? string.Empty;
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
