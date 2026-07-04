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
namespace Electron2D.Build;

internal sealed class TestCommand(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const int DefaultTimeoutSeconds = 3600;
    private const int HangTimeoutSeconds = 300;
    private const int DiagnosticTailLength = 12_000;
    private const string BaselineFilter = "Category!=Baseline";
    private const string IntegrationProject = "tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj";
    private const string IntegrationSliceAll = "all";
    private const string IntegrationSliceFast = "fast";
    private const string IntegrationSliceRepositoryTooling = "repository-tooling";
    private const string IntegrationSliceAuditPackage = "audit-package";
    private const string IntegrationSliceExternalProcess = "external-process";
    private const string IntegrationSliceSlow = "slow";
    private const string AuditTierFast = "Fast";
    private const string AuditTierMedium = "Medium";
    private const string AuditTierHeavy = "Heavy";

    private static readonly string[] TestProjects =
    [
        "tests/Electron2D.Tests.Unit/Electron2D.Tests.Unit.csproj",
        IntegrationProject,
        "tests/Electron2D.Tests.RuntimeSmoke/Electron2D.Tests.RuntimeSmoke.csproj",
        "tests/Electron2D.Tests.GoldenData/Electron2D.Tests.GoldenData.csproj"
    ];

    private static readonly string[] FastIntegrationExclusions =
    [
        "RepositoryBuildToolTests",
        "LocalDocumentationCliTests",
        "Electron2DCliWorkflowTests",
        "AgentAcceptanceBenchmarkTests",
        "DataStabilityStressTests",
        "LeakVerificationTests",
        "ReferencePerformanceVerificationTests",
        "RuntimeHostTests.RuntimeSdlRendererFallbackThrowsForUnsupportedTextureResource",
        "RuntimeHostTests.RuntimeSdlRendererFallbackThrowsForUnknownRenderCommandKind",
        "EditorAgentWorkspacePanelTests",
        "EditorFileSystemDockTests",
        "EditorInspectorTests",
        "EditorManagedDebuggerTests",
        "EditorProjectManagerTests",
        "EditorProjectSettingsUiTests",
        "EditorProjectShellTests",
        "EditorProjectTasksBoardTests",
        "EditorRunWorkflowTests",
        "EditorSceneTreeDockTests",
        "EditorScriptLanguageServicesTests",
        "EditorScriptWorkflowTests",
        "EditorScriptWorkspaceTests",
        "EditorShellLayoutTests",
        "EditorSpecializedEditorsTests",
        "EditorViewport2DTests"
    ];

    private static readonly string[] ExternalProcessIncludes =
    [
        "LocalDocumentationCliTests",
        "Electron2DCliWorkflowTests",
        "EditorAgentWorkspacePanelTests",
        "EditorFileSystemDockTests",
        "EditorInspectorTests",
        "EditorManagedDebuggerTests",
        "EditorProjectManagerTests",
        "EditorProjectSettingsUiTests",
        "EditorProjectShellTests",
        "EditorProjectTasksBoardTests",
        "EditorRunWorkflowTests",
        "EditorSceneTreeDockTests",
        "EditorScriptLanguageServicesTests",
        "EditorScriptWorkflowTests",
        "EditorScriptWorkspaceTests",
        "EditorShellLayoutTests",
        "EditorSpecializedEditorsTests",
        "EditorViewport2DTests"
    ];

    private static readonly string[] SlowIncludes =
    [
        "AgentAcceptanceBenchmarkTests",
        "DataStabilityStressTests",
        "LeakVerificationTests",
        "ReferencePerformanceVerificationTests"
    ];

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = ParseOptions(args);
        if (options is null)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "test",
            "test",
            "info",
            "E2D-BUILD-TEST-STARTED",
            $"Running {TestProjects.Length} test projects.",
            TimeoutSeconds: options.TimeoutSeconds));

        var testProjects = GetTestProjects(options);
        foreach (var project in testProjects)
        {
            var step = $"test {project}";
            var arguments = CreateDotnetTestArguments(project, options);
            diagnostics.Write(new BuildDiagnostic(
                "test",
                step,
                "info",
                "E2D-BUILD-TEST-PROJECT-STARTED",
                $"Running dotnet test for '{project}'.",
                ProjectPath: project,
                TimeoutSeconds: options.TimeoutSeconds));

            var result = await processRunner.RunAsync(
                new ProcessRunRequest(
                    step,
                    "dotnet",
                    arguments,
                    repositoryRoot,
                    TimeSpan.FromSeconds(options.TimeoutSeconds)),
                cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in result.Diagnostics)
            {
                diagnostics.Write(diagnostic);
            }

            if (result.TimedOut)
            {
                diagnostics.Write(new BuildDiagnostic(
                    "test",
                    step,
                    "error",
                    "E2D-BUILD-TEST-PROJECT-TIMEOUT",
                    $"dotnet test timed out for '{project}' after {options.TimeoutSeconds} seconds.",
                    TimedOut: true,
                    ProjectPath: project,
                    TimeoutSeconds: options.TimeoutSeconds,
                    StandardOutputTail: Tail(result.StandardOutput),
                    StandardErrorTail: Tail(result.StandardError)));
                return RepositoryBuildExitCodes.Failed;
            }

            if (result.ExitCode != 0)
            {
                diagnostics.Write(new BuildDiagnostic(
                    "test",
                    step,
                    "error",
                    "E2D-BUILD-TEST-PROJECT-FAILED",
                    $"dotnet test failed for '{project}' with exit code {result.ExitCode}.",
                    ProcessExitCode: result.ExitCode,
                    TimedOut: false,
                    ProjectPath: project,
                    TimeoutSeconds: options.TimeoutSeconds,
                    StandardOutputTail: Tail(result.StandardOutput),
                    StandardErrorTail: Tail(result.StandardError)));
                return result.ExitCode ?? RepositoryBuildExitCodes.Failed;
            }

            diagnostics.Write(new BuildDiagnostic(
                "test",
                step,
                "info",
                "E2D-BUILD-TEST-PROJECT-PASSED",
                $"dotnet test passed for '{project}'.",
                ProcessExitCode: 0,
                TimedOut: false,
                ProjectPath: project,
                TimeoutSeconds: options.TimeoutSeconds));
        }

        diagnostics.Write(new BuildDiagnostic(
            "test",
            "test",
            "info",
            "E2D-BUILD-TEST-PASSED",
            "All configured test projects passed.",
            TimeoutSeconds: options.TimeoutSeconds));
        return RepositoryBuildExitCodes.Success;
    }

    private TestCommandOptions? ParseOptions(string[] args)
    {
        var includeBaseline = false;
        var integrationSlice = IntegrationSliceAll;
        var noBuild = false;
        var noRestore = false;
        var timeoutSeconds = DefaultTimeoutSeconds;

        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == "--include-baseline")
            {
                includeBaseline = true;
                continue;
            }

            if (argument == "--no-build")
            {
                noBuild = true;
                continue;
            }

            if (argument == "--no-restore")
            {
                noRestore = true;
                continue;
            }

            if (argument == "--integration-slice")
            {
                if (index + 1 >= args.Length || !TryParseIntegrationSlice(args[index + 1], out integrationSlice))
                {
                    WriteInvalidArguments();
                    return null;
                }

                index++;
                continue;
            }

            if (argument == "--timeout-seconds")
            {
                if (index + 1 >= args.Length ||
                    !int.TryParse(args[index + 1], out timeoutSeconds) ||
                    timeoutSeconds <= 0)
                {
                    WriteInvalidArguments();
                    return null;
                }

                index++;
                continue;
            }

            WriteInvalidArguments();
            return null;
        }

        return new TestCommandOptions(includeBaseline, integrationSlice, noBuild, noRestore, timeoutSeconds);
    }

    private void WriteInvalidArguments()
    {
        diagnostics.Write(new BuildDiagnostic(
            "test",
            "test",
            "error",
            "E2D-BUILD-CLI-INVALID-ARGUMENTS",
            "Expected: test [--include-baseline] [--integration-slice <all|fast|repository-tooling|audit-package|external-process|slow>] [--no-build] [--no-restore] [--timeout-seconds <n>]."));
    }

    private static string[] GetTestProjects(TestCommandOptions options)
    {
        return options.IntegrationSlice is IntegrationSliceAll or IntegrationSliceFast
            ? TestProjects
            : [IntegrationProject];
    }

    private static string[] CreateDotnetTestArguments(string project, TestCommandOptions options)
    {
        var arguments = new List<string>
        {
            "test",
            project
        };

        var filter = CreateFilter(project, options);
        if (filter is not null)
        {
            arguments.Add("--filter");
            arguments.Add(filter);
        }

        if (options.NoBuild)
        {
            arguments.Add("--no-build");
        }

        if (options.NoRestore)
        {
            arguments.Add("--no-restore");
        }

        arguments.AddRange([
            "--blame-hang",
            "--blame-hang-timeout",
            $"{HangTimeoutSeconds}s",
            "--blame-hang-dump-type",
            "none",
            "--logger",
            "console;verbosity=normal"
        ]);

        return arguments.ToArray();
    }

    private static string? CreateFilter(string project, TestCommandOptions options)
    {
        var filters = new List<string>();
        if (!options.IncludeBaseline)
        {
            filters.Add(BaselineFilter);
        }

        if (project == IntegrationProject)
        {
            var sliceFilter = CreateIntegrationSliceFilter(options.IntegrationSlice);
            if (sliceFilter is not null)
            {
                filters.Add(sliceFilter);
            }
        }

        return filters.Count switch
        {
            0 => null,
            1 => filters[0],
            _ => string.Join("&", filters.Select(filter => $"({filter})"))
        };
    }

    private static string? CreateIntegrationSliceFilter(string integrationSlice)
    {
        return integrationSlice switch
        {
            IntegrationSliceAll => null,
            IntegrationSliceFast => And([.. FastIntegrationExclusions.Select(NotFullyQualifiedNameContains)]),
            IntegrationSliceRepositoryTooling => And(
                [FullyQualifiedNameContains("RepositoryBuildToolTests"),
                    NotAuditTier(AuditTierFast),
                    NotAuditTier(AuditTierMedium),
                    NotAuditTier(AuditTierHeavy)]),
            IntegrationSliceAuditPackage => Or([AuditTier(AuditTierMedium), AuditTier(AuditTierHeavy)]),
            IntegrationSliceExternalProcess => Or([.. ExternalProcessIncludes.Select(FullyQualifiedNameContains)]),
            IntegrationSliceSlow => Or([.. SlowIncludes.Select(FullyQualifiedNameContains)]),
            _ => throw new InvalidOperationException($"Unsupported integration slice: {integrationSlice}")
        };
    }

    private static bool TryParseIntegrationSlice(string value, out string integrationSlice)
    {
        integrationSlice = value;
        return value is IntegrationSliceAll or
            IntegrationSliceFast or
            IntegrationSliceRepositoryTooling or
            IntegrationSliceAuditPackage or
            IntegrationSliceExternalProcess or
            IntegrationSliceSlow;
    }

    private static string FullyQualifiedNameContains(string value)
    {
        return $"FullyQualifiedName~{value}";
    }

    private static string NotFullyQualifiedNameContains(string value)
    {
        return $"FullyQualifiedName!~{value}";
    }

    private static string AuditTier(string value)
    {
        return $"AuditTier={value}";
    }

    private static string NotAuditTier(string value)
    {
        return $"AuditTier!={value}";
    }

    private static string And(IReadOnlyList<string> filters)
    {
        return string.Join("&", filters.Select(filter => $"({filter})"));
    }

    private static string Or(IReadOnlyList<string> filters)
    {
        return string.Join("|", filters.Select(filter => $"({filter})"));
    }

    private static string? Tail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= DiagnosticTailLength
            ? value
            : value[^DiagnosticTailLength..];
    }

    private sealed record TestCommandOptions(bool IncludeBaseline, string IntegrationSlice, bool NoBuild, bool NoRestore, int TimeoutSeconds);
}
