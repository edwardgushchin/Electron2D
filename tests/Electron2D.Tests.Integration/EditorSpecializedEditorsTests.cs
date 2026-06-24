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
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorSpecializedEditorsTests
{
    [Fact]
    [Trait("Category", "Baseline")]
    public async Task SpecializedEditorsSmokeWritesRuntimeResourcesAndShowsRealWindowUi()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-specialized-editors-");

        try
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
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--specialized-editors-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(45)));
            if (completed != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Editor specialized editors smoke did not exit within the expected time.");
            }

            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Specialized editors smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor specialized editors smoke passed", output);
            Assert.Equal("True", lines["SpriteFramesRoundTrip"]);
            Assert.Equal("True", lines["TileMapRoundTrip"]);
            Assert.Equal("True", lines["AnimationTimelineRoundTrip"]);
            Assert.Equal("True", lines["SceneRoundTrip"]);
            Assert.Equal("idle|run", lines["SpriteAnimations"]);
            Assert.Equal("0,0,3,2", lines["TileMapUsedRect"]);
            Assert.Equal("position|call", lines["AnimationTracks"]);
            Assert.Equal("True", lines["WindowCreated"]);
            Assert.Equal("True", lines["WindowShown"]);
            Assert.Equal("True", lines["FramePresented"]);
            Assert.Equal("True", lines["PointerInteractionObserved"]);
            Assert.Equal("True", lines["KeyboardInteractionObserved"]);
            Assert.Equal("0", lines["TextOverflowCount"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var projectRoot = lines["ProjectPath"];
            var projectSettingsPath = lines["ProjectSettingsPath"];
            var spriteFramesPath = lines["SpriteFramesPath"];
            var tileSetPath = lines["TileSetPath"];
            var animationPath = lines["AnimationPath"];
            var scenePath = lines["ScenePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(projectSettingsPath), $"Missing project settings: {projectSettingsPath}");
            Assert.Equal(projectRoot, Path.GetDirectoryName(projectSettingsPath));
            Assert.True(File.Exists(spriteFramesPath), $"Missing SpriteFrames resource: {spriteFramesPath}");
            Assert.True(File.Exists(tileSetPath), $"Missing TileSet resource: {tileSetPath}");
            Assert.True(File.Exists(animationPath), $"Missing Animation resource: {animationPath}");
            Assert.True(File.Exists(scenePath), $"Missing specialized editors scene: {scenePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing screenshot: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing visual analysis: {analysisPath}");

            using var spriteFramesDocument = JsonDocument.Parse(File.ReadAllText(spriteFramesPath));
            AssertSerializedResource(
                spriteFramesDocument.RootElement,
                "Electron2D.SpriteFrames",
                "res://resources/player_frames.e2res");
            var spriteFramesText = File.ReadAllText(spriteFramesPath);
            Assert.Contains("\"idle\"", spriteFramesText, StringComparison.Ordinal);
            Assert.Contains("\"run\"", spriteFramesText, StringComparison.Ordinal);
            Assert.Contains("\"duration\"", spriteFramesText, StringComparison.Ordinal);
            Assert.Contains("\"loop_mode\"", spriteFramesText, StringComparison.Ordinal);

            using var tileSetDocument = JsonDocument.Parse(File.ReadAllText(tileSetPath));
            AssertSerializedResource(
                tileSetDocument.RootElement,
                "Electron2D.TileSet",
                "res://resources/terrain_tileset.e2res");
            var tileSetText = File.ReadAllText(tileSetPath);
            Assert.Contains("\"tile_size\"", tileSetText, StringComparison.Ordinal);
            Assert.Contains("\"atlas_coords\"", tileSetText, StringComparison.Ordinal);
            Assert.Contains("\"texture_region_size\"", tileSetText, StringComparison.Ordinal);

            using var animationDocument = JsonDocument.Parse(File.ReadAllText(animationPath));
            AssertSerializedResource(
                animationDocument.RootElement,
                "Electron2D.Animation",
                "res://resources/player_motion.e2res");
            var animationText = File.ReadAllText(animationPath);
            Assert.Contains("\"tracks\"", animationText, StringComparison.Ordinal);
            Assert.Contains("\"Player:position:x\"", animationText, StringComparison.Ordinal);
            Assert.Contains("\"method\"", animationText, StringComparison.Ordinal);

            using var sceneDocument = JsonDocument.Parse(File.ReadAllText(scenePath));
            var scene = sceneDocument.RootElement;
            Assert.Equal("Electron2D.SceneFile", scene.GetProperty("format").GetString());
            var nodeTypes = scene.GetProperty("nodes").EnumerateArray()
                .Select(node => node.GetProperty("type").GetString())
                .ToArray();
            Assert.Contains("Electron2D.AnimatedSprite2D", nodeTypes);
            Assert.Contains("Electron2D.TileMapLayer", nodeTypes);
            Assert.Contains("Electron2D.AnimationPlayer", nodeTypes);

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysisDocument = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var analysis = analysisDocument.RootElement;
            Assert.Equal("Electron2D.SpecializedEditorsVisualAnalysis", analysis.GetProperty("format").GetString());
            Assert.True(analysis.GetProperty("window").GetProperty("actualWindow").GetBoolean());
            Assert.True(analysis.GetProperty("window").GetProperty("shown").GetBoolean());
            Assert.True(analysis.GetProperty("rendering").GetProperty("framePresented").GetBoolean());
            Assert.True(analysis.GetProperty("input").GetProperty("pointerInteractionObserved").GetBoolean());
            Assert.True(analysis.GetProperty("input").GetProperty("keyboardInteractionObserved").GetBoolean());
            Assert.Equal(0, analysis.GetProperty("layout").GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, analysis.GetProperty("layout").GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.Contains(
                analysis.GetProperty("layout").GetProperty("panelLabels").EnumerateArray(),
                label => label.GetString() == "SpriteFrames");
            Assert.Contains(
                analysis.GetProperty("layout").GetProperty("panelLabels").EnumerateArray(),
                label => label.GetString() == "TileMap");
            Assert.Contains(
                analysis.GetProperty("layout").GetProperty("panelLabels").EnumerateArray(),
                label => label.GetString() == "AnimationPlayer");
            Assert.True(analysis.GetProperty("screenshotReviewed").GetBoolean());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
    }

    private static void AssertSerializedResource(JsonElement root, string expectedType, string expectedPath)
    {
        Assert.Equal("Electron2D.SerializedResource", root.GetProperty("format").GetString());
        Assert.Equal(expectedType, root.GetProperty("type").GetString());
        Assert.Equal(expectedPath, root.GetProperty("path").GetString());
        Assert.True(root.TryGetProperty("properties", out var properties));
        Assert.Equal(JsonValueKind.Object, properties.ValueKind);
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG must contain a signature and IHDR chunk.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, bytes.Take(4).ToArray());
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));

        return (
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static Dictionary<string, string> ParseMachineReadableOutput(string output)
    {
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
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
