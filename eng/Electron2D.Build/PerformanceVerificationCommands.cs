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
using System.Text;
using System.Text.Json;

namespace Electron2D.Build;

internal sealed class PerformanceBudgetDocsVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private static readonly string[] RequiredFragments =
    [
        "Windows",
        "Linux",
        "macOS",
        "Android",
        "iOS",
        "60 FPS",
        "16.67 ms",
        "Бюджет памяти",
        "30-минут",
        "длительн",
        "сворачивания/возврата",
        "мобиль"
    ];

    public int Verify()
    {
        const string relativePath = "docs/release-management/performance-budgets.md";
        var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify performance-budgets",
                "error",
                "E2D-BUILD-PERFORMANCE-BUDGETS-DOC-MISSING",
                $"Performance budget document was not found: {relativePath}.",
                Path: relativePath));
            return RepositoryBuildExitCodes.Failed;
        }

        var content = File.ReadAllText(path);
        foreach (var fragment in RequiredFragments)
        {
            if (content.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
            {
                diagnostics.Write(new BuildDiagnostic(
                    "verify",
                    "verify performance-budgets",
                    "error",
                    "E2D-BUILD-PERFORMANCE-BUDGETS-FRAGMENT-MISSING",
                    $"Performance budget documentation is missing required fragment: {fragment}.",
                    Path: relativePath));
                return RepositoryBuildExitCodes.Failed;
            }
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify performance-budgets",
            "info",
            "E2D-BUILD-PERFORMANCE-BUDGETS-PASSED",
            "Performance budget documentation verification passed.",
            Path: relativePath));
        return RepositoryBuildExitCodes.Success;
    }
}

