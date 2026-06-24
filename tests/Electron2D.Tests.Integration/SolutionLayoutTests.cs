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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SolutionLayoutTests
{
    [Fact]
    public void RepositoryContainsRuntimeProjectAndReleaseSpecification()
    {
        var root = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(root, "src", "Electron2D", "Electron2D.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "releases", "0.1.0-preview.md")));
        Assert.True(Directory.Exists(Path.Combine(root, "data", "templates", "electron2d-empty")));
        Assert.True(Directory.Exists(Path.Combine(root, "data", "assets", "reference-games")));
        Assert.False(Directory.Exists(Path.Combine(root, "templates")));
        Assert.False(Directory.Exists(Path.Combine(root, "assets")));
    }

    [Fact]
    public void RepositoryDoesNotTrackLocalWorkMaterials()
    {
        var root = FindRepositoryRoot();
        var trackedFiles = GetTrackedFiles();
        var forbidden = new[]
        {
            "TASKS.md",
            "CHANGELOG.md",
            "RELEASE-NOTES.md"
        };

        foreach (var file in forbidden)
        {
            Assert.DoesNotContain(file, trackedFiles);
        }

        Assert.DoesNotContain(trackedFiles, file => file.StartsWith("completed-tasks/", StringComparison.Ordinal));
        Assert.DoesNotContain(trackedFiles, file => file.StartsWith("dev-diary/", StringComparison.Ordinal));

        var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
        foreach (var pattern in new[] { "/TASKS.md", "/CHANGELOG*", "/RELEASE-NOTES*", "/completed-tasks/", "/dev-diary/", ".electron2d/user/" })
        {
            Assert.Contains(pattern, gitignore, StringComparison.Ordinal);
        }

        var gitattributes = File.ReadAllText(Path.Combine(root, ".gitattributes"));
        Assert.Contains("data/assets/reference-games/** binary", gitattributes, StringComparison.Ordinal);
    }

    private static string[] GetTrackedFiles()
    {
        var root = FindRepositoryRoot();
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("ls-files");

        using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, $"git ls-files failed: {error}");
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
