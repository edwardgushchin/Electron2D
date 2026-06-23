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

internal static class Electron2DWebAssemblyPackageBuilder
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static Electron2DWebAssemblyPackageBuildResult Build(
        Electron2DWebAssemblyExportPlan plan,
        string projectRoot,
        Electron2DProjectSettings projectSettings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(projectSettings);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        var files = new List<string>();
        try
        {
            Directory.CreateDirectory(plan.WebRootDirectory);
            Directory.CreateDirectory(plan.FrameworkDirectory);
            Directory.CreateDirectory(plan.AssetsDirectory);

            WriteText(plan.IndexHtmlPath, CreateIndexHtml(projectSettings, plan));
            files.Add("index.html");
            WriteText(plan.LoaderScriptPath, CreateLoaderScript());
            files.Add("electron2d.loader.js");

            CopyProjectSettings(projectRoot, plan, files, diagnostics);
            CopyMainScene(projectRoot, plan.WebRootDirectory, projectSettings.MainScene, files, diagnostics);
            CopyAssets(projectRoot, plan.AssetsDirectory, files, diagnostics);

            WriteText(plan.WebManifestPath, CreateManifest(projectSettings, plan, files));
            files.Add("electron2d.webmanifest.json");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0010",
                "web-package",
                $"WebAssembly browser package could not be written: {exception.Message}"));
        }

        return new Electron2DWebAssemblyPackageBuildResult(files, diagnostics);
    }

    private static string CreateIndexHtml(Electron2DProjectSettings settings, Electron2DWebAssemblyExportPlan plan)
    {
        var title = EscapeHtml(settings.Name);
        var mainScene = EscapeHtml(NormalizePortablePath(settings.MainScene));
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="dark light">
              <link rel="manifest" href="electron2d.webmanifest.json">
              <title>{{title}}</title>
              <style>
                html, body {
                  width: 100%;
                  height: 100%;
                  margin: 0;
                  background: #111318;
                  color: #f5f7fb;
                  overflow: hidden;
                  font-family: system-ui, sans-serif;
                }

                #electron2d-root, #electron2d-canvas {
                  width: 100%;
                  height: 100%;
                  display: block;
                }

                #electron2d-status {
                  position: fixed;
                  left: 16px;
                  bottom: 16px;
                  padding: 6px 8px;
                  border-radius: 4px;
                  background: rgb(0 0 0 / 60%);
                  font-size: 12px;
                }
              </style>
            </head>
            <body>
              <main id="electron2d-root" data-main-scene="{{mainScene}}" data-renderer-profile="{{plan.RendererProfile}}">
                <canvas id="electron2d-canvas" aria-label="{{title}}"></canvas>
                <div id="electron2d-status" role="status">Loading Electron2D...</div>
              </main>
              <script src="electron2d.loader.js"></script>
            </body>
            </html>
            """;
    }

    private static string CreateLoaderScript()
    {
        return """
            (() => {
              "use strict";

              const state = {
                startup: false,
                manifestLoaded: false,
                projectLoaded: false,
                sceneLoaded: false,
                renderingReady: false,
                inputEvents: [],
                audioPolicy: {
                  state: "locked",
                  userGestureRequired: true
                },
                filesystemPolicy: {
                  state: "browserSandbox",
                  persistentAvailable: false,
                  fallback: "memoryOnly"
                },
                diagnostics: []
              };

              async function readJson(path) {
                const response = await fetch(path, { cache: "no-store" });
                if (!response.ok) {
                  throw new Error(`Failed to load ${path}: ${response.status}`);
                }

                return await response.json();
              }

              function updateStatus(text) {
                const status = document.getElementById("electron2d-status");
                if (status) {
                  status.textContent = text;
                }
              }

              function recordInput(kind, event) {
                state.inputEvents.push({
                  kind,
                  type: event.type,
                  timeStamp: event.timeStamp
                });
              }

              document.addEventListener("pointerdown", event => recordInput("pointer", event), { passive: true });
              document.addEventListener("keydown", event => recordInput("keyboard", event), { passive: true });
              document.addEventListener("visibilitychange", () => {
                state.visibilityState = document.visibilityState;
              });

              async function detectStorage() {
                try {
                  const key = "electron2d.web.smoke";
                  window.localStorage.setItem(key, "1");
                  window.localStorage.removeItem(key);
                  state.filesystemPolicy.persistentAvailable = true;
                } catch {
                  state.filesystemPolicy.persistentAvailable = false;
                  state.filesystemPolicy.fallback = "memoryOnly";
                }
              }

              async function boot() {
                if (state.startup) {
                  return state;
                }

                state.startup = true;
                updateStatus("Loading package...");
                try {
                  const manifest = await readJson("electron2d.webmanifest.json");
                  state.manifestLoaded = true;
                  await readJson(manifest.projectFile || "project.e2d.json");
                  state.projectLoaded = true;
                  await readJson(manifest.mainScene);
                  state.sceneLoaded = true;
                  await detectStorage();
                  state.renderingReady = document.getElementById("electron2d-canvas") !== null;
                  updateStatus(state.renderingReady ? "Ready" : "Canvas missing");
                  window.dispatchEvent(new CustomEvent("electron2d:ready", { detail: state }));
                } catch (error) {
                  state.diagnostics.push(String(error && error.message ? error.message : error));
                  updateStatus("Failed");
                  window.dispatchEvent(new CustomEvent("electron2d:error", { detail: state }));
                }

                return state;
              }

              window.Electron2DWebRuntime = {
                boot,
                state
              };

              window.Electron2DWebRuntimeSmoke = {
                async run() {
                  await boot();
                  return {
                    startup: state.startup,
                    sceneLoad: state.sceneLoaded,
                    renderingReadiness: state.renderingReady,
                    inputEventPath: Array.isArray(state.inputEvents),
                    audioPolicyState: state.audioPolicy.userGestureRequired === true,
                    resourceLoading: state.manifestLoaded && state.projectLoaded && state.sceneLoaded,
                    saveDataPolicy: state.filesystemPolicy.state === "browserSandbox",
                    diagnostics: state.diagnostics.slice()
                  };
                }
              };

              if (document.readyState === "loading") {
                document.addEventListener("DOMContentLoaded", () => void boot(), { once: true });
              } else {
                void boot();
              }
            })();
            """;
    }

    private static string CreateManifest(
        Electron2DProjectSettings settings,
        Electron2DWebAssemblyExportPlan plan,
        IReadOnlyList<string> files)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.WebAssemblyPackage",
            ["formatVersion"] = FormatVersion,
            ["projectName"] = settings.Name,
            ["projectVersion"] = settings.ProjectVersion,
            ["engineVersion"] = settings.EngineVersion,
            ["projectFile"] = plan.ProjectSettingsPackagePath,
            ["mainScene"] = NormalizePortablePath(settings.MainScene),
            ["rendererProfile"] = plan.RendererProfile.ToString(),
            ["graphicsBackend"] = plan.GraphicsBackend,
            ["audioPolicy"] = plan.AudioPolicy,
            ["filesystemPolicy"] = plan.FilesystemPolicy,
            ["browserPolicies"] = WriteStringArray(plan.BrowserPolicies),
            ["smokeCriteria"] = WriteStringArray(plan.SmokeCriteria),
            ["packageFiles"] = WriteStringArray(files.OrderBy(path => path, StringComparer.Ordinal))
        };

        return root.ToJsonString(IndentedJsonOptions);
    }

    private static void CopyProjectSettings(
        string projectRoot,
        Electron2DWebAssemblyExportPlan plan,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var source = Path.GetFullPath(plan.ProjectSettingsPath);
        var normalizedRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        if (!source.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(Error("E2D-EXPORT-WEB-0012", "web-package", "Project settings path must stay inside the project root."));
            return;
        }

        if (!File.Exists(source))
        {
            diagnostics.Add(Error("E2D-EXPORT-WEB-0011", "web-package", $"Project settings file {Path.GetFileName(source)} was not found."));
            return;
        }

        if (plan.ProjectSettingsPackagePath.StartsWith("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(plan.ProjectSettingsPackagePath))
        {
            diagnostics.Add(Error("E2D-EXPORT-WEB-0012", "web-package", "Project settings package path must stay inside the web package root."));
            return;
        }

        CopyFile(source, Path.Combine(plan.WebRootDirectory, plan.ProjectSettingsPackagePath));
        files.Add(plan.ProjectSettingsPackagePath);
    }

    private static void CopyMainScene(
        string projectRoot,
        string webRootDirectory,
        string mainScene,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        if (!TryResolveProjectFile(projectRoot, mainScene, out var source))
        {
            diagnostics.Add(Error("E2D-EXPORT-WEB-0012", "web-package", $"Main scene path '{mainScene}' must stay inside the project root."));
            return;
        }

        if (!File.Exists(source))
        {
            diagnostics.Add(Error("E2D-EXPORT-WEB-0011", "web-package", $"Main scene file '{NormalizePortablePath(mainScene)}' was not found."));
            return;
        }

        var relative = NormalizePortablePath(mainScene);
        CopyFile(source, Path.Combine(webRootDirectory, relative));
        files.Add(relative);
    }

    private static void CopyAssets(string projectRoot, string assetsDirectory, List<string> files, List<Electron2DExportDiagnostic> diagnostics)
    {
        var sourceRoot = Path.Combine(projectRoot, "assets");
        if (!Directory.Exists(sourceRoot))
        {
            return;
        }

        foreach (var source in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativeToAssets = Path.GetRelativePath(sourceRoot, source);
            if (relativeToAssets.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativeToAssets))
            {
                diagnostics.Add(Error("E2D-EXPORT-WEB-0012", "web-package", "Asset path must stay inside the project assets directory."));
                continue;
            }

            var relative = "assets/" + NormalizePortablePath(relativeToAssets);
            CopyFile(source, Path.Combine(assetsDirectory, relativeToAssets));
            files.Add(relative);
        }
    }

    private static bool TryResolveProjectFile(string projectRoot, string relativePath, out string fullPath)
    {
        fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        var normalizedRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyFile(string source, string destination)
    {
        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(source, destination, overwrite: true);
    }

    private static void WriteText(string path, string text)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, text.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var result = new JsonArray();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private static string NormalizePortablePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
