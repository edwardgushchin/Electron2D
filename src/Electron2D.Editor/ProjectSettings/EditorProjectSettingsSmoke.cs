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
using Electron2D.Editor.ProjectManagement;
using Electron2D.Editor.Shell;

namespace Electron2D.Editor.ProjectSettings;

internal static class EditorProjectSettingsSmoke
{
    private const string ProjectName = "ProjectSettingsSmoke";
    private const string UpdatedMainScene = "scenes/settings-smoke.scene.json";

    public static EditorProjectSettingsSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);
        var runRoot = Path.Combine(fullWorkRoot, "run-" + Guid.NewGuid().ToString("N"));
        var visualRoot = Path.Combine(fullWorkRoot, "visual");
        Directory.CreateDirectory(runRoot);
        Directory.CreateDirectory(visualRoot);

        var repositoryRoot = FindRepositoryRoot();
        var templateRoot = Path.Combine(repositoryRoot, "data", "templates", "electron2d-empty");
        var manager = new EditorProjectManager(templateRoot);
        var creation = manager.CreateProject(new EditorProjectCreateOptions(
            ProjectName,
            Path.Combine(runRoot, "projects"),
            Electron2DRendererProfileSetting.Compatibility));

        var userSettingsPath = Path.Combine(runRoot, "user", "user.e2settings.json");
        var openResult = manager.OpenProject(creation.ProjectPath, userSettingsPath);
        if (!openResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, openResult.Diagnostics));
        }

        CreateUpdatedMainScene(creation.ProjectPath);
        var settings = LoadProjectSettings(creation.ProjectSettingsPath);
        settings.MainScene = UpdatedMainScene;
        settings.RendererProfile = Electron2DRendererProfileSetting.Standard;
        settings.PhysicsTicksPerSecond = 120;
        settings.Display = new Electron2DDisplaySettings
        {
            WindowSize = new Vector2I(960, 540),
            Fullscreen = false,
            DpiScale = 1f,
            StretchMode = ViewportStretchMode.Viewport,
            StretchAspect = ViewportStretchAspect.Keep,
            StretchScaleMode = ViewportStretchScaleMode.Fractional,
            StretchScale = 1f,
            Orientation = DisplayServer.ScreenOrientation.Landscape,
            SafeArea = new Rect2I(0, 0, 960, 540)
        };
        settings.InputActions =
        [
            new InputMapActionSnapshot(
                "jump",
                0.2f,
                [new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = true }]),
            new InputMapActionSnapshot(
                "dash",
                0.2f,
                [new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true }])
        ];

        Electron2DSettingsStore.SaveProject(creation.ProjectSettingsPath, settings);
        var exportPresetsPath = Path.Combine(creation.ProjectPath, "export_presets.e2export.json");
        Electron2DExportPresetStore.Save(exportPresetsPath, CreateExportPresetDocument());

        var loadedSettings = LoadProjectSettings(creation.ProjectSettingsPath);
        var loadedPresetsResult = Electron2DExportPresetStore.Load(exportPresetsPath);
        if (!loadedPresetsResult.Succeeded || loadedPresetsResult.Document is null)
        {
            throw new InvalidOperationException(FormatExportDiagnostics(loadedPresetsResult.Diagnostics));
        }

        var projectSettingsWritten = loadedSettings.MainScene == UpdatedMainScene &&
            loadedSettings.RendererProfile == Electron2DRendererProfileSetting.Standard &&
            loadedSettings.PhysicsTicksPerSecond == 120 &&
            loadedSettings.Display.WindowSize == new Vector2I(960, 540);
        var inputMapRoundTrip = loadedSettings.InputActions
            .Select(action => action.Name)
            .Order(StringComparer.Ordinal)
            .SequenceEqual(["dash", "jump"]);
        var exportPresetNames = loadedPresetsResult.Document.Presets.Select(preset => preset.Name).ToArray();
        var exportPresetsRoundTrip = exportPresetNames.SequenceEqual([
            "android-release",
            "browser-debug",
            "ios-release",
            "linux-debug",
            "macos-release",
            "windows-debug"
        ]);

        var formattedInputActions = FormatInputActions(loadedSettings.InputActions);
        var snapshot = new EditorProjectSettingsVisualSnapshot(
            creation.ProjectPath,
            creation.ProjectSettingsPath,
            exportPresetsPath,
            loadedSettings.MainScene,
            loadedSettings.RendererProfile.ToString(),
            loadedSettings.PhysicsTicksPerSecond,
            $"{loadedSettings.Display.WindowSize.X}x{loadedSettings.Display.WindowSize.Y}",
            loadedSettings.Display.Fullscreen,
            formattedInputActions,
            string.Join('|', exportPresetNames));
        var visual = EditorProjectSettingsVisualHarness.WriteArtifacts(snapshot, visualRoot);
        var regions = EditorProjectSettingsVisualHarness.CreateVisualRegions(snapshot);
        var pointerInteractionObserved = EditorProjectSettingsVisualHarness.DispatchPointerSelection(regions);
        var keyboardInteractionObserved = EditorProjectSettingsVisualHarness.DispatchKeyboardSave();
        var window = EditorWindowSmoke.PresentCanvasForSmoke(visual.Canvas, smokeFrameCount: 4);
        var screenshotReviewed = visual.ScreenshotReviewed &&
            projectSettingsWritten &&
            inputMapRoundTrip &&
            exportPresetsRoundTrip &&
            window.WindowCreated &&
            window.WindowShown &&
            window.FramePresented &&
            pointerInteractionObserved &&
            keyboardInteractionObserved &&
            visual.TextOverflowCount == 0 &&
            visual.ForbiddenUiMatchCount == 0;

        EditorProjectSettingsVisualHarness.UpdateWindowAnalysis(
            visual.AnalysisPath,
            window,
            pointerInteractionObserved,
            keyboardInteractionObserved,
            screenshotReviewed);

        return new EditorProjectSettingsSmokeResult(
            creation.ProjectPath,
            creation.ProjectSettingsPath,
            exportPresetsPath,
            loadedSettings.MainScene,
            loadedSettings.RendererProfile.ToString(),
            loadedSettings.PhysicsTicksPerSecond,
            $"{loadedSettings.Display.WindowSize.X}x{loadedSettings.Display.WindowSize.Y}",
            loadedSettings.Display.Fullscreen,
            formattedInputActions,
            string.Join('|', exportPresetNames),
            projectSettingsWritten,
            inputMapRoundTrip,
            exportPresetsRoundTrip,
            window.WindowCreated,
            window.WindowShown,
            window.FramePresented,
            window.EventPumpObserved,
            pointerInteractionObserved,
            keyboardInteractionObserved,
            visual.TextOverflowCount,
            visual.ClickableControlCount,
            visual.ForbiddenUiMatchCount,
            screenshotReviewed,
            visual.ScreenshotPath,
            visual.AnalysisPath);
    }

    private static Electron2DProjectSettings LoadProjectSettings(string projectSettingsPath)
    {
        var result = Electron2DSettingsStore.LoadProject(projectSettingsPath);
        if (!result.Succeeded || result.Settings is null)
        {
            throw new InvalidOperationException(FormatSettingsDiagnostics(result.Diagnostics));
        }

        return result.Settings;
    }

    private static void CreateUpdatedMainScene(string projectPath)
    {
        var source = Path.Combine(projectPath, "scenes", "main.scene.json");
        var destination = Path.Combine(projectPath, UpdatedMainScene.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? projectPath);
        File.Copy(source, destination, overwrite: true);
    }

    private static Electron2DExportPresetDocument CreateExportPresetDocument()
    {
        return new Electron2DExportPresetDocument
        {
            Presets =
            [
                new Electron2DExportPreset
                {
                    Name = "windows-debug",
                    Target = Electron2DExportTarget.WindowsX64,
                    Configuration = Electron2DExportConfiguration.Debug,
                    RuntimeIdentifier = "win-x64",
                    SelfContained = true,
                    RendererProfile = Electron2DRendererProfileSetting.Compatibility,
                    OutputDirectory = "exports/windows",
                    IncludeDebugSymbols = true
                },
                new Electron2DExportPreset
                {
                    Name = "linux-debug",
                    Target = Electron2DExportTarget.LinuxX64,
                    Configuration = Electron2DExportConfiguration.Debug,
                    RuntimeIdentifier = "linux-x64",
                    SelfContained = true,
                    RendererProfile = Electron2DRendererProfileSetting.Standard,
                    OutputDirectory = "exports/linux",
                    IncludeDebugSymbols = true
                },
                new Electron2DExportPreset
                {
                    Name = "macos-release",
                    Target = Electron2DExportTarget.MacOSArm64,
                    Configuration = Electron2DExportConfiguration.Release,
                    RuntimeIdentifier = "osx-arm64",
                    SelfContained = true,
                    RendererProfile = Electron2DRendererProfileSetting.Standard,
                    OutputDirectory = "exports/macos"
                },
                new Electron2DExportPreset
                {
                    Name = "android-release",
                    Target = Electron2DExportTarget.AndroidArm64,
                    Configuration = Electron2DExportConfiguration.Release,
                    RuntimeIdentifier = "android-arm64",
                    SelfContained = true,
                    RendererProfile = Electron2DRendererProfileSetting.Automatic,
                    OutputDirectory = "exports/android",
                    Signing = new Electron2DExportSigningSettings
                    {
                        Required = true,
                        Identity = "android-release",
                        CredentialReference = "env:E2D_ANDROID_KEYSTORE"
                    }
                },
                new Electron2DExportPreset
                {
                    Name = "ios-release",
                    Target = Electron2DExportTarget.IosArm64,
                    Configuration = Electron2DExportConfiguration.Release,
                    RuntimeIdentifier = "ios-arm64",
                    SelfContained = true,
                    RendererProfile = Electron2DRendererProfileSetting.Standard,
                    OutputDirectory = "exports/ios",
                    Signing = new Electron2DExportSigningSettings
                    {
                        Required = true,
                        Identity = "ios-development",
                        CredentialReference = "env:E2D_IOS_CERTIFICATE"
                    }
                },
                new Electron2DExportPreset
                {
                    Name = "browser-debug",
                    Target = Electron2DExportTarget.WebAssemblyBrowser,
                    Configuration = Electron2DExportConfiguration.Debug,
                    RuntimeIdentifier = "browser-wasm",
                    SelfContained = true,
                    RendererProfile = Electron2DRendererProfileSetting.Automatic,
                    OutputDirectory = "exports/web",
                    IncludeDebugSymbols = true
                }
            ]
        };
    }

    private static string FormatInputActions(IEnumerable<InputMapActionSnapshot> actions)
    {
        var names = actions.Select(action => action.Name).ToArray();
        var preferred = new[] { "jump", "dash" };
        return string.Join('|', preferred
            .Where(name => names.Contains(name, StringComparer.Ordinal))
            .Concat(names.Except(preferred, StringComparer.Ordinal).Order(StringComparer.Ordinal)));
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

    private static string FormatSettingsDiagnostics(IEnumerable<Electron2DSettingsDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static string FormatExportDiagnostics(IEnumerable<Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
