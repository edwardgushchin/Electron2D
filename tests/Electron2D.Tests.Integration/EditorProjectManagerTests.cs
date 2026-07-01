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
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorProjectManagerTests
{
    [Fact]
    public async Task ProjectManagerSmokeRunCreatesOpensProjectTracksRecentProjectsAndChecksSdk()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-project-manager-");
        var userDataRoot = CreateTemporaryDirectory("electron2d-editor-user-data-");

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
            startInfo.ArgumentList.Add("--project-manager-smoke");
            startInfo.ArgumentList.Add(workRoot);
            startInfo.ArgumentList.Add("--user-data-dir");
            startInfo.ArgumentList.Add(userDataRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Project Manager smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);
            var createdProjectPath = lines["ProjectPath"];
            var projectSettingsPath = lines["ProjectSettingsPath"];
            var mainScenePath = lines["MainScenePath"];
            var userSettingsPath = lines["UserSettingsPath"];

            Assert.Contains("Electron2D.Editor project manager smoke passed", output);
            Assert.Equal("ProjectManagerSmoke", lines["ProjectName"]);
            Assert.Equal("Compatibility", lines["RendererProfile"]);
            Assert.Equal("True", lines["SdkAvailable"]);
            Assert.False(string.IsNullOrWhiteSpace(lines["SdkVersion"]));
            Assert.Equal("1", lines["RecentProjects"]);

            Assert.True(Directory.Exists(createdProjectPath));
            Assert.True(File.Exists(Path.Combine(createdProjectPath, "ProjectManagerSmoke.csproj")));
            Assert.True(File.Exists(Path.Combine(createdProjectPath, "ProjectManagerSmoke.e2d")));
            Assert.False(File.Exists(Path.Combine(createdProjectPath, "project.e2d.json")));
            Assert.False(File.Exists(Path.Combine(createdProjectPath, "electron2d.lock.json")));
            Assert.False(File.Exists(Path.Combine(createdProjectPath, "export_presets.e2export.json")));
            Assert.True(Directory.Exists(Path.Combine(createdProjectPath, "scripts")));
            Assert.False(ExactDirectoryExists(createdProjectPath, "Scripts"));
            Assert.False(Directory.Exists(Path.Combine(createdProjectPath, ".template.config")));
            Assert.True(File.Exists(projectSettingsPath));
            Assert.Equal(Path.Combine(createdProjectPath, "ProjectManagerSmoke.e2d"), projectSettingsPath);
            Assert.True(File.Exists(mainScenePath));
            Assert.True(File.Exists(userSettingsPath));
            AssertAgentReadyProject(createdProjectPath, "Compatibility");

            using var projectDocument = JsonDocument.Parse(File.ReadAllText(projectSettingsPath));
            Assert.Equal("Electron2D.ProjectSettings", projectDocument.RootElement.GetProperty("format").GetString());
            Assert.Equal("ProjectManagerSmoke", projectDocument.RootElement.GetProperty("name").GetString());
            Assert.Equal("Compatibility", projectDocument.RootElement.GetProperty("rendererProfile").GetString());
            Assert.Equal("scenes/main.scene.json", projectDocument.RootElement.GetProperty("mainScene").GetString());
            Assert.True(projectDocument.RootElement.TryGetProperty("exportPresets", out _));
            Assert.True(projectDocument.RootElement.TryGetProperty("reproducibilityLock", out _));

            using var userDocument = JsonDocument.Parse(File.ReadAllText(userSettingsPath));
            Assert.Equal("Electron2D.UserSettings", userDocument.RootElement.GetProperty("format").GetString());
            Assert.Equal(createdProjectPath, userDocument.RootElement.GetProperty("lastProjectPath").GetString());
            var recentProjects = userDocument.RootElement.GetProperty("recentProjects").EnumerateArray()
                .Select(project => project.GetString() ?? string.Empty)
                .ToArray();
            Assert.Equal(new[] { createdProjectPath }, recentProjects);
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
            Directory.Delete(userDataRoot, recursive: true);
        }
    }

    [Fact]
    public async Task EditorOpenProjectSmokeAcceptsNamedE2DFileAndLoadsMainScene()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var referenceProjectFile = Path.Combine(root, "examples", "platformer", "Platformer.e2d");
        var userDataRoot = CreateTemporaryDirectory("electron2d-editor-open-project-");

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
            startInfo.ArgumentList.Add("--open-project-smoke");
            startInfo.ArgumentList.Add(referenceProjectFile);
            startInfo.ArgumentList.Add("--user-data-dir");
            startInfo.ArgumentList.Add(userDataRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor open project smoke failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor open project smoke passed", output);
            Assert.Equal("Platformer", lines["ProjectName"]);
            Assert.Equal(referenceProjectFile, lines["ProjectSettingsPath"]);
            Assert.Equal(Path.Combine(root, "examples", "platformer"), lines["ProjectPath"]);
            Assert.Equal(Path.Combine(root, "examples", "platformer", "scenes", "main.scene.json"), lines["MainScenePath"]);
            Assert.Equal("True", lines["MainSceneLoaded"]);
            Assert.Equal("1", lines["RecentProjects"]);
        }
        finally
        {
            Directory.Delete(userDataRoot, recursive: true);
        }
    }

    [Fact]
    public void FileArgumentStartupDoesNotDependOnLegacyAssociationScript()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(root, "tools", "Register-Electron2DFileAssociation.ps1");
        var projectManagerDoc = File.ReadAllText(Path.Combine(root, "docs", "editor", "project-manager.md"));

        Assert.False(File.Exists(scriptPath), "Legacy .e2d association helper must stay removed from the repository workflow.");
        Assert.Contains("запуск с файлом проекта", projectManagerDoc, StringComparison.Ordinal);
        Assert.Contains("Electron2D.Editor.exe \"<ProjectName>.e2d\"", projectManagerDoc, StringComparison.Ordinal);
        Assert.DoesNotContain("Register-Electron2DFileAssociation", projectManagerDoc, StringComparison.Ordinal);
        Assert.DoesNotContain("Software\\Classes\\.e2d", projectManagerDoc, StringComparison.Ordinal);
    }

    private static void AssertAgentReadyProject(string projectRoot, string rendererProfile)
    {
        Assert.True(Directory.Exists(Path.Combine(projectRoot, ".git")));
        Assert.False(File.Exists(Path.Combine(projectRoot, "TASKS.md")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "completed-tasks")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "dev-diary")));

        var gitIgnorePath = Path.Combine(projectRoot, ".gitignore");
        Assert.True(File.Exists(gitIgnorePath));
        var gitIgnore = File.ReadAllText(gitIgnorePath);
        Assert.Contains(".electron2d/import-cache/", gitIgnore, StringComparison.Ordinal);
        Assert.Contains(".electron2d/workspaces/", gitIgnore, StringComparison.Ordinal);
        Assert.Contains(".electron2d/context/", gitIgnore, StringComparison.Ordinal);
        Assert.Contains(".electron2d/session/", gitIgnore, StringComparison.Ordinal);
        Assert.Contains(".electron2d/user/", gitIgnore, StringComparison.Ordinal);
        Assert.DoesNotContain(".electron2d/", gitIgnore.Split(Environment.NewLine, StringSplitOptions.TrimEntries));
        Assert.DoesNotContain(".electron2d/tasks/", gitIgnore, StringComparison.Ordinal);

        var agentsPath = Path.Combine(projectRoot, "AGENTS.md");
        Assert.True(File.Exists(agentsPath));
        var agents = File.ReadAllText(agentsPath);
        Assert.Contains("Electron2D 0.1.0-preview", agents, StringComparison.Ordinal);
        Assert.Contains(".NET 10.0.101", agents, StringComparison.Ordinal);
        Assert.Contains($"Renderer profile: `{rendererProfile}`", agents, StringComparison.Ordinal);
        Assert.Contains("e2d validate", agents, StringComparison.Ordinal);
        Assert.Contains("e2d api compare-godot <type>", agents, StringComparison.Ordinal);
        Assert.Contains("active Editor session", agents, StringComparison.Ordinal);
        Assert.Contains("ProjectTaskManager", agents, StringComparison.Ordinal);
        Assert.Contains("task_submit_for_acceptance", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("completed-tasks", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("dev-diary", agents, StringComparison.Ordinal);

        var skillFiles = Directory.EnumerateFiles(Path.Combine(projectRoot, ".codex", "skills"), "SKILL.md", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(5, skillFiles.Length);
        foreach (var skillFile in skillFiles)
        {
            var skill = File.ReadAllText(skillFile);
            Assert.StartsWith("---", skill, StringComparison.Ordinal);
            Assert.Contains("name:", skill, StringComparison.Ordinal);
            Assert.Contains("description:", skill, StringComparison.Ordinal);
            Assert.DoesNotContain("G:\\", skill, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TASKS.md", skill, StringComparison.Ordinal);
        }

        var boardPath = Path.Combine(projectRoot, ".electron2d", "tasks", "board.e2tasks");
        var taskPath = Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task");
        Assert.True(File.Exists(boardPath));
        Assert.True(File.Exists(taskPath));

        using var boardDocument = JsonDocument.Parse(File.ReadAllText(boardPath));
        Assert.Equal("Electron2D.TaskBoard", boardDocument.RootElement.GetProperty("format").GetString());
        Assert.Contains(
            boardDocument.RootElement.GetProperty("columns").EnumerateArray(),
            column => column.GetProperty("status").GetString() == "Backlog" &&
                column.GetProperty("taskIds").EnumerateArray().Any(task => task.GetString() == "welcome"));

        using var taskDocument = JsonDocument.Parse(File.ReadAllText(taskPath));
        Assert.Equal("Electron2D.TaskFile", taskDocument.RootElement.GetProperty("format").GetString());
        Assert.Equal("welcome", taskDocument.RootElement.GetProperty("taskId").GetString());
        Assert.Equal("Backlog", taskDocument.RootElement.GetProperty("status").GetString());
        Assert.NotEmpty(taskDocument.RootElement.GetProperty("acceptanceCriteria").EnumerateArray());
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
}
