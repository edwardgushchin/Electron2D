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

internal static class Electron2DAndroidExportPlanner
{
    private static readonly string[] MobilePolicies =
    [
        "touchInput",
        "lifecyclePauseResume",
        "orientation",
        "safeArea",
        "immersiveFullscreen",
        "audioFocus",
        "appSandboxFilesystem",
        "packagedResources",
        "mobileGraphicsProfile",
        "compatibilityFallback"
    ];

    private static readonly string[] SmokeCriteria =
    [
        "install",
        "launch",
        "render",
        "input",
        "pauseResume",
        "orientation",
        "safeArea",
        "audio",
        "resources",
        "filesystem",
        "rendererFallback",
        "shutdown"
    ];

    public static Electron2DAndroidExportPlanResult CreatePlan(
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
            return new Electron2DAndroidExportPlanResult(null, diagnostics);
        }

        var outputDirectory = preset.OutputDirectory;
        var stagingDirectory = Path.Combine(outputDirectory, "android");
        var configurationName = preset.Configuration == Electron2DExportConfiguration.Release ? "release" : "debug";
        var packageFormat = preset.Configuration == Electron2DExportConfiguration.Release ? "aab" : "apk";
        var dotnetCommand = preset.Configuration == Electron2DExportConfiguration.Release ? "publish" : "build";
        var artifactsDirectory = Path.Combine(outputDirectory, "artifacts", configurationName);
        var projectAssetsDirectory = Path.Combine(stagingDirectory, "Assets", "electron2d");
        var androidProjectFilePath = Path.Combine(stagingDirectory, "Electron2D.Android.csproj");
        var mainScene = NormalizePortablePath(projectSettings.MainScene);

        var buildArguments = new List<string>
        {
            dotnetCommand,
            androidProjectFilePath,
            "--configuration",
            preset.Configuration.ToString(),
            "--framework",
            "net10.0-android",
            "-p:RuntimeIdentifier=android-arm64",
            "-p:RuntimeIdentifiers=android-arm64",
            "-p:AndroidPackageFormat=" + packageFormat,
            "-p:AndroidCreatePackagePerAbi=false"
        };

        if (preset.Signing.Required)
        {
            buildArguments.Add("-p:AndroidKeyStore=true");
            if (!string.IsNullOrWhiteSpace(preset.Signing.Identity))
            {
                buildArguments.Add("-p:AndroidSigningKeyAlias=" + preset.Signing.Identity);
            }
        }

        var plan = new Electron2DAndroidExportPlan
        {
            ProjectFilePath = projectFilePath,
            Configuration = preset.Configuration,
            RuntimeIdentifier = preset.RuntimeIdentifier,
            Abi = "arm64-v8a",
            PackageFormat = packageFormat,
            DotnetCommand = dotnetCommand,
            SelfContained = preset.SelfContained,
            OutputDirectory = outputDirectory,
            StagingDirectory = stagingDirectory,
            ArtifactsDirectory = artifactsDirectory,
            SmokeDirectory = Path.Combine(outputDirectory, "smoke"),
            AndroidProjectFilePath = androidProjectFilePath,
            MainActivityPath = Path.Combine(stagingDirectory, "MainActivity.cs"),
            ManifestPath = Path.Combine(stagingDirectory, "AndroidManifest.xml"),
            ExportMetadataPath = Path.Combine(stagingDirectory, "Resources", "values", "electron2d_export.xml"),
            ProjectAssetsDirectory = projectAssetsDirectory,
            BuildArguments = buildArguments.ToArray(),
            RendererProfile = preset.RendererProfile,
            GraphicsBackend = GetGraphicsBackendName(preset.RendererProfile),
            MobileGraphicsProfile = "android-mobile",
            FallbackPolicy = "compatibility-renderer-fallback",
            RequiredFiles =
            [
                "Electron2D.Android.csproj",
                "MainActivity.cs",
                "AndroidManifest.xml",
                "project.e2d.json",
                mainScene
            ],
            MobilePolicies = MobilePolicies.ToArray(),
            SmokeCriteria = SmokeCriteria.ToArray(),
            IncludeDebugSymbols = preset.IncludeDebugSymbols,
            SigningRequired = preset.Signing.Required,
            SigningIdentity = preset.Signing.Identity ?? string.Empty,
            SigningCredentialReference = preset.Signing.CredentialReference ?? string.Empty,
            Orientation = ToAndroidOrientation(projectSettings.Display.Orientation)
        };

        return new Electron2DAndroidExportPlanResult(plan, diagnostics);
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

        if (preset.Target != Electron2DExportTarget.AndroidArm64)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0003",
                preset.Name,
                $"Android export preset '{preset.Name}' must target AndroidArm64."));
        }

        if (!string.Equals(preset.RuntimeIdentifier, "android-arm64", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0004",
                preset.Name,
                $"Android export preset '{preset.Name}' must use runtime identifier android-arm64."));
        }

        if (!preset.SelfContained)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0005",
                preset.Name,
                $"Android export preset '{preset.Name}' must be self-contained."));
        }

        if (preset.Configuration == Electron2DExportConfiguration.Release)
        {
            if (!preset.Signing.Required ||
                string.IsNullOrWhiteSpace(preset.Signing.Identity) ||
                string.IsNullOrWhiteSpace(preset.Signing.CredentialReference))
            {
                diagnostics.Add(Error(
                    "E2D-EXPORT-ANDROID-0006",
                    preset.Name,
                    $"Android release export preset '{preset.Name}' requires non-secret signing identity and credential reference."));
            }
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
                "E2D-EXPORT-ANDROID-0007",
                presetName,
                $"Android export preset '{presetName}' requires a project file path."));
        }

        if (projectSettings is null)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0008",
                presetName,
                $"Android export preset '{presetName}' requires project settings."));
            return;
        }

        try
        {
            projectSettings.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-ANDROID-0009", presetName, exception.Message));
        }
    }

    private static string GetGraphicsBackendName(Electron2DRendererProfileSetting rendererProfile)
    {
        return rendererProfile switch
        {
            Electron2DRendererProfileSetting.Standard => "mobile-standard",
            Electron2DRendererProfileSetting.Compatibility => "mobile-compatibility",
            Electron2DRendererProfileSetting.Automatic => "mobile-automatic",
            _ => "mobile-unknown"
        };
    }

    private static string ToAndroidOrientation(DisplayServer.ScreenOrientation orientation)
    {
        return orientation switch
        {
            DisplayServer.ScreenOrientation.Portrait => "portrait",
            DisplayServer.ScreenOrientation.ReverseLandscape => "reverseLandscape",
            DisplayServer.ScreenOrientation.ReversePortrait => "reversePortrait",
            DisplayServer.ScreenOrientation.SensorLandscape => "sensorLandscape",
            DisplayServer.ScreenOrientation.SensorPortrait => "sensorPortrait",
            DisplayServer.ScreenOrientation.Sensor => "fullSensor",
            _ => "landscape"
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
