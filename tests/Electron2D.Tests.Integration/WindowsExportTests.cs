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

public sealed class WindowsExportTests
{
    [Fact]
    public void WindowsReleaseExportPlanUsesWinX64SelfContainedFullscreenStandardProfile()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "windows-release",
            Target = Electron2D.Electron2DExportTarget.WindowsX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "win-x64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "windows", "release"),
            IncludeDebugSymbols = false
        };
        var settings = CreateProjectSettings(
            Electron2D.Electron2DRendererProfileSetting.Standard,
            fullscreen: true,
            new Electron2D.Vector2I(1920, 1080));

        var result = Electron2D.WindowsExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(Electron2D.Electron2DExportConfiguration.Release, result.Plan.Configuration);
        Assert.Equal("win-x64", result.Plan.RuntimeIdentifier);
        Assert.True(result.Plan.SelfContained);
        Assert.False(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Standard, result.Plan.RendererProfile);
        Assert.Equal("standard", result.Plan.GraphicsBackend);
        Assert.Equal(Electron2D.WindowsDisplayMode.Fullscreen, result.Plan.DisplayMode);
        Assert.Equal(new Electron2D.Vector2I(1920, 1080), result.Plan.WindowSize);
        Assert.Equal(Path.Combine("exports", "windows", "release"), result.Plan.OutputDirectory);
        Assert.Equal(Path.Combine("exports", "windows", "release", "Electron2D.Empty.exe"), result.Plan.ExecutablePath);
        Assert.DoesNotContain("project.e2d.json", result.Plan.RequiredFiles);
        Assert.DoesNotContain("scenes/main.scene.json", result.Plan.RequiredFiles);
        Assert.Equal(
            Path.Combine("exports", "windows", "release", "electron2d.pack.json"),
            ReadStringPlanProperty(result.Plan, "ResourceManifestPath"));
        Assert.Contains(
            Path.Combine("exports", "windows", "release", "packs", "project.e2dpkg"),
            ReadStringArrayPlanProperty(result.Plan, "ResourcePackPaths"));
        Assert.Contains(
            Path.Combine("exports", "windows", "release", "packs", "scenes", "main.e2dpkg"),
            ReadStringArrayPlanProperty(result.Plan, "ResourcePackPaths"));
        Assert.Contains("packs/project.e2dpkg::project.e2d.json", ReadStringArrayPlanProperty(result.Plan, "ResourcePackEntries"));
        Assert.Contains("packs/scenes/main.e2dpkg::scenes/main.scene.json", ReadStringArrayPlanProperty(result.Plan, "ResourcePackEntries"));
        Assert.Contains("project.e2d.json", ReadStringArrayPlanProperty(result.Plan, "ForbiddenLooseFiles"));
        Assert.Contains("assets/", ReadStringArrayPlanProperty(result.Plan, "ForbiddenLooseFiles"));
        Assert.Contains(".electron2d/tasks/", ReadStringArrayPlanProperty(result.Plan, "ForbiddenLooseFiles"));
        Assert.Equal(
            new[]
            {
                "publish",
                projectFilePath,
                "--configuration",
                "Release",
                "--runtime",
                "win-x64",
                "--self-contained",
                "true",
                "--output",
                Path.Combine("exports", "windows", "release")
            },
            result.Plan.PublishArguments);
    }

    [Fact]
    public void WindowsDebugExportPlanKeepsDebugSymbolsAndWindowedCompatibilityProfile()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "windows-debug",
            Target = Electron2D.Electron2DExportTarget.WindowsX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Debug,
            RuntimeIdentifier = "win-x64",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Compatibility,
            OutputDirectory = Path.Combine("exports", "windows", "debug"),
            IncludeDebugSymbols = true
        };
        var settings = CreateProjectSettings(
            Electron2D.Electron2DRendererProfileSetting.Compatibility,
            fullscreen: false,
            new Electron2D.Vector2I(1280, 720));

        var result = Electron2D.WindowsExportPlanner.CreatePlan(preset, projectFilePath, settings);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.NotNull(result.Plan);
        Assert.True(result.Plan.IncludeDebugSymbols);
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Compatibility, result.Plan.RendererProfile);
        Assert.Equal("compatibility", result.Plan.GraphicsBackend);
        Assert.Equal(Electron2D.WindowsDisplayMode.Windowed, result.Plan.DisplayMode);
        Assert.Equal(new Electron2D.Vector2I(1280, 720), result.Plan.WindowSize);
        Assert.Contains("Debug", result.Plan.PublishArguments);
    }

    [Fact]
    public void WindowsExportPlanFailsClosedForUnsupportedTargetRuntimeAndDeploymentMode()
    {
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "linux-release",
            Target = Electron2D.Electron2DExportTarget.LinuxX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "linux-x64",
            SelfContained = false,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "linux", "release")
        };
        var settings = CreateProjectSettings(
            Electron2D.Electron2DRendererProfileSetting.Standard,
            fullscreen: false,
            new Electron2D.Vector2I(1280, 720));

        var result = Electron2D.WindowsExportPlanner.CreatePlan(
            preset,
            Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj"),
            settings);

        Assert.False(result.Succeeded);
        Assert.Null(result.Plan);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-WINDOWS-0001",
                "E2D-EXPORT-WINDOWS-0002",
                "E2D-EXPORT-WINDOWS-0003"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    private static Electron2D.Electron2DProjectSettings CreateProjectSettings(
        Electron2D.Electron2DRendererProfileSetting rendererProfile,
        bool fullscreen,
        Electron2D.Vector2I windowSize)
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
                WindowSize = windowSize,
                Fullscreen = fullscreen
            }
        };
    }

    private static string FormatDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static string ReadStringPlanProperty(object plan, string propertyName)
    {
        var property = plan.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<string>(property.GetValue(plan));
    }

    private static string[] ReadStringArrayPlanProperty(object plan, string propertyName)
    {
        var property = plan.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<string[]>(property.GetValue(plan));
    }
}
