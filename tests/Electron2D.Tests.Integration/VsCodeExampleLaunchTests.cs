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

public sealed class VsCodeExampleLaunchTests
{
    [Fact]
    public void VsCodeExampleLaunchDocumentationDescribesRunAndDebugWorkflow()
    {
        var root = FindRepositoryRoot();
        var documentPath = Path.Combine(root, "docs", "tooling", "vscode-example-launch.md");

        Assert.True(File.Exists(documentPath), $"Missing VS Code example launch document: {documentPath}");

        var document = File.ReadAllText(documentPath);
        Assert.Contains("Run and Debug", document, StringComparison.Ordinal);
        Assert.Contains("Electron2D: Platformer", document, StringComparison.Ordinal);
        Assert.Contains("examples/platformer", document, StringComparison.Ordinal);
        Assert.Contains("src/Electron2D.Cli/Electron2D.Cli.csproj", document, StringComparison.Ordinal);
        Assert.Contains("debug target", document, StringComparison.Ordinal);
        Assert.Contains("e2d.exe", document, StringComparison.Ordinal);
        Assert.Contains("Platformer.csproj", document, StringComparison.Ordinal);
    }

    [Fact]
    public void LaunchJsonRunsSelectedExampleThroughElectron2DCliRunner()
    {
        var root = FindRepositoryRoot();
        var launchPath = Path.Combine(root, ".vscode", "launch.json");

        Assert.True(File.Exists(launchPath), $"Missing VS Code launch configuration: {launchPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(launchPath));
        var rootElement = document.RootElement;
        Assert.Equal("0.2.0", rootElement.GetProperty("version").GetString());

        var configuration = Assert.Single(rootElement.GetProperty("configurations").EnumerateArray());
        Assert.Equal("Electron2D: Platformer", configuration.GetProperty("name").GetString());
        Assert.Equal("coreclr", configuration.GetProperty("type").GetString());
        Assert.Equal("launch", configuration.GetProperty("request").GetString());
        Assert.Equal("Electron2D: build CLI", configuration.GetProperty("preLaunchTask").GetString());
        Assert.Equal("${workspaceFolder}/src/Electron2D.Cli/bin/Debug/net10.0/e2d.dll", configuration.GetProperty("program").GetString());
        Assert.Equal(
            "${workspaceFolder}/src/Electron2D.Cli/bin/Debug/net10.0/e2d.exe",
            configuration.GetProperty("windows").GetProperty("program").GetString());
        Assert.Equal("${workspaceFolder}", configuration.GetProperty("cwd").GetString());
        Assert.Equal("integratedTerminal", configuration.GetProperty("console").GetString());
        Assert.Equal("neverOpen", configuration.GetProperty("internalConsoleOptions").GetString());

        var args = ReadStringArray(configuration.GetProperty("args"));
        Assert.Equal(
            [
                "run",
                "--project",
                "${workspaceFolder}/examples/platformer"
            ],
            args);
        Assert.DoesNotContain(args, argument => argument.Contains("Platformer.csproj", StringComparison.Ordinal));
        Assert.DoesNotContain(args, argument => argument.Contains("ui-heavy", StringComparison.OrdinalIgnoreCase));
        Assert.False(rootElement.TryGetProperty("inputs", out _));
    }

    [Fact]
    public void TasksJsonBuildsCliAndProvidesSelectedExampleTask()
    {
        var root = FindRepositoryRoot();
        var tasksPath = Path.Combine(root, ".vscode", "tasks.json");

        Assert.True(File.Exists(tasksPath), $"Missing VS Code tasks configuration: {tasksPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(tasksPath));
        var rootElement = document.RootElement;
        Assert.Equal("2.0.0", rootElement.GetProperty("version").GetString());

        var tasks = rootElement.GetProperty("tasks").EnumerateArray()
            .ToDictionary(task => task.GetProperty("label").GetString()!, StringComparer.Ordinal);
        Assert.True(tasks.ContainsKey("Electron2D: build CLI"));
        Assert.True(tasks.ContainsKey("Electron2D: run Platformer"));

        var buildArgs = ReadStringArray(tasks["Electron2D: build CLI"].GetProperty("args"));
        Assert.Equal(
            [
                "build",
                "${workspaceFolder}/src/Electron2D.Cli/Electron2D.Cli.csproj"
            ],
            buildArgs);

        var runArgs = ReadStringArray(tasks["Electron2D: run Platformer"].GetProperty("args"));
        Assert.Equal(
            [
                "run",
                "--project",
                "${workspaceFolder}/src/Electron2D.Cli/Electron2D.Cli.csproj",
                "--",
                "run",
                "--project",
                "${workspaceFolder}/examples/platformer"
            ],
            runArgs);
        Assert.DoesNotContain(runArgs, argument => argument.Contains("Platformer.csproj", StringComparison.Ordinal));
        Assert.DoesNotContain(runArgs, argument => argument.Contains("ui-heavy", StringComparison.OrdinalIgnoreCase));
        Assert.False(rootElement.TryGetProperty("inputs", out _));
    }

    private static string[] ReadStringArray(JsonElement element)
    {
        return element.EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
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
