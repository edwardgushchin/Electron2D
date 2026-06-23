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
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;

internal static partial class Electron2DCommandLine
{
    private static bool IsWindowsExportCommand(CliOptions options)
    {
        return options.Values.Count > 0 &&
            (string.Equals(options.Values[0], "plan-windows", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Values[0], "build-windows", StringComparison.OrdinalIgnoreCase));
    }

    private static int RunWindowsExport(CliOptions options, TextWriter output, TextWriter error)
    {
        return options.Values[0].ToLowerInvariant() switch
        {
            "plan-windows" => RunWindowsExportPlan(options, output, error),
            "build-windows" => RunWindowsExportBuild(options, output, error),
            _ => WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "Use `e2d export plan-windows` or `e2d export build-windows`.")),
                output,
                error)
        };
    }

    private static int RunWindowsExportPlan(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export plan-windows` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateWindowsExportPlanContext("export plan-windows", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        return WriteResult(
            CliResult.Success(
                "export plan-windows",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "Windows x64 export plan created.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildWindowsExportPlanData(planContext.Plan)),
            output,
            error);
    }

    private static int RunWindowsExportBuild(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("export", options),
                    options,
                    "Unknown export command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d export build-windows` does not take a subcommand.")),
                output,
                error);
        }

        if (!TryCreateWindowsExportPlanContext("export build-windows", options, out var planContext, out var failure))
        {
            return WriteResult(failure!, output, error);
        }

        PrepareWindowsOutputDirectory(planContext.Plan);

        var assemblyPath = BuildAndResolveAssembly(
            planContext.ProjectRoot,
            planContext.Plan.ProjectFilePath,
            options,
            planContext.Plan.Configuration.ToString());
        var metadata = ReadProjectMetadata(planContext.Plan.ProjectFilePath);

        var publishDiagnostics = PublishWindowsPlayer(planContext.Plan);
        if (publishDiagnostics.Count > 0)
        {
            return WriteResult(
                CliResult.Failure(
                    "export build-windows",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "Windows player publish failed.",
                    publishDiagnostics,
                    BuildWindowsExportFailureData("export.windows.build", "publish_failed", planContext.Plan)),
                output,
                error);
        }

        CopyProjectAssemblyToWindowsOutput(assemblyPath, planContext.Plan.OutputDirectory);
        var packageResult = Electron2D.WindowsPackageBuilder.Build(
            planContext.Plan,
            planContext.ProjectRoot,
            planContext.Settings,
            Path.GetFileName(assemblyPath),
            metadata.RootNamespace,
            metadata.TargetFramework);
        if (!packageResult.Succeeded)
        {
            return WriteResult(
                CliResult.Failure(
                    "export build-windows",
                    options,
                    planContext.ProjectRoot,
                    CliRoute.None,
                    "Windows resource package build failed.",
                    MapExportDiagnostics(packageResult.Diagnostics),
                    BuildWindowsExportFailureData("export.windows.build", "package_failed", planContext.Plan)),
                output,
                error);
        }

        return WriteResult(
            CliResult.Success(
                "export build-windows",
                options,
                planContext.ProjectRoot,
                CliRoute.None,
                "Windows player package created.",
                changedFiles: ToProjectRelativeWindowsFiles(planContext.ProjectRoot, planContext.Plan, packageResult),
                dirtyDocuments: [],
                operation: null,
                job: null,
                data: BuildWindowsExportBuildData(planContext.Plan, packageResult)),
            output,
            error);
    }

    private static bool TryCreateWindowsExportPlanContext(
        string command,
        CliOptions options,
        out WindowsExportPlanContext planContext,
        out CliResult? failure)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var settingsPath = Electron2D.ProjectFileLocator.ResolveProjectFilePath(projectRoot);
        var settingsResult = Electron2D.Electron2DSettingsStore.LoadProject(settingsPath);
        if (!settingsResult.Succeeded || settingsResult.Settings is null)
        {
            planContext = WindowsExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Windows x64 export planning failed.",
                settingsResult.Diagnostics.Select(diagnostic => CreateCliDiagnostic(
                    "E2D-CLI-0002",
                    $"{diagnostic.Code}: {diagnostic.Message}")).ToArray(),
                BuildWindowsExportFailureData(ModeForWindowsCommand(command), "settings_load_failed", plan: null));
            return false;
        }

        var configuration = ReadExportConfiguration(options.GetOption("--configuration") ?? "Debug", options, command);
        var outputDirectory = ResolveProjectChildPath(
            projectRoot,
            options.GetOption("--output") ?? Path.Combine("exports", "windows", configuration.ToString().ToLowerInvariant()));
        var projectFilePath = ResolveProjectFilePath(projectRoot, options.GetOption("--project-file"));
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = options.GetOption("--preset-name") ??
                (configuration == Electron2D.Electron2DExportConfiguration.Release ? "windows-release" : "windows-debug"),
            Target = Electron2D.Electron2DExportTarget.WindowsX64,
            Configuration = configuration,
            RuntimeIdentifier = "win-x64",
            SelfContained = true,
            RendererProfile = ReadRendererProfile(options.GetOption("--renderer-profile") ?? settingsResult.Settings.RendererProfile.ToString(), options, command),
            OutputDirectory = outputDirectory,
            IncludeDebugSymbols = ReadBooleanOption(options, "--debug-symbols", defaultValue: configuration == Electron2D.Electron2DExportConfiguration.Debug, command)
        };

        var planResult = Electron2D.WindowsExportPlanner.CreatePlan(preset, projectFilePath, settingsResult.Settings, settingsPath);
        if (!planResult.Succeeded || planResult.Plan is null)
        {
            planContext = WindowsExportPlanContext.Empty;
            failure = CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Windows x64 export planning failed.",
                MapExportDiagnostics(planResult.Diagnostics),
                BuildWindowsExportFailureData(ModeForWindowsCommand(command), "plan_failed", plan: null));
            return false;
        }

        planContext = new WindowsExportPlanContext(projectRoot, settingsResult.Settings, preset, planResult.Plan);
        failure = null;
        return true;
    }

    private static void PrepareWindowsOutputDirectory(Electron2D.WindowsExportPlan plan)
    {
        Directory.CreateDirectory(plan.OutputDirectory);
        foreach (var relative in plan.ForbiddenLooseFiles)
        {
            var normalized = relative.TrimEnd('/', '\\');
            var path = Path.Combine(plan.OutputDirectory, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }

    private static IReadOnlyList<StructuredDiagnostic> PublishWindowsPlayer(Electron2D.WindowsExportPlan plan)
    {
        try
        {
            var repositoryRoot = FindRepositoryRoot();
            var cliProject = Path.Combine(repositoryRoot, "src", "Electron2D.Cli", "Electron2D.Cli.csproj");
            var publish = RunProcess(
                repositoryRoot,
                "dotnet",
                [
                    "publish",
                    cliProject,
                    "--configuration",
                    plan.Configuration.ToString(),
                    "--runtime",
                    plan.RuntimeIdentifier,
                    "--self-contained",
                    plan.SelfContained ? "true" : "false",
                    "--output",
                    plan.OutputDirectory,
                    "--nologo"
                ]);
            if (publish.ExitCode != 0)
            {
                return [CreateCliDiagnostic("E2D-CLI-0002", "E2D-EXPORT-WINDOWS-0011: " + ExtractDotnetFailureMessage(publish.Output, publish.Error, publish.ExitCode, "dotnet publish failed."))];
            }

            var publishedAppHost = Path.Combine(plan.OutputDirectory, "e2d.exe");
            if (!File.Exists(publishedAppHost))
            {
                return [CreateCliDiagnostic("E2D-CLI-0002", "E2D-EXPORT-WINDOWS-0011: published player apphost e2d.exe was not found.")];
            }

            Directory.CreateDirectory(Path.GetDirectoryName(plan.ExecutablePath) ?? plan.OutputDirectory);
            File.Copy(publishedAppHost, plan.ExecutablePath, overwrite: true);
            if (!string.Equals(Path.GetFullPath(publishedAppHost), Path.GetFullPath(plan.ExecutablePath), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(publishedAppHost);
            }

            return [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return [CreateCliDiagnostic("E2D-CLI-0002", $"E2D-EXPORT-WINDOWS-0011: Windows player publish could not run: {exception.Message}")];
        }
    }

    private static void CopyProjectAssemblyToWindowsOutput(string assemblyPath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        CopyIfExists(assemblyPath, Path.Combine(outputDirectory, Path.GetFileName(assemblyPath)));
        CopyIfExists(Path.ChangeExtension(assemblyPath, ".pdb"), Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb"));
    }

    private static void CopyIfExists(string source, string destination)
    {
        if (File.Exists(source))
        {
            File.Copy(source, destination, overwrite: true);
        }
    }

    private static string[] ToProjectRelativeWindowsFiles(
        string projectRoot,
        Electron2D.WindowsExportPlan plan,
        Electron2D.WindowsPackageBuildResult packageResult)
    {
        var files = new List<string>
        {
            plan.ExecutablePath,
            plan.ResourceManifestPath
        };
        files.AddRange(packageResult.Files);
        return files
            .Select(path => path.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                ? Path.GetRelativePath(projectRoot, path).Replace('\\', '/')
                : path.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ModeForWindowsCommand(string command)
    {
        return command.Contains("build-windows", StringComparison.OrdinalIgnoreCase)
            ? "export.windows.build"
            : "export.windows.plan";
    }

    private static JsonObject BuildWindowsExportFailureData(
        string mode,
        string status,
        Electron2D.WindowsExportPlan? plan)
    {
        return new JsonObject
        {
            ["mode"] = mode,
            ["target"] = "WindowsX64",
            ["runtimeIdentifier"] = "win-x64",
            ["result"] = new JsonObject
            {
                ["status"] = status
            },
            ["plan"] = plan is null ? null : BuildWindowsExportPlanData(plan)["plan"]?.DeepClone()
        };
    }

    private static JsonObject BuildWindowsExportPlanData(Electron2D.WindowsExportPlan plan)
    {
        return new JsonObject
        {
            ["mode"] = "export.windows.plan",
            ["target"] = "WindowsX64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = "planned"
            },
            ["plan"] = new JsonObject
            {
                ["projectFilePath"] = plan.ProjectFilePath,
                ["projectSettingsPath"] = plan.ProjectSettingsPath,
                ["configuration"] = plan.Configuration.ToString(),
                ["runtimeIdentifier"] = plan.RuntimeIdentifier,
                ["selfContained"] = plan.SelfContained,
                ["outputDirectory"] = plan.OutputDirectory,
                ["executablePath"] = plan.ExecutablePath,
                ["resourceManifestPath"] = plan.ResourceManifestPath,
                ["resourcePackPaths"] = WriteStringArray(plan.ResourcePackPaths),
                ["publishArguments"] = WriteStringArray(plan.PublishArguments),
                ["rendererProfile"] = plan.RendererProfile.ToString(),
                ["graphicsBackend"] = plan.GraphicsBackend,
                ["displayMode"] = plan.DisplayMode.ToString(),
                ["windowSize"] = new JsonObject
                {
                    ["x"] = plan.WindowSize.X,
                    ["y"] = plan.WindowSize.Y
                },
                ["requiredFiles"] = WriteStringArray(plan.RequiredFiles),
                ["resourcePackEntries"] = WriteStringArray(plan.ResourcePackEntries),
                ["forbiddenLooseFiles"] = WriteStringArray(plan.ForbiddenLooseFiles),
                ["includeDebugSymbols"] = plan.IncludeDebugSymbols
            }
        };
    }

    private static JsonObject BuildWindowsExportBuildData(
        Electron2D.WindowsExportPlan plan,
        Electron2D.WindowsPackageBuildResult packageResult)
    {
        return new JsonObject
        {
            ["mode"] = "export.windows.build",
            ["target"] = "WindowsX64",
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["result"] = new JsonObject
            {
                ["status"] = "packaged"
            },
            ["plan"] = BuildWindowsExportPlanData(plan)["plan"]?.DeepClone(),
            ["package"] = new JsonObject
            {
                ["executablePath"] = plan.ExecutablePath,
                ["resourceManifestPath"] = plan.ResourceManifestPath,
                ["resourcePackPaths"] = WriteStringArray(plan.ResourcePackPaths),
                ["files"] = WriteStringArray(packageResult.Files),
                ["runCommand"] = $"\"{plan.ExecutablePath}\""
            }
        };
    }

    private sealed class WindowsExportPlanContext
    {
        public static readonly WindowsExportPlanContext Empty = new(
            string.Empty,
            new Electron2D.Electron2DProjectSettings(),
            new Electron2D.Electron2DExportPreset(),
            new Electron2D.WindowsExportPlan());

        public WindowsExportPlanContext(
            string projectRoot,
            Electron2D.Electron2DProjectSettings settings,
            Electron2D.Electron2DExportPreset preset,
            Electron2D.WindowsExportPlan plan)
        {
            ProjectRoot = projectRoot;
            Settings = settings;
            Preset = preset;
            Plan = plan;
        }

        public string ProjectRoot { get; }

        public Electron2D.Electron2DProjectSettings Settings { get; }

        public Electron2D.Electron2DExportPreset Preset { get; }

        public Electron2D.WindowsExportPlan Plan { get; }
    }
}
