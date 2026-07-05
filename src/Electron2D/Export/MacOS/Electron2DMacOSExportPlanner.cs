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

internal static class Electron2DMacOSExportPlanner
{
    private static readonly string[] UnsupportedRuntimeIdentifiers =
    [
        "osx-x64",
        "osx.10.15-x64"
    ];

    public static Electron2DMacOSExportPlanResult CreatePlan(
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
            return new Electron2DMacOSExportPlanResult(null, diagnostics);
        }

        var outputDirectory = preset.OutputDirectory;
        var publishOutputDirectory = Path.Combine(outputDirectory, "publish");
        var executableName = Path.GetFileNameWithoutExtension(projectFilePath);
        var bundleName = CreateBundleName(projectSettings.Name, executableName);
        var appBundlePath = Path.Combine(outputDirectory, bundleName + ".app");
        var contentsDirectory = Path.Combine(appBundlePath, "Contents");
        var macOSDirectory = Path.Combine(contentsDirectory, "MacOS");
        var resourcesDirectory = Path.Combine(contentsDirectory, "Resources");
        var signingIdentity = preset.Signing.Identity ?? "";
        var signingCredentialReference = preset.Signing.CredentialReference ?? "";

        var plan = new Electron2DMacOSExportPlan
        {
            ProjectFilePath = projectFilePath,
            Configuration = preset.Configuration,
            RuntimeIdentifier = preset.RuntimeIdentifier,
            Architecture = "arm64",
            SelfContained = preset.SelfContained,
            OutputDirectory = outputDirectory,
            PublishOutputDirectory = publishOutputDirectory,
            AppBundlePath = appBundlePath,
            ContentsDirectory = contentsDirectory,
            MacOSDirectory = macOSDirectory,
            ResourcesDirectory = resourcesDirectory,
            ExecutablePath = Path.Combine(macOSDirectory, executableName),
            InfoPlistPath = Path.Combine(contentsDirectory, "Info.plist"),
            BundleName = bundleName,
            ExecutableName = executableName,
            BundleIdentifier = CreateBundleIdentifier(projectSettings.Name, executableName),
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
                publishOutputDirectory
            ],
            RendererProfile = preset.RendererProfile,
            GraphicsBackend = "metal",
            RequiredBundleFiles =
            [
                "Contents/Info.plist",
                "Contents/MacOS/" + executableName,
                "Contents/MacOS/project.e2d.json",
                "Contents/MacOS/" + NormalizePortablePath(projectSettings.MainScene)
            ],
            UnsupportedRuntimeIdentifiers = UnsupportedRuntimeIdentifiers.ToArray(),
            X64Policy = "unsupported-in-0.1-preview",
            IncludeDebugSymbols = preset.IncludeDebugSymbols,
            SigningRequired = preset.Signing.Required,
            SigningIdentity = signingIdentity,
            SigningCredentialReference = signingCredentialReference,
            CodesignArguments = preset.Signing.Required
                ?
                [
                    "--force",
                    "--options",
                    "runtime",
                    "--sign",
                    signingIdentity,
                    appBundlePath
                ]
                : []
        };

        return new Electron2DMacOSExportPlanResult(plan, diagnostics);
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

        if (preset.Target != Electron2DExportTarget.MacOSArm64)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-MACOS-0001",
                preset.Name,
                $"macOS export preset '{preset.Name}' must target MacOSArm64."));
        }

        if (!string.Equals(preset.RuntimeIdentifier, "osx-arm64", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-MACOS-0002",
                preset.Name,
                $"macOS export preset '{preset.Name}' must use runtime identifier osx-arm64."));
        }

        if (!preset.SelfContained)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-MACOS-0003",
                preset.Name,
                $"macOS export preset '{preset.Name}' must be self-contained."));
        }

        if (IsX64RuntimeIdentifier(preset.RuntimeIdentifier))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-MACOS-0004",
                preset.Name,
                $"macOS export preset '{preset.Name}' is limited to arm64; x64 is out of scope."));
        }

        if (preset.Signing.Required && string.IsNullOrWhiteSpace(preset.Signing.Identity))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-MACOS-0008",
                preset.Name,
                $"macOS export preset '{preset.Name}' requires a user-provided signing identity."));
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
                "E2D-EXPORT-MACOS-0005",
                presetName,
                $"macOS export preset '{presetName}' requires a project file path."));
        }

        if (projectSettings is null)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-MACOS-0006",
                presetName,
                $"macOS export preset '{presetName}' requires project settings."));
            return;
        }

        try
        {
            projectSettings.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-MACOS-0007", presetName, exception.Message));
        }
    }

    private static bool IsX64RuntimeIdentifier(string runtimeIdentifier)
    {
        return runtimeIdentifier.Contains("x64", StringComparison.Ordinal);
    }

    private static string CreateBundleName(string projectName, string executableName)
    {
        var source = string.IsNullOrWhiteSpace(projectName) ? executableName : projectName;
        var chars = source.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? executableName : new string(chars);
    }

    private static string CreateBundleIdentifier(string projectName, string executableName)
    {
        var source = string.IsNullOrWhiteSpace(projectName) ? executableName : projectName;
        var chars = source
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        var suffix = chars.Length == 0 ? "game" : new string(chars);
        return "dev.electron2d." + suffix;
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
