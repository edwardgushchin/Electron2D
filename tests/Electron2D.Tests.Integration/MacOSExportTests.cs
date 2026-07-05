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

public sealed class MacOSExportTests
{
    [Fact]
    public void MacOSReleaseExportPlanCreatesArm64AppBundleWithSigningPlan()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "macos-release",
            Target = Electron2D.Electron2DExportTarget.MacOSArm64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "osx-arm64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "macos", "release"),
            IncludeDebugSymbols = false,
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = true,
                Identity = "Developer ID Application: Example Studio (TEAMID1234)",
                CredentialReference = "env:E2D_MACOS_SIGNING"
            }
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Standard);

        var result = Electron2D.Electron2DMacOSExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(Electron2D.Electron2DExportConfiguration.Release, result.Plan.Configuration);
        Assert.Equal("osx-arm64", result.Plan.RuntimeIdentifier);
        Assert.Equal("arm64", result.Plan.Architecture);
        Assert.True(result.Plan.SelfContained);
        Assert.False(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Standard, result.Plan.RendererProfile);
        Assert.Equal("metal", result.Plan.GraphicsBackend);
        Assert.Equal("unsupported-in-0.1-preview", result.Plan.X64Policy);
        Assert.Equal(Path.Combine("exports", "macos", "release"), result.Plan.OutputDirectory);
        Assert.Equal(Path.Combine("exports", "macos", "release", "publish"), result.Plan.PublishOutputDirectory);
        Assert.Equal(Path.Combine("exports", "macos", "release", "ReferenceGame.app"), result.Plan.AppBundlePath);
        Assert.Equal(Path.Combine("exports", "macos", "release", "ReferenceGame.app", "Contents"), result.Plan.ContentsDirectory);
        Assert.Equal(Path.Combine("exports", "macos", "release", "ReferenceGame.app", "Contents", "MacOS"), result.Plan.MacOSDirectory);
        Assert.Equal(Path.Combine("exports", "macos", "release", "ReferenceGame.app", "Contents", "Resources"), result.Plan.ResourcesDirectory);
        Assert.Equal(Path.Combine("exports", "macos", "release", "ReferenceGame.app", "Contents", "MacOS", "Electron2D.Empty"), result.Plan.ExecutablePath);
        Assert.Equal(Path.Combine("exports", "macos", "release", "ReferenceGame.app", "Contents", "Info.plist"), result.Plan.InfoPlistPath);
        Assert.Equal("ReferenceGame", result.Plan.BundleName);
        Assert.Equal("Electron2D.Empty", result.Plan.ExecutableName);
        Assert.Equal("dev.electron2d.referencegame", result.Plan.BundleIdentifier);
        Assert.Contains("Contents/Info.plist", result.Plan.RequiredBundleFiles);
        Assert.Contains("Contents/MacOS/Electron2D.Empty", result.Plan.RequiredBundleFiles);
        Assert.Contains("Contents/MacOS/project.e2d.json", result.Plan.RequiredBundleFiles);
        Assert.Contains("Contents/MacOS/scenes/main.scene.json", result.Plan.RequiredBundleFiles);
        Assert.Equal(
            new[] { "osx-x64", "osx.10.15-x64" },
            result.Plan.UnsupportedRuntimeIdentifiers);
        Assert.True(result.Plan.SigningRequired);
        Assert.Equal("Developer ID Application: Example Studio (TEAMID1234)", result.Plan.SigningIdentity);
        Assert.Equal("env:E2D_MACOS_SIGNING", result.Plan.SigningCredentialReference);
        Assert.Equal(
            new[]
            {
                "--force",
                "--options",
                "runtime",
                "--sign",
                "Developer ID Application: Example Studio (TEAMID1234)",
                Path.Combine("exports", "macos", "release", "ReferenceGame.app")
            },
            result.Plan.CodesignArguments);
        Assert.Equal(
            new[]
            {
                "publish",
                projectFilePath,
                "--configuration",
                "Release",
                "--runtime",
                "osx-arm64",
                "--self-contained",
                "true",
                "--output",
                Path.Combine("exports", "macos", "release", "publish")
            },
            result.Plan.PublishArguments);
    }

    [Fact]
    public void MacOSDebugExportPlanKeepsDebugSymbolsAndDoesNotRequireSigningByDefault()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "macos-debug",
            Target = Electron2D.Electron2DExportTarget.MacOSArm64,
            Configuration = Electron2D.Electron2DExportConfiguration.Debug,
            RuntimeIdentifier = "osx-arm64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Compatibility,
            OutputDirectory = Path.Combine("exports", "macos", "debug"),
            IncludeDebugSymbols = true
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Compatibility);

        var result = Electron2D.Electron2DMacOSExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.True(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Compatibility, result.Plan.RendererProfile);
        Assert.Equal("metal", result.Plan.GraphicsBackend);
        Assert.False(result.Plan.SigningRequired);
        Assert.Equal("", result.Plan.SigningIdentity);
        Assert.Equal("", result.Plan.SigningCredentialReference);
        Assert.Empty(result.Plan.CodesignArguments);
        Assert.Contains("Debug", result.Plan.PublishArguments);
    }

    [Fact]
    public void MacOSExportPlanFailsClosedForUnsupportedTargetRuntimeDeploymentModeAndSigning()
    {
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "macos-x64",
            Target = Electron2D.Electron2DExportTarget.LinuxX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "osx-x64",
            SelfContained = false,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "macos", "release"),
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = true,
                Identity = "",
                CredentialReference = "env:E2D_MACOS_SIGNING"
            }
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Standard);

        var result = Electron2D.Electron2DMacOSExportPlanner.CreatePlan(
            preset,
            Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj"),
            settings);

        Assert.False(result.Succeeded);
        Assert.Null(result.Plan);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-MACOS-0001",
                "E2D-EXPORT-MACOS-0002",
                "E2D-EXPORT-MACOS-0003",
                "E2D-EXPORT-MACOS-0004",
                "E2D-EXPORT-MACOS-0008"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(result.Diagnostics, diagnostic => Assert.Contains("macOS", diagnostic.Message, StringComparison.Ordinal));
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
                Fullscreen = false
            }
        };
    }

    private static string FormatDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
