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

public sealed class EditorAgentWorkspacePanelTests
{
    [Fact]
    public async Task AgentWorkspacePanelSmokeRunWritesModelAndVisualAcceptanceArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-agent-workspace-panel-");

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
            startInfo.ArgumentList.Add("--agent-workspace-panel-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor Agent Workspace smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor agent workspace panel smoke passed", output);
            Assert.Equal("agent-session-t0150", lines["AgentSessionId"]);
            Assert.Equal("codex", lines["ProfileId"]);
            Assert.Equal("Connected", lines["ConnectionState"]);
            Assert.Equal("Connected", lines["HandshakeState"]);
            Assert.Equal("activeEditor", lines["Route"]);
            Assert.Equal("Captured runtime screenshot", lines["LastAction"]);
            Assert.Equal("T-0150", lines["CurrentTask"]);
            Assert.Equal("InProgress", lines["TaskStatus"]);
            Assert.Equal("Working", lines["AcceptanceState"]);
            Assert.Equal("transaction://txn-agent-001", lines["LinkedTransactions"]);
            Assert.Equal("job://job-run-001", lines["LinkedJobs"]);
            Assert.Equal("E2D-RUNTIME-0001", lines["LinkedDiagnostics"]);
            Assert.Equal("artifact://screenshots/frame-0001.png|artifact://runtime/tree.json", lines["LinkedArtifacts"]);
            Assert.Equal(
                "scene:res://scenes/main.e2scene.json|node:/root/Player|resource:res://textures/player.png|script:Scripts/PlayerController.cs|setting:project.e2d.json",
                lines["ChangedObjects"]);
            Assert.Equal("code|severity|message|location|relatedLocations|suggestedFixes", lines["DiagnosticFields"]);
            Assert.Equal("Run", lines["JobKind"]);
            Assert.Equal("Running", lines["JobState"]);
            Assert.Equal("65", lines["JobProgressPercent"]);
            Assert.Equal("True", lines["CanCancel"]);
            Assert.Equal("snap-run-001", lines["InputSnapshotId"]);
            Assert.Equal("42", lines["InputWorkspaceRevision"]);
            Assert.Equal("17", lines["InputContentRevision"]);
            Assert.Equal("build-hash-001", lines["InputBuildConfigurationHash"]);
            Assert.Equal("runtime-tree", lines["StaleMarkers"]);
            Assert.Equal("True", lines["GroupedUndoAvailable"]);
            Assert.Equal("undo-agent-001", lines["UndoGroupId"]);
            Assert.Equal("True", lines["SubmitForAcceptanceActionAvailable"]);
            Assert.Equal("False", lines["DoneActionAvailable"]);
            Assert.Equal("BottomPanel/Agent", lines["DockPlacement"]);
            Assert.Equal("True", lines["DockPersisted"]);
            Assert.Equal("2D|Script|Game|Tasks", lines["VisibleWorkspaces"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(screenshotPath), $"Missing Agent Workspace screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing Agent Workspace visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.AgentWorkspaceVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-agent-workspace-panel-harness", data.GetProperty("harness").GetString());
            Assert.Equal("BottomPanel/Agent", data.GetProperty("dock").GetProperty("placement").GetString());
            Assert.True(data.GetProperty("dock").GetProperty("persisted").GetBoolean());
            Assert.Equal(1280, data.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(720, data.GetProperty("viewport").GetProperty("height").GetInt32());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenActionMatches").GetInt32());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 6);
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
            Assert.False(data.GetProperty("doneActionAvailable").GetBoolean());
            Assert.True(data.GetProperty("submitForAcceptanceActionAvailable").GetBoolean());
            Assert.True(data.GetProperty("groupedUndoAvailable").GetBoolean());
            Assert.Equal(
                new[] { "Overview", "Changes", "Jobs", "Diagnostics", "Artifacts", "Terminal" },
                data.GetProperty("sections").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.Equal(
                new[] { "2D", "Script", "Game", "Tasks" },
                data.GetProperty("dock").GetProperty("visibleWorkspaces").EnumerateArray().Select(item => item.GetString()).ToArray());
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
