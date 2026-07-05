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

public sealed class ManagedDebugAdapterSelectionTests
{
    [Fact]
    public void DebugAdapterSelectionManifestDocumentsRedistributableDapAdapter()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "debugging", "dotnet-debug-adapter-selection.json");
        var documentationPath = Path.Combine(root, "docs", "scripting", "managed-debug-adapter-selection.md");

        Assert.True(File.Exists(manifestPath), $"Missing debug adapter selection manifest: {manifestPath}");
        Assert.True(File.Exists(documentationPath), $"Missing debug adapter selection documentation: {documentationPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var rootElement = document.RootElement;

        Assert.Equal(1, rootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("T-0163", rootElement.GetProperty("taskId").GetString());
        Assert.True(rootElement.GetProperty("t0160Handoff").GetProperty("ready").GetBoolean());

        var selected = rootElement.GetProperty("selectedAdapter");
        Assert.Equal("netcoredbg", selected.GetProperty("id").GetString());
        Assert.Equal("MIT", selected.GetProperty("licenseSpdx").GetString());
        Assert.Equal("stdio", selected.GetProperty("dap").GetProperty("transport").GetString());
        Assert.Contains("--interpreter=vscode", selected.GetProperty("dap").GetProperty("arguments").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("github.com/Samsung/netcoredbg", selected.GetProperty("sourceRepository").GetString());

        var candidates = rootElement.GetProperty("candidateReview").EnumerateArray().ToArray();
        Assert.Contains(candidates, candidate => candidate.GetProperty("id").GetString() == "netcoredbg" && candidate.GetProperty("status").GetString() == "selected");
        Assert.Contains(candidates, candidate => candidate.GetProperty("id").GetString() == "microsoft-vsdbg" && candidate.GetProperty("status").GetString() == "rejected");
        Assert.Contains(candidates, candidate => candidate.GetProperty("id").GetString() == "sharpdbg" && candidate.GetProperty("status").GetString() == "rejected-for-0.1-preview");

        var platformTargets = rootElement.GetProperty("platformTargets").EnumerateArray().ToArray();
        AssertPlatform(platformTargets, "windows-x64", "upstream-release-binary", "executed-local-dap-smoke");
        AssertPlatform(platformTargets, "linux-x64", "upstream-release-binary", "release-asset-available");
        AssertPlatform(platformTargets, "macos-arm64", "electron2d-source-build", "source-build-required");

        var windowsEvidence = platformTargets.Single(platform => platform.GetProperty("platform").GetString() == "windows-x64").GetProperty("validationEvidence");
        Assert.True(windowsEvidence.GetProperty("launchSmoke").GetBoolean());
        Assert.True(windowsEvidence.GetProperty("attachSmoke").GetBoolean());
        Assert.True(windowsEvidence.GetProperty("portablePdb").GetBoolean());

        var capabilities = rootElement.GetProperty("capabilityMatrix");
        foreach (var capability in new[]
        {
            "initialize",
            "launch",
            "attach",
            "setBreakpoints",
            "configurationDone",
            "continue",
            "next",
            "threads",
            "stackTrace",
            "scopes",
            "variables",
            "exceptionFilters",
            "expressionEvaluation"
        })
        {
            Assert.True(capabilities.GetProperty(capability).GetBoolean(), $"Capability must be enabled: {capability}");
        }

        var documentation = File.ReadAllText(documentationPath);
        Assert.Contains("netcoredbg", documentation, StringComparison.Ordinal);
        Assert.Contains("Windows x64", documentation, StringComparison.Ordinal);
        Assert.Contains("Linux x64", documentation, StringComparison.Ordinal);
        Assert.Contains("macOS arm64", documentation, StringComparison.Ordinal);
        Assert.Contains("MIT", documentation, StringComparison.Ordinal);
        Assert.Contains("T-0160", documentation, StringComparison.Ordinal);
    }

    private static void AssertPlatform(JsonElement[] platformTargets, string platform, string sourceKind, string validationStatus)
    {
        var target = platformTargets.Single(item => item.GetProperty("platform").GetString() == platform);
        Assert.Equal(sourceKind, target.GetProperty("sourceKind").GetString());
        Assert.Equal(validationStatus, target.GetProperty("validationStatus").GetString());
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;

        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "src", "Electron2D.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
