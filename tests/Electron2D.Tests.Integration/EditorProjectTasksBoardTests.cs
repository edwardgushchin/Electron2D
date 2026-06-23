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

public sealed class EditorProjectTasksBoardTests
{
    [Fact]
    public async Task ProjectTasksBoardSmokeRunWritesModelAndVisualAcceptanceArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-project-tasks-board-");

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
            startInfo.ArgumentList.Add("--tasks-board-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor Project Tasks board smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor project tasks board smoke passed", output);
            Assert.Equal("2D|Script|Game|Tasks", lines["WorkspaceSwitcher"]);
            Assert.Equal("Tasks", lines["SelectedWorkspace"]);
            Assert.Equal("Backlog|Ready|In Progress|Blocked|Review|Awaiting Acceptance|Done|Cancelled", lines["Columns"]);
            Assert.Equal("T-0155|T-review|T-blocked|T-done|T-archived", lines["TaskIds"]);
            Assert.Equal("T-0155", lines["SelectedTaskId"]);
            Assert.Equal("Project Tasks board UI", lines["SelectedTaskTitle"]);
            Assert.Equal("P0", lines["SelectedTaskPriority"]);
            Assert.Equal("editor|tasks|manual", lines["SelectedTaskLabels"]);
            Assert.Equal("engineer", lines["SelectedTaskAssignee"]);
            Assert.Equal("Ready", lines["SelectedTaskReadiness"]);
            Assert.Equal("Manual", lines["ManualBlockingReasons"]);
            Assert.Equal("Dependency", lines["DependencyBlockingReasons"]);
            Assert.Equal("Task Details", lines["InspectorTitle"]);
            Assert.Equal("True", lines["DescriptionVisible"]);
            Assert.Equal("True", lines["AcceptanceCriteriaVisible"]);
            Assert.Equal("True", lines["SubtasksVisible"]);
            Assert.Equal("Comment|Decision|Investigation|Blocker|TestResult|StatusChange|AgentSummary|AcceptanceResult", lines["ActivityKinds"]);
            Assert.Equal("transaction://txn-task-001", lines["LinkedTransactions"]);
            Assert.Equal("job://job-run-001", lines["LinkedJobs"]);
            Assert.Equal("E2D-TASK-0003", lines["LinkedDiagnostics"]);
            Assert.Equal("artifact://screenshots/tasks-board.png|artifact://runtime/tree.json", lines["LinkedArtifacts"]);
            Assert.Equal("res://scenes/main.e2scene.json|res://textures/player.png|/root/Player", lines["LinkedObjects"]);
            Assert.Equal("T-0155:Ready->InProgress@rank-020", lines["DragDropIntent"]);
            Assert.Equal("True", lines["DragDropAllowed"]);
            Assert.Equal("E2D-TASK-0002", lines["RejectedDropDiagnostic"]);
            Assert.Equal("True", lines["RankRoundTripStable"]);
            Assert.Equal("True", lines["ArchiveViewAvailable"]);
            Assert.Equal("True", lines["ArchivedHiddenFromBoard"]);
            Assert.Equal("True", lines["HardDeleteRequiresConfirmation"]);
            Assert.Equal("True", lines["HumanAcceptActionUsesTrustedContext"]);
            Assert.Equal("False", lines["AgentAcceptActionAvailable"]);
            Assert.Equal("Review|Awaiting Acceptance", lines["ReviewStatesDiffer"]);
            Assert.Equal("Status|Priority|Labels|Assignee|Text|Linked Object", lines["Filters"]);
            Assert.Equal("43", lines["WorkspaceEventRevision"]);
            Assert.Equal("True", lines["WorksWithoutAi"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var statePath = lines["StatePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing Project Tasks board state artifact: {statePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing Project Tasks board screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing Project Tasks board visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.ProjectTasksBoardVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-project-tasks-board-harness", data.GetProperty("harness").GetString());
            Assert.Equal("Tasks", data.GetProperty("selectedWorkspace").GetString());
            Assert.Equal(1280, data.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(720, data.GetProperty("viewport").GetProperty("height").GetInt32());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 16);
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
            Assert.True(data.GetProperty("details").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("dragDrop").GetProperty("allowed").GetBoolean());
            Assert.False(data.GetProperty("acceptance").GetProperty("agentAcceptActionAvailable").GetBoolean());
            Assert.True(data.GetProperty("acceptance").GetProperty("humanAcceptActionUsesTrustedContext").GetBoolean());
            Assert.Equal(
                new[] { "Backlog", "Ready", "In Progress", "Blocked", "Review", "Awaiting Acceptance", "Done", "Cancelled" },
                data.GetProperty("columns").EnumerateArray().Select(item => item.GetProperty("label").GetString()).ToArray());
            Assert.Equal(
                new[] { "Status", "Priority", "Labels", "Assignee", "Text", "Linked Object" },
                data.GetProperty("filters").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.Equal(
                new[] { "Accept", "Request Changes", "Cancel", "Create", "Edit", "Archive", "Hard Delete" },
                data.GetProperty("actions").EnumerateArray().Select(item => item.GetString()).ToArray());
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
