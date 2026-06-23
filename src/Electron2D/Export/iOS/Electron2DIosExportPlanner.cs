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

internal static class Electron2DIosExportPlanner
{
    internal static readonly string[] MobilePolicies =
    [
        "touchInput",
        "lifecycleForegroundBackground",
        "safeArea",
        "orientation",
        "audioSession",
        "resourceLoading",
        "filesystemSandbox",
        "precompiledRenderingArtifacts"
    ];

    internal static readonly string[] SmokeCriteria =
    [
        "build",
        "install",
        "launch",
        "render",
        "input",
        "lifecycle",
        "orientation",
        "safeArea",
        "audio",
        "resources",
        "filesystem",
        "precompiledArtifacts",
        "shutdown"
    ];

    public static Electron2DIosExportPlanResult CreatePlan(
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
            return new Electron2DIosExportPlanResult(null, diagnostics);
        }

        var outputDirectory = preset.OutputDirectory;
        var stagingDirectory = Path.Combine(outputDirectory, "ios");
        var artifactsDirectory = Path.Combine(outputDirectory, "artifacts", preset.Configuration.ToString().ToLowerInvariant());
        var smokeDirectory = Path.Combine(outputDirectory, "smoke");
        var executableName = Path.GetFileNameWithoutExtension(projectFilePath);
        var appName = CreateAppName(projectSettings.Name, executableName);
        var bundleIdentifier = CreateBundleIdentifier(projectSettings.Name, executableName);
        var xcodeProjectDirectory = Path.Combine(stagingDirectory, "Electron2D.iOS.xcodeproj");
        var signingIdentity = preset.Signing.Identity ?? string.Empty;
        var signingCredentialReference = preset.Signing.CredentialReference ?? string.Empty;

        var plan = new Electron2DIosExportPlan
        {
            ProjectFilePath = projectFilePath,
            Configuration = preset.Configuration,
            RuntimeIdentifier = preset.RuntimeIdentifier,
            Architecture = "arm64",
            TargetFramework = "net10.0-ios",
            SelfContained = preset.SelfContained,
            OutputDirectory = outputDirectory,
            StagingDirectory = stagingDirectory,
            ArtifactsDirectory = artifactsDirectory,
            SmokeDirectory = smokeDirectory,
            IosProjectFilePath = Path.Combine(stagingDirectory, "Electron2D.iOS.csproj"),
            AppDelegatePath = Path.Combine(stagingDirectory, "AppDelegate.cs"),
            InfoPlistPath = Path.Combine(stagingDirectory, "Info.plist"),
            EntitlementsPath = Path.Combine(stagingDirectory, "Entitlements.plist"),
            ExportMetadataPath = Path.Combine(stagingDirectory, "ExportMetadata.json"),
            XcodeProjectDirectory = xcodeProjectDirectory,
            XcodeProjectFilePath = Path.Combine(xcodeProjectDirectory, "project.pbxproj"),
            AppBundlePath = Path.Combine(artifactsDirectory, appName + ".app"),
            ProjectAssetsDirectory = Path.Combine(stagingDirectory, "Assets", "electron2d"),
            AppName = appName,
            ExecutableName = executableName,
            BundleIdentifier = bundleIdentifier,
            PublishArguments =
            [
                "publish",
                Path.Combine(stagingDirectory, "Electron2D.iOS.csproj"),
                "--configuration",
                preset.Configuration.ToString(),
                "--framework",
                "net10.0-ios",
                "--runtime",
                preset.RuntimeIdentifier,
                "--self-contained",
                preset.SelfContained ? "true" : "false",
                "--output",
                artifactsDirectory
            ],
            XcodeBuildArguments =
            [
                "-project",
                xcodeProjectDirectory,
                "-scheme",
                "Electron2D.iOS",
                "-configuration",
                preset.Configuration.ToString(),
                "-destination",
                "generic/platform=iOS"
            ],
            RendererProfile = preset.RendererProfile,
            GraphicsBackend = "metal",
            RequiredFiles =
            [
                "Electron2D.iOS.csproj",
                "AppDelegate.cs",
                "Info.plist",
                "Entitlements.plist",
                "ExportMetadata.json",
                "Electron2D.iOS.xcodeproj/project.pbxproj",
                "Assets/electron2d/project.e2d.json",
                "Assets/electron2d/" + NormalizePortablePath(projectSettings.MainScene)
            ],
            MobilePolicies = MobilePolicies.ToArray(),
            SmokeCriteria = SmokeCriteria.ToArray(),
            IncludeDebugSymbols = preset.IncludeDebugSymbols,
            SigningRequired = preset.Signing.Required,
            SigningIdentity = signingIdentity,
            SigningCredentialReference = signingCredentialReference
        };

        return new Electron2DIosExportPlanResult(plan, diagnostics);
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

        if (preset.Target != Electron2DExportTarget.IosArm64)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0001",
                preset.Name,
                $"iOS export preset '{preset.Name}' must target IosArm64."));
        }

        if (!string.Equals(preset.RuntimeIdentifier, "ios-arm64", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0002",
                preset.Name,
                $"iOS export preset '{preset.Name}' must use runtime identifier ios-arm64."));
        }

        if (!preset.SelfContained)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0003",
                preset.Name,
                $"iOS export preset '{preset.Name}' must be self-contained."));
        }

        if (preset.Signing.Required && string.IsNullOrWhiteSpace(preset.Signing.Identity))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0007",
                preset.Name,
                $"iOS export preset '{preset.Name}' requires a user-provided signing identity."));
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
                "E2D-EXPORT-IOS-0004",
                presetName,
                $"iOS export preset '{presetName}' requires a project file path."));
        }

        if (projectSettings is null)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0005",
                presetName,
                $"iOS export preset '{presetName}' requires project settings."));
            return;
        }

        try
        {
            projectSettings.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-IOS-0006", presetName, exception.Message));
        }
    }

    private static string CreateAppName(string projectName, string executableName)
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
