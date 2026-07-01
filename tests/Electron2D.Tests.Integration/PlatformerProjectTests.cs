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
public sealed class PlatformerProjectTests
{
    [Fact]
    public void PlatformerProjectHasEditorProjectContract()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "platformer");

        foreach (var relativePath in RequiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(projectRoot, relativePath)),
                $"Platformer is missing required file: {relativePath}");
        }

        Assert.False(File.Exists(Path.Combine(projectRoot, "project.e2d.json")), "Platformer must not keep the legacy project.e2d.json manifest.");
        Assert.False(File.Exists(Path.Combine(projectRoot, "Electron2D.Platformer.csproj")), "Platformer project file must not include the engine namespace prefix.");
        Assert.False(File.Exists(Path.Combine(projectRoot, "electron2d.lock.json")), "Platformer lock data must be embedded in Platformer.e2d.");
        Assert.False(File.Exists(Path.Combine(projectRoot, "export_presets.e2export.json")), "Platformer export presets must be embedded in Platformer.e2d.");
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "bin")), "Platformer root must not keep build output bin/.");
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "obj")), "Platformer root must not keep build output obj/.");
        Assert.False(ExactDirectoryExists(projectRoot, "Scripts"), "Platformer source folder must use lower-case scripts/.");

        var projectManifestPath = Path.Combine(projectRoot, "Platformer.e2d");
        var settings = Electron2D.Electron2DSettingsStore.LoadProject(projectManifestPath);
        Assert.True(settings.Succeeded, FormatSettingsDiagnostics(settings.Diagnostics));
        Assert.NotNull(settings.Settings);
        Assert.Equal("Platformer", settings.Settings.Name);
        Assert.Equal("scenes/main.scene.json", settings.Settings.MainScene);
        Assert.Equal(
            new[] { "jump", "move_left", "move_right", "pause" },
            settings.Settings.InputActions.Select(action => action.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

        using var projectManifest = JsonDocument.Parse(File.ReadAllText(projectManifestPath));
        Assert.True(projectManifest.RootElement.TryGetProperty("exportPresets", out _), "Platformer.e2d must embed export presets.");
        Assert.True(projectManifest.RootElement.TryGetProperty("reproducibilityLock", out _), "Platformer.e2d must embed reproducibility lock data.");

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
        Assert.Equal(1, taskBoard.RootElement.GetProperty("version").GetInt32());
        var taskBoardColumns = taskBoard.RootElement.GetProperty("columns")
            .EnumerateArray()
            .ToDictionary(
                column => column.GetProperty("status").GetString()!,
                column => ReadStringArray(column.GetProperty("taskIds")),
                StringComparer.Ordinal);
        Assert.Equal(["T-0222"], taskBoardColumns["Ready"]);
        Assert.Equal(["T-0223", "T-0225", "T-0221", "T-0166", "platformer-acceptance"], taskBoardColumns["Blocked"]);
        Assert.Empty(taskBoardColumns["AwaitingAcceptance"]);

        foreach (var (taskId, expectedTask) in ExpectedProjectTasks)
        {
            using var taskDocument = ReadJson(Path.Combine(projectRoot, ".electron2d", "tasks", $"{taskId}.e2task"));
            var taskRoot = taskDocument.RootElement;
            Assert.Equal("Electron2D.TaskFile", taskRoot.GetProperty("format").GetString());
            Assert.Equal(1, taskRoot.GetProperty("version").GetInt32());
            Assert.Equal(taskId, taskRoot.GetProperty("taskId").GetString());
            Assert.Equal(expectedTask.Status, taskRoot.GetProperty("status").GetString());
            Assert.Equal(expectedTask.Title, taskRoot.GetProperty("title").GetString());
            Assert.Equal(expectedTask.Priority, taskRoot.GetProperty("priority").GetString());
            Assert.Equal(expectedTask.Dependencies, ReadStringArray(taskRoot.GetProperty("dependencies")));
            Assert.False(
                string.IsNullOrWhiteSpace(taskRoot.GetProperty("description").GetString()),
                $"Task {taskId} must keep a description.");
            Assert.True(taskRoot.GetProperty("acceptanceCriteria").EnumerateArray().Any(), $"Task {taskId} must keep acceptance criteria.");
            var subtasks = ReadStringArray(taskRoot.GetProperty("subtasks"));
            foreach (var expectedSubtask in expectedTask.RequiredSubtasks)
            {
                Assert.Contains(expectedSubtask, subtasks);
            }

            var activityPayloads = taskRoot.GetProperty("activity")
                .EnumerateArray()
                .Select(entry => entry.GetProperty("payload").GetString() ?? string.Empty)
                .ToArray();
            foreach (var expectedActivity in expectedTask.RequiredActivityPayloads)
            {
                Assert.Contains(activityPayloads, payload => payload.Contains(expectedActivity, StringComparison.Ordinal));
            }
        }

        using var acceptanceTask = ReadJson(Path.Combine(projectRoot, ".electron2d", "tasks", "platformer-acceptance.e2task"));
        Assert.Equal("BlockedByDependencies", acceptanceTask.RootElement.GetProperty("readiness").GetString());
        Assert.Equal("ChangesRequested", acceptanceTask.RootElement.GetProperty("acceptanceState").GetString());
        Assert.Equal(["T-0166"], ReadStringArray(acceptanceTask.RootElement.GetProperty("dependencies")));

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
    public void PlatformerKeepsScreenUiInCanvasLayers()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "platformer");

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
    public void PlatformerScriptCoversRequiredGameplaySubsystems()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(root, "examples", "platformer", "scripts", "PlatformerGame.cs");

        Assert.True(File.Exists(scriptPath), $"Platformer script is missing: {scriptPath}");

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
    public void PlatformerVerifierExists()
    {
        var root = FindRepositoryRoot();
        var buildToolPath = Path.Combine(root, "eng", "Electron2D.Build", "RepositoryWorkflowVerifiers.cs");

        Assert.True(File.Exists(buildToolPath), $"Repository build tool is missing: {buildToolPath}");

        var verifier = File.ReadAllText(buildToolPath);
        Assert.Contains("verify platformer", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("Verify-Platformer.ps1", verifier, StringComparison.Ordinal);
        var projectDocument = File.ReadAllText(Path.Combine(root, "docs", "examples", "platformer.md"));
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify platformer", projectDocument, StringComparison.Ordinal);
        Assert.Contains("Platformer.csproj", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2D.Platformer.csproj", verifier, StringComparison.Ordinal);
        Assert.Contains("verify platformer", projectDocument, StringComparison.Ordinal);
        Assert.Contains(".electron2d/tasks", projectDocument, StringComparison.Ordinal);
        Assert.Contains("T-0222.e2task", projectDocument, StringComparison.Ordinal);
        Assert.Contains("ChangesRequested", File.ReadAllText(Path.Combine(root, "examples", "platformer", ".electron2d", "tasks", "platformer-acceptance.e2task")), StringComparison.Ordinal);
    }

    [Fact]
    public void PlatformerPlayableUsesOnlyElectron2DApi()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "platformer");
        var programPath = Path.Combine(projectRoot, "Program.cs");
        var source = File.ReadAllText(Path.Combine(projectRoot, "scripts", "PlatformerGame.cs"));

        Assert.False(File.Exists(programPath), "Platformer must be launched by e2d run, not by a user Program.cs bootstrap.");
        Assert.DoesNotContain("Electron2DRuntimeHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectRuntimeRunner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DApplication", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DRunOptions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DRunResult", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.ReadKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWindow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FRAME platformer", source, StringComparison.Ordinal);
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
    [Trait("Category", "Baseline")]
    public async Task PlatformerPlayableScriptRunsGameLoop()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "platformer");
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-PlatformerPlayableTests",
            Guid.NewGuid().ToString("N"),
            "platformer-playable.png");

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

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Platformer playable script.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await WaitForExitAsync(process, TimeSpan.FromSeconds(60));
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Platformer playable script failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

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
    [Trait("Category", "Baseline")]
    public async Task PlatformerIdleScriptKeepsPlayerGrounded()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "platformer");
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-PlatformerPlayableTests",
            Guid.NewGuid().ToString("N"),
            "platformer-idle.png");

        var output = await RunPlayableScriptAsync(root, projectRoot, "idle,idle,idle,save,quit", screenshotPath);
        var lines = ParseMachineReadableOutput(output);
        var (_, y) = ParsePosition(lines["PlayerPosition"]);

        Assert.InRange(y, 4.8f, 5.1f);
        Assert.True(File.Exists(screenshotPath), $"Missing idle screenshot path: {screenshotPath}");
    }

    [Fact]
    public async Task PlatformerWindowsExportBuildCreatesPlayerBinaryAndResourcePack()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "platformer");
        var exportRoot = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-PlatformerWindowsExportTests",
            Guid.NewGuid().ToString("N"));
        var screenshotPath = Path.Combine(exportRoot, "platformer-exported.png");

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

        var executablePath = Path.Combine(exportRoot, "Platformer.exe");
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

        Assert.False(File.Exists(Path.Combine(exportRoot, "Platformer.e2d")), "Export output must not contain loose Platformer.e2d.");
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
            Assert.Contains("Platformer.e2d", entries);
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
            Assert.Contains("resources/platformer.manifest.json", entries);
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
        Assert.Equal("Platformer", lines["Project"]);
        Assert.Equal("True", lines["WindowCreated"]);
        Assert.Equal("True", lines["FramePresented"]);
        Assert.Equal(screenshotPath, lines["ScreenshotPath"]);
        Assert.True(File.Exists(screenshotPath), $"Missing exported player screenshot path: {screenshotPath}");
        AssertPngHasVisibleColorDiversity(File.ReadAllBytes(screenshotPath), minDistinctColors: 8);
    }

    private static readonly string[] RequiredFiles =
    [
        "Platformer.csproj",
        "scripts/PlatformerGame.cs",
        "Platformer.e2d",
        "global.json",
        "scenes/main.scene.json",
        "resources/platformer.manifest.json",
        ".electron2d/tasks/board.e2tasks",
        ".electron2d/tasks/platformer-acceptance.e2task",
        ".electron2d/tasks/T-0166.e2task",
        ".electron2d/tasks/T-0221.e2task",
        ".electron2d/tasks/T-0222.e2task",
        ".electron2d/tasks/T-0223.e2task",
        ".electron2d/tasks/T-0225.e2task"
    ];

    private static readonly Dictionary<string, ExpectedProjectTask> ExpectedProjectTasks = new(StringComparer.Ordinal)
    {
        ["T-0222"] = new ExpectedProjectTask(
            "Пересобрать Platformer как законченную приёмочную игру",
            "Ready",
            "P0",
            [],
            ["Открыто: Пересобрать scene data и gameplay script без ручных verifier shortcuts."],
            [
                "2026-06-24T19:20:00+03:00 - Создано по пользовательскому audit rejection.",
                "Задача перенесена из корневого TASKS.md"
            ]),
        ["T-0223"] = new ExpectedProjectTask(
            "Переписать Platformer acceptance без самопроверки",
            "Blocked",
            "P0",
            ["T-0222"],
            ["Открыто: Переписать play-script/verifier path на наблюдение gameplay events."],
            [
                "2026-06-24T19:20:00+03:00 - Создано по пользовательскому audit rejection.",
                "Задача перенесена из корневого TASKS.md"
            ]),
        ["T-0225"] = new ExpectedProjectTask(
            "Вынести Platformer visual gate в отдельные screenshots и runtime probes",
            "Blocked",
            "P0",
            ["T-0222", "T-0223"],
            ["Открыто: Реализовать сбор probes и pixel analysis поверх probes."],
            [
                "2026-06-24T19:45:00+03:00 - Создано по замечанию пользователя: screenshot gate нужно отделить от `T-0223`",
                "Задача перенесена из корневого TASKS.md"
            ]),
        ["T-0221"] = new ExpectedProjectTask(
            "Восстановить performance gate для настоящего Platformer и RuntimeHost",
            "Blocked",
            "P0",
            ["T-0215", "T-0223", "T-0225"],
            ["Открыто: Подключить `Platformer` scenario к generic runner/schema из `T-0215`."],
            [
                "2026-06-24T19:45:00+03:00 - Ownership уточнён по аудиту пользователя",
                "Задача перенесена из корневого TASKS.md"
            ]),
        ["T-0166"] = new ExpectedProjectTask(
            "Ужесточить Platformer после аудита Godot-переносимости",
            "Blocked",
            "P0",
            ["T-0221", "T-0222", "T-0223", "T-0225"],
            ["Открыто: Выполнить gameplay, acceptance, visual и performance дочерние задачи."],
            [
                "2026-06-23T21:02:00+03:00 - Задача создана по текущему аудиту пользователя.",
                "Задача перенесена из корневого TASKS.md"
            ]),
        ["platformer-acceptance"] = new ExpectedProjectTask(
            "Verify the 0.1.0 Preview Platformer",
            "Blocked",
            "P0",
            ["T-0166"],
            [],
            [
                "Initial acceptance task created with the Platformer project files.",
                "Предыдущая заявка на приёмку Platformer заблокирована по T-0234"
            ])
    };

    private sealed record ExpectedProjectTask(
        string Title,
        string Status,
        string Priority,
        string[] Dependencies,
        string[] RequiredSubtasks,
        string[] RequiredActivityPayloads);

    private static JsonDocument ReadJson(string path)
    {
        Assert.True(File.Exists(path), $"JSON file is missing: {path}");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string[] ReadStringArray(JsonElement element)
    {
        return element.EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();
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

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Platformer playable script.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await WaitForExitAsync(process, TimeSpan.FromSeconds(60));
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Platformer playable script failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

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
