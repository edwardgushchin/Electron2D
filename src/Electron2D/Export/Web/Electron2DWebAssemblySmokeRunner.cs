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

internal static class Electron2DWebAssemblySmokeRunner
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static Electron2DWebAssemblySmokeResult Run(
        Electron2DWebAssemblyExportPlan plan,
        string artifactPath,
        Uri launchUrl,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);
        ArgumentNullException.ThrowIfNull(launchUrl);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        var manifest = TryReadManifest(plan.WebManifestPath, diagnostics);
        var index = TryReadText(plan.IndexHtmlPath, diagnostics);
        var loader = TryReadText(plan.LoaderScriptPath, diagnostics);
        var mainScene = manifest?["mainScene"]?.GetValue<string>() ?? string.Empty;
        var projectFile = manifest?["projectFile"]?.GetValue<string>() ?? "project.e2d.json";
        var criteria = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["startup"] = File.Exists(plan.IndexHtmlPath) && File.Exists(plan.LoaderScriptPath),
            ["sceneLoad"] = !string.IsNullOrWhiteSpace(mainScene) && File.Exists(Path.Combine(plan.WebRootDirectory, mainScene)),
            ["renderingReadiness"] = index.Contains("electron2d-canvas", StringComparison.Ordinal) &&
                loader.Contains("renderingReady", StringComparison.Ordinal),
            ["inputEventPath"] = loader.Contains("pointerdown", StringComparison.Ordinal) &&
                loader.Contains("keydown", StringComparison.Ordinal),
            ["audioPolicyState"] = string.Equals(manifest?["audioPolicy"]?.GetValue<string>(), "userGestureRequired", StringComparison.Ordinal) &&
                loader.Contains("userGestureRequired", StringComparison.Ordinal),
            ["resourceLoading"] = File.Exists(plan.WebManifestPath) &&
                File.Exists(Path.Combine(plan.WebRootDirectory, projectFile)) &&
                !string.IsNullOrWhiteSpace(mainScene),
            ["saveDataPolicy"] = string.Equals(manifest?["filesystemPolicy"]?.GetValue<string>(), "browserSandbox", StringComparison.Ordinal) &&
                loader.Contains("localStorage", StringComparison.Ordinal)
        };

        foreach (var failed in criteria.Where(item => !item.Value))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0013",
                "web-smoke",
                $"WebAssembly browser smoke criterion '{failed.Key}' failed for package '{plan.WebRootDirectory}'."));
        }

        var status = diagnostics.Count == 0 ? "passed" : "failed";
        WriteArtifact(artifactPath, plan, launchUrl, generatedAtUtc, status, criteria, diagnostics);
        return new Electron2DWebAssemblySmokeResult(artifactPath, launchUrl, status, criteria, diagnostics);
    }

    private static JsonObject? TryReadManifest(string path, List<Electron2DExportDiagnostic> diagnostics)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0011",
                "web-smoke",
                $"WebAssembly browser package manifest could not be read: {exception.Message}"));
            return null;
        }
    }

    private static string TryReadText(string path, List<Electron2DExportDiagnostic> diagnostics)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0011",
                "web-smoke",
                $"WebAssembly browser package file '{path}' could not be read: {exception.Message}"));
            return string.Empty;
        }
    }

    private static void WriteArtifact(
        string artifactPath,
        Electron2DWebAssemblyExportPlan plan,
        Uri launchUrl,
        DateTimeOffset generatedAtUtc,
        string status,
        IReadOnlyDictionary<string, bool> criteria,
        IReadOnlyList<Electron2DExportDiagnostic> diagnostics)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.WebAssemblySmokeArtifact",
            ["formatVersion"] = 1,
            ["generatedAtUtc"] = generatedAtUtc.UtcDateTime.ToString("O"),
            ["status"] = status,
            ["launchUrl"] = launchUrl.ToString(),
            ["webRootDirectory"] = plan.WebRootDirectory,
            ["browser"] = new JsonObject
            {
                ["name"] = "manual-or-automation",
                ["version"] = "not-started"
            },
            ["runtimePolicies"] = new JsonObject
            {
                ["audioPolicy"] = plan.AudioPolicy,
                ["filesystemPolicy"] = plan.FilesystemPolicy
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
