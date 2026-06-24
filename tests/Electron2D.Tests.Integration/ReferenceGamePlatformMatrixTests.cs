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
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ReferenceGamePlatformMatrixTests
{
    private static readonly string[] ExpectedTargets =
    [
        "AndroidArm64",
        "IosArm64",
        "LinuxX64",
        "MacOSArm64",
        "WebAssemblyBrowser",
        "WindowsX64"
    ];

    private static readonly string[] ProjectIds =
    [
        "reference-platformer"
    ];

    [Fact]
    public void ReferenceGamePlatformMatrixSpecificationDefinesGateContract()
    {
        var root = FindRepositoryRoot();
        var specPath = Path.Combine(root, "docs", "examples", "reference-game-platform-matrix.md");

        Assert.True(File.Exists(specPath), $"Missing reference game platform matrix specification: {specPath}");

        var spec = File.ReadAllText(specPath);
        Assert.Contains("tools\\Verify-ReferenceGamePlatformMatrix.ps1", spec, StringComparison.Ordinal);
        Assert.Contains("data/quality/reference-game-platform-matrix.json", spec, StringComparison.Ordinal);
        Assert.Contains("tools\\Verify-ReferencePlatformer.ps1", spec, StringComparison.Ordinal);
        Assert.Contains("platform-specific игровой fork", spec, StringComparison.Ordinal);
        Assert.Contains("browser hosting metadata", spec, StringComparison.Ordinal);

        foreach (var target in ExpectedTargets)
        {
            Assert.Contains(target, spec, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReferenceGamePlatformMatrixArtifactCoversProjectsTargetsAndAllowedDifferences()
    {
        var root = FindRepositoryRoot();
        var artifactPath = Path.Combine(root, "data", "quality", "reference-game-platform-matrix.json");

        Assert.True(File.Exists(artifactPath), $"Missing reference game platform matrix artifact: {artifactPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        var artifact = document.RootElement;

        Assert.Equal("Electron2D.ReferenceGamePlatformMatrix", artifact.GetProperty("format").GetString());
        Assert.Equal(1, artifact.GetProperty("version").GetInt32());
        Assert.Equal("0.1.0-preview", artifact.GetProperty("release").GetString());
        Assert.Equal(ExpectedTargets, ReadStringArray(artifact.GetProperty("targetSet")).Order(StringComparer.Ordinal).ToArray());

        var allowedDifferences = ReadStringArray(artifact.GetProperty("allowedDifferences"));
        foreach (var requiredDifference in new[]
        {
            "export preset target/configuration/runtime identifier/output directory",
            "renderer profile",
            "application icon and branding metadata",
            "signing references without secrets",
            "storefront metadata",
            "browser hosting metadata"
        })
        {
            Assert.Contains(requiredDifference, allowedDifferences);
        }

        var projects = artifact.GetProperty("projects").EnumerateArray().ToDictionary(
            project => project.GetProperty("id").GetString()!,
            StringComparer.Ordinal);
        Assert.Equal(ProjectIds.Order(StringComparer.Ordinal), projects.Keys.Order(StringComparer.Ordinal));

        foreach (var projectId in ProjectIds)
        {
            var project = projects[projectId];
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("projectPath").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("projectFile").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("settingsFile").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("exportPresetFile").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("mainScene").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("verifier").GetString()));
            Assert.Equal(ExpectedTargets, ReadStringArray(project.GetProperty("expectedTargets")).Order(StringComparer.Ordinal).ToArray());
            Assert.NotEmpty(ReadStringArray(project.GetProperty("scriptRoots")));
            Assert.NotEmpty(ReadStringArray(project.GetProperty("sceneRoots")));
            Assert.NotEmpty(ReadStringArray(project.GetProperty("resourceRoots")));
            Assert.NotEmpty(ReadStringArray(project.GetProperty("editorMetadataRoots")));
            Assert.NotEmpty(ReadStringArray(project.GetProperty("forbiddenPlatformSpecificRoots")));
            Assert.Contains(project.GetProperty("verifier").GetString()!, ReadStringArray(project.GetProperty("evidence")));
        }
    }

    [Fact]
    public void ReferenceGamesDeclareTheSameExportTargetSetFromTheirProjectPresets()
    {
        var root = FindRepositoryRoot();
        foreach (var projectId in ProjectIds)
        {
            var projectRoot = Path.Combine(root, "examples", projectId);
            var loosePresetPath = Path.Combine(projectRoot, "export_presets.e2export.json");
            var embeddedProjectPath = Path.Combine(projectRoot, "ReferencePlatformer.e2d");
            var exportPresets = File.Exists(loosePresetPath)
                ? Electron2D.ExportPresetStore.Load(loosePresetPath)
                : Electron2D.ExportPresetStore.LoadFromProjectFile(embeddedProjectPath);

            Assert.True(exportPresets.Succeeded, FormatExportDiagnostics(exportPresets.Diagnostics));
            Assert.NotNull(exportPresets.Document);
            Assert.Equal(
                ExpectedTargets,
                exportPresets.Document.Presets
                    .Select(preset => preset.Target.ToString())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(target => target, StringComparer.Ordinal)
                    .ToArray());
        }
    }

    [Fact]
    public void ReferenceGamePlatformMatrixVerifierDeclaresActiveProjectVerifierAndSharedCodeChecks()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-ReferenceGamePlatformMatrix.ps1");

        Assert.True(File.Exists(verifierPath), $"Missing reference game platform matrix verifier: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("project.verifier", verifier, StringComparison.Ordinal);
        Assert.Contains("& $verifierPath", verifier, StringComparison.Ordinal);
        Assert.Contains("reference-game-platform-matrix.json", verifier, StringComparison.Ordinal);
        Assert.Contains("forbiddenPlatformSpecificRoots", verifier, StringComparison.Ordinal);
        Assert.Contains("credentialReference", verifier, StringComparison.Ordinal);
        Assert.Contains(".electron2d/tasks", verifier, StringComparison.Ordinal);

        foreach (var target in ExpectedTargets)
        {
            Assert.Contains(target, verifier, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public async Task ReferenceGamePlatformMatrixVerifierPasses()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-ReferenceGamePlatformMatrix.ps1");

        Assert.True(File.Exists(verifierPath), $"Missing reference game platform matrix verifier: {verifierPath}");

        var startInfo = PowerShellProcess.CreateScriptStartInfo(root, verifierPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start reference game platform matrix verifier.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Reference game platform matrix verifier failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");
        Assert.Contains("Reference game platform matrix verification passed", output, StringComparison.Ordinal);
    }

    private static string[] ReadStringArray(JsonElement element)
    {
        return element.EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static string FormatExportDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
