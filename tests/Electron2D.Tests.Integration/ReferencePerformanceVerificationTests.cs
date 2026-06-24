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

public sealed class ReferencePerformanceVerificationTests
{
    private static readonly string[] ScenarioIds =
    [
        "empty-scene",
        "sprite-scene",
        "reference-platformer"
    ];

    [Fact]
    public void PerformanceSpecificationDefinesReferenceVerificationContract()
    {
        var root = FindRepositoryRoot();
        var specPath = Path.Combine(root, "docs", "quality", "performance-verification.md");

        Assert.True(File.Exists(specPath), $"Missing performance verification specification: {specPath}");

        var spec = File.ReadAllText(specPath);
        Assert.Contains("tools\\Verify-ReferencePerformance.ps1", spec, StringComparison.Ordinal);
        Assert.Contains("data/quality/performance-reference-metrics.json", spec, StringComparison.Ordinal);
        Assert.Contains("tools\\Verify-ReferencePlatformer.ps1", spec, StringComparison.Ordinal);
        Assert.Contains("0 B/frame", spec, StringComparison.Ordinal);
        Assert.Contains("reductionRatio >= 1.5", spec, StringComparison.Ordinal);

        foreach (var scenarioId in ScenarioIds)
        {
            Assert.Contains(scenarioId, spec, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReferencePerformanceVerifierDeclaresReferenceGameValidationAndMetricsChecks()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-ReferencePerformance.ps1");

        Assert.True(File.Exists(verifierPath), $"Missing reference performance verifier: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("Verify-ReferencePlatformer.ps1", verifier, StringComparison.Ordinal);
        Assert.Contains("performance-reference-metrics.json", verifier, StringComparison.Ordinal);
        Assert.Contains("p95FrameTimeMs", verifier, StringComparison.Ordinal);
        Assert.Contains("steadyManagedAllocatedBytesPerFrame", verifier, StringComparison.Ordinal);
        Assert.Contains("drawCallBatching", verifier, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferencePerformanceMetricsArtifactCoversBudgetsDevicesScenariosAndBatching()
    {
        var root = FindRepositoryRoot();
        var metricsPath = Path.Combine(root, "data", "quality", "performance-reference-metrics.json");

        Assert.True(File.Exists(metricsPath), $"Missing reference performance metrics artifact: {metricsPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(metricsPath));
        var metrics = document.RootElement;

        Assert.Equal("Electron2D.ReferencePerformanceMetrics", metrics.GetProperty("format").GetString());
        Assert.Equal(1, metrics.GetProperty("version").GetInt32());
        Assert.Equal("0.1.0-preview", metrics.GetProperty("release").GetString());

        var budgets = metrics.GetProperty("budgets");
        Assert.Equal(60, budgets.GetProperty("targetFps").GetInt32());
        Assert.True(budgets.GetProperty("minimumWarmupFrames").GetInt32() >= 120);
        Assert.True(budgets.GetProperty("minimumMeasuredFrames").GetInt32() >= 600);
        Assert.Equal(0, budgets.GetProperty("maxSteadyManagedAllocatedBytesPerFrame").GetInt64());

        var devices = metrics.GetProperty("devices").EnumerateArray().ToDictionary(
            device => device.GetProperty("deviceId").GetString()!,
            StringComparer.Ordinal);
        Assert.NotEmpty(devices);
        Assert.Contains("local-windows-x64", devices.Keys);

        var scenarios = metrics.GetProperty("scenarios").EnumerateArray().ToDictionary(
            scenario => scenario.GetProperty("scenarioId").GetString()!,
            StringComparer.Ordinal);
        Assert.Equal(ScenarioIds.Order(StringComparer.Ordinal), scenarios.Keys.Order(StringComparer.Ordinal));

        foreach (var scenarioId in ScenarioIds)
        {
            var scenario = scenarios[scenarioId];
            Assert.False(string.IsNullOrWhiteSpace(scenario.GetProperty("projectPath").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(scenario.GetProperty("scenePath").GetString()));
            Assert.Contains(scenario.GetProperty("deviceId").GetString()!, devices.Keys);
            Assert.True(scenario.GetProperty("warmupFrames").GetInt32() >= 120);
            Assert.True(scenario.GetProperty("measuredFrames").GetInt32() >= 600);
            Assert.Equal(60, scenario.GetProperty("targetFps").GetInt32());
            Assert.InRange(scenario.GetProperty("p95FrameTimeMs").GetDouble(), 0, 16.67);
            Assert.InRange(scenario.GetProperty("p99FrameTimeMs").GetDouble(), 0, scenarioId == "empty-scene" ? 25 : 33);
            Assert.InRange(scenario.GetProperty("averageFrameTimeMs").GetDouble(), 0, 16.67);
            Assert.Equal(0, scenario.GetProperty("steadyManagedAllocatedBytesPerFrame").GetInt64());
            Assert.NotEmpty(scenario.GetProperty("evidence").EnumerateArray());
        }

        Assert.Contains(
            scenarios["reference-platformer"].GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetString() == "tools/Verify-ReferencePlatformer.ps1");

        var batching = metrics.GetProperty("drawCallBatching");
        Assert.Equal("sprite-scene", batching.GetProperty("scenarioId").GetString());
        Assert.True(batching.GetProperty("commandCount").GetInt32() > batching.GetProperty("drawCallCount").GetInt32());
        Assert.True(batching.GetProperty("reductionRatio").GetDouble() >= 1.5);
        Assert.NotEmpty(batching.GetProperty("evidence").EnumerateArray());
    }

    [Fact]
    public async Task ReferencePerformanceVerifierPasses()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-ReferencePerformance.ps1");

        Assert.True(File.Exists(verifierPath), $"Missing reference performance verifier: {verifierPath}");

        var startInfo = PowerShellProcess.CreateScriptStartInfo(root, verifierPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start reference performance verifier.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Reference performance verifier failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");
        Assert.Contains("Reference performance verification passed", output, StringComparison.Ordinal);
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
