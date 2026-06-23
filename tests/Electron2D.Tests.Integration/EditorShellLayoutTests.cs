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

public sealed class EditorShellLayoutTests
{
    [Fact]
    public async Task ShellLayoutSmokeRunPersistsLayoutAndWritesVisualArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-shell-layout-");

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
            startInfo.ArgumentList.Add("--shell-layout-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor shell layout smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor shell layout smoke passed", output);
            Assert.Equal("Scene|Project|Debug|Editor|Help", lines["MenuItems"]);
            Assert.Equal("2D|Script|Game|Tasks", lines["WorkspaceSwitcher"]);
            Assert.Equal("Scene|FileSystem", lines["LeftDocks"]);
            Assert.Equal("Inspector|Node|Agent Workspace", lines["RightDocks"]);
            Assert.Equal("Output|Debugger|Diagnostics|Search|Animation|Audio|Tests", lines["BottomPanelTabs"]);
            Assert.Equal("Tasks", lines["SelectedWorkspace"]);
            Assert.Equal("True", lines["BottomPanelCollapseRoundTrip"]);
            Assert.Equal("True", lines["PersistenceRoundTripStable"]);
            Assert.Equal("True", lines["WorkspaceStateRoundTripStable"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.Equal("0", lines["ForbiddenShortcutMatches"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);
            Assert.Equal("Player", lines["TwoDSelection"]);
            Assert.Equal("64,96", lines["TwoDScroll"]);
            Assert.Equal("1.5", lines["TwoDZoom"]);
            Assert.Equal("Scripts/PlayerController.cs|Scripts/EnemyController.cs", lines["ScriptDocuments"]);
            Assert.Equal("res://scenes/main.e2scene.json", lines["GameDocuments"]);
            Assert.Equal("T-0157", lines["TasksDocuments"]);

            var statePath = lines["StatePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing shell state artifact: {statePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing shell screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing shell visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.EditorShellVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-shell-layout-harness", data.GetProperty("harness").GetString());
            Assert.Equal(1280, data.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(720, data.GetProperty("viewport").GetProperty("height").GetInt32());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 16);
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
            Assert.Equal(
                new[] { "2D", "Script", "Game", "Tasks" },
                data.GetProperty("workspaceSwitcher").GetProperty("labels").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.Equal(
                new[] { "Scene", "FileSystem" },
                data.GetProperty("leftDocks").EnumerateArray().Select(item => item.GetProperty("label").GetString()).ToArray());
            Assert.Equal(
                new[] { "Inspector", "Node", "Agent Workspace" },
                data.GetProperty("rightDocks").EnumerateArray().Select(item => item.GetProperty("label").GetString()).ToArray());
            Assert.Equal(
                new[] { "Output", "Debugger", "Diagnostics", "Search", "Animation", "Audio", "Tests" },
                data.GetProperty("bottomPanel").GetProperty("tabs").EnumerateArray().Select(item => item.GetString()).ToArray());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
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