internal sealed class ReferencePerformanceVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string MetricsRelativePath = "data/quality/performance-reference-metrics.json";
    private const string DefaultPlanRelativePath = ".temp/reference-performance/verification-plan.json";
    private const int DefaultRunnerTimeoutSeconds = 300;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] RequiredScenarioIds = ["empty-scene", "sprite-scene", "platformer"];

    private readonly List<BuildDiagnostic> errors = [];

    public Task<int> VerifyAsync(string[] args, CancellationToken cancellationToken)
    {
        return args is ["verify", "performance", "run", ..]
            ? RunScenarioAsync(args, cancellationToken)
            : Task.FromResult(VerifyMetrics(args));
    }

    private int VerifyMetrics(string[] args)
    {
        if (!TryParseOptions(args, out var outputRelativePath))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var metricsPath = ResolveRepositoryPath(MetricsRelativePath);
        if (!File.Exists(metricsPath))
        {
            WriteError(
                "E2D-BUILD-PERFORMANCE-METRICS-MISSING",
                $"Reference performance metrics artifact was not found: {MetricsRelativePath}.",
                MetricsRelativePath);
            return FlushResult();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(metricsPath));
        }
        catch (JsonException ex)
        {
            WriteError(
                "E2D-BUILD-PERFORMANCE-METRICS-JSON",
                $"Reference performance metrics JSON is invalid: {ex.Message}",
                MetricsRelativePath);
            return FlushResult();
        }

        using (document)
        {
            var root = document.RootElement;
            var plan = ValidateMetrics(root);
            if (errors.Count > 0)
            {
                return FlushResult();
            }

            var outputPath = ResolveRepositoryPath(outputRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(
                outputPath,
                JsonSerializer.Serialize(
                    plan,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }) + Environment.NewLine,
                Utf8NoBom);

            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify performance",
                "info",
                "E2D-BUILD-PERFORMANCE-PASSED",
                $"Reference performance verification passed. Scenarios: {plan.Scenarios.Length}, devices: {plan.Devices.Length}.",
                Path: MetricsRelativePath,
                OutputPath: outputRelativePath));
            return RepositoryBuildExitCodes.Success;
        }
    }

    private bool TryParseOptions(string[] args, out string outputRelativePath)
    {
        outputRelativePath = DefaultPlanRelativePath;
        if (args is ["verify", "performance"])
        {
            return true;
        }

        if (args is ["verify", "performance", "--out", var path] && !string.IsNullOrWhiteSpace(path))
        {
            if (!TryValidateOutputPath(path, out outputRelativePath))
            {
                return false;
            }

            return true;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify performance",
            "error",
            "E2D-BUILD-CLI-INVALID-ARGUMENTS",
            "Expected: verify performance [--out <path>] or verify performance run --scenario <id> [--out <path>] [--timeout-seconds <n>] -- <fileName> [args...]."));
        return false;
    }

    private async Task<int> RunScenarioAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!TryParseRunOptions(args, out var options))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var step = $"verify performance run {options.ScenarioId}";
        var result = await processRunner.RunAsync(
            new ProcessRunRequest(
                step,
                options.FileName,
                options.Arguments,
                repositoryRoot,
                TimeSpan.FromSeconds(options.TimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in result.Diagnostics)
        {
            diagnostics.Write(diagnostic);
        }

        var outputPath = ResolveRepositoryPath(options.OutputRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var runResult = new PerformanceScenarioRunResult(
            "Electron2D.PerformanceScenarioRun",
            1,
            options.ScenarioId,
            options.TimeoutSeconds,
            ".",
            options.FileName,
            options.Arguments,
            result.ExitCode,
            result.TimedOut,
            result.StandardOutput,
            result.StandardError);
        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(
                runResult,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }) + Environment.NewLine,
            Utf8NoBom);

        if (result.TimedOut)
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                step,
                "error",
                "E2D-BUILD-PERFORMANCE-RUN-TIMEOUT",
                $"Performance scenario '{options.ScenarioId}' timed out after {options.TimeoutSeconds} seconds.",
                TimedOut: true,
                OutputPath: options.OutputRelativePath,
                ScenarioId: options.ScenarioId,
                TimeoutSeconds: options.TimeoutSeconds));
            return RepositoryBuildExitCodes.Failed;
        }

        if (result.ExitCode != 0)
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                step,
                "error",
                "E2D-BUILD-PERFORMANCE-RUN-FAILED",
                $"Performance scenario '{options.ScenarioId}' exited with code {result.ExitCode}.",
                ProcessExitCode: result.ExitCode,
                TimedOut: false,
                OutputPath: options.OutputRelativePath,
                ScenarioId: options.ScenarioId,
                TimeoutSeconds: options.TimeoutSeconds));
            return result.ExitCode ?? RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            step,
            "info",
            "E2D-BUILD-PERFORMANCE-RUN-PASSED",
            $"Performance scenario '{options.ScenarioId}' completed.",
            ProcessExitCode: 0,
            TimedOut: false,
            OutputPath: options.OutputRelativePath,
            ScenarioId: options.ScenarioId,
            TimeoutSeconds: options.TimeoutSeconds));
        return RepositoryBuildExitCodes.Success;
    }

    private bool TryParseRunOptions(string[] args, out PerformanceRunOptions options)
    {
        options = new PerformanceRunOptions(string.Empty, string.Empty, DefaultRunnerTimeoutSeconds, string.Empty, []);

        var scenarioId = string.Empty;
        var outputRelativePath = string.Empty;
        var timeoutSeconds = DefaultRunnerTimeoutSeconds;
        var index = 3;
        for (; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == "--")
            {
                index++;
                break;
            }

            if (argument == "--scenario")
            {
                if (index + 1 >= args.Length || !IsValidScenarioId(args[index + 1]))
                {
                    WriteInvalidRunArguments("Performance runner scenario id must be a non-empty portable identifier.");
                    return false;
                }

                scenarioId = args[++index];
                continue;
            }

            if (argument == "--out")
            {
                if (index + 1 >= args.Length || !TryValidateOutputPath(args[index + 1], out outputRelativePath))
                {
                    return false;
                }

                index++;
                continue;
            }

            if (argument == "--timeout-seconds")
            {
                if (index + 1 >= args.Length ||
                    !int.TryParse(args[index + 1], out timeoutSeconds) ||
                    timeoutSeconds <= 0)
                {
                    WriteInvalidRunArguments("Performance runner timeout must be a positive integer.");
                    return false;
                }

                index++;
                continue;
            }

            WriteInvalidRunArguments("Unknown performance runner argument.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            WriteInvalidRunArguments("Performance runner requires --scenario <id>.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputRelativePath))
        {
            outputRelativePath = $".temp/reference-performance/runs/{scenarioId}.json";
        }

        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            WriteInvalidRunArguments("Performance runner requires a child command after --.");
            return false;
        }

        var fileName = args[index];
        if (Path.IsPathRooted(fileName) || fileName.Contains('\\', StringComparison.Ordinal) || IsUrl(fileName) || fileName.Contains("..", StringComparison.Ordinal))
        {
            WriteInvalidRunArguments("Performance runner command must be a PATH command or repository-local relative path.");
            return false;
        }

        options = new PerformanceRunOptions(
            scenarioId,
            outputRelativePath,
            timeoutSeconds,
            fileName,
            args[(index + 1)..]);
        return true;
    }

    private void WriteInvalidRunArguments(string message)
    {
        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify performance run",
            "error",
            "E2D-BUILD-CLI-INVALID-ARGUMENTS",
            $"{message} Expected: verify performance run --scenario <id> [--out <path>] [--timeout-seconds <n>] -- <fileName> [args...]."));
    }

    private bool TryValidateOutputPath(string path, out string outputRelativePath)
    {
        outputRelativePath = string.Empty;
        if (string.IsNullOrWhiteSpace(path) ||
            Path.IsPathRooted(path) ||
            path.Contains('\\', StringComparison.Ordinal) ||
            IsUrl(path) ||
            EscapesRepository(path))
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify performance",
                "error",
                "E2D-BUILD-PERFORMANCE-OUTPUT-PATH",
                "Reference performance output path must be a repository-local relative path that uses forward slashes.",
                OutputPath: path));
            return false;
        }

        outputRelativePath = path;
        return true;
    }

    private static bool IsValidScenarioId(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.');
    }

    private ReferencePerformancePlan ValidateMetrics(JsonElement root)
    {
        if (!TryGetString(root, "format", "metrics", out var format) ||
            format != "Electron2D.ReferencePerformanceMetrics")
        {
            WriteError("E2D-BUILD-PERFORMANCE-METRICS-FORMAT", "Reference performance metrics format is invalid.", MetricsRelativePath);
        }

        if (!TryGetInt(root, "version", "metrics", out var version) || version != 1)
        {
            WriteError("E2D-BUILD-PERFORMANCE-METRICS-VERSION", "Reference performance metrics version must be 1.", MetricsRelativePath);
        }

        if (!TryGetString(root, "release", "metrics", out var release) || release != "0.1.0-preview")
        {
            WriteError("E2D-BUILD-PERFORMANCE-METRICS-RELEASE", "Reference performance metrics release must be 0.1.0-preview.", MetricsRelativePath);
        }

        if (!TryGetObject(root, "budgets", "metrics", out var budgets) ||
            !TryGetInt(budgets, "targetFps", "budgets", out var targetFps) ||
            !TryGetInt(budgets, "minimumWarmupFrames", "budgets", out var minimumWarmupFrames) ||
            !TryGetInt(budgets, "minimumMeasuredFrames", "budgets", out var minimumMeasuredFrames) ||
            !TryGetLong(budgets, "maxSteadyManagedAllocatedBytesPerFrame", "budgets", out var maxSteadyAllocatedBytes) ||
            !TryGetObject(budgets, "scenarios", "budgets", out var scenarioBudgets) ||
            !TryGetObject(budgets, "drawCallBatching", "budgets", out var drawCallBatchingBudget) ||
            !TryGetDouble(drawCallBatchingBudget, "minReductionRatio", "drawCallBatching budget", out var minReductionRatio))
        {
            return new ReferencePerformancePlan("Electron2D.ReferencePerformanceVerificationPlan", 1, release ?? string.Empty, MetricsRelativePath, [], [], string.Empty);
        }

        if (targetFps != 60)
        {
            WriteError("E2D-BUILD-PERFORMANCE-BUDGET-TARGET-FPS", $"Performance target FPS must be 60, got {targetFps}.", MetricsRelativePath);
        }

        if (minimumWarmupFrames < 120 || minimumMeasuredFrames < 600)
        {
            WriteError("E2D-BUILD-PERFORMANCE-BUDGET-FRAMES", "Reference performance verifier requires at least 120 warm-up frames and 600 measured frames.", MetricsRelativePath);
        }

        if (maxSteadyAllocatedBytes != 0)
        {
            WriteError("E2D-BUILD-PERFORMANCE-BUDGET-ALLOCATIONS", "Reference performance verifier requires 0 steady managed allocations per frame.", MetricsRelativePath);
        }

        var deviceIds = ValidateDevices(root);
        var scenarioIds = ValidateScenarios(root, targetFps, minimumWarmupFrames, minimumMeasuredFrames, maxSteadyAllocatedBytes, scenarioBudgets, deviceIds);
        ValidateBatching(root, minReductionRatio);

        return new ReferencePerformancePlan(
            "Electron2D.ReferencePerformanceVerificationPlan",
            1,
            release ?? string.Empty,
            MetricsRelativePath,
            scenarioIds.Order(StringComparer.Ordinal).ToArray(),
            deviceIds.Order(StringComparer.Ordinal).ToArray(),
            "dotnet run --project eng/Electron2D.Build -- verify performance run");
    }

    private string[] ValidateDevices(JsonElement root)
    {
        if (!TryGetArray(root, "devices", "metrics", out var devices))
        {
            return [];
        }

        var deviceIds = new List<string>();
        foreach (var device in devices.EnumerateArray())
        {
            if (!TryGetString(device, "deviceId", "device", out var deviceId))
            {
                continue;
            }

            deviceIds.Add(deviceId);
            foreach (var propertyName in new[] { "platform", "cpuClass", "graphicsClass", "notes" })
            {
                TryGetString(device, propertyName, $"device {deviceId}", out _);
            }

            TryGetInt(device, "memoryGb", $"device {deviceId}", out _);
        }

        if (deviceIds.Count == 0)
        {
            WriteError("E2D-BUILD-PERFORMANCE-DEVICE-MISSING", "Reference performance metrics must contain at least one documented device.", MetricsRelativePath);
        }

        if (!deviceIds.Contains("local-windows-x64", StringComparer.Ordinal))
        {
            WriteError("E2D-BUILD-PERFORMANCE-DEVICE-LOCAL-WINDOWS", "Reference performance metrics must document local-windows-x64.", MetricsRelativePath);
        }

        return deviceIds.ToArray();
    }

    private string[] ValidateScenarios(
        JsonElement root,
        int targetFps,
        int minimumWarmupFrames,
        int minimumMeasuredFrames,
        long maxSteadyAllocatedBytes,
        JsonElement scenarioBudgets,
        string[] deviceIds)
    {
        if (!TryGetArray(root, "scenarios", "metrics", out var scenarios))
        {
            return [];
        }

        var scenarioIds = new List<string>();
        foreach (var scenario in scenarios.EnumerateArray())
        {
            if (!TryGetString(scenario, "scenarioId", "scenario", out var scenarioId))
            {
                continue;
            }

            scenarioIds.Add(scenarioId);
            TryGetString(scenario, "projectPath", $"scenario {scenarioId}", out var projectPath);
            TryGetString(scenario, "scenePath", $"scenario {scenarioId}", out var scenePath);
            TryGetString(scenario, "deviceId", $"scenario {scenarioId}", out var deviceId);
            TryGetInt(scenario, "warmupFrames", $"scenario {scenarioId}", out var warmupFrames);
            TryGetInt(scenario, "measuredFrames", $"scenario {scenarioId}", out var measuredFrames);
            TryGetInt(scenario, "targetFps", $"scenario {scenarioId}", out var scenarioTargetFps);
            TryGetDouble(scenario, "p95FrameTimeMs", $"scenario {scenarioId}", out var p95);
            TryGetDouble(scenario, "p99FrameTimeMs", $"scenario {scenarioId}", out var p99);
            TryGetDouble(scenario, "averageFrameTimeMs", $"scenario {scenarioId}", out var average);
            TryGetLong(scenario, "steadyManagedAllocatedBytesPerFrame", $"scenario {scenarioId}", out var steadyAllocations);
            TryGetArray(scenario, "evidence", $"scenario {scenarioId}", out var evidence);

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                ValidateRepositoryPath(projectPath, $"scenario {scenarioId} project", scenarioId);
            }

            if (!string.IsNullOrWhiteSpace(projectPath) && !string.IsNullOrWhiteSpace(scenePath))
            {
                ValidateRepositoryPath($"{projectPath}/{scenePath}", $"scenario {scenarioId} scene", scenarioId);
            }

            if (!string.IsNullOrWhiteSpace(deviceId) && !deviceIds.Contains(deviceId, StringComparer.Ordinal))
            {
                WriteScenarioError("E2D-BUILD-PERFORMANCE-SCENARIO-DEVICE", $"Scenario {scenarioId} references unknown device: {deviceId}.", scenarioId);
            }

            if (warmupFrames < minimumWarmupFrames || measuredFrames < minimumMeasuredFrames)
            {
                WriteScenarioError("E2D-BUILD-PERFORMANCE-SCENARIO-FRAMES", $"Scenario {scenarioId} does not meet warm-up or measured frame minimums.", scenarioId);
            }

            if (scenarioTargetFps != targetFps)
            {
                WriteScenarioError("E2D-BUILD-PERFORMANCE-SCENARIO-TARGET-FPS", $"Scenario {scenarioId} target FPS mismatch. Expected {targetFps}, got {scenarioTargetFps}.", scenarioId);
            }

            if (!scenarioBudgets.TryGetProperty(scenarioId, out var budget) ||
                !TryGetDouble(budget, "maxP95FrameTimeMs", $"budget {scenarioId}", out var maxP95) ||
                !TryGetDouble(budget, "maxP99FrameTimeMs", $"budget {scenarioId}", out var maxP99))
            {
                WriteScenarioError("E2D-BUILD-PERFORMANCE-SCENARIO-BUDGET", $"Scenario {scenarioId} is missing a complete budget entry.", scenarioId);
            }
            else
            {
                ValidateLessOrEqual(p95, maxP95, $"Scenario {scenarioId} p95 frame time", scenarioId);
                ValidateLessOrEqual(p99, maxP99, $"Scenario {scenarioId} p99 frame time", scenarioId);
                ValidateLessOrEqual(average, maxP95, $"Scenario {scenarioId} average frame time", scenarioId);
            }

            if (steadyAllocations != maxSteadyAllocatedBytes)
            {
                WriteScenarioError("E2D-BUILD-PERFORMANCE-SCENARIO-ALLOCATIONS", $"Scenario {scenarioId} steady managed allocations must be 0 B/frame, got {steadyAllocations}.", scenarioId);
            }

            ValidateEvidence(evidence, $"scenario {scenarioId}", scenarioId);
        }

        var orderedActual = scenarioIds.Order(StringComparer.Ordinal).ToArray();
        var orderedExpected = RequiredScenarioIds.Order(StringComparer.Ordinal).ToArray();
        if (!orderedActual.SequenceEqual(orderedExpected, StringComparer.Ordinal))
        {
            WriteError(
                "E2D-BUILD-PERFORMANCE-SCENARIOS",
                $"Reference performance scenarios mismatch. Expected {string.Join(',', orderedExpected)}, got {string.Join(',', orderedActual)}.",
                MetricsRelativePath);
        }

        return scenarioIds.ToArray();
    }

    private void ValidateBatching(JsonElement root, double minReductionRatio)
    {
        if (!TryGetObject(root, "drawCallBatching", "metrics", out var batching))
        {
            return;
        }

        TryGetString(batching, "scenarioId", "drawCallBatching", out var scenarioId);
        TryGetInt(batching, "commandCount", "drawCallBatching", out var commandCount);
        TryGetInt(batching, "drawCallCount", "drawCallBatching", out var drawCallCount);
        TryGetDouble(batching, "reductionRatio", "drawCallBatching", out var reductionRatio);
        TryGetArray(batching, "evidence", "drawCallBatching", out var evidence);

        if (scenarioId != "sprite-scene")
        {
            WriteError("E2D-BUILD-PERFORMANCE-BATCHING-SCENARIO", "drawCallBatching must measure sprite-scene.", MetricsRelativePath);
        }

        if (commandCount <= drawCallCount)
        {
            WriteError("E2D-BUILD-PERFORMANCE-BATCHING-COUNTS", $"Batching must reduce draw calls. commandCount={commandCount}, drawCallCount={drawCallCount}.", MetricsRelativePath);
        }

        if (double.IsNaN(reductionRatio) || reductionRatio < minReductionRatio)
        {
            WriteError("E2D-BUILD-PERFORMANCE-BATCHING-RATIO", $"Batching reduction ratio is too low. Actual: {reductionRatio}, minimum: {minReductionRatio}.", MetricsRelativePath);
        }

        ValidateEvidence(evidence, "drawCallBatching", null);
    }

    private void ValidateEvidence(JsonElement evidence, string context, string? scenarioId)
    {
        if (evidence.ValueKind != JsonValueKind.Array || !evidence.EnumerateArray().Any())
        {
            WriteError("E2D-BUILD-PERFORMANCE-EVIDENCE-MISSING", $"{context} must contain at least one evidence path.", MetricsRelativePath, scenarioId);
            return;
        }

        foreach (var item in evidence.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                WriteError("E2D-BUILD-PERFORMANCE-EVIDENCE-PATH", $"{context} evidence path must be a string.", MetricsRelativePath, scenarioId);
                continue;
            }

            ValidateRepositoryPath(item.GetString() ?? string.Empty, $"{context} evidence", scenarioId);
        }
    }

    private void ValidateRepositoryPath(string relativePath, string context, string? scenarioId)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) ||
            IsUrl(relativePath) ||
            EscapesRepository(relativePath))
        {
            WriteError("E2D-BUILD-PERFORMANCE-PATH-INVALID", $"{context} path must be repository-local and use forward slashes: {relativePath}.", MetricsRelativePath, scenarioId);
            return;
        }

        if (!File.Exists(ResolveRepositoryPath(relativePath)) && !Directory.Exists(ResolveRepositoryPath(relativePath)))
        {
            WriteError("E2D-BUILD-PERFORMANCE-PATH-MISSING", $"{context} path does not exist: {relativePath}.", relativePath, scenarioId);
        }
    }

    private int FlushResult()
    {
        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        return RepositoryBuildExitCodes.Failed;
    }

    private void ValidateLessOrEqual(double actual, double max, string context, string? scenarioId)
    {
        if (double.IsNaN(actual) || actual < 0 || actual > max)
        {
            WriteError("E2D-BUILD-PERFORMANCE-BUDGET-EXCEEDED", $"{context} exceeds budget. Actual: {actual}, max: {max}.", MetricsRelativePath, scenarioId);
        }
    }

    private bool TryGetObject(JsonElement target, string propertyName, string context, out JsonElement value)
    {
        if (target.ValueKind == JsonValueKind.Object &&
            target.TryGetProperty(propertyName, out value) &&
            value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        WriteError("E2D-BUILD-PERFORMANCE-METRICS-SCHEMA", $"{context} is missing required object property: {propertyName}.", MetricsRelativePath);
        value = default;
        return false;
    }

    private bool TryGetArray(JsonElement target, string propertyName, string context, out JsonElement value)
    {
        if (target.ValueKind == JsonValueKind.Object &&
            target.TryGetProperty(propertyName, out value) &&
            value.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        WriteError("E2D-BUILD-PERFORMANCE-METRICS-SCHEMA", $"{context} is missing required array property: {propertyName}.", MetricsRelativePath);
        value = default;
        return false;
    }

    private bool TryGetString(JsonElement target, string propertyName, string context, out string value)
    {
        if (target.ValueKind == JsonValueKind.Object &&
            target.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString()!;
            return true;
        }

        WriteError("E2D-BUILD-PERFORMANCE-METRICS-SCHEMA", $"{context} is missing required string property: {propertyName}.", MetricsRelativePath);
        value = string.Empty;
        return false;
    }

    private bool TryGetInt(JsonElement target, string propertyName, string context, out int value)
    {
        if (target.ValueKind == JsonValueKind.Object &&
            target.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out value))
        {
            return true;
        }

        WriteError("E2D-BUILD-PERFORMANCE-METRICS-SCHEMA", $"{context} is missing required integer property: {propertyName}.", MetricsRelativePath);
        value = 0;
        return false;
    }

    private bool TryGetLong(JsonElement target, string propertyName, string context, out long value)
    {
        if (target.ValueKind == JsonValueKind.Object &&
            target.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt64(out value))
        {
            return true;
        }

        WriteError("E2D-BUILD-PERFORMANCE-METRICS-SCHEMA", $"{context} is missing required integer property: {propertyName}.", MetricsRelativePath);
        value = 0;
        return false;
    }

    private bool TryGetDouble(JsonElement target, string propertyName, string context, out double value)
    {
        if (target.ValueKind == JsonValueKind.Object &&
            target.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out value))
        {
            return true;
        }

        WriteError("E2D-BUILD-PERFORMANCE-METRICS-SCHEMA", $"{context} is missing required number property: {propertyName}.", MetricsRelativePath);
        value = double.NaN;
        return false;
    }

    private void WriteScenarioError(string code, string message, string scenarioId)
    {
        WriteError(code, message, MetricsRelativePath, scenarioId);
    }

    private void WriteError(string code, string message, string path, string? scenarioId = null)
    {
        errors.Add(new BuildDiagnostic(
            "verify",
            "verify performance",
            "error",
            code,
            message,
            Path: path,
            ScenarioId: scenarioId));
    }

    private string ResolveRepositoryPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private bool EscapesRepository(string relativePath)
    {
        var fullPath = ResolveRepositoryPath(relativePath);
        var root = Path.GetFullPath(repositoryRoot);
        return !fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUrl(string path)
    {
        return path.Contains("://", StringComparison.Ordinal);
    }

    private sealed record ReferencePerformancePlan(
        string Format,
        int Version,
        string Release,
        string MetricsArtifact,
        string[] Scenarios,
        string[] Devices,
        string RunnerCommand);

    private sealed record PerformanceRunOptions(
        string ScenarioId,
        string OutputRelativePath,
        int TimeoutSeconds,
        string FileName,
        string[] Arguments);

    private sealed record PerformanceScenarioRunResult(
        string Format,
        int Version,
        string ScenarioId,
        int TimeoutSeconds,
        string WorkingDirectory,
        string FileName,
        string[] Arguments,
        int? ExitCode,
        bool TimedOut,
        string StandardOutput,
        string StandardError);
}
