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
using System.Xml.Linq;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorProjectShellTests
{
    [Fact]
    public void EditorProjectIsPartOfSolutionAndUsesElectron2DRuntimeOnly()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");

        Assert.True(File.Exists(projectPath), "Electron2D.Editor project file must exist.");

        var solutionText = File.ReadAllText(Path.Combine(root, "src", "Electron2D.sln"));

        Assert.Contains("Electron2D.Editor", solutionText);

        var project = XDocument.Load(projectPath);
        var packageReferences = project.Descendants("PackageReference")
            .Select(item => item.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        var projectReferences = project.Descendants("ProjectReference")
            .Select(item => item.Attribute("Include")?.Value?.Replace('\\', '/'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        Assert.Contains("../Electron2D/Electron2D.csproj", projectReferences);
        Assert.DoesNotContain(packageReferences, package => package!.Contains("Avalonia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packageReferences, package => package!.Contains("WinForms", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packageReferences, package => package!.Contains("WPF", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EditorProjectSmokeRunStartsOnElectron2D()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
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
        startInfo.ArgumentList.Add("--smoke");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Editor smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");
        Assert.Contains("Electron2D.Editor smoke passed", output);
        Assert.Contains("Runtime=Electron2D", output);
        Assert.Contains("UiRoot=Electron2D.Panel", output);
    }

    [Fact]
    public async Task EditorWindowSmokeRunCreatesRealWindowAndWritesVisualArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-window-smoke-");

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
            startInfo.ArgumentList.Add("--window-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(45)));
            if (completed != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Editor window smoke did not exit within the expected time.");
            }

            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor window smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor window smoke passed", output);
            Assert.Equal("Electron2D.Editor", lines["WindowTitle"]);
            Assert.Equal("True", lines["WindowCreated"]);
            Assert.Equal("True", lines["WindowShown"]);
            Assert.Equal("True", lines["FramePresented"]);
            Assert.Equal("True", lines["EventPumpObserved"]);
            Assert.Equal("True", lines["PointerInteractionObserved"]);
            Assert.Equal("True", lines["KeyboardInteractionObserved"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);
            Assert.Equal("Tasks", lines["SelectedWorkspace"]);
            Assert.Equal("0", lines["TextOverflowCount"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.Equal("T-0157|T-0150|T-0155|T-0158|T-0159|T-0160|T-0161", lines["ReattestedVisibleLayers"]);

            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(screenshotPath), $"Missing window screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing window visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.EditorWindowSmokeAnalysis", data.GetProperty("format").GetString());
            Assert.True(data.GetProperty("window").GetProperty("actualWindow").GetBoolean());
            Assert.Equal("Electron2D.Editor", data.GetProperty("window").GetProperty("title").GetString());
            Assert.True(data.GetProperty("window").GetProperty("shown").GetBoolean());
            Assert.True(data.GetProperty("eventLoop").GetProperty("observed").GetBoolean());
            Assert.True(data.GetProperty("rendering").GetProperty("framePresented").GetBoolean());
            Assert.True(data.GetProperty("input").GetProperty("pointerInteractionObserved").GetBoolean());
            Assert.True(data.GetProperty("input").GetProperty("keyboardInteractionObserved").GetBoolean());
            Assert.Equal("Tasks", data.GetProperty("layout").GetProperty("selectedWorkspace").GetString());
            Assert.Equal(0, data.GetProperty("layout").GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("layout").GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("layout").GetProperty("clickableControlCount").GetInt32() >= 16);
            var reattestations = data.GetProperty("reattestedVisibleLayers").EnumerateArray().ToArray();
            Assert.Equal(7, reattestations.Length);
            Assert.All(reattestations, item => Assert.True(item.GetProperty("presentedInWindow").GetBoolean()));
            Assert.Equal(
                new[] { "T-0157", "T-0150", "T-0155", "T-0158", "T-0159", "T-0160", "T-0161" },
                reattestations.Select(item => item.GetProperty("taskId").GetString()).ToArray());
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
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
