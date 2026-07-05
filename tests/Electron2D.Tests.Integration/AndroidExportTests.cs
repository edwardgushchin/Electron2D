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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class AndroidExportTests
{
    [Fact]
    public void AndroidToolchainValidationRequiresJdkAndReleaseSigningReferences()
    {
        var preset = CreateAndroidPreset(
            Electron2D.Electron2DExportConfiguration.Release,
            Path.Combine("exports", "android", "release"),
            signingRequired: true);
        var environment = new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = true,
            AndroidSdkPath = "G:/Android/Sdk",
            AndroidNdkPath = "G:/Android/Sdk/ndk/23.2.8568313",
            JavaSdkPath = ""
        };

        var result = Electron2D.Electron2DExportToolchainValidator.Validate(preset, environment);

        Assert.False(result.Succeeded);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-ANDROID-0016",
                "E2D-EXPORT-SIGNING-0001",
                "E2D-EXPORT-SIGNING-0002"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(
            result.Diagnostics,
            diagnostic => Assert.DoesNotContain("SECRET", diagnostic.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AndroidDebugApkAndReleaseAabPlansUseArm64MobilePolicies()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Automatic);

        var debugPreset = CreateAndroidPreset(
            Electron2D.Electron2DExportConfiguration.Debug,
            Path.Combine("exports", "android", "debug"),
            signingRequired: false);
        var debugResult = Electron2D.Electron2DAndroidExportPlanner.CreatePlan(debugPreset, projectFilePath, settings);

        Assert.True(debugResult.Succeeded, FormatDiagnostics(debugResult.Diagnostics));
        Assert.NotNull(debugResult.Plan);
        Assert.Equal("android-arm64", debugResult.Plan.RuntimeIdentifier);
        Assert.Equal("arm64-v8a", debugResult.Plan.Abi);
        Assert.Equal("apk", debugResult.Plan.PackageFormat);
        Assert.Equal("build", debugResult.Plan.DotnetCommand);
        Assert.False(debugResult.Plan.SigningRequired);
        Assert.Equal("mobile-automatic", debugResult.Plan.GraphicsBackend);
        Assert.Equal("android-mobile", debugResult.Plan.MobileGraphicsProfile);
        Assert.Equal("compatibility-renderer-fallback", debugResult.Plan.FallbackPolicy);
        Assert.Contains("touchInput", debugResult.Plan.MobilePolicies);
        Assert.Contains("lifecyclePauseResume", debugResult.Plan.MobilePolicies);
        Assert.Contains("safeArea", debugResult.Plan.MobilePolicies);
        Assert.Contains("render", debugResult.Plan.SmokeCriteria);
        Assert.Contains("filesystem", debugResult.Plan.SmokeCriteria);
        Assert.Contains("project.e2d.json", debugResult.Plan.RequiredFiles);
        Assert.Contains("scenes/main.scene.json", debugResult.Plan.RequiredFiles);
        Assert.Contains("-p:AndroidPackageFormat=apk", debugResult.Plan.BuildArguments);
        Assert.Contains("-p:RuntimeIdentifiers=android-arm64", debugResult.Plan.BuildArguments);

        var releasePreset = CreateAndroidPreset(
            Electron2D.Electron2DExportConfiguration.Release,
            Path.Combine("exports", "android", "release"),
            signingRequired: true);
        releasePreset.Signing = new Electron2D.Electron2DExportSigningSettings
        {
            Required = true,
            Identity = "release-key",
            CredentialReference = "env:E2D_ANDROID_KEYSTORE"
        };

        var releaseResult = Electron2D.Electron2DAndroidExportPlanner.CreatePlan(releasePreset, projectFilePath, settings);

        Assert.True(releaseResult.Succeeded, FormatDiagnostics(releaseResult.Diagnostics));
        Assert.NotNull(releaseResult.Plan);
        Assert.Equal("aab", releaseResult.Plan.PackageFormat);
        Assert.Equal("publish", releaseResult.Plan.DotnetCommand);
        Assert.True(releaseResult.Plan.SigningRequired);
        Assert.Equal("release-key", releaseResult.Plan.SigningIdentity);
        Assert.Equal("env:E2D_ANDROID_KEYSTORE", releaseResult.Plan.SigningCredentialReference);
        Assert.Contains("-p:AndroidPackageFormat=aab", releaseResult.Plan.BuildArguments);
        Assert.DoesNotContain(releaseResult.Plan.BuildArguments, argument => argument.Contains("SECRET", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AndroidPackageBuilderCreatesStagingProjectWithoutEditorMetadata()
    {
        var projectRoot = CreateAndroidProjectRoot("electron2d-android-package-");
        var outputDirectory = Path.Combine(projectRoot, "exports", "android", "debug");
        var preset = CreateAndroidPreset(Electron2D.Electron2DExportConfiguration.Debug, outputDirectory, signingRequired: false);
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json")).Settings!;
        var planResult = Electron2D.Electron2DAndroidExportPlanner.CreatePlan(
            preset,
            Path.Combine(projectRoot, "Electron2D.Empty.csproj"),
            settings);

        Assert.True(planResult.Succeeded, FormatDiagnostics(planResult.Diagnostics));
        Assert.NotNull(planResult.Plan);

        var packageResult = Electron2D.Electron2DAndroidPackageBuilder.Build(
            planResult.Plan,
            projectRoot,
            settings);

        Assert.True(packageResult.Succeeded, FormatDiagnostics(packageResult.Diagnostics));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Electron2D.Android.csproj")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "MainActivity.cs")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "AndroidManifest.xml")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Resources", "values", "electron2d_export.xml")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Resources", "drawable", "electron2d_icon.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Assets", "electron2d", "project.e2d.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Assets", "electron2d", "scenes", "main.scene.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Assets", "electron2d", "branding", "electron2d_logo_dark.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "android", "Assets", "electron2d", "assets", "sprite.txt")));
        Assert.False(Directory.Exists(Path.Combine(outputDirectory, "android", "Assets", "electron2d", ".electron2d")));

        var manifest = File.ReadAllText(Path.Combine(outputDirectory, "android", "AndroidManifest.xml"));
        var projectFile = File.ReadAllText(Path.Combine(outputDirectory, "android", "Electron2D.Android.csproj"));
        var activity = File.ReadAllText(Path.Combine(outputDirectory, "android", "MainActivity.cs"));
        var metadata = File.ReadAllText(Path.Combine(outputDirectory, "android", "Resources", "values", "electron2d_export.xml"));

        Assert.Contains("<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<AndroidFastDeploymentType>None</AndroidFastDeploymentType>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<AndroidTargetSdkVersion>34</AndroidTargetSdkVersion>", projectFile, StringComparison.Ordinal);
        Assert.Contains("android:targetSdkVersion=\"34\"", manifest, StringComparison.Ordinal);
        Assert.Contains("android:icon=\"@drawable/electron2d_icon\"", manifest, StringComparison.Ordinal);
        Assert.Contains($"android:label=\"{settings.Name}\"", manifest, StringComparison.Ordinal);
        Assert.Contains("android:resizeableActivity=\"true\"", manifest, StringComparison.Ordinal);
        Assert.Contains("android:maxAspectRatio=\"3.0\"", manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("android.intent.category.LAUNCHER", manifest, StringComparison.Ordinal);
        Assert.Contains("RequestWindowFeature(WindowFeatures.NoTitle)", activity, StringComparison.Ordinal);
        Assert.Contains("WindowManagerFlags.Fullscreen", activity, StringComparison.Ordinal);
        Assert.Contains("WindowManagerFlags.KeepScreenOn", activity, StringComparison.Ordinal);
        Assert.Contains("WindowManagerFlags.TurnScreenOn", activity, StringComparison.Ordinal);
        Assert.Contains("WindowManagerFlags.ShowWhenLocked", activity, StringComparison.Ordinal);
        Assert.Contains("WindowManagerFlags.DismissKeyguard", activity, StringComparison.Ordinal);
        Assert.Contains("SetShowWhenLocked(true)", activity, StringComparison.Ordinal);
        Assert.Contains("SetTurnScreenOn(true)", activity, StringComparison.Ordinal);
        Assert.Contains("RequestDismissKeyguard", activity, StringComparison.Ordinal);
        Assert.Contains("SetStatusBarColor(Color.Black)", activity, StringComparison.Ordinal);
        Assert.Contains("SetNavigationBarColor(Color.Black)", activity, StringComparison.Ordinal);
        Assert.Contains("SetBackgroundColor(Color.Black)", activity, StringComparison.Ordinal);
        Assert.Contains("SetDecorFitsSystemWindows(false)", activity, StringComparison.Ordinal);
        Assert.Contains("LayoutInDisplayCutoutMode.ShortEdges", activity, StringComparison.Ordinal);
        Assert.Contains("OnWindowFocusChanged", activity, StringComparison.Ordinal);
        Assert.Contains("DispatchTouchEvent", activity, StringComparison.Ordinal);
        Assert.Contains("electron2d/branding/electron2d_logo_dark.png", activity, StringComparison.Ordinal);
        Assert.DoesNotContain("electron2d_logo_light.png", activity, StringComparison.Ordinal);
        Assert.Contains("OnPause", activity, StringComparison.Ordinal);
        Assert.Contains("OnResume", activity, StringComparison.Ordinal);
        Assert.Contains("OnTouchEvent", activity, StringComparison.Ordinal);
        Assert.Contains("ScreenOrientation.Landscape", activity, StringComparison.Ordinal);
        Assert.Contains("E2D_SMOKE_SAFE_AREA_READY", activity, StringComparison.Ordinal);
        Assert.Contains("E2D_SMOKE_LOGO_BLACK_READY", activity, StringComparison.Ordinal);
        Assert.Contains("compatibility-renderer-fallback", metadata, StringComparison.Ordinal);
        Assert.Contains("Assets/electron2d/branding/electron2d_logo_dark.png", packageResult.Files);
        Assert.Contains("Resources/drawable/electron2d_icon.png", packageResult.Files);
        Assert.Contains("Assets/electron2d/assets/sprite.txt", packageResult.Files);
        Assert.DoesNotContain(packageResult.Files, path => path.Contains(".electron2d", StringComparison.Ordinal));
    }

    [Fact]
    public void AndroidDeviceSmokeRunnerWritesBlockedArtifactWhenDeviceIsMissing()
    {
        var projectRoot = CreateAndroidProjectRoot("electron2d-android-smoke-");
        var outputDirectory = Path.Combine(projectRoot, "exports", "android", "debug");
        var preset = CreateAndroidPreset(Electron2D.Electron2DExportConfiguration.Debug, outputDirectory, signingRequired: false);
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json")).Settings!;
        var planResult = Electron2D.Electron2DAndroidExportPlanner.CreatePlan(
            preset,
            Path.Combine(projectRoot, "Electron2D.Empty.csproj"),
            settings);
        Assert.True(planResult.Succeeded, FormatDiagnostics(planResult.Diagnostics));
        Assert.NotNull(planResult.Plan);

        var artifactPath = Path.Combine(projectRoot, ".electron2d", "export-smoke", "android-smoke.json");
        var smokeResult = Electron2D.Electron2DAndroidDeviceSmokeRunner.Run(
            planResult.Plan,
            artifactPath,
            Electron2D.Electron2DAndroidDeviceSmokeObservation.Blocked("no connected Android device or emulator"),
            new DateTimeOffset(2026, 6, 23, 0, 0, 0, TimeSpan.Zero));

        Assert.False(smokeResult.Succeeded);
        Assert.Equal("blocked", smokeResult.Status);
        Assert.True(File.Exists(artifactPath));
        Assert.Contains("E2D-EXPORT-ANDROID-0014", smokeResult.Diagnostics.Select(diagnostic => diagnostic.Code));
        var artifact = File.ReadAllText(artifactPath);
        Assert.Contains("\"format\": \"Electron2D.AndroidDeviceSmokeArtifact\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"status\": \"blocked\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"pauseResume\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"render\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"input\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"audio\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"resources\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"filesystem\"", artifact, StringComparison.Ordinal);
    }

    private static Electron2D.Electron2DExportPreset CreateAndroidPreset(
        Electron2D.Electron2DExportConfiguration configuration,
        string outputDirectory,
        bool signingRequired)
    {
        return new Electron2D.Electron2DExportPreset
        {
            Name = configuration == Electron2D.Electron2DExportConfiguration.Release
                ? "android-release"
                : "android-debug",
            Target = Electron2D.Electron2DExportTarget.AndroidArm64,
            Configuration = configuration,
            RuntimeIdentifier = "android-arm64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Automatic,
            OutputDirectory = outputDirectory,
            IncludeDebugSymbols = configuration == Electron2D.Electron2DExportConfiguration.Debug,
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = signingRequired
            }
        };
    }

    private static Electron2D.Electron2DProjectSettings CreateProjectSettings(
        Electron2D.Electron2DRendererProfileSetting rendererProfile)
    {
        return new Electron2D.Electron2DProjectSettings
        {
            Name = "ReferenceGame",
            ProjectVersion = "0.1.0",
            EngineVersion = "0.1-preview",
            MainScene = "scenes/main.scene.json",
            RendererProfile = rendererProfile,
            Display = new Electron2D.Electron2DDisplaySettings
            {
                WindowSize = new Electron2D.Vector2I(1280, 720),
                Fullscreen = true,
                Orientation = Electron2D.DisplayServer.ScreenOrientation.Landscape
            }
        };
    }

    private static string CreateAndroidProjectRoot(string prefix)
    {
        var root = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        Directory.CreateDirectory(Path.Combine(root, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), "{\"format\":\"Electron2D.SceneFile\"}");
        File.WriteAllText(Path.Combine(root, "assets", "sprite.txt"), "sprite");
        File.WriteAllText(Path.Combine(root, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");
        File.WriteAllText(
            Path.Combine(root, "Electron2D.Empty.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        Electron2D.Electron2DSettingsStore.SaveProject(
            Path.Combine(root, "project.e2d.json"),
            CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Automatic));
        return root;
    }

    private static string FormatDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
