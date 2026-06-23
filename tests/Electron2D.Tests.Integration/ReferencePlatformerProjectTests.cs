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
using System.Diagnostics;
using System.IO.Compression;
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(RuntimeWindowCollection.Name)]
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

        Assert.False(File.Exists(Path.Combine(projectRoot, "project.e2d.json")), "Reference platformer must not keep the legacy project.e2d.json manifest.");
        Assert.False(File.Exists(Path.Combine(projectRoot, "Electron2D.ReferencePlatformer.csproj")), "Reference platformer project file must not include the engine namespace prefix.");
        Assert.False(File.Exists(Path.Combine(projectRoot, "electron2d.lock.json")), "Reference platformer lock data must be embedded in ReferencePlatformer.e2d.");
        Assert.False(File.Exists(Path.Combine(projectRoot, "export_presets.e2export.json")), "Reference platformer export presets must be embedded in ReferencePlatformer.e2d.");
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "bin")), "Reference platformer root must not keep build output bin/.");
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "obj")), "Reference platformer root must not keep build output obj/.");
        Assert.False(ExactDirectoryExists(projectRoot, "Scripts"), "Reference platformer source folder must use lower-case scripts/.");

        var projectManifestPath = Path.Combine(projectRoot, "ReferencePlatformer.e2d");
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(projectManifestPath);
        Assert.True(settings.Succeeded, FormatSettingsDiagnostics(settings.Diagnostics));
        Assert.NotNull(settings.Settings);
        Assert.Equal("ReferencePlatformer", settings.Settings.Name);
        Assert.Equal("scenes/main.scene.json", settings.Settings.MainScene);
        Assert.Equal(
            new[] { "jump", "move_left", "move_right", "pause" },
            settings.Settings.InputActions.Select(action => action.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

        using var projectManifest = JsonDocument.Parse(File.ReadAllText(projectManifestPath));
        Assert.True(projectManifest.RootElement.TryGetProperty("exportPresets", out _), "ReferencePlatformer.e2d must embed export presets.");
        Assert.True(projectManifest.RootElement.TryGetProperty("reproducibilityLock", out _), "ReferencePlatformer.e2d must embed reproducibility lock data.");

        var exportPresets = Electron2D.ExportPresetStore.LoadFromProjectFile(projectManifestPath);
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

        using var scene = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")));
        var sceneRoot = scene.RootElement.GetProperty("root");
        Assert.True(sceneRoot.TryGetProperty("scriptProperties", out var scriptProperties), "Scene must serialize exported script node paths.");
        foreach (var requiredScriptProperty in new[]
        {
            "Ground",
            "Player",
            "Camera",
            "PlayerSprite",
            "PlayerTimeline",
            "JumpAudio",
            "CheckpointAudio",
            "PauseMenu",
            "StatusLabel",
            "ResumeButton"
        })
        {
            Assert.True(scriptProperties.TryGetProperty(requiredScriptProperty, out _), $"Scene is missing script property: {requiredScriptProperty}");
        }

        var sceneNodes = FlattenSceneNodes(sceneRoot).ToArray();
        foreach (var requiredNode in new[]
        {
            ("TileMapLayer", "PlatformTileMap"),
            ("CharacterBody2D", "Player"),
            ("CollisionShape2D", "PlayerCollider"),
            ("AnimatedSprite2D", "PlayerSprite"),
            ("Camera2D", "PlayerCamera"),
            ("Sprite2D", "CheckpointStar"),
            ("TextureRect", "BackgroundTexture"),
            ("Panel", "HudPanel"),
            ("Label", "TitleLabel"),
            ("Label", "StatusLabel"),
            ("Panel", "PauseMenu"),
            ("Button", "ResumeButton")
        })
        {
            Assert.Contains(sceneNodes, node =>
                string.Equals(node.Type, requiredNode.Item1, StringComparison.Ordinal) &&
                string.Equals(node.Name, requiredNode.Item2, StringComparison.Ordinal));
        }

        Assert.Contains(sceneNodes, node =>
            node.Name == "PlatformTileMap" &&
            node.Element.TryGetProperty("tileSet", out _) &&
            node.Element.TryGetProperty("cells", out _));
        Assert.Contains(sceneNodes, node =>
            node.Name == "PlatformTileMap" &&
            node.Element.TryGetProperty("visible", out var visible) &&
            visible.ValueKind == JsonValueKind.False);
        Assert.Contains(sceneNodes, node =>
            node.Name == "PlayerCollider" &&
            node.Element.TryGetProperty("shape", out _));
        Assert.Contains(sceneNodes, node =>
            node.Name == "PlayerSprite" &&
            node.Element.TryGetProperty("spriteFrames", out _));
        Assert.Contains(sceneNodes, node =>
            node.Name == "JumpAudio" &&
            node.Element.TryGetProperty("stream", out _));
        Assert.Contains(sceneNodes, node =>
            node.Name == "BackgroundTexture" &&
            node.Element.TryGetProperty("texture", out _));
    }

    [Fact]
    public void ReferencePlatformerKeepsScreenUiInCanvasLayers()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "reference-platformer");

        using var scene = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")));
        var sceneRoot = scene.RootElement.GetProperty("root");
        var sceneNodes = FlattenSceneNodes(sceneRoot).ToArray();

        Assert.Contains(sceneNodes, node =>
            string.Equals(node.Type, "CanvasLayer", StringComparison.Ordinal) &&
            string.Equals(node.Name, "Hud", StringComparison.Ordinal));
        Assert.True(
            HasCanvasLayerAncestor(sceneRoot, "PauseMenu"),
            "Pause menu must be under a CanvasLayer so it remains in viewport coordinates while Camera2D follows the player.");
        Assert.True(
            HasCanvasLayerAncestor(sceneRoot, "ResumeButton"),
            "Pause menu controls must be under the same screen-space CanvasLayer as the pause overlay.");
        Assert.Contains(sceneNodes, node =>
            string.Equals(node.Name, "PauseMenu", StringComparison.Ordinal) &&
            node.Element.TryGetProperty("processMode", out var processMode) &&
            string.Equals(processMode.GetString(), "WhenPaused", StringComparison.Ordinal));
        Assert.DoesNotContain(sceneNodes, node =>
            string.Equals(node.Name, "PauseMenu", StringComparison.Ordinal) &&
            node.Element.TryGetProperty("zIndex", out _));
    }

    [Fact]
    public void ReferencePlatformerScriptCoversRequiredGameplaySubsystems()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(root, "examples", "reference-platformer", "scripts", "PlatformerGame.cs");

        Assert.True(File.Exists(scriptPath), $"Reference platformer script is missing: {scriptPath}");

        var script = File.ReadAllText(scriptPath);
        foreach (var requiredText in new[]
        {
            "TileMapLayer",
            "CharacterBody2D",
            "MoveAndSlide",
            "Camera2D",
            "MakeCurrent",
            "SpriteFrames",
            "AnimatedSprite2D",
            "[Export]",
            "AudioStreamPlayer",
            "Input.GetActionStrength",
            "Input.IsActionJustPressed",
            "Input.IsActionJustPressed(\"pause\")",
            "GetTree()",
            "tree.Paused",
            "InputMap.HasAction",
            "Player.IsOnFloor()",
            "ScreenRelative",
            "IsCollisionPolygonOneWay",
            "JsonDocument.Parse(File.ReadAllText(fullPath))",
            "InputEventScreenTouch",
            "InputEventScreenDrag",
            "SaveProgress"
        })
        {
            Assert.Contains(requiredText, script, StringComparison.Ordinal);
        }

        Assert.Contains("public partial class PlatformerGame : Node2D", script, StringComparison.Ordinal);
        Assert.DoesNotContain("internal sealed class PlatformerGame : Node2D", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Input.GetVector(\"move_left\", \"move_right\", \"jump\", \"pause\")", script, StringComparison.Ordinal);
        Assert.DoesNotContain("actionNames.Contains", script, StringComparison.Ordinal);
        Assert.DoesNotContain("touch.Position.X >= 0.5f", script, StringComparison.Ordinal);
        Assert.DoesNotContain("touch.Position.Y <= 0.4f", script, StringComparison.Ordinal);
        Assert.DoesNotContain("drag.Relative.X > 0f", script, StringComparison.Ordinal);
        Assert.DoesNotContain("collided = true;", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Player.Position = new Vector2(0f, 0f);", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Player.Position.Y > 0f", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Player.Position = new Vector2(Player.Position.X, 0f)", script, StringComparison.Ordinal);

        Assert.DoesNotContain("ReferenceTexture2D", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferencePlatformerVerifierExists()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-ReferencePlatformer.ps1");

        Assert.True(File.Exists(verifierPath), $"Reference platformer verifier is missing: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("ReferencePlatformer.csproj", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2D.ReferencePlatformer.csproj", verifier, StringComparison.Ordinal);
        Assert.Contains("dotnet build", verifier, StringComparison.Ordinal);
        Assert.Contains("-- run --project", verifier, StringComparison.Ordinal);
        Assert.Contains("e2d validate", verifier, StringComparison.Ordinal);
        Assert.Contains(".electron2d/tasks", verifier, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferencePlatformerPlayableUsesOnlyElectron2DApi()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "reference-platformer");
        var programPath = Path.Combine(projectRoot, "Program.cs");
        var source = File.ReadAllText(Path.Combine(projectRoot, "scripts", "PlatformerGame.cs"));

        Assert.False(File.Exists(programPath), "Reference platformer must be launched by e2d run, not by a user Program.cs bootstrap.");
        Assert.DoesNotContain("Electron2DRuntimeHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectRuntimeRunner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DApplication", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DRunOptions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DRunResult", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.ReadKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWindow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FRAME reference-platformer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawRect(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawString(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Label", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Button", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new TextureRect", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Sprite2D", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Panel", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddChild(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ImageTexture.LoadFromFile", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireNode<", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetNode(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NodePath", source, StringComparison.Ordinal);
        Assert.DoesNotContain("res://", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"World/", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Hud/", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"PauseMenu/", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReferencePlatformerPlayableScriptRunsGameLoop()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "reference-platformer");
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-ReferencePlatformerPlayableTests",
            Guid.NewGuid().ToString("N"),
            "reference-platformer-playable.png");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj"));
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectRoot);
        startInfo.ArgumentList.Add("--play-script");
        startInfo.ArgumentList.Add("right,jump,right,pause,save,quit");
        startInfo.ArgumentList.Add("--screenshot");
        startInfo.ArgumentList.Add(screenshotPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start reference platformer playable script.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await WaitForExitAsync(process, TimeSpan.FromSeconds(60));
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Reference platformer playable script failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

        var lines = ParseMachineReadableOutput(output);
        Assert.Equal("playable", lines["Mode"]);
        Assert.Equal("True", lines["Playable"]);
        Assert.True(int.Parse(lines["FramesAdvanced"], System.Globalization.CultureInfo.InvariantCulture) >= 4);
        Assert.Equal("6", lines["CommandsApplied"]);
        Assert.NotEqual("0,0", lines["PlayerPosition"]);
        Assert.Equal("True", lines["Paused"]);
        Assert.True(int.Parse(lines["Coins"], System.Globalization.CultureInfo.InvariantCulture) >= 1);
        Assert.True(File.Exists(lines["SavePath"]), $"Missing playable save path: {lines["SavePath"]}");
        Assert.Equal("True", lines["WindowCreated"]);
        Assert.Equal("True", lines["WindowShown"]);
        Assert.Equal("True", lines["FramePresented"]);
        Assert.True(int.Parse(lines["InputEventsDispatched"], System.Globalization.CultureInfo.InvariantCulture) >= 0);
        Assert.True(int.Parse(lines["DrawCommands"], System.Globalization.CultureInfo.InvariantCulture) > 0);
        Assert.Equal(screenshotPath, lines["ScreenshotPath"]);
        Assert.True(File.Exists(screenshotPath), $"Missing playable screenshot path: {screenshotPath}");
        Assert.DoesNotContain("FRAME", output, StringComparison.Ordinal);

        var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
        Assert.True(width >= 640, $"Expected a normal playable screenshot width, got {width}.");
        Assert.True(height >= 360, $"Expected a normal playable screenshot height, got {height}.");
        AssertPngHasVisibleColorDiversity(File.ReadAllBytes(screenshotPath), minDistinctColors: 8);
    }

    [Fact]
    public async Task ReferencePlatformerIdleScriptKeepsPlayerGrounded()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "reference-platformer");
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-ReferencePlatformerPlayableTests",
            Guid.NewGuid().ToString("N"),
            "reference-platformer-idle.png");

        var output = await RunPlayableScriptAsync(root, projectRoot, "idle,idle,idle,save,quit", screenshotPath);
        var lines = ParseMachineReadableOutput(output);
        var (_, y) = ParsePosition(lines["PlayerPosition"]);

        Assert.InRange(y, 4.8f, 5.1f);
        Assert.True(File.Exists(screenshotPath), $"Missing idle screenshot path: {screenshotPath}");
    }

    [Fact]
    public async Task ReferencePlatformerWindowsExportBuildCreatesPlayerBinaryAndResourcePack()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "reference-platformer");
        var exportRoot = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-ReferencePlatformerWindowsExportTests",
            Guid.NewGuid().ToString("N"));
        var screenshotPath = Path.Combine(exportRoot, "reference-platformer-exported.png");

        var build = await RunProcessAsync(
            root,
            "dotnet",
            [
                "run",
                "--project",
                Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj"),
                "--",
                "export",
                "build-windows",
                "--project",
                projectRoot,
                "--output",
                exportRoot,
                "--configuration",
                "Debug",
                "--format",
                "json"
            ],
            TimeSpan.FromMinutes(3));

        Assert.True(
            build.ExitCode == 0,
            $"Windows export build failed with exit code {build.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{build.Output}{Environment.NewLine}stderr:{Environment.NewLine}{build.Error}");

        var executablePath = Path.Combine(exportRoot, "ReferencePlatformer.exe");
        var manifestPath = Path.Combine(exportRoot, "electron2d.pack.json");
        var projectPackPath = Path.Combine(exportRoot, "packs", "project.e2dpkg");
        var scenePackPath = Path.Combine(exportRoot, "packs", "scenes", "main.e2dpkg");
        var assetPackPath = Path.Combine(exportRoot, "packs", "assets", "platformer.e2dpkg");
        var resourcePackPath = Path.Combine(exportRoot, "packs", "resources.e2dpkg");
        Assert.True(File.Exists(executablePath), $"Missing exported player executable: {executablePath}");
        Assert.True(File.Exists(manifestPath), $"Missing exported resource pack manifest: {manifestPath}");
        Assert.True(File.Exists(projectPackPath), $"Missing exported project package: {projectPackPath}");
        Assert.True(File.Exists(scenePackPath), $"Missing exported scene package: {scenePackPath}");
        Assert.True(File.Exists(assetPackPath), $"Missing exported asset package: {assetPackPath}");
        Assert.True(File.Exists(resourcePackPath), $"Missing exported resource package: {resourcePackPath}");

        Assert.False(File.Exists(Path.Combine(exportRoot, "ReferencePlatformer.e2d")), "Export output must not contain loose ReferencePlatformer.e2d.");
        Assert.False(File.Exists(Path.Combine(exportRoot, "project.e2d.json")), "Export output must not contain loose project.e2d.json.");
        Assert.False(File.Exists(Path.Combine(exportRoot, "export_presets.e2export.json")), "Export output must not contain loose export presets.");
        Assert.False(Directory.Exists(Path.Combine(exportRoot, "assets")), "Export output must not contain loose assets directory.");
        Assert.False(Directory.Exists(Path.Combine(exportRoot, "resources")), "Export output must not contain loose resources directory.");
        Assert.False(Directory.Exists(Path.Combine(exportRoot, "scenes")), "Export output must not contain loose scenes directory.");
        Assert.False(Directory.Exists(Path.Combine(exportRoot, ".electron2d")), "Export output must not contain editor metadata directories.");
        Assert.DoesNotContain(
            Directory.EnumerateFiles(exportRoot, "*", SearchOption.AllDirectories),
            path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

        using (var projectPack = ZipFile.OpenRead(projectPackPath))
        {
            var entries = projectPack.Entries
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.Contains("ReferencePlatformer.e2d", entries);
            Assert.DoesNotContain("project.e2d.json", entries);
            Assert.DoesNotContain(entries, entry => entry.StartsWith(".electron2d/tasks/", StringComparison.Ordinal));
        }

        using (var scenePack = ZipFile.OpenRead(scenePackPath))
        {
            var entries = scenePack.Entries
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.Contains("scenes/main.scene.json", entries);
        }

        using (var assetPack = ZipFile.OpenRead(assetPackPath))
        {
            var entries = assetPack.Entries
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.Contains("assets/platformer/graphics/player-idle.png", entries);
            Assert.DoesNotContain(entries, entry => entry.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entries, entry => entry.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        }

        using (var resourcePack = ZipFile.OpenRead(resourcePackPath))
        {
            var entries = resourcePack.Entries
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.Contains("resources/reference-platformer.manifest.json", entries);
        }

        var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var packPaths = manifest.RootElement.GetProperty("packs")
            .EnumerateArray()
            .Select(pack => pack.GetProperty("path").GetString())
            .ToArray();
        Assert.Contains("packs/project.e2dpkg", packPaths);
        Assert.Contains("packs/scenes/main.e2dpkg", packPaths);
        Assert.Contains("packs/assets/platformer.e2dpkg", packPaths);
        Assert.Contains("packs/resources.e2dpkg", packPaths);

        var run = await RunProcessAsync(
            exportRoot,
            executablePath,
            [
                "--play-script",
                "right,jump,right,pause,save,quit",
                "--screenshot",
                screenshotPath
            ],
            TimeSpan.FromSeconds(60));

        Assert.True(
            run.ExitCode == 0,
            $"Exported player failed with exit code {run.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{run.Output}{Environment.NewLine}stderr:{Environment.NewLine}{run.Error}");

        var lines = ParseMachineReadableOutput(run.Output);
        Assert.Equal("playable", lines["Mode"]);
        Assert.Equal("True", lines["Playable"]);
        Assert.Equal("ReferencePlatformer", lines["Project"]);
        Assert.Equal("True", lines["WindowCreated"]);
        Assert.Equal("True", lines["FramePresented"]);
        Assert.Equal(screenshotPath, lines["ScreenshotPath"]);
        Assert.True(File.Exists(screenshotPath), $"Missing exported player screenshot path: {screenshotPath}");
        AssertPngHasVisibleColorDiversity(File.ReadAllBytes(screenshotPath), minDistinctColors: 8);
    }

    private static readonly string[] RequiredFiles =
    [
        "ReferencePlatformer.csproj",
        "scripts/PlatformerGame.cs",
        "ReferencePlatformer.e2d",
        "global.json",
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

    private static IEnumerable<(string Type, string Name, JsonElement Element)> FlattenSceneNodes(JsonElement node)
    {
        var type = node.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        var name = node.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
        yield return (type, name, node);

        if (!node.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var child in children.EnumerateArray())
        {
            foreach (var descendant in FlattenSceneNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool HasCanvasLayerAncestor(JsonElement node, string targetName, bool hasCanvasLayerAncestor = false)
    {
        var type = node.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        var name = node.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
        if (string.Equals(name, targetName, StringComparison.Ordinal))
        {
            return hasCanvasLayerAncestor;
        }

        var nextHasCanvasLayerAncestor = hasCanvasLayerAncestor || string.Equals(type, "CanvasLayer", StringComparison.Ordinal);
        if (!node.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return children.EnumerateArray().Any(child => HasCanvasLayerAncestor(child, targetName, nextHasCanvasLayerAncestor));
    }

    private static Dictionary<string, string> ParseMachineReadableOutput(string output)
    {
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
    }

    private static async Task<string> RunPlayableScriptAsync(string root, string projectRoot, string script, string screenshotPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj"));
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectRoot);
        startInfo.ArgumentList.Add("--play-script");
        startInfo.ArgumentList.Add(script);
        startInfo.ArgumentList.Add("--screenshot");
        startInfo.ArgumentList.Add(screenshotPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start reference platformer playable script.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await WaitForExitAsync(process, TimeSpan.FromSeconds(60));
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Reference platformer playable script failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

        return output;
    }

    private static async Task<ProcessRunResult> RunProcessAsync(
        string workingDirectory,
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process: {fileName}.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await WaitForExitAsync(process, timeout);
        return new ProcessRunResult(process.ExitCode, await outputTask, await errorTask);
    }

    private static (float X, float Y) ParsePosition(string position)
    {
        var parts = position.Split(',', 2);
        Assert.Equal(2, parts.Length);
        return (
            float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    private static async Task WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException($"Process did not exit within {timeout.TotalSeconds:0} seconds.");
        }
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG is too small.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, bytes[..8]);
        var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        return (width, height);
    }

    private static void AssertPngHasVisibleColorDiversity(byte[] bytes, int minDistinctColors)
    {
        var rgba = DecodeRuntimePngRgba(bytes);
        var colors = new HashSet<int>();
        var visiblePixels = 0;
        var redDominantPixels = 0;
        for (var index = 0; index < rgba.Length; index += 4)
        {
            var alpha = rgba[index + 3];
            if (alpha < 16)
            {
                continue;
            }

            visiblePixels++;
            var red = rgba[index];
            var green = rgba[index + 1];
            var blue = rgba[index + 2];
            colors.Add((red << 16) | (green << 8) | blue);
            if (red > 200 && green < 90 && blue < 90)
            {
                redDominantPixels++;
            }
        }

        Assert.True(colors.Count >= minDistinctColors, $"Expected a visually varied screenshot, got {colors.Count} colors.");
        Assert.True(visiblePixels > 0, "Screenshot does not contain visible pixels.");
        Assert.True(redDominantPixels < visiblePixels * 0.60f, "Screenshot is dominated by red pixels.");
    }

    private static byte[] DecodeRuntimePngRgba(byte[] bytes)
    {
        var (width, height) = ReadPngDimensions(bytes);
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset + 12 <= bytes.Length)
        {
            var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            offset += 8;
            if (type == "IDAT")
            {
                idat.Write(bytes, offset, length);
            }

            offset += length + 4;
            if (type == "IEND")
            {
                break;
            }
        }

        idat.Position = 0;
        using var zlib = new System.IO.Compression.ZLibStream(idat, System.IO.Compression.CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);
        var scanlines = raw.ToArray();
        var stride = width * 4;
        Assert.Equal(height * (stride + 1), scanlines.Length);

        var rgba = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            var scanlineOffset = row * (stride + 1);
            Assert.Equal(0, scanlines[scanlineOffset]);
            Buffer.BlockCopy(scanlines, scanlineOffset + 1, rgba, row * stride, stride);
        }

        return rgba;
    }

    private static string FormatSettingsDiagnostics(IEnumerable<Electron2D.Electron2DSettingsDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static string FormatExportDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static bool ExactDirectoryExists(string parentDirectory, string directoryName)
    {
        return Directory.Exists(parentDirectory) &&
            Directory.EnumerateDirectories(parentDirectory, "*", SearchOption.TopDirectoryOnly)
                .Any(path => string.Equals(Path.GetFileName(path), directoryName, StringComparison.Ordinal));
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

    private sealed record ProcessRunResult(int ExitCode, string Output, string Error);
}
