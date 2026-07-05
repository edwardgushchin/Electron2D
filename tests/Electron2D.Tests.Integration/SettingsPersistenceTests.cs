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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SettingsPersistenceTests
{
    [Fact]
    public void ProjectSettingsRoundTripsProjectInputAndDisplayState()
    {
        ResetRuntimeState();
        var directory = CreateTemporaryDirectory("electron2d-project-settings-");
        var path = Path.Combine(directory, "project.e2d.json");

        try
        {
            Electron2D.InputMap.AddAction("jump", 0.25f);
            Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });
            Electron2D.InputMap.ActionAddEvent(
                "jump",
                new Electron2D.InputEventJoypadMotion
                {
                    Axis = Electron2D.JoyAxis.LeftX,
                    AxisValue = 1f
                });

            var settings = Electron2D.Electron2DProjectSettings.Capture(
                name: "Sample",
                projectVersion: "0.1.0",
                engineVersion: "0.1-preview",
                mainScene: "scenes/main.scene.json");
            settings.RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard;
            settings.PhysicsTicksPerSecond = 120;
            settings.Display = new Electron2D.Electron2DDisplaySettings
            {
                WindowSize = new Electron2D.Vector2I(1280, 720),
                Fullscreen = true,
                DpiScale = 2f,
                StretchMode = Electron2D.ViewportStretchMode.Viewport,
                StretchAspect = Electron2D.ViewportStretchAspect.Expand,
                StretchScaleMode = Electron2D.ViewportStretchScaleMode.Integer,
                StretchScale = 1.5f,
                Orientation = Electron2D.DisplayServer.ScreenOrientation.Portrait,
                SafeArea = new Electron2D.Rect2I(0, 24, 1280, 672)
            };

            Electron2D.Electron2DSettingsStore.SaveProject(path, settings);

            Electron2D.InputMap.ClearForTests();
            Electron2D.DisplayServer.ResetForTests();

            var result = Electron2D.Electron2DSettingsStore.LoadProject(path);

            Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
            Assert.Empty(result.Diagnostics);
            Assert.NotNull(result.Settings);

            var loaded = result.Settings;
            Assert.Equal("Sample", loaded.Name);
            Assert.Equal("0.1.0", loaded.ProjectVersion);
            Assert.Equal("0.1-preview", loaded.EngineVersion);
            Assert.Equal("scenes/main.scene.json", loaded.MainScene);
            Assert.Equal(Electron2D.Electron2DRendererProfileSetting.Standard, loaded.RendererProfile);
            Assert.Equal(120, loaded.PhysicsTicksPerSecond);
            Assert.Equal(new Electron2D.Vector2I(1280, 720), loaded.Display.WindowSize);
            Assert.True(loaded.Display.Fullscreen);
            Assert.Equal(2f, loaded.Display.DpiScale);
            Assert.Equal(Electron2D.ViewportStretchMode.Viewport, loaded.Display.StretchMode);
            Assert.Equal(Electron2D.ViewportStretchAspect.Expand, loaded.Display.StretchAspect);
            Assert.Equal(Electron2D.ViewportStretchScaleMode.Integer, loaded.Display.StretchScaleMode);
            Assert.Equal(1.5f, loaded.Display.StretchScale);
            Assert.Equal(Electron2D.DisplayServer.ScreenOrientation.Portrait, loaded.Display.Orientation);
            Assert.Equal(new Electron2D.Rect2I(0, 24, 1280, 672), loaded.Display.SafeArea);

            loaded.ApplyToRuntime();

            Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space }, "jump"));
            Assert.True(Electron2D.InputMap.EventIsAction(
                new Electron2D.InputEventJoypadMotion
                {
                    Axis = Electron2D.JoyAxis.LeftX,
                    AxisValue = 0.75f
                },
                "jump"));
            Assert.Equal(Electron2D.DisplayServer.ScreenOrientation.Portrait, Electron2D.DisplayServer.ScreenGetOrientation());
            Assert.Equal(new Electron2D.Rect2I(0, 24, 1280, 672), Electron2D.DisplayServer.GetDisplaySafeArea());
        }
        finally
        {
            ResetRuntimeState();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UserSettingsRoundTripsLocaleRecentProjectsAndWindowState()
    {
        var directory = CreateTemporaryDirectory("electron2d-user-settings-");
        var path = Path.Combine(directory, "user.e2settings.json");

        try
        {
            var settings = new Electron2D.Electron2DUserSettings
            {
                Locale = "fr-ca",
                LastProjectPath = "projects/card-game",
                RecentProjects =
                [
                    "projects/card-game",
                    "",
                    "projects/platformer",
                    "projects/card-game"
                ],
                Window = new Electron2D.Electron2DUserWindowSettings
                {
                    Position = new Electron2D.Vector2I(32, 48),
                    Size = new Electron2D.Vector2I(1440, 900),
                    Maximized = true
                }
            };

            Electron2D.Electron2DSettingsStore.SaveUser(path, settings);

            var result = Electron2D.Electron2DSettingsStore.LoadUser(path);

            Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
            Assert.Empty(result.Diagnostics);
            Assert.NotNull(result.Settings);
            Assert.Equal("fr_CA", result.Settings.Locale);
            Assert.Equal("projects/card-game", result.Settings.LastProjectPath);
            Assert.Equal(new[] { "projects/card-game", "projects/platformer" }, result.Settings.RecentProjects);
            Assert.Equal(new Electron2D.Vector2I(32, 48), result.Settings.Window.Position);
            Assert.Equal(new Electron2D.Vector2I(1440, 900), result.Settings.Window.Size);
            Assert.True(result.Settings.Window.Maximized);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CorruptProjectSettingsFailClosedWithoutReplacingInputMap()
    {
        ResetRuntimeState();
        var directory = CreateTemporaryDirectory("electron2d-corrupt-project-settings-");
        var path = Path.Combine(directory, "project.e2d.json");

        try
        {
            Electron2D.InputMap.AddAction("keep", 0.1f);
            Electron2D.InputMap.ActionAddEvent("keep", new Electron2D.InputEventKey { Keycode = Electron2D.Key.K });
            Electron2D.DisplayServer.ScreenSetOrientation(Electron2D.DisplayServer.ScreenOrientation.ReverseLandscape);
            File.WriteAllText(path, "{ this is not json");

            var result = Electron2D.Electron2DSettingsStore.LoadProjectAndApply(path);

            Assert.False(result.Succeeded);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "settings.malformed_json");
            Assert.Null(result.Settings);
            Assert.True(Electron2D.InputMap.HasAction("keep"));
            Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.K }, "keep"));
            Assert.Equal(Electron2D.DisplayServer.ScreenOrientation.ReverseLandscape, Electron2D.DisplayServer.ScreenGetOrientation());
        }
        finally
        {
            ResetRuntimeState();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ProjectSettingsRejectRuntimeOnlyActionEventsInsteadOfDroppingThem()
    {
        var directory = CreateTemporaryDirectory("electron2d-runtime-only-input-settings-");
        var path = Path.Combine(directory, "project.e2d.json");

        try
        {
            var settings = Electron2D.Electron2DProjectSettings.Capture(
                name: "Sample",
                projectVersion: "0.1.0",
                engineVersion: "0.1-preview",
                mainScene: "scenes/main.scene.json");
            settings.InputActions =
            [
                new Electron2D.InputMapActionSnapshot(
                    "runtime_only",
                    0.5f,
                    [new Electron2D.InputEventAction { Action = "runtime_only", Pressed = true, Strength = 1f }])
            ];

            var exception = Assert.Throws<FormatException>(() => Electron2D.Electron2DSettingsStore.SaveProject(path, settings));

            Assert.Contains("cannot be persisted", exception.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CorruptUserSettingsFailClosedWithoutChangingLocale()
    {
        var originalLocale = Electron2D.TranslationServer.GetLocale();
        var directory = CreateTemporaryDirectory("electron2d-corrupt-user-settings-");
        var path = Path.Combine(directory, "user.e2settings.json");

        try
        {
            Electron2D.TranslationServer.SetLocale("en-us");
            File.WriteAllText(path, "[");

            var result = Electron2D.Electron2DSettingsStore.LoadUserAndApply(path);

            Assert.False(result.Succeeded);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "settings.malformed_json");
            Assert.Null(result.Settings);
            Assert.Equal("en_US", Electron2D.TranslationServer.GetLocale());
        }
        finally
        {
            Electron2D.TranslationServer.SetLocale(originalLocale);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FormatDiagnostics(IEnumerable<Electron2D.Electron2DSettingsDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static void ResetRuntimeState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();
        Electron2D.DisplayServer.ResetForTests();
    }
}
