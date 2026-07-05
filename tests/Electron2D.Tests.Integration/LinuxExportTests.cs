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

public sealed class LinuxExportTests
{
    [Fact]
    public void LinuxReleaseExportPlanUsesLinuxX64SelfContainedGlibcStandardProfile()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "linux-release",
            Target = Electron2D.Electron2DExportTarget.LinuxX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "linux-x64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "linux", "release"),
            IncludeDebugSymbols = false
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Standard);

        var result = Electron2D.Electron2DLinuxExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(Electron2D.Electron2DExportConfiguration.Release, result.Plan.Configuration);
        Assert.Equal("linux-x64", result.Plan.RuntimeIdentifier);
        Assert.Equal("glibc", result.Plan.LibcFamily);
        Assert.True(result.Plan.SelfContained);
        Assert.False(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Standard, result.Plan.RendererProfile);
        Assert.Equal("standard", result.Plan.GraphicsBackend);
        Assert.Equal(new[] { "wayland", "x11" }, result.Plan.DesktopDisplayProtocols);
        Assert.Equal(
            new[] { "linux-musl-x64", "linux-arm64", "linux-musl-arm64" },
            result.Plan.ExcludedRuntimeIdentifiers);
        Assert.Equal(Path.Combine("exports", "linux", "release"), result.Plan.OutputDirectory);
        Assert.Equal(Path.Combine("exports", "linux", "release", "Electron2D.Empty"), result.Plan.ExecutablePath);
        Assert.Contains("project.e2d.json", result.Plan.RequiredFiles);
        Assert.Contains("scenes/main.scene.json", result.Plan.RequiredFiles);
        Assert.Equal(
            new[]
            {
                "publish",
                projectFilePath,
                "--configuration",
                "Release",
                "--runtime",
                "linux-x64",
                "--self-contained",
                "true",
                "--output",
                Path.Combine("exports", "linux", "release")
            },
            result.Plan.PublishArguments);
    }

    [Fact]
    public void LinuxDebugExportPlanKeepsDebugSymbolsAndCompatibilityProfile()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "linux-debug",
            Target = Electron2D.Electron2DExportTarget.LinuxX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Debug,
            RuntimeIdentifier = "linux-x64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Compatibility,
            OutputDirectory = Path.Combine("exports", "linux", "debug"),
            IncludeDebugSymbols = true
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Compatibility);

        var result = Electron2D.Electron2DLinuxExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.True(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Compatibility, result.Plan.RendererProfile);
        Assert.Equal("compatibility", result.Plan.GraphicsBackend);
        Assert.Contains("Debug", result.Plan.PublishArguments);
    }

    [Fact]
    public void LinuxExportPlanFailsClosedForUnsupportedTargetRuntimeAndDeploymentMode()
    {
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "linux-musl-arm",
            Target = Electron2D.Electron2DExportTarget.WindowsX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "linux-musl-arm64",
            SelfContained = false,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "linux", "release")
        };
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Standard);

        var result = Electron2D.Electron2DLinuxExportPlanner.CreatePlan(
            preset,
            Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj"),
            settings);

        Assert.False(result.Succeeded);
        Assert.Null(result.Plan);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-LINUX-0001",
                "E2D-EXPORT-LINUX-0002",
                "E2D-EXPORT-LINUX-0003",
                "E2D-EXPORT-LINUX-0004"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(result.Diagnostics, diagnostic => Assert.Contains("Linux", diagnostic.Message, StringComparison.Ordinal));
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
