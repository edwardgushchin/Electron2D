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
using Electron2D.Mcp;
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorCapabilityManifestTests
{
    private static readonly string[] RequiredCategories =
    [
        "scene",
        "node",
        "inspector",
        "resources",
        "signals",
        "groups",
        "input-map",
        "project-settings",
        "spriteframes",
        "animationplayer",
        "tilemap",
        "ui-themes",
        "import-settings",
        "main-scene",
        "export-presets",
        "tests",
        "diagnostics",
        "runtime-control"
    ];

    [Fact]
    public void DefaultManifestMatchesTrackedJsonAndCoversReleaseRequiredOperations()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "editor", "electron2d-editor-capabilities.json");

        Assert.True(File.Exists(manifestPath), $"Editor capability manifest was not found: {manifestPath}");

        var manifest = EditorCapabilityManifestFactory.CreateDefault();
        var generatedJson = EditorCapabilityManifestSerializer.Serialize(manifest);
        var trackedJson = File.ReadAllText(manifestPath).ReplaceLineEndings("\n");

        Assert.Equal(trackedJson, generatedJson);
        Assert.Equal("data/api/electron2d-api-manifest.json", manifest.ApiManifest.Path);
        Assert.Contains("electron2d://api/type/Electron2D.Node", manifest.ApiManifest.References);
        Assert.Contains("electron2d://api/type/Electron2D.Control", manifest.ApiManifest.References);

        var categories = manifest.Capabilities.SelectMany(capability => capability.Categories).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var category in RequiredCategories)
        {
            Assert.Contains(category, categories);
        }

        Assert.All(manifest.Capabilities, capability =>
        {
            Assert.False(string.IsNullOrWhiteSpace(capability.Id));
            Assert.NotEqual(EditorCapabilityKind.Unknown, capability.Kind);
            Assert.NotEqual(EditorCapabilitySupportStatus.Unknown, capability.Editor.Status);
            Assert.False(string.IsNullOrWhiteSpace(capability.Editor.Explanation));
            Assert.False(string.IsNullOrWhiteSpace(capability.Tooling.Command));
            Assert.False(string.IsNullOrWhiteSpace(capability.Mcp.Command));
            Assert.NotEqual(EditorCapabilityCliBindingKind.Unknown, capability.Cli.Kind);
            Assert.False(string.IsNullOrWhiteSpace(capability.Cli.Explanation));
        });

        var verification = EditorCapabilityManifestVerifier.Verify(
            manifest,
            new EditorCapabilityManifestVerificationInput(
                McpServerSession.DefaultToolNames,
                ProjectToolingHost.SupportedCommandNames,
                root));

        Assert.True(verification.Succeeded);
        Assert.Empty(verification.Diagnostics);
    }

    [Fact]
    public void McpResourceReturnsEditorCapabilityManifest()
    {
        using var session = McpServerSession.Open(CreateProjectRoot(), registry: null, new DateTimeOffset(2026, 6, 23, 0, 0, 0, TimeSpan.Zero));

        var resource = session.ReadResource("electron2d://editor/capabilities");

        Assert.Equal("electron2d://editor/capabilities", resource.Uri);
        Assert.Equal("0.1.0-preview", resource.Content["manifestVersion"]!.GetValue<string>());
        Assert.Contains(resource.Content["capabilities"]!.AsArray(), node =>
            node!["capability"]!.GetValue<string>() == "scene.node.set_property" &&
            node["tooling"]!["status"]!.GetValue<string>() == "supported" &&
            node["mcp"]!["status"]!.GetValue<string>() == "supported");
    }

    [Fact]
    public void VerifierRejectsSupportedEditorCapabilityWithoutToolingAndMcpParity()
    {
        var manifest = EditorCapabilityManifestFactory.CreateDefault();
        var invalid = manifest with
        {
            Capabilities = manifest.Capabilities
                .Select(capability => capability.Id == "scene.node.set_property"
                    ? capability with
                    {
                        Tooling = capability.Tooling with
                        {
                            Status = EditorCapabilitySupportStatus.Partial,
                            Explanation = "Intentionally broken for verifier coverage."
                        },
                        Mcp = capability.Mcp with
                        {
                            Status = EditorCapabilitySupportStatus.Experimental,
                            Explanation = "Intentionally broken for verifier coverage."
                        }
                    }
                    : capability)
                .ToArray()
        };

        var result = EditorCapabilityManifestVerifier.Verify(
            invalid,
            new EditorCapabilityManifestVerificationInput(
                McpServerSession.DefaultToolNames,
                ProjectToolingHost.SupportedCommandNames,
                FindRepositoryRoot()));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "E2D-CAPABILITY-0001");
    }

    [Fact]
    public void VerifierRejectsReleaseRequiredOperationWithoutCliPolicy()
    {
        var manifest = EditorCapabilityManifestFactory.CreateDefault();
        var invalid = manifest with
        {
            Capabilities = manifest.Capabilities
                .Select(capability => capability.Id == "resource.import"
                    ? capability with
                    {
                        Cli = new EditorCapabilityCliBinding(
                            EditorCapabilityCliBindingKind.NotApplicable,
                            Command: null,
                            Explanation: "Intentionally broken for verifier coverage.")
                    }
                    : capability)
                .ToArray()
        };

        var result = EditorCapabilityManifestVerifier.Verify(
            invalid,
            new EditorCapabilityManifestVerificationInput(
                McpServerSession.DefaultToolNames,
                ProjectToolingHost.SupportedCommandNames,
                FindRepositoryRoot()));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "E2D-CAPABILITY-0002");
    }

    private static string CreateProjectRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-EditorCapabilityManifestTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
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
