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

public sealed class EditorFileSystemDockTests
{
    [Fact]
    public async Task FileSystemDockSmokeRunBrowsesMovesReimportsSearchesAndShowsImportErrors()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-file-system-dock-");

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
            startInfo.ArgumentList.Add("--file-system-dock-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"FileSystem dock smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);
            var scenePath = lines["ScenePath"];

            Assert.Contains("Electron2D.Editor file system dock smoke passed", output);
            Assert.True(int.Parse(lines["InitialItemCount"], System.Globalization.CultureInfo.InvariantCulture) >= 2);
            Assert.Equal("True", lines["FolderCreated"]);
            Assert.Equal("True", lines["MovedFileExists"]);
            Assert.Equal("res://assets/player_texture.e2res", lines["RenamedResourcePath"]);
            Assert.Equal("res://assets/characters/player_texture.e2res", lines["MovedResourcePath"]);
            Assert.Equal(lines["UidBefore"], lines["UidAfter"]);
            Assert.Equal("True", lines["UidStable"]);
            Assert.Equal(lines["UidAfter"], lines["SceneExternalReferenceUid"]);
            Assert.Equal("res://assets/characters/player_texture.e2res", lines["SceneExternalReferencePath"]);
            Assert.Equal("Electron2D.Sprite2D", lines["DraggedNodeType"]);
            Assert.Equal("res://assets/characters/player_texture.e2res", lines["SearchResults"]);
            Assert.Equal("1", lines["ImportErrorCount"]);
            Assert.Equal("res://assets/broken.e2res", lines["ImportErrorPath"]);
            Assert.Equal("True", lines["ImportErrorVisible"]);
            Assert.Equal("True", lines["LiveImportStatusVisible"]);
            Assert.Equal("True", lines["RoundTripStable"]);

            using var document = JsonDocument.Parse(File.ReadAllText(scenePath));
            Assert.Equal("Electron2D.SceneFile", document.RootElement.GetProperty("format").GetString());
            var externalReference = Assert.Single(document.RootElement.GetProperty("external").EnumerateArray());
            Assert.Equal(lines["UidAfter"], externalReference.GetProperty("uid").GetString());
            Assert.Equal("res://assets/characters/player_texture.e2res", externalReference.GetProperty("path").GetString());

            var draggedNode = Assert.Single(document.RootElement.GetProperty("nodes").EnumerateArray(), node =>
                node.GetProperty("type").GetString() == "Electron2D.Sprite2D");
            Assert.Equal("Player Texture", draggedNode.GetProperty("name").GetString());
            Assert.Equal("Resource", draggedNode.GetProperty("properties").GetProperty("texture").GetProperty("kind").GetString());
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
