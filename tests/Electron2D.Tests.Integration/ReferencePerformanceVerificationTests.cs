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
using System.Text;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ReferencePerformanceVerificationTests
{
    private static readonly string[] ScenarioIds =
    [
        "empty-scene",
        "sprite-scene",
        "platformer"
    ];

    [Fact]
    public void PerformanceSpecificationDefinesReferenceVerificationContract()
    {
        var root = FindRepositoryRoot();
        var specPath = Path.Combine(root, "docs", "quality", "performance-verification.md");

        Assert.True(File.Exists(specPath), $"Missing performance verification specification: {specPath}");

        var spec = File.ReadAllText(specPath);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify performance", spec, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify performance run", spec, StringComparison.Ordinal);
        Assert.Contains("data/quality/performance-reference-metrics.json", spec, StringComparison.Ordinal);
        Assert.Contains(".temp/reference-performance/verification-plan.json", spec, StringComparison.Ordinal);
        Assert.Contains("Если `--out <path>` передан", spec, StringComparison.Ordinal);
        Assert.Contains("0 B/frame", spec, StringComparison.Ordinal);
        Assert.Contains("reductionRatio >= 1.5", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("powershell -ExecutionPolicy Bypass -File tools\\Verify-ReferencePerformance.ps1", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("tools/Verify-Platformer.ps1", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("Временный результат создаётся только в `.temp/reference-performance/`", spec, StringComparison.Ordinal);

        foreach (var scenarioId in ScenarioIds)
        {
            Assert.Contains(scenarioId, spec, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task VerifyPerformanceBudgetsRunsCSharpDocsCheck()
    {
        using var workspace = CreatePerformanceFixture("performance-budgets-valid");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance-budgets");

        Assert.Equal(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-BUDGETS-PASSED");
        Assert.DoesNotContain("E2D-BUILD-ROUTED", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyPerformanceBudgetsRejectsMissingRequiredFragment()
    {
        using var workspace = CreatePerformanceFixture("performance-budgets-missing-fragment");
        ReplaceInFile(workspace.Root, "docs/release-management/performance-budgets.md", "мобильные", "переносные");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance-budgets");

        Assert.NotEqual(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-BUDGETS-FRAGMENT-MISSING");
    }

    [Fact]
    public async Task VerifyPerformanceWritesMachineReadablePlan()
    {
        using var workspace = CreatePerformanceFixture("reference-performance-valid");
        var planPath = ".temp/reference-performance/custom-plan.json";

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance", "--out", planPath);

        Assert.Equal(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-PASSED");
        Assert.DoesNotContain("E2D-BUILD-ROUTED", result.Stdout, StringComparison.Ordinal);

        var fullPlanPath = Path.Combine(workspace.Root, planPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPlanPath), $"Expected verification plan: {fullPlanPath}");
        using var plan = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
        Assert.Equal("Electron2D.ReferencePerformanceVerificationPlan", plan.RootElement.GetProperty("format").GetString());
        Assert.Equal(ScenarioIds.Order(StringComparer.Ordinal), plan.RootElement.GetProperty("scenarios").EnumerateArray().Select(item => item.GetString()!).Order(StringComparer.Ordinal));
        Assert.Contains("local-windows-x64", plan.RootElement.GetProperty("devices").EnumerateArray().Select(item => item.GetString()!));
        Assert.Equal("dotnet run --project eng/Electron2D.Build -- verify performance run", plan.RootElement.GetProperty("runnerCommand").GetString());
    }

    [Fact]
    public async Task VerifyPerformanceRejectsFixtureBudgetViolation()
    {
        using var workspace = CreatePerformanceFixture("reference-performance-budget-violation");
        ReplaceInFile(workspace.Root, "data/quality/performance-reference-metrics.json", "\"p95FrameTimeMs\": 16.66", "\"p95FrameTimeMs\": 99.0");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance", "--out", ".temp/reference-performance/plan.json");

        Assert.NotEqual(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-BUDGET-EXCEEDED");
    }

    [Fact]
    public async Task VerifyPerformanceRejectsMissingEvidencePath()
    {
        using var workspace = CreatePerformanceFixture("reference-performance-missing-evidence");
        ReplaceInFile(workspace.Root, "data/quality/performance-reference-metrics.json", "examples/platformer/scripts/PlatformerGame.cs", "examples/platformer/scripts/MissingGame.cs");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance", "--out", ".temp/reference-performance/plan.json");

        Assert.NotEqual(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-PATH-MISSING");
    }

    [Fact]
    public async Task VerifyPerformanceRejectsMissingLocalWindowsDevice()
    {
        using var workspace = CreatePerformanceFixture("reference-performance-missing-local-device");
        ReplaceInFile(workspace.Root, "data/quality/performance-reference-metrics.json", "local-windows-x64", "local-linux-x64");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance", "--out", ".temp/reference-performance/plan.json");

        Assert.NotEqual(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-DEVICE-LOCAL-WINDOWS");
    }

    [Fact]
    public async Task VerifyPerformanceRejectsEscapingOutputPath()
    {
        using var workspace = CreatePerformanceFixture("reference-performance-output-path");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "verify", "performance", "--out", "../plan.json");

        Assert.NotEqual(0, result.ExitCode);
        AssertDiagnosticCode(result, "E2D-BUILD-PERFORMANCE-OUTPUT-PATH");
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

        Assert.DoesNotContain(
            scenarios["platformer"].GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetString() == "tools/Verify-Platformer.ps1");

        var batching = metrics.GetProperty("drawCallBatching");
        Assert.Equal("sprite-scene", batching.GetProperty("scenarioId").GetString());
        Assert.True(batching.GetProperty("commandCount").GetInt32() > batching.GetProperty("drawCallCount").GetInt32());
        Assert.True(batching.GetProperty("reductionRatio").GetDouble() >= 1.5);
        Assert.NotEmpty(batching.GetProperty("evidence").EnumerateArray());
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

    private static TemporaryDirectory CreatePerformanceFixture(string name)
    {
        var workspace = TemporaryDirectory.Create(name);
        WriteText(workspace.Root, "AGENTS.md", "# Test repository instructions\n");
        WriteText(workspace.Root, "README.md", "# Temporary repository\n");
        Directory.CreateDirectory(Path.Combine(workspace.Root, "src"));
        WriteText(
            workspace.Root,
            "docs/release-management/performance-budgets.md",
            """
            # Performance budgets

            Windows, Linux, macOS, Android и iOS используют общую цель 60 FPS.
            Бюджет кадра равен 16.67 ms. Бюджет памяти покрывает 30-минутный
            длительный прогон, циклы сворачивания/возврата и мобильные устройства.
            """);
        WriteText(workspace.Root, "data/quality/performance-reference-metrics.json", CreateValidMetricsJson());
        WriteText(workspace.Root, "data/quality/reference-performance/empty-scene.scene.json", "{}\n");
        WriteText(workspace.Root, "data/quality/reference-performance/sprite-scene.scene.json", "{}\n");
        WriteText(workspace.Root, "examples/platformer/scenes/main.scene.json", "{}\n");
        WriteText(workspace.Root, "examples/platformer/scripts/PlatformerGame.cs", "namespace Fixture;\n");
        WriteText(workspace.Root, "tests/Electron2D.Tests.Integration/Electron2DHeadlessRuntimeAutomationTests.cs", "namespace Fixture;\n");
        WriteText(workspace.Root, "tests/Electron2D.Tests.Integration/CanvasItemRenderQueueTests.cs", "namespace Fixture;\n");
        WriteText(workspace.Root, "docs/rendering/canvas-item-render-queue.md", "# Render queue\n");
        return workspace;
    }

    private static string CreateValidMetricsJson()
    {
        return """
        {
          "format": "Electron2D.ReferencePerformanceMetrics",
          "version": 1,
          "release": "0.1.0-preview",
          "budgets": {
            "targetFps": 60,
            "minimumWarmupFrames": 120,
            "minimumMeasuredFrames": 600,
            "maxSteadyManagedAllocatedBytesPerFrame": 0,
            "scenarios": {
              "empty-scene": { "maxP95FrameTimeMs": 16.67, "maxP99FrameTimeMs": 25.0 },
              "sprite-scene": { "maxP95FrameTimeMs": 16.67, "maxP99FrameTimeMs": 33.0 },
              "platformer": { "maxP95FrameTimeMs": 16.67, "maxP99FrameTimeMs": 33.0 }
            },
            "drawCallBatching": { "minReductionRatio": 1.5 }
          },
          "devices": [
            {
              "deviceId": "local-windows-x64",
              "platform": "Windows 11 x64",
              "cpuClass": "4-core development workstation",
              "memoryGb": 8,
              "graphicsClass": "Desktop Tier 1 GPU",
              "notes": "Fixture device."
            }
          ],
          "scenarios": [
            {
              "scenarioId": "empty-scene",
              "projectPath": "data/quality/reference-performance",
              "scenePath": "empty-scene.scene.json",
              "deviceId": "local-windows-x64",
              "warmupFrames": 120,
              "measuredFrames": 600,
              "targetFps": 60,
              "p95FrameTimeMs": 16.66,
              "p99FrameTimeMs": 16.66,
              "averageFrameTimeMs": 16.66,
              "steadyManagedAllocatedBytesPerFrame": 0,
              "evidence": [
                "data/quality/reference-performance/empty-scene.scene.json",
                "tests/Electron2D.Tests.Integration/Electron2DHeadlessRuntimeAutomationTests.cs"
              ]
            },
            {
              "scenarioId": "sprite-scene",
              "projectPath": "data/quality/reference-performance",
              "scenePath": "sprite-scene.scene.json",
              "deviceId": "local-windows-x64",
              "warmupFrames": 120,
              "measuredFrames": 600,
              "targetFps": 60,
              "p95FrameTimeMs": 16.66,
              "p99FrameTimeMs": 16.66,
              "averageFrameTimeMs": 16.66,
              "steadyManagedAllocatedBytesPerFrame": 0,
              "evidence": [
                "data/quality/reference-performance/sprite-scene.scene.json",
                "tests/Electron2D.Tests.Integration/CanvasItemRenderQueueTests.cs"
              ]
            },
            {
              "scenarioId": "platformer",
              "projectPath": "examples/platformer",
              "scenePath": "scenes/main.scene.json",
              "deviceId": "local-windows-x64",
              "warmupFrames": 120,
              "measuredFrames": 600,
              "targetFps": 60,
              "p95FrameTimeMs": 16.66,
              "p99FrameTimeMs": 16.66,
              "averageFrameTimeMs": 16.66,
              "steadyManagedAllocatedBytesPerFrame": 0,
              "evidence": [
                "examples/platformer/scenes/main.scene.json",
                "examples/platformer/scripts/PlatformerGame.cs"
              ]
            }
          ],
          "drawCallBatching": {
            "scenarioId": "sprite-scene",
            "commandCount": 6,
            "drawCallCount": 3,
            "reductionRatio": 2.0,
            "evidence": [
              "tests/Electron2D.Tests.Integration/CanvasItemRenderQueueTests.cs",
              "docs/rendering/canvas-item-render-queue.md"
            ]
          }
        }
        """;
    }

    private static async Task<CommandResult> RunBuildToolFromDirectoryAsync(string workingDirectory, params string[] arguments)
    {
        var root = FindRepositoryRoot();
        return await RunProcessAsync(
            "dotnet",
            ["run", "--project", Path.Combine(root, "eng", "Electron2D.Build", "Electron2D.Build.csproj"), "--", .. arguments],
            workingDirectory,
            TimeSpan.FromSeconds(120));
    }

    private static async Task<CommandResult> RunProcessAsync(string fileName, string[] arguments, string workingDirectory, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);

        await process.WaitForExitAsync(cancellation.Token);
        return new CommandResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static void AssertDiagnosticCode(CommandResult result, string expectedCode)
    {
        using var diagnostics = ReadDiagnostics(result);
        var codes = diagnostics.RootElement
            .EnumerateArray()
            .Select(diagnostic => diagnostic.GetProperty("code").GetString())
            .ToArray();

        Assert.True(
            codes.Contains(expectedCode, StringComparer.Ordinal),
            $"Expected diagnostic '{expectedCode}'. Actual codes: {string.Join(", ", codes)}.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
    }

    private static JsonDocument ReadDiagnostics(CommandResult result)
    {
        var lines = result.Stdout
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        Assert.NotEmpty(lines);
        return JsonDocument.Parse("[" + string.Join(",", lines) + "]");
    }

    private static void ReplaceInFile(string root, string relativePath, string oldValue, string newValue)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var content = File.ReadAllText(path);
        Assert.Contains(oldValue, content, StringComparison.Ordinal);
        File.WriteAllText(path, content.Replace(oldValue, newValue, StringComparison.Ordinal), Encoding.UTF8);
    }

    private static void WriteText(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private sealed record CommandResult(int ExitCode, string Stdout, string Stderr);

    private sealed record TemporaryDirectory(string Root) : IDisposable
    {
        public static TemporaryDirectory Create(string name)
        {
            var root = Path.Combine(Path.GetTempPath(), "Electron2D-ReferencePerformanceVerificationTests", name, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TemporaryDirectory(root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
