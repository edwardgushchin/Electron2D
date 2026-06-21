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

internal static class Electron2DLinuxExportPlanner
{
    private static readonly string[] DesktopDisplayProtocols = ["wayland", "x11"];

    private static readonly string[] ExcludedRuntimeIdentifiers =
    [
        "linux-musl-x64",
        "linux-arm64",
        "linux-musl-arm64"
    ];

    public static Electron2DLinuxExportPlanResult CreatePlan(
        Electron2DExportPreset preset,
        string projectFilePath,
        Electron2DProjectSettings? projectSettings)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        ValidatePreset(preset, diagnostics);
        ValidateProject(projectFilePath, projectSettings, preset.Name, diagnostics);

        if (diagnostics.Count > 0 || projectSettings is null)
        {
            return new Electron2DLinuxExportPlanResult(null, diagnostics);
        }

        var outputDirectory = preset.OutputDirectory;
        var executableName = Path.GetFileNameWithoutExtension(projectFilePath);
        var plan = new Electron2DLinuxExportPlan
        {
            ProjectFilePath = projectFilePath,
            Configuration = preset.Configuration,
            RuntimeIdentifier = preset.RuntimeIdentifier,
            LibcFamily = "glibc",
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
            DesktopDisplayProtocols = DesktopDisplayProtocols.ToArray(),
            ExcludedRuntimeIdentifiers = ExcludedRuntimeIdentifiers.ToArray(),
            RequiredFiles =
            [
                "project.e2d.json",
                NormalizePortablePath(projectSettings.MainScene)
            ],
            IncludeDebugSymbols = preset.IncludeDebugSymbols
        };

        return new Electron2DLinuxExportPlanResult(plan, diagnostics);
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

        if (preset.Target != Electron2DExportTarget.LinuxX64)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-LINUX-0001",
                preset.Name,
                $"Linux export preset '{preset.Name}' must target LinuxX64."));
        }

        if (!string.Equals(preset.RuntimeIdentifier, "linux-x64", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-LINUX-0002",
                preset.Name,
                $"Linux export preset '{preset.Name}' must use runtime identifier linux-x64."));
        }

        if (!preset.SelfContained)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-LINUX-0003",
                preset.Name,
                $"Linux export preset '{preset.Name}' must be self-contained."));
        }

        if (IsMuslOrArmRuntimeIdentifier(preset.RuntimeIdentifier))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-LINUX-0004",
                preset.Name,
                $"Linux export preset '{preset.Name}' is limited to glibc x64; musl and ARM runtime identifiers are out of scope."));
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
                "E2D-EXPORT-LINUX-0005",
                presetName,
                $"Linux export preset '{presetName}' requires a project file path."));
        }

        if (projectSettings is null)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-LINUX-0006",
                presetName,
                $"Linux export preset '{presetName}' requires project settings."));
            return;
        }

        try
        {
            projectSettings.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-LINUX-0007", presetName, exception.Message));
        }
    }

    private static bool IsMuslOrArmRuntimeIdentifier(string runtimeIdentifier)
    {
        return runtimeIdentifier.Contains("musl", StringComparison.Ordinal)
            || runtimeIdentifier.Contains("arm", StringComparison.Ordinal);
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

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
