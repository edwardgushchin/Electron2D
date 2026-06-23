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
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class Electron2DAndroidDeviceSmokeRunner
{
    internal static readonly string[] RequiredCriteria =
    [
        "install",
        "launch",
        "render",
        "input",
        "pauseResume",
        "orientation",
        "safeArea",
        "audio",
        "resources",
        "filesystem",
        "rendererFallback",
        "shutdown"
    ];

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static Electron2DAndroidDeviceSmokeResult Run(
        Electron2DAndroidExportPlan plan,
        string artifactPath,
        Electron2DAndroidDeviceSmokeObservation observation,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);
        ArgumentNullException.ThrowIfNull(observation);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        var criteria = RequiredCriteria.ToDictionary(
            criterion => criterion,
            criterion => observation.Criteria.TryGetValue(criterion, out var passed) && passed,
            StringComparer.Ordinal);

        if (string.Equals(observation.Status, "blocked", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0014",
                "android-smoke",
                string.IsNullOrWhiteSpace(observation.Reason)
                    ? "Android device or emulator is required for device smoke."
                    : observation.Reason));
        }

        foreach (var failed in criteria.Where(item => !item.Value))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0015",
                "android-smoke",
                $"Android device smoke criterion '{failed.Key}' did not pass for package '{plan.OutputDirectory}'."));
        }

        var status = diagnostics.Any(diagnostic => diagnostic.Code == "E2D-EXPORT-ANDROID-0014")
            ? "blocked"
            : diagnostics.Count == 0 ? "passed" : "failed";
        WriteArtifact(artifactPath, plan, observation, generatedAtUtc, status, criteria, diagnostics);
        return new Electron2DAndroidDeviceSmokeResult(artifactPath, observation.DeviceSerial, status, criteria, diagnostics);
    }

    private static void WriteArtifact(
        string artifactPath,
        Electron2DAndroidExportPlan plan,
        Electron2DAndroidDeviceSmokeObservation observation,
        DateTimeOffset generatedAtUtc,
        string status,
        IReadOnlyDictionary<string, bool> criteria,
        IReadOnlyList<Electron2DExportDiagnostic> diagnostics)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.AndroidDeviceSmokeArtifact",
            ["formatVersion"] = 1,
            ["generatedAtUtc"] = generatedAtUtc.UtcDateTime.ToString("O"),
            ["status"] = status,
            ["deviceSerial"] = observation.DeviceSerial,
            ["outputDirectory"] = plan.OutputDirectory,
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["abi"] = plan.Abi,
            ["packageFormat"] = plan.PackageFormat,
            ["runtimePolicies"] = new JsonObject
            {
                ["orientation"] = plan.Orientation,
                ["mobileGraphicsProfile"] = plan.MobileGraphicsProfile,
                ["fallbackPolicy"] = plan.FallbackPolicy
            },
            ["criteria"] = WriteCriteria(criteria),
            ["diagnostics"] = WriteDiagnostics(diagnostics)
        };

        var directory = Path.GetDirectoryName(artifactPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(artifactPath, root.ToJsonString(IndentedJsonOptions).Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private static JsonObject WriteCriteria(IReadOnlyDictionary<string, bool> criteria)
    {
        var result = new JsonObject();
        foreach (var item in criteria.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            result[item.Key] = new JsonObject
            {
                ["passed"] = item.Value
            };
        }

        return result;
    }

    private static JsonArray WriteDiagnostics(IEnumerable<Electron2DExportDiagnostic> diagnostics)
    {
        var result = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            result.Add(new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["message"] = diagnostic.Message,
                ["severity"] = diagnostic.Severity.ToString(),
                ["presetName"] = diagnostic.PresetName
            });
        }

        return result;
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
