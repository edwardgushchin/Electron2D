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
            Assert.False(Directory.Exists(Path.Combine(createdProjectPath, ".template.config")));
            Assert.True(File.Exists(projectSettingsPath));
            Assert.True(File.Exists(mainScenePath));
            Assert.True(File.Exists(userSettingsPath));

            using var projectDocument = JsonDocument.Parse(File.ReadAllText(projectSettingsPath));
            Assert.Equal("Electron2D.ProjectSettings", projectDocument.RootElement.GetProperty("format").GetString());
            Assert.Equal("ProjectManagerSmoke", projectDocument.RootElement.GetProperty("name").GetString());
            Assert.Equal("Compatibility", projectDocument.RootElement.GetProperty("rendererProfile").GetString());
            Assert.Equal("scenes/main.scene.json", projectDocument.RootElement.GetProperty("mainScene").GetString());

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
