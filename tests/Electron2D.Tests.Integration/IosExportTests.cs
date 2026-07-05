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

public sealed class IosExportTests
{
    [Fact]
    public void IosReleaseExportPlanCreatesArm64XcodeProjectWithSigningPlan()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = CreateIosPreset(
            Electron2D.Electron2DExportConfiguration.Release,
            Path.Combine("exports", "ios", "release"),
            signingRequired: true);
        preset.RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard;
        preset.Signing = new Electron2D.Electron2DExportSigningSettings
        {
            Required = true,
            Identity = "Apple Development: Example Studio (TEAMID1234)",
            CredentialReference = "env:E2D_IOS_SIGNING"
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Standard);

        var result = Electron2D.Electron2DIosExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(Electron2D.Electron2DExportConfiguration.Release, result.Plan.Configuration);
        Assert.Equal("ios-arm64", result.Plan.RuntimeIdentifier);
        Assert.Equal("arm64", result.Plan.Architecture);
        Assert.Equal("net10.0-ios", result.Plan.TargetFramework);
        Assert.True(result.Plan.SelfContained);
        Assert.False(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Standard, result.Plan.RendererProfile);
        Assert.Equal("metal", result.Plan.GraphicsBackend);
        Assert.Equal(Path.Combine("exports", "ios", "release"), result.Plan.OutputDirectory);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios"), result.Plan.StagingDirectory);
        Assert.Equal(Path.Combine("exports", "ios", "release", "artifacts", "release"), result.Plan.ArtifactsDirectory);
        Assert.Equal(Path.Combine("exports", "ios", "release", "smoke"), result.Plan.SmokeDirectory);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "Electron2D.iOS.csproj"), result.Plan.IosProjectFilePath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "AppDelegate.cs"), result.Plan.AppDelegatePath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "Info.plist"), result.Plan.InfoPlistPath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "Entitlements.plist"), result.Plan.EntitlementsPath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "ExportMetadata.json"), result.Plan.ExportMetadataPath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "Electron2D.iOS.xcodeproj"), result.Plan.XcodeProjectDirectory);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "Electron2D.iOS.xcodeproj", "project.pbxproj"), result.Plan.XcodeProjectFilePath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "artifacts", "release", "ReferenceGame.app"), result.Plan.AppBundlePath);
        Assert.Equal(Path.Combine("exports", "ios", "release", "ios", "Assets", "electron2d"), result.Plan.ProjectAssetsDirectory);
        Assert.Equal("ReferenceGame", result.Plan.AppName);
        Assert.Equal("Electron2D.Empty", result.Plan.ExecutableName);
        Assert.Equal("dev.electron2d.referencegame", result.Plan.BundleIdentifier);
        Assert.True(result.Plan.SigningRequired);
        Assert.Equal("Apple Development: Example Studio (TEAMID1234)", result.Plan.SigningIdentity);
        Assert.Equal("env:E2D_IOS_SIGNING", result.Plan.SigningCredentialReference);
        Assert.Contains("touchInput", result.Plan.MobilePolicies);
        Assert.Contains("lifecycleForegroundBackground", result.Plan.MobilePolicies);
        Assert.Contains("safeArea", result.Plan.MobilePolicies);
        Assert.Contains("precompiledRenderingArtifacts", result.Plan.MobilePolicies);
        Assert.Contains("render", result.Plan.SmokeCriteria);
        Assert.Contains("precompiledArtifacts", result.Plan.SmokeCriteria);
        Assert.Contains("Assets/electron2d/project.e2d.json", result.Plan.RequiredFiles);
        Assert.Contains("Assets/electron2d/scenes/main.scene.json", result.Plan.RequiredFiles);
        Assert.Contains("--framework", result.Plan.PublishArguments);
        Assert.Contains("net10.0-ios", result.Plan.PublishArguments);
        Assert.Contains("--runtime", result.Plan.PublishArguments);
        Assert.Contains("ios-arm64", result.Plan.PublishArguments);
        Assert.Contains("-project", result.Plan.XcodeBuildArguments);
        Assert.Contains(result.Plan.XcodeProjectDirectory, result.Plan.XcodeBuildArguments);
        Assert.Contains("-destination", result.Plan.XcodeBuildArguments);
    }

    [Fact]
    public void IosExportPlanFailsClosedForUnsupportedTargetRuntimeDeploymentModeAndSigning()
    {
        var preset = CreateIosPreset(
            Electron2D.Electron2DExportConfiguration.Release,
            Path.Combine("exports", "ios", "release"),
            signingRequired: true);
        preset.Target = Electron2D.Electron2DExportTarget.MacOSArm64;
        preset.RuntimeIdentifier = "iossimulator-arm64";
        preset.SelfContained = false;
        preset.Signing = new Electron2D.Electron2DExportSigningSettings
        {
            Required = true,
            Identity = "",
            CredentialReference = "env:E2D_IOS_SIGNING"
        };

        var result = Electron2D.Electron2DIosExportPlanner.CreatePlan(
            preset,
            "",
            null);

        Assert.False(result.Succeeded);
        Assert.Null(result.Plan);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-IOS-0001",
                "E2D-EXPORT-IOS-0002",
                "E2D-EXPORT-IOS-0003",
                "E2D-EXPORT-IOS-0007",
                "E2D-EXPORT-IOS-0004",
                "E2D-EXPORT-IOS-0005"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(result.Diagnostics, diagnostic => Assert.Contains("iOS", diagnostic.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void IosToolchainValidationUsesDedicatedXcodeDiagnosticCode()
    {
        var preset = CreateIosPreset(
            Electron2D.Electron2DExportConfiguration.Release,
            Path.Combine("exports", "ios", "release"),
            signingRequired: true);
        preset.Signing = new Electron2D.Electron2DExportSigningSettings
        {
            Required = true,
            Identity = "Apple Development: Example Studio (TEAMID1234)",
            CredentialReference = "env:E2D_IOS_SIGNING"
        };
        var environment = new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = true,
            XcodePath = "",
            SigningIdentityAvailable = false,
            SigningCredentialReferenceAvailable = false
        };

        var result = Electron2D.Electron2DExportToolchainValidator.Validate(preset, environment);

        Assert.False(result.Succeeded);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-IOS-0013",
                "E2D-EXPORT-SIGNING-0001",
                "E2D-EXPORT-SIGNING-0002"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "E2D-EXPORT-IOS-0001");
        Assert.All(result.Diagnostics, diagnostic => Assert.DoesNotContain("E2D_IOS_SIGNING", diagnostic.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void IosXcodeProjectBuilderCreatesStagingProjectWithoutEditorMetadata()
    {
        var projectRoot = CreateIosProjectRoot("electron2d-ios-package-");
        var outputDirectory = Path.Combine(projectRoot, "exports", "ios", "debug");
        var preset = CreateIosPreset(Electron2D.Electron2DExportConfiguration.Debug, outputDirectory, signingRequired: false);
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json")).Settings!;
        var planResult = Electron2D.Electron2DIosExportPlanner.CreatePlan(
            preset,
            Path.Combine(projectRoot, "Electron2D.Empty.csproj"),
            settings);

        Assert.True(planResult.Succeeded, FormatDiagnostics(planResult.Diagnostics));
        Assert.NotNull(planResult.Plan);

        var packageResult = Electron2D.Electron2DIosXcodeProjectBuilder.Build(
            planResult.Plan,
            projectRoot,
            settings);

        Assert.True(packageResult.Succeeded, FormatDiagnostics(packageResult.Diagnostics));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Electron2D.iOS.csproj")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "AppDelegate.cs")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Info.plist")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Entitlements.plist")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "ExportMetadata.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Electron2D.iOS.xcodeproj", "project.pbxproj")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Assets", "electron2d", "project.e2d.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Assets", "electron2d", "scenes", "main.scene.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ios", "Assets", "electron2d", "assets", "sprite.txt")));
        Assert.False(Directory.Exists(Path.Combine(outputDirectory, "ios", "Assets", "electron2d", ".electron2d")));

        var projectFile = File.ReadAllText(Path.Combine(outputDirectory, "ios", "Electron2D.iOS.csproj"));
        var appDelegate = File.ReadAllText(Path.Combine(outputDirectory, "ios", "AppDelegate.cs"));
        var infoPlist = File.ReadAllText(Path.Combine(outputDirectory, "ios", "Info.plist"));
        var metadata = File.ReadAllText(Path.Combine(outputDirectory, "ios", "ExportMetadata.json"));
        var xcodeProject = File.ReadAllText(Path.Combine(outputDirectory, "ios", "Electron2D.iOS.xcodeproj", "project.pbxproj"));

        Assert.Contains("<TargetFramework>net10.0-ios</TargetFramework>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<RuntimeIdentifier>ios-arm64</RuntimeIdentifier>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<MtouchLink>SdkOnly</MtouchLink>", projectFile, StringComparison.Ordinal);
        Assert.Contains("E2D_SMOKE_SAFE_AREA_READY", appDelegate, StringComparison.Ordinal);
        Assert.Contains("E2D_SMOKE_TOUCH_READY", appDelegate, StringComparison.Ordinal);
        Assert.Contains("E2D_SMOKE_PRECOMPILED_RENDERING_READY", appDelegate, StringComparison.Ordinal);
        Assert.Contains("DidEnterBackground", appDelegate, StringComparison.Ordinal);
        Assert.Contains("WillEnterForeground", appDelegate, StringComparison.Ordinal);
        Assert.Contains("<key>CFBundleIdentifier</key>", infoPlist, StringComparison.Ordinal);
        Assert.Contains("<string>dev.electron2d.referencegame</string>", infoPlist, StringComparison.Ordinal);
        Assert.Contains("<key>UIRequiresFullScreen</key>", infoPlist, StringComparison.Ordinal);
        Assert.Contains("metal", metadata, StringComparison.Ordinal);
        Assert.Contains("ios-arm64", metadata, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Electron2D.iOS", xcodeProject, StringComparison.Ordinal);
        Assert.Contains("Assets/electron2d/assets/sprite.txt", packageResult.Files);
        Assert.DoesNotContain(packageResult.Files, path => path.Contains(".electron2d", StringComparison.Ordinal));
    }

    [Fact]
    public void IosDeviceSmokeRunnerWritesBlockedArtifactWhenSimulatorOrDeviceIsMissing()
    {
        var projectRoot = CreateIosProjectRoot("electron2d-ios-smoke-");
        var outputDirectory = Path.Combine(projectRoot, "exports", "ios", "debug");
        var preset = CreateIosPreset(Electron2D.Electron2DExportConfiguration.Debug, outputDirectory, signingRequired: false);
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json")).Settings!;
        var planResult = Electron2D.Electron2DIosExportPlanner.CreatePlan(
            preset,
            Path.Combine(projectRoot, "Electron2D.Empty.csproj"),
            settings);
        Assert.True(planResult.Succeeded, FormatDiagnostics(planResult.Diagnostics));
        Assert.NotNull(planResult.Plan);

        var artifactPath = Path.Combine(projectRoot, ".electron2d", "export-smoke", "ios-smoke.json");
        var smokeResult = Electron2D.Electron2DIosDeviceSmokeRunner.Run(
            planResult.Plan,
            artifactPath,
            Electron2D.Electron2DIosDeviceSmokeObservation.Blocked("no iOS simulator or device available"),
            new DateTimeOffset(2026, 6, 23, 0, 0, 0, TimeSpan.Zero));

        Assert.False(smokeResult.Succeeded);
        Assert.Equal("blocked", smokeResult.Status);
        Assert.True(File.Exists(artifactPath));
        Assert.Contains("E2D-EXPORT-IOS-0011", smokeResult.Diagnostics.Select(diagnostic => diagnostic.Code));
        var artifact = File.ReadAllText(artifactPath);
        Assert.Contains("\"format\": \"Electron2D.IosDeviceSmokeArtifact\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"status\": \"blocked\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"safeArea\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"render\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"input\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"audio\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"resources\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"filesystem\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"precompiledArtifacts\"", artifact, StringComparison.Ordinal);
    }

    private static Electron2D.Electron2DExportPreset CreateIosPreset(
        Electron2D.Electron2DExportConfiguration configuration,
        string outputDirectory,
        bool signingRequired)
    {
        return new Electron2D.Electron2DExportPreset
        {
            Name = configuration == Electron2D.Electron2DExportConfiguration.Release
                ? "ios-release"
                : "ios-debug",
            Target = Electron2D.Electron2DExportTarget.IosArm64,
            Configuration = configuration,
            RuntimeIdentifier = "ios-arm64",
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

    private static string CreateIosProjectRoot(string prefix)
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
