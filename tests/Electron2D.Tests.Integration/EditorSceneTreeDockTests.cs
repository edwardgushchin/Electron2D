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

public sealed class EditorSceneTreeDockTests
{
    [Fact]
    public async Task SceneTreeDockSmokeRunEditsSavesReloadsAndKeepsOwnershipValid()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-scene-tree-dock-");

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
            startInfo.ArgumentList.Add("--scene-tree-dock-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Scene Tree dock smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);
            var scenePath = lines["ScenePath"];

            Assert.Contains("Electron2D.Editor scene tree dock smoke passed", output);
            Assert.Equal("6", lines["NodeCount"]);
            Assert.Equal("0", lines["InvalidOwnerCount"]);
            Assert.Equal("True", lines["UndoAvailable"]);
            Assert.Equal("True", lines["UndoRestored"]);
            Assert.Equal("True", lines["RedoRemoved"]);
            Assert.Equal("Main", lines["TreeRootText"]);
            Assert.Equal(
                "Main/Player|Main/UI|Main/EnemySpawner|Main/EnemySpawner/Player Copy|Main/EnemySpawner/Player Copy/Weapon Copy",
                lines["ScenePaths"]);

            using var document = JsonDocument.Parse(File.ReadAllText(scenePath));
            Assert.Equal("Electron2D.SceneFile", document.RootElement.GetProperty("format").GetString());
            var nodes = document.RootElement.GetProperty("nodes").EnumerateArray().ToArray();
            Assert.Equal(6, nodes.Length);
            Assert.DoesNotContain(nodes, node => node.GetProperty("name").GetString() == "Weapon");

            var ids = nodes.Select(node => node.GetProperty("id").GetInt32()).ToHashSet();
            Assert.Contains(nodes, node =>
                node.GetProperty("name").GetString() == "EnemySpawner" &&
                node.GetProperty("parent").GetInt32() == 1 &&
                node.GetProperty("owner").GetInt32() == 1);
            Assert.Contains(nodes, node =>
                node.GetProperty("name").GetString() == "Player Copy" &&
                node.GetProperty("parent").GetInt32() != 1 &&
                node.GetProperty("owner").GetInt32() == 1);

            foreach (var node in nodes)
            {
                if (node.GetProperty("parent").ValueKind != JsonValueKind.Null)
                {
                    Assert.Contains(node.GetProperty("parent").GetInt32(), ids);
                }

                if (node.GetProperty("owner").ValueKind != JsonValueKind.Null)
                {
                    Assert.Contains(node.GetProperty("owner").GetInt32(), ids);
                }
            }
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
