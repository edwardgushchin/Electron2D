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
namespace Electron2D;

internal static class Electron2DWebAssemblyExportPlanner
{
    private static readonly string[] BrowserPolicies =
    [
        "staticHosting",
        "browserSandboxStorage",
        "audioRequiresUserGesture",
        "packageLocalResourcesOnly",
        "noRuntimeUserCodeLoading"
    ];

    private static readonly string[] SmokeCriteria =
    [
        "startup",
        "sceneLoad",
        "renderingReadiness",
        "inputEventPath",
        "audioPolicyState",
        "resourceLoading",
        "saveDataPolicy"
    ];

    public static Electron2DWebAssemblyExportPlanResult CreatePlan(
        Electron2DExportPreset preset,
        string projectFilePath,
        Electron2DProjectSettings? projectSettings)
    {
        return CreatePlan(preset, projectFilePath, projectSettings, string.Empty);
    }

    internal static Electron2DWebAssemblyExportPlanResult CreatePlan(
        Electron2DExportPreset preset,
        string projectFilePath,
        Electron2DProjectSettings? projectSettings,
        string projectSettingsPath)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        ValidatePreset(preset, diagnostics);
        ValidateProject(projectFilePath, projectSettings, preset.Name, diagnostics);

        if (diagnostics.Count > 0 || projectSettings is null)
        {
            return new Electron2DWebAssemblyExportPlanResult(null, diagnostics);
        }

        var outputDirectory = preset.OutputDirectory;
        var webRootDirectory = Path.Combine(outputDirectory, "wwwroot");
        var frameworkDirectory = Path.Combine(webRootDirectory, "_framework");
        var assetsDirectory = Path.Combine(webRootDirectory, "assets");
        var mainScene = NormalizePortablePath(projectSettings.MainScene);
        var resolvedProjectSettingsPath = ResolveProjectSettingsPath(projectFilePath, projectSettingsPath);
        var projectSettingsPackagePath = ResolveProjectSettingsPackagePath(projectFilePath, resolvedProjectSettingsPath);
        var plan = new Electron2DWebAssemblyExportPlan
        {
            ProjectFilePath = projectFilePath,
            ProjectSettingsPath = resolvedProjectSettingsPath,
            ProjectSettingsPackagePath = projectSettingsPackagePath,
            Configuration = preset.Configuration,
            RuntimeIdentifier = preset.RuntimeIdentifier,
            SelfContained = preset.SelfContained,
            OutputDirectory = outputDirectory,
            WebRootDirectory = webRootDirectory,
            FrameworkDirectory = frameworkDirectory,
            AssetsDirectory = assetsDirectory,
            IndexHtmlPath = Path.Combine(webRootDirectory, "index.html"),
            LoaderScriptPath = Path.Combine(webRootDirectory, "electron2d.loader.js"),
            WebManifestPath = Path.Combine(webRootDirectory, "electron2d.webmanifest.json"),
            PublishArguments =
            [
                "publish",
                projectFilePath,
                "--configuration",
                preset.Configuration.ToString(),
                "--runtime",
                preset.RuntimeIdentifier,
                "--self-contained",
                preset.SelfContained ? "true" : "false",
                "--output",
                frameworkDirectory
            ],
            RendererProfile = preset.RendererProfile,
            GraphicsBackend = GetGraphicsBackendName(preset.RendererProfile),
            RequiredFiles =
            [
                "index.html",
                "electron2d.loader.js",
                "electron2d.webmanifest.json",
                "_framework",
                "assets",
                projectSettingsPackagePath,
                mainScene
            ],
            BrowserPolicies = BrowserPolicies.ToArray(),
            SmokeCriteria = SmokeCriteria.ToArray(),
            IncludeDebugSymbols = preset.IncludeDebugSymbols,
            SigningRequired = preset.Signing.Required,
            AudioPolicy = "userGestureRequired",
            FilesystemPolicy = "browserSandbox"
        };

        return new Electron2DWebAssemblyExportPlanResult(plan, diagnostics);
    }

    private static void ValidatePreset(Electron2DExportPreset preset, List<Electron2DExportDiagnostic> diagnostics)
    {
        try
        {
            preset.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-PRESET-0002", preset.Name, exception.Message));
            return;
        }

        if (preset.Target != Electron2DExportTarget.WebAssemblyBrowser)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0002",
                preset.Name,
                $"WebAssembly browser export preset '{preset.Name}' must target WebAssemblyBrowser."));
        }

        if (!string.Equals(preset.RuntimeIdentifier, "browser-wasm", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0003",
                preset.Name,
                $"WebAssembly browser export preset '{preset.Name}' must use runtime identifier browser-wasm."));
        }

        if (!preset.SelfContained)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0004",
                preset.Name,
                $"WebAssembly browser export preset '{preset.Name}' must be self-contained."));
        }

        if (preset.Signing.Required)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0005",
                preset.Name,
                $"WebAssembly browser export preset '{preset.Name}' must not require signing."));
        }
    }

    private static void ValidateProject(
        string projectFilePath,
        Electron2DProjectSettings? projectSettings,
        string presetName,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0006",
                presetName,
                $"WebAssembly browser export preset '{presetName}' requires a project file path."));
        }

        if (projectSettings is null)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0007",
                presetName,
                $"WebAssembly browser export preset '{presetName}' requires project settings."));
            return;
        }

        try
        {
            projectSettings.Validate();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            diagnostics.Add(Error("E2D-EXPORT-WEB-0008", presetName, exception.Message));
        }
    }

    private static string GetGraphicsBackendName(Electron2DRendererProfileSetting rendererProfile)
    {
        return rendererProfile switch
        {
            Electron2DRendererProfileSetting.Standard => "web-standard",
            Electron2DRendererProfileSetting.Compatibility => "web-compatibility",
            Electron2DRendererProfileSetting.Automatic => "web-automatic",
            _ => "web-unknown"
        };
    }

    private static string NormalizePortablePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string ResolveProjectSettingsPath(string projectFilePath, string projectSettingsPath)
    {
        if (!string.IsNullOrWhiteSpace(projectSettingsPath))
        {
            return Path.GetFullPath(projectSettingsPath);
        }

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath)) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectDirectory, "project.e2d.json");
    }

    private static string ResolveProjectSettingsPackagePath(string projectFilePath, string projectSettingsPath)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath)) ?? Directory.GetCurrentDirectory();
        return NormalizePortablePath(Path.GetRelativePath(projectDirectory, projectSettingsPath));
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
