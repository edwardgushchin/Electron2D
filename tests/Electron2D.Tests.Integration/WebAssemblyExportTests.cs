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
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class WebAssemblyExportTests
{
    [Fact]
    public void WebAssemblyPresetRoundTripsAndValidatesToolchainFailClosed()
    {
        var directory = CreateTemporaryDirectory("electron2d-web-export-presets-");
        var path = Path.Combine(directory, "export_presets.e2export.json");

        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "format": "Electron2D.ExportPresets",
                  "formatVersion": 1,
                  "presets": [
                    {
                      "name": "web-release",
                      "target": "WebAssemblyBrowser",
                      "configuration": "Release",
                      "runtimeIdentifier": "browser-wasm",
                      "selfContained": true,
                      "rendererProfile": "Automatic",
                      "outputDirectory": "exports/web",
                      "includeDebugSymbols": false,
                      "signing": { "required": false, "identity": "", "credentialReference": "" }
                    }
                  ]
                }
                """);

            var load = Electron2D.Electron2DExportPresetStore.Load(path);

            Assert.True(load.Succeeded, FormatDiagnostics(load.Diagnostics));
            Assert.NotNull(load.Document);
            var preset = Assert.Single(load.Document.Presets);
            Assert.Equal("WebAssemblyBrowser", preset.Target.ToString());
            Assert.Equal("browser-wasm", preset.RuntimeIdentifier);

            var environment = new Electron2D.Electron2DExportToolchainEnvironment
            {
                DotnetSdkAvailable = true
            };
            SetWebAssemblyBuildToolsAvailable(environment, available: false);

            var validation = Electron2D.Electron2DExportToolchainValidator.Validate(preset, environment);

            Assert.False(validation.Succeeded);
            var diagnostic = Assert.Single(validation.Diagnostics);
            Assert.Equal("E2D-EXPORT-WEB-0001", diagnostic.Code);
            Assert.Contains("WebAssembly build tools", diagnostic.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("credential", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WebAssemblyExportPlanCreatesBrowserPackageLayoutAndSmokeContract()
    {
        var projectFilePath = Path.Combine("projects", "ReferenceGame", "Electron2D.Empty.csproj");
        var outputDirectory = Path.Combine("exports", "web", "release");
        var preset = CreateWebPreset(outputDirectory);
        var settings = CreateProjectSettings(Electron2D.Electron2DRendererProfileSetting.Automatic);

        var result = InvokeWebPlanner(preset, projectFilePath, settings);

        Assert.True(Read<bool>(result, "Succeeded"), FormatDiagnostics(ReadDiagnostics(result)));
        var plan = Read<object>(result, "Plan");
        Assert.NotNull(plan);
        Assert.Equal(Electron2D.Electron2DExportConfiguration.Release, Read<Electron2D.Electron2DExportConfiguration>(plan, "Configuration"));
        Assert.Equal("browser-wasm", Read<string>(plan, "RuntimeIdentifier"));
        Assert.True(Read<bool>(plan, "SelfContained"));
        Assert.False(Read<bool>(plan, "SigningRequired"));
        Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Automatic, Read<Electron2D.Electron2DRendererProfileSetting>(plan, "RendererProfile"));
        Assert.Equal("web-automatic", Read<string>(plan, "GraphicsBackend"));
        Assert.Equal(outputDirectory, Read<string>(plan, "OutputDirectory"));
        Assert.Equal(Path.Combine(outputDirectory, "wwwroot"), Read<string>(plan, "WebRootDirectory"));
        Assert.Equal(Path.Combine(outputDirectory, "wwwroot", "_framework"), Read<string>(plan, "FrameworkDirectory"));
        Assert.Equal(Path.Combine(outputDirectory, "wwwroot", "assets"), Read<string>(plan, "AssetsDirectory"));
        Assert.Equal(Path.Combine(outputDirectory, "wwwroot", "index.html"), Read<string>(plan, "IndexHtmlPath"));
        Assert.Equal(Path.Combine(outputDirectory, "wwwroot", "electron2d.loader.js"), Read<string>(plan, "LoaderScriptPath"));
        Assert.Equal(Path.Combine(outputDirectory, "wwwroot", "electron2d.webmanifest.json"), Read<string>(plan, "WebManifestPath"));
        Assert.Equal("userGestureRequired", Read<string>(plan, "AudioPolicy"));
        Assert.Equal("browserSandbox", Read<string>(plan, "FilesystemPolicy"));
        Assert.Equal(
            new[]
            {
                "startup",
                "sceneLoad",
                "renderingReadiness",
                "inputEventPath",
                "audioPolicyState",
                "resourceLoading",
                "saveDataPolicy"
            },
            Read<string[]>(plan, "SmokeCriteria"));
        Assert.Contains("project.e2d.json", Read<string[]>(plan, "RequiredFiles"));
        Assert.Contains("scenes/main.scene.json", Read<string[]>(plan, "RequiredFiles"));
        Assert.Equal(
            new[]
            {
                "publish",
                projectFilePath,
                "--configuration",
                "Release",
                "--runtime",
                "browser-wasm",
                "--self-contained",
                "true",
                "--output",
                Path.Combine(outputDirectory, "wwwroot", "_framework")
            },
            Read<string[]>(plan, "PublishArguments"));
    }

    [Fact]
    public void WebAssemblyPackageBuilderCreatesBrowserRuntimeFilesWithoutEditorMetadata()
    {
        var projectRoot = CreateWebProjectRoot("electron2d-web-package-");
        var outputDirectory = Path.Combine(projectRoot, "exports", "web");
        var preset = CreateWebPreset(outputDirectory);
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json")).Settings!;
        var planResult = Electron2D.Electron2DWebAssemblyExportPlanner.CreatePlan(
            preset,
            Path.Combine(projectRoot, "Electron2D.Empty.csproj"),
            settings);

        Assert.True(planResult.Succeeded, FormatDiagnostics(planResult.Diagnostics));
        Assert.NotNull(planResult.Plan);
        var packageResult = Electron2D.Electron2DWebAssemblyPackageBuilder.Build(
            planResult.Plan,
            projectRoot,
            settings);

        Assert.True(packageResult.Succeeded, FormatDiagnostics(packageResult.Diagnostics));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "wwwroot", "index.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "wwwroot", "electron2d.loader.js")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "wwwroot", "electron2d.webmanifest.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "wwwroot", "project.e2d.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "wwwroot", "scenes", "main.scene.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "wwwroot", "assets", "sprite.txt")));
        Assert.False(Directory.Exists(Path.Combine(outputDirectory, "wwwroot", ".electron2d")));

        var index = File.ReadAllText(Path.Combine(outputDirectory, "wwwroot", "index.html"));
        var loader = File.ReadAllText(Path.Combine(outputDirectory, "wwwroot", "electron2d.loader.js"));
        var manifest = File.ReadAllText(Path.Combine(outputDirectory, "wwwroot", "electron2d.webmanifest.json"));

        Assert.Contains("electron2d-canvas", index, StringComparison.Ordinal);
        Assert.Contains("electron2d.loader.js", index, StringComparison.Ordinal);
        Assert.Contains("window.Electron2DWebRuntimeSmoke", loader, StringComparison.Ordinal);
        Assert.Contains("pointerdown", loader, StringComparison.Ordinal);
        Assert.Contains("keydown", loader, StringComparison.Ordinal);
        Assert.Contains("localStorage", loader, StringComparison.Ordinal);
        Assert.Contains("\"mainScene\": \"scenes/main.scene.json\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"audioPolicy\": \"userGestureRequired\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"filesystemPolicy\": \"browserSandbox\"", manifest, StringComparison.Ordinal);
        Assert.Contains("assets/sprite.txt", packageResult.Files);
        Assert.DoesNotContain(packageResult.Files, path => path.Contains(".electron2d", StringComparison.Ordinal));
    }

    [Fact]
    public void WebAssemblySmokeRunnerWritesStructuredBrowserRunArtifact()
    {
        var projectRoot = CreateWebProjectRoot("electron2d-web-smoke-");
        var outputDirectory = Path.Combine(projectRoot, "exports", "web");
        var preset = CreateWebPreset(outputDirectory);
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json")).Settings!;
        var planResult = Electron2D.Electron2DWebAssemblyExportPlanner.CreatePlan(
            preset,
            Path.Combine(projectRoot, "Electron2D.Empty.csproj"),
            settings);
        Assert.True(planResult.Succeeded, FormatDiagnostics(planResult.Diagnostics));
        Assert.NotNull(planResult.Plan);
        var packageResult = Electron2D.Electron2DWebAssemblyPackageBuilder.Build(
            planResult.Plan,
            projectRoot,
            settings);
        Assert.True(packageResult.Succeeded, FormatDiagnostics(packageResult.Diagnostics));

        var artifactPath = Path.Combine(projectRoot, ".electron2d", "export-smoke", "web-smoke.json");
        var smokeResult = Electron2D.Electron2DWebAssemblySmokeRunner.Run(
            planResult.Plan,
            artifactPath,
            new Uri("http://127.0.0.1:8080/index.html"),
            new DateTimeOffset(2026, 6, 23, 0, 0, 0, TimeSpan.Zero));

        Assert.True(smokeResult.Succeeded, FormatDiagnostics(smokeResult.Diagnostics));
        Assert.Equal("passed", smokeResult.Status);
        Assert.True(File.Exists(artifactPath));
        var artifact = File.ReadAllText(artifactPath);
        Assert.Contains("\"format\": \"Electron2D.WebAssemblySmokeArtifact\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"launchUrl\": \"http://127.0.0.1:8080/index.html\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"startup\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"sceneLoad\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"renderingReadiness\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"inputEventPath\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"audioPolicyState\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"resourceLoading\"", artifact, StringComparison.Ordinal);
        Assert.Contains("\"saveDataPolicy\"", artifact, StringComparison.Ordinal);
        Assert.Contains("browserSandbox", artifact, StringComparison.Ordinal);
    }

    [Fact]
    public void WebAssemblyExportPlanFailsClosedForInvalidPresetAndMissingProjectSettings()
    {
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "linux-web",
            Target = Electron2D.Electron2DExportTarget.LinuxX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "linux-x64",
            SelfContained = false,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard,
            OutputDirectory = Path.Combine("exports", "web"),
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = true,
                Identity = "not-used",
                CredentialReference = "env:SECRET_WEB_SIGNING"
            }
        };

        var result = InvokeWebPlanner(preset, "", projectSettings: null);

        Assert.False(Read<bool>(result, "Succeeded"));
        Assert.Null(Read<object?>(result, "Plan"));
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-WEB-0002",
                "E2D-EXPORT-WEB-0003",
                "E2D-EXPORT-WEB-0004",
                "E2D-EXPORT-WEB-0005",
                "E2D-EXPORT-WEB-0006",
                "E2D-EXPORT-WEB-0007"
            },
            ReadDiagnostics(result).Select(diagnostic => diagnostic.Code));
        Assert.All(
            ReadDiagnostics(result),
            diagnostic => Assert.DoesNotContain("SECRET_WEB_SIGNING", diagnostic.Message, StringComparison.Ordinal));
    }

    private static Electron2D.Electron2DExportPreset CreateWebPreset(string outputDirectory)
    {
        Assert.True(
            Enum.TryParse("WebAssemblyBrowser", ignoreCase: false, out Electron2D.Electron2DExportTarget target),
            "WebAssemblyBrowser export target must exist.");

        return new Electron2D.Electron2DExportPreset
        {
            Name = "web-release",
            Target = target,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "browser-wasm",
            SelfContained = true,
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Automatic,
            OutputDirectory = outputDirectory,
            IncludeDebugSymbols = false
        };
    }

    private static object InvokeWebPlanner(
        Electron2D.Electron2DExportPreset preset,
        string projectFilePath,
        Electron2D.Electron2DProjectSettings? projectSettings)
    {
        var planner = typeof(Electron2D.Node).Assembly.GetType("Electron2D.Electron2DWebAssemblyExportPlanner");
        Assert.NotNull(planner);
        var method = planner.GetMethod("CreatePlan", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return method.Invoke(null, [preset, projectFilePath, projectSettings])
            ?? throw new InvalidOperationException("WebAssembly export planner returned null.");
    }

    private static void SetWebAssemblyBuildToolsAvailable(Electron2D.Electron2DExportToolchainEnvironment environment, bool available)
    {
        var property = typeof(Electron2D.Electron2DExportToolchainEnvironment)
            .GetProperty("WebAssemblyBuildToolsAvailable", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        property.SetValue(environment, available);
    }

    private static T Read<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return (T)property.GetValue(source)!;
    }

    private static Electron2D.Electron2DExportDiagnostic[] ReadDiagnostics(object result)
    {
        return Read<Electron2D.Electron2DExportDiagnostic[]>(result, "Diagnostics");
    }

    private static Electron2D.Electron2DProjectSettings CreateProjectSettings(
        Electron2D.Electron2DRendererProfileSetting rendererProfile)
    {
        return new Electron2D.Electron2DProjectSettings
        {
            Name = "ReferenceGame",
            ProjectVersion = "0.1.0",
            EngineVersion = "0.1.0-preview",
            MainScene = "scenes/main.scene.json",
            RendererProfile = rendererProfile,
            Display = new Electron2D.Electron2DDisplaySettings
            {
                WindowSize = new Electron2D.Vector2I(1280, 720),
                Fullscreen = false
            }
        };
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateWebProjectRoot(string prefix)
    {
        var root = CreateTemporaryDirectory(prefix);
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
