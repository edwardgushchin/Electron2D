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
    public async Task ShellLayoutSmokeRunPersistsLayoutWithoutVisualHarnessArtifacts()
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
            Assert.Equal("Inspector|Node", lines["RightDocks"]);
            Assert.Equal("Output|Debugger|Agent|Diagnostics|Search|Animation|Audio|Tests", lines["BottomPanelTabs"]);
            Assert.Equal("Tasks", lines["SelectedWorkspace"]);
            Assert.Equal("True", lines["BottomPanelCollapseRoundTrip"]);
            Assert.Equal("True", lines["PersistenceRoundTripStable"]);
            Assert.Equal("True", lines["WorkspaceStateRoundTripStable"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.Equal("0", lines["ForbiddenShortcutMatches"]);
            Assert.Equal("False", lines["VisualHarnessPresent"]);
            Assert.Equal("Player", lines["TwoDSelection"]);
            Assert.Equal("64,96", lines["TwoDScroll"]);
            Assert.Equal("1.5", lines["TwoDZoom"]);
            Assert.Equal("Scripts/PlayerController.cs|Scripts/EnemyController.cs", lines["ScriptDocuments"]);
            Assert.Equal("res://scenes/main.e2scene.json", lines["GameDocuments"]);
            Assert.Equal("T-0157", lines["TasksDocuments"]);

            var statePath = lines["StatePath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing shell state artifact: {statePath}");
            Assert.True(File.Exists(analysisPath), $"Missing shell layout analysis artifact: {analysisPath}");
            Assert.False(lines.ContainsKey("ScreenshotPath"));
            Assert.False(Directory.Exists(Path.Combine(workRoot, "visual")), "Shell layout smoke must not create synthetic visual harness artifacts.");

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.EditorShellLayoutAnalysis", data.GetProperty("format").GetString());
            Assert.False(data.GetProperty("visualHarnessPresent").GetBoolean());
            Assert.Equal(1280, data.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(720, data.GetProperty("viewport").GetProperty("height").GetInt32());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 16);
            Assert.Equal(
                new[] { "2D", "Script", "Game", "Tasks" },
                data.GetProperty("workspaceSwitcher").GetProperty("labels").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.Equal(
                new[] { "Scene", "FileSystem" },
                data.GetProperty("leftDocks").EnumerateArray().Select(item => item.GetProperty("label").GetString()).ToArray());
            Assert.Equal(
                new[] { "Inspector", "Node" },
                data.GetProperty("rightDocks").EnumerateArray().Select(item => item.GetProperty("label").GetString()).ToArray());
            Assert.Equal(
                new[] { "Output", "Debugger", "Agent", "Diagnostics", "Search", "Animation", "Audio", "Tests" },
                data.GetProperty("bottomPanel").GetProperty("tabs").EnumerateArray().Select(item => item.GetString()).ToArray());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
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
