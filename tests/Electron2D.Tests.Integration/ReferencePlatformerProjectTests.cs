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

public sealed class ReferencePlatformerProjectTests
{
    [Fact]
    public void ReferencePlatformerProjectHasEditorProjectContract()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "reference-platformer");

        foreach (var relativePath in RequiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(projectRoot, relativePath)),
                $"Reference platformer is missing required file: {relativePath}");
        }

        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json"));
        Assert.True(settings.Succeeded, FormatSettingsDiagnostics(settings.Diagnostics));
        Assert.NotNull(settings.Settings);
        Assert.Equal("Electron2D.ReferencePlatformer", settings.Settings.Name);
        Assert.Equal("scenes/main.scene.json", settings.Settings.MainScene);
        Assert.Equal(
            new[] { "jump", "move_left", "move_right", "pause" },
            settings.Settings.InputActions.Select(action => action.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

        var exportPresets = Electron2D.Electron2DExportPresetStore.Load(Path.Combine(projectRoot, "export_presets.e2export.json"));
        Assert.True(exportPresets.Succeeded, FormatExportDiagnostics(exportPresets.Diagnostics));
        Assert.NotNull(exportPresets.Document);
        Assert.Equal(
            [
                "AndroidArm64",
                "IosArm64",
                "LinuxX64",
                "MacOSArm64",
                "WebAssemblyBrowser",
                "WindowsX64"
            ],
            exportPresets.Document.Presets
                .Select(preset => preset.Target.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(target => target, StringComparer.Ordinal)
                .ToArray());

        var taskBoard = ReadJson(Path.Combine(projectRoot, ".electron2d", "tasks", "board.e2tasks"));
        Assert.Equal("Electron2D.TaskBoard", taskBoard.RootElement.GetProperty("format").GetString());
        Assert.DoesNotContain(
            Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories),
            path => Path.GetFileName(path).Equals("TASKS.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReferencePlatformerScriptCoversRequiredGameplaySubsystems()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(root, "examples", "reference-platformer", "Scripts", "PlatformerGame.cs");

        Assert.True(File.Exists(scriptPath), $"Reference platformer script is missing: {scriptPath}");

        var script = File.ReadAllText(scriptPath);
        foreach (var requiredText in new[]
        {
            "TileMapLayer",
            "TileSetAtlasSource",
            "CharacterBody2D",
            "MoveAndSlide",
            "Camera2D",
            "MakeCurrent",
            "SpriteFrames",
            "AnimatedSprite2D",
            "AudioStreamPlayer",
            "Input.GetVector",
            "Input.IsActionJustPressed",
            "InputEventScreenTouch",
            "InputEventScreenDrag",
            "SaveProgress"
        })
        {
            Assert.Contains(requiredText, script, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReferencePlatformerVerifierExists()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-ReferencePlatformer.ps1");

        Assert.True(File.Exists(verifierPath), $"Reference platformer verifier is missing: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("Electron2D.ReferencePlatformer.csproj", verifier, StringComparison.Ordinal);
        Assert.Contains("dotnet build", verifier, StringComparison.Ordinal);
        Assert.Contains("dotnet run", verifier, StringComparison.Ordinal);
        Assert.Contains("e2d validate", verifier, StringComparison.Ordinal);
        Assert.Contains(".electron2d/tasks", verifier, StringComparison.Ordinal);
    }

    private static readonly string[] RequiredFiles =
    [
        "Electron2D.ReferencePlatformer.csproj",
        "Program.cs",
        "Scripts/PlatformerGame.cs",
        "project.e2d.json",
        "electron2d.lock.json",
        "global.json",
        "export_presets.e2export.json",
        "scenes/main.scene.json",
        "resources/reference-platformer.manifest.json",
        ".electron2d/tasks/board.e2tasks",
        ".electron2d/tasks/reference-platformer-acceptance.e2task"
    ];

    private static JsonDocument ReadJson(string path)
    {
        Assert.True(File.Exists(path), $"JSON file is missing: {path}");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string FormatSettingsDiagnostics(IEnumerable<Electron2D.Electron2DSettingsDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static string FormatExportDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
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
