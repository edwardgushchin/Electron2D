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
namespace Electron2D;

internal static class WindowsExportPlanner
{
    public static WindowsExportPlanResult CreatePlan(
        Electron2DExportPreset preset,
        string projectFilePath,
        Electron2DProjectSettings? projectSettings,
        string projectSettingsPath = "")
    {
        ArgumentNullException.ThrowIfNull(preset);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        ValidatePreset(preset, diagnostics);
        ValidateProject(projectFilePath, projectSettings, preset.Name, diagnostics);

        if (diagnostics.Count > 0 || projectSettings is null)
        {
            return new WindowsExportPlanResult(null, diagnostics);
        }

        var outputDirectory = preset.OutputDirectory;
        var executableName = Path.GetFileNameWithoutExtension(projectFilePath) + ".exe";
        var resolvedProjectSettingsPath = ResolveProjectSettingsPath(projectFilePath, projectSettingsPath);
        var projectSettingsFileName = Path.GetFileName(resolvedProjectSettingsPath);
        var mainScene = NormalizePortablePath(projectSettings.MainScene);
        var scenePackage = GetScenePackagePath(outputDirectory, mainScene);
        var plan = new WindowsExportPlan
        {
            ProjectFilePath = projectFilePath,
            ProjectSettingsPath = resolvedProjectSettingsPath,
            Configuration = preset.Configuration,
            RuntimeIdentifier = preset.RuntimeIdentifier,
            SelfContained = preset.SelfContained,
            OutputDirectory = outputDirectory,
            ExecutablePath = Path.Combine(outputDirectory, executableName),
            PublishArguments =
            [
                "publish",
                projectFilePath,
                "--configuration",
                preset.Configuration.ToString(),
                "--runtime",
                preset.RuntimeIdentifier,
                "--self-contained",
                preset.SelfContained ? "true" : "false",
                "--output",
                outputDirectory
            ],
            RendererProfile = preset.RendererProfile,
            GraphicsBackend = GetGraphicsBackendName(preset.RendererProfile),
            DisplayMode = projectSettings.Display.Fullscreen
                ? WindowsDisplayMode.Fullscreen
                : WindowsDisplayMode.Windowed,
            WindowSize = projectSettings.Display.WindowSize,
            RequiredFiles =
            [
                executableName,
                "electron2d.pack.json",
                "packs/project.e2dpkg",
                NormalizePortablePath(Path.GetRelativePath(outputDirectory, scenePackage))
            ],
            ResourceManifestPath = Path.Combine(outputDirectory, "electron2d.pack.json"),
            ResourcePackPaths =
            [
                Path.Combine(outputDirectory, "packs", "project.e2dpkg"),
                scenePackage
            ],
            ResourcePackEntries =
            [
                $"packs/project.e2dpkg::{projectSettingsFileName}",
                $"{NormalizePortablePath(Path.GetRelativePath(outputDirectory, scenePackage))}::{mainScene}",
                "assets/",
                "resources/",
                "scenes/"
            ],
            ForbiddenLooseFiles =
            [
                projectSettingsFileName,
                "project.e2d.json",
                "export_presets.e2export.json",
                "assets/",
                "resources/",
                "scenes/",
                ".electron2d/tasks/"
            ],
            IncludeDebugSymbols = preset.IncludeDebugSymbols
        };

        return new WindowsExportPlanResult(plan, diagnostics);
    }

    private static void ValidatePreset(Electron2DExportPreset preset, List<Electron2DExportDiagnostic> diagnostics)
    {
        try
        {
            preset.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-PRESET-0002", preset.Name, exception.Message));
            return;
        }

        if (preset.Target != Electron2DExportTarget.WindowsX64)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WINDOWS-0001",
                preset.Name,
                $"Windows export preset '{preset.Name}' must target WindowsX64."));
        }

        if (!string.Equals(preset.RuntimeIdentifier, "win-x64", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WINDOWS-0002",
                preset.Name,
                $"Windows export preset '{preset.Name}' must use runtime identifier win-x64."));
        }

        if (!preset.SelfContained)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WINDOWS-0003",
                preset.Name,
                $"Windows export preset '{preset.Name}' must be self-contained."));
        }
    }

    private static void ValidateProject(
        string projectFilePath,
        Electron2DProjectSettings? projectSettings,
        string presetName,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WINDOWS-0004",
                presetName,
                $"Windows export preset '{presetName}' requires a project file path."));
        }

        if (projectSettings is null)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WINDOWS-0006",
                presetName,
                $"Windows export preset '{presetName}' requires project settings."));
            return;
        }

        try
        {
            projectSettings.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-WINDOWS-0007", presetName, exception.Message));
        }
    }

    private static string GetGraphicsBackendName(Electron2DRendererProfileSetting rendererProfile)
    {
        return rendererProfile switch
        {
            Electron2DRendererProfileSetting.Standard => "standard",
            Electron2DRendererProfileSetting.Compatibility => "compatibility",
            Electron2DRendererProfileSetting.Automatic => "automatic",
            _ => "unknown"
        };
    }

    private static string NormalizePortablePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string GetScenePackagePath(string outputDirectory, string scenePath)
    {
        var relativeWithoutExtension = NormalizePortablePath(scenePath);
        if (relativeWithoutExtension.StartsWith("scenes/", StringComparison.OrdinalIgnoreCase))
        {
            relativeWithoutExtension = relativeWithoutExtension["scenes/".Length..];
        }

        relativeWithoutExtension = relativeWithoutExtension.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase)
            ? relativeWithoutExtension[..^".scene.json".Length]
            : Path.ChangeExtension(relativeWithoutExtension, null) ?? relativeWithoutExtension;

        return Path.Combine(
            outputDirectory,
            "packs",
            "scenes",
            (relativeWithoutExtension + ".e2dpkg").Replace('/', Path.DirectorySeparatorChar));
    }

    private static string ResolveProjectSettingsPath(string projectFilePath, string projectSettingsPath)
    {
        if (!string.IsNullOrWhiteSpace(projectSettingsPath))
        {
            return Path.GetFullPath(projectSettingsPath);
        }

        var projectRoot = Path.GetDirectoryName(Path.GetFullPath(projectFilePath)) ?? Environment.CurrentDirectory;
        return ProjectFileLocator.TryResolveProjectFilePath(projectRoot, out var resolved)
            ? resolved
            : Path.Combine(projectRoot, ProjectFileLocator.LegacyProjectFileName);
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
