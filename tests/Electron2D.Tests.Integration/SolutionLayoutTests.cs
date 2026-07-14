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
using System.Text;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SolutionLayoutTests
{
    [Fact]
    public void RepositoryContainsRuntimeProjectAndReleaseSpecification()
    {
        var root = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(root, "src", "Electron2D", "Electron2D.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "releases", "0.1-preview.md")));
        Assert.True(Directory.Exists(Path.Combine(root, "data", "templates", "electron2d-empty")));
        Assert.True(Directory.Exists(Path.Combine(root, "data", "assets", "reference-games")));
        Assert.True(Directory.Exists(Path.Combine(root, "data", "schemas")));
        Assert.True(Directory.Exists(Path.Combine(root, ".taskboard")));
        Assert.True(Directory.Exists(Path.Combine(root, "data", "dev-diary")));
        Assert.False(Directory.Exists(Path.Combine(root, "templates")));
        Assert.False(Directory.Exists(Path.Combine(root, "assets")));
        Assert.False(Directory.Exists(Path.Combine(root, "schemas")));
        Assert.False(Directory.Exists(Path.Combine(root, "completed-tasks")));
        Assert.False(Directory.Exists(Path.Combine(root, "dev-diary")));
        Assert.False(Directory.Exists(Path.Combine(root, "tools")));
    }

    [Fact]
    public void RepositoryTracksTaskboardAndDiaryWithoutLegacyTaskFiles()
    {
        var root = FindRepositoryRoot();
        var repositoryFiles = GetRepositoryFilesVisibleToGit();
        Assert.Contains(".taskboard/board.e2tasks", repositoryFiles);
        Assert.Contains(repositoryFiles, file => file.StartsWith(".taskboard/tasks/", StringComparison.Ordinal));
        Assert.Contains(repositoryFiles, file => file.StartsWith(".taskboard/completed/", StringComparison.Ordinal));
        Assert.Contains(repositoryFiles, file => file.StartsWith("data/dev-diary/", StringComparison.Ordinal));
        Assert.Contains(repositoryFiles, file => file.StartsWith("data/schemas/", StringComparison.Ordinal));
        Assert.DoesNotContain("TASKS.md", repositoryFiles);
        Assert.DoesNotContain(repositoryFiles, file => file.StartsWith("data/completed-tasks/", StringComparison.Ordinal));

        var forbidden = new[]
        {
            "CHANGELOG.md",
            "RELEASE-NOTES.md"
        };

        foreach (var file in forbidden)
        {
            Assert.DoesNotContain(file, repositoryFiles);
        }

        Assert.DoesNotContain(repositoryFiles, file => file.StartsWith("completed-tasks/", StringComparison.Ordinal));
        Assert.DoesNotContain(repositoryFiles, file => file.StartsWith("dev-diary/", StringComparison.Ordinal));
        Assert.DoesNotContain(repositoryFiles, file => file.StartsWith("schemas/", StringComparison.Ordinal));

        var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
        foreach (var pattern in new[] { "/CHANGELOG*", "/RELEASE-NOTES*", ".electron2d/user/" })
        {
            Assert.Contains(pattern, gitignore, StringComparison.Ordinal);
        }
    }

    private static string[] GetRepositoryFilesVisibleToGit()
    {
        var root = FindRepositoryRoot();
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("core.quotePath=false");
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("--cached");
        startInfo.ArgumentList.Add("--others");
        startInfo.ArgumentList.Add("--exclude-standard");

        using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, $"git ls-files failed: {error}");
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(file => File.Exists(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar))))
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
