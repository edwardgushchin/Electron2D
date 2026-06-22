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

public sealed class EditorInspectorTests
{
    [Fact]
    public async Task InspectorSmokeRunEditsSerializedPropertiesDefaultsNestedResourcesAndUndoRedo()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-inspector-");

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
            startInfo.ArgumentList.Add("--inspector-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Inspector smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);
            var scenePath = lines["ScenePath"];

            Assert.Contains("Electron2D.Editor inspector smoke passed", output);
            Assert.Equal("8", lines["PropertyCount"]);
            Assert.Equal("8", lines["ExportedProperties"]);
            Assert.Equal("42", lines["SerializedHealth"]);
            Assert.Equal("Player", lines["SerializedName"]);
            Assert.Equal("Captain", lines["UndoName"]);
            Assert.Equal("Player", lines["RedoName"]);
            Assert.Equal("Attack", lines["SerializedMode"]);
            Assert.Equal("Air|Ground", lines["SerializedFlags"]);
            Assert.Equal("player,captain", lines["SerializedTags"]);
            Assert.Equal("Root/Player/Weapon", lines["SerializedPath"]);
            Assert.Equal("1", lines["ResourceReference"]);
            Assert.Equal("250", lines["NestedMaxHealth"]);
            Assert.Equal("True", lines["RoundTripStable"]);

            using var document = JsonDocument.Parse(File.ReadAllText(scenePath));
            Assert.Equal("Electron2D.SceneFile", document.RootElement.GetProperty("format").GetString());
            var node = Assert.Single(document.RootElement.GetProperty("nodes").EnumerateArray(), item =>
                item.GetProperty("name").GetString() == "Player");
            var properties = node.GetProperty("properties");
            Assert.True(properties.TryGetProperty("health", out _));
            Assert.True(properties.TryGetProperty("display_name", out _));
            Assert.True(properties.TryGetProperty("stats", out var statsProperty));
            Assert.Equal("Resource", statsProperty.GetProperty("kind").GetString());

            var internalResource = Assert.Single(document.RootElement.GetProperty("internal").EnumerateArray());
            Assert.Equal(1, internalResource.GetProperty("id").GetInt32());
            Assert.True(internalResource.GetProperty("properties").TryGetProperty("max_health", out _));
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
