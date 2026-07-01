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
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ReferenceGamePlatformMatrixTests
{
    private static readonly string[] ExpectedRuntimeTargets =
    [
        "AndroidArm64",
        "IosArm64",
        "LinuxX64",
        "MacOSArm64",
        "WebAssemblyBrowser",
        "WindowsX64"
    ];

    private static readonly string[] ExpectedEditorTargets =
    [
        "Linux",
        "Windows",
        "macOS"
    ];

    private static readonly string[] ProjectIds =
    [
        "platformer"
    ];

    [Fact]
    public void ReferenceGamePlatformMatrixSpecificationDefinesGateContract()
    {
        var root = FindRepositoryRoot();
        var specPath = Path.Combine(root, "docs", "examples", "reference-game-platform-matrix.md");

        Assert.True(File.Exists(specPath), $"Missing reference game platform matrix specification: {specPath}");

        var spec = File.ReadAllText(specPath);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify reference-game-platform-matrix", spec, StringComparison.Ordinal);
        Assert.Contains("data/quality/reference-game-platform-matrix.json", spec, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify platformer", spec, StringComparison.Ordinal);
        Assert.Contains("platform-specific игровой fork", spec, StringComparison.Ordinal);
        Assert.Contains("browser hosting metadata", spec, StringComparison.Ordinal);
        Assert.Contains("runtimeTargets", spec, StringComparison.Ordinal);
        Assert.Contains("editorTargets", spec, StringComparison.Ordinal);
        Assert.Contains("releaseVerificationTargets", spec, StringComparison.Ordinal);
        Assert.Contains("blocked-environment artifact", spec, StringComparison.Ordinal);
        Assert.Contains("кандидатом на приёмочный проект", spec, StringComparison.Ordinal);
        Assert.Contains("T-0222", spec, StringComparison.Ordinal);
        Assert.Contains("T-0223", spec, StringComparison.Ordinal);
        Assert.Contains("T-0225", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("единственным активным", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("активным полноценным приёмочным проектом", spec, StringComparison.Ordinal);

        foreach (var target in ExpectedRuntimeTargets)
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
        Assert.Equal(2, artifact.GetProperty("version").GetInt32());
        Assert.Equal("0.1.0-preview", artifact.GetProperty("release").GetString());
        Assert.False(artifact.TryGetProperty("targetSet", out _), "Use runtimeTargets and releaseVerificationTargets instead of a generic targetSet.");
        Assert.Equal(ExpectedRuntimeTargets, ReadStringArray(artifact.GetProperty("runtimeTargets")).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(ExpectedEditorTargets, ReadStringArray(artifact.GetProperty("editorTargets")).Order(StringComparer.Ordinal).ToArray());
        AssertReleaseVerificationTargets(artifact.GetProperty("releaseVerificationTargets"));

        var releaseDecision = artifact.GetProperty("releaseVerificationDecision");
        Assert.Equal("all-runtime-targets-for-0.1.0-preview", releaseDecision.GetProperty("id").GetString());
        Assert.Equal("docs/releases/0.1.0-preview.md", releaseDecision.GetProperty("source").GetString());
        Assert.Contains("all six runtime targets", releaseDecision.GetProperty("summary").GetString(), StringComparison.Ordinal);

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
            Assert.False(project.TryGetProperty("expectedTargets", out _), "Use expectedRuntimeTargets so project expectations do not look like a release verification tier.");
            Assert.Equal(ExpectedRuntimeTargets, ReadStringArray(project.GetProperty("expectedRuntimeTargets")).Order(StringComparer.Ordinal).ToArray());
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
            var embeddedProjectPath = Path.Combine(projectRoot, "Platformer.e2d");
            var exportPresets = File.Exists(loosePresetPath)
                ? Electron2D.ExportPresetStore.Load(loosePresetPath)
                : Electron2D.ExportPresetStore.LoadFromProjectFile(embeddedProjectPath);

            Assert.True(exportPresets.Succeeded, FormatExportDiagnostics(exportPresets.Diagnostics));
            Assert.NotNull(exportPresets.Document);
            Assert.Equal(
                ExpectedRuntimeTargets,
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
        var verifierPath = Path.Combine(root, "eng", "Electron2D.Build", "RepositoryWorkflowVerifiers.cs");

        Assert.True(File.Exists(verifierPath), $"Missing reference game platform matrix verifier: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("ReferenceGamePlatformMatrixVerifier", verifier, StringComparison.Ordinal);
        Assert.Contains("VerifyPlatformer", verifier, StringComparison.Ordinal);
        Assert.Contains("reference-game-platform-matrix.json", verifier, StringComparison.Ordinal);
        Assert.Contains("runtimeTargets", verifier, StringComparison.Ordinal);
        Assert.Contains("releaseVerificationTargets", verifier, StringComparison.Ordinal);
        Assert.Contains("expectedRuntimeTargets", File.ReadAllText(Path.Combine(root, "data", "quality", "reference-game-platform-matrix.json")), StringComparison.Ordinal);

        foreach (var target in ExpectedRuntimeTargets)
        {
            Assert.Contains(target, verifier, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReleaseDocumentationAndReadmeSeparateRuntimeEditorAndVerificationTargets()
    {
        var root = FindRepositoryRoot();
        var releasePath = Path.Combine(root, "docs", "releases", "0.1.0-preview.md");
        var exportGuidePath = Path.Combine(root, "docs", "export", "export-guide.md");
        var readmeContractPath = Path.Combine(root, "docs", "documentation", "repository-readme.md");
        var readmePath = Path.Combine(root, "README.md");

        var release = File.ReadAllText(releasePath);
        Assert.Contains("runtimeTargets", release, StringComparison.Ordinal);
        Assert.Contains("editorTargets", release, StringComparison.Ordinal);
        Assert.Contains("releaseVerificationTargets", release, StringComparison.Ordinal);
        Assert.Contains("Продуктовое решение для `0.1.0 Preview`", release, StringComparison.Ordinal);
        Assert.Contains("| iOS arm64 | Да | Да |", release, StringComparison.Ordinal);
        Assert.Contains("| WebAssembly browser | Да | Да |", release, StringComparison.Ordinal);
        Assert.Contains("статус законченной приёмочной игры требует принятия `T-0222`, `T-0223` и `T-0225`", release, StringComparison.Ordinal);

        var exportGuide = File.ReadAllText(exportGuidePath);
        Assert.Contains("Матрица runtime/export targets", exportGuide, StringComparison.Ordinal);
        Assert.Contains("`editorTargets`", exportGuide, StringComparison.Ordinal);
        Assert.Contains("`releaseVerificationTargets`", exportGuide, StringComparison.Ordinal);
        Assert.Contains("финальная релизная проверка не закрыта", exportGuide, StringComparison.Ordinal);

        var readmeContract = File.ReadAllText(readmeContractPath);
        Assert.Contains("Cross-platform runtime должен формулироваться прямо: `Build and run games on Windows, Linux, macOS and Android. iOS and Web are planned as future runtime targets.`", readmeContract, StringComparison.Ordinal);
        Assert.Contains("README не отображает уровень релизной проверки; текущий состав `releaseVerificationTargets` задаётся в `docs/releases/0.1.0-preview.md`.", readmeContract, StringComparison.Ordinal);
        Assert.Contains("README закрепляет текущее публичное имя `Platformer` и ссылку на `examples/platformer`.", readmeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("iOS и Web показываются только как future runtime targets и не входят в mandatory `0.1.0 Preview` gate.", readmeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("- `platformer`;", readmeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("- `Platformer`;", readmeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("`Platformer` в `Platformer`", readmeContract, StringComparison.Ordinal);

        var readme = File.ReadAllText(readmePath);
        Assert.Contains("Build and run games on Windows, Linux, macOS and Android. iOS and Web are planned as future runtime targets.", readme, StringComparison.Ordinal);
        Assert.Contains("| Platform | Editor | Runtime |", readme, StringComparison.Ordinal);
        Assert.Contains("| Windows | ✅ Done | ✅ Done |", readme, StringComparison.Ordinal);
        Assert.Contains("| Linux | ✅ Done | ✅ Done |", readme, StringComparison.Ordinal);
        Assert.Contains("| macOS | ✅ Done | ✅ Done |", readme, StringComparison.Ordinal);
        Assert.Contains("| Android | ❌ Not planned | ✅ Done |", readme, StringComparison.Ordinal);
        Assert.Contains("| iOS | ❌ Not planned | 🕓 Planned |", readme, StringComparison.Ordinal);
        Assert.Contains("| Web | ❌ Not planned | 🕓 Planned |", readme, StringComparison.Ordinal);
        Assert.Contains("A 2D platformer example built with Electron2D.", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("A complete 2D platformer", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("0.1.0 Preview verification", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("Verified desktop export", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("Browser package and smoke", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferenceGamePlatformMatrixVerifierDeclaresStandaloneGateOutput()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "eng", "Electron2D.Build", "RepositoryWorkflowVerifiers.cs");

        Assert.True(File.Exists(verifierPath), $"Missing reference game platform matrix verifier: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("Reference game platform matrix verification passed", verifier, StringComparison.Ordinal);
        Assert.Contains("summary.json", verifier, StringComparison.Ordinal);
        Assert.Contains("releaseVerificationTargets", verifier, StringComparison.Ordinal);
    }

    private static string[] ReadStringArray(JsonElement element)
    {
        return element.EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static void AssertReleaseVerificationTargets(JsonElement element)
    {
        var targets = element.EnumerateArray().ToDictionary(
            item => item.GetProperty("target").GetString()!,
            StringComparer.Ordinal);

        Assert.Equal(ExpectedRuntimeTargets, targets.Keys.Order(StringComparer.Ordinal).ToArray());

        foreach (var target in ExpectedRuntimeTargets)
        {
            var verificationTarget = targets[target];
            Assert.True(verificationTarget.GetProperty("realSmokeSoakRequired").GetBoolean(), $"{target} must require real smoke/soak for 0.1.0 Preview.");
            Assert.True(verificationTarget.GetProperty("blockedEnvironmentArtifactAllowed").GetBoolean(), $"{target} must allow blocked-environment diagnostics without passing the release gate.");
            Assert.False(string.IsNullOrWhiteSpace(verificationTarget.GetProperty("releaseGateBlocker").GetString()));
        }

        Assert.Contains("Xcode", targets["IosArm64"].GetProperty("releaseGateBlocker").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("browser", targets["WebAssemblyBrowser"].GetProperty("releaseGateBlocker").GetString(), StringComparison.OrdinalIgnoreCase);
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
