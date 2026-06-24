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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorRunWorkflowTests
{
    [Fact]
    [Trait("Category", "Baseline")]
    public async Task RunWorkflowSmokeRunBuildsRunsStopsAndReportsDiagnostics()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-run-workflow-");

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
            startInfo.ArgumentList.Add("--run-workflow-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Run workflow smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor run workflow smoke passed", output);
            Assert.True(int.Parse(lines["BuildDiagnosticCount"], System.Globalization.CultureInfo.InvariantCulture) >= 1);
            Assert.Equal("CS1029", lines["BuildFirstCode"]);
            Assert.True(int.Parse(lines["BuildFirstLine"], System.Globalization.CultureInfo.InvariantCulture) > 0);
            Assert.True(int.Parse(lines["BuildFirstColumn"], System.Globalization.CultureInfo.InvariantCulture) > 0);
            Assert.Equal("False", lines["BuildFailureStartedProcess"]);
            Assert.Equal("0", lines["ProjectRunExitCode"]);
            Assert.Equal("0", lines["CurrentSceneRunExitCode"]);
            Assert.Equal("scenes/main.scene.json", lines["ProjectRunScene"]);
            Assert.Equal("scenes/alternate.scene.json", lines["CurrentSceneRunScene"]);
            Assert.Equal("True", lines["CurrentSceneOverrideStable"]);
            Assert.Equal("True", lines["OutputContainsProjectRun"]);
            Assert.Equal("True", lines["OutputContainsCurrentSceneRun"]);
            Assert.True(int.Parse(lines["OutputLineCount"], System.Globalization.CultureInfo.InvariantCulture) >= 4);
            Assert.Equal("1", lines["RuntimeFailureExitCode"]);
            Assert.Equal("True", lines["RuntimeStackTraceContains"]);
            Assert.Equal("1", lines["ShaderDiagnosticCount"]);
            Assert.Equal("res://shaders/broken.e2shader", lines["ShaderDiagnosticFile"]);
            Assert.True(int.Parse(lines["ShaderDiagnosticLine"], System.Globalization.CultureInfo.InvariantCulture) > 0);
            Assert.True(int.Parse(lines["ShaderDiagnosticColumn"], System.Globalization.CultureInfo.InvariantCulture) > 0);
            Assert.Equal("True", lines["StopRequested"]);
            Assert.Equal("True", lines["StopObserved"]);
            Assert.Equal("3", lines["RepeatedRunStopCycles"]);
            Assert.Equal("False", lines["ActiveSessionAfterStop"]);
            Assert.True(int.Parse(lines["FrameSamples"], System.Globalization.CultureInfo.InvariantCulture) >= 3);
            Assert.True(double.Parse(lines["LastFrameTimeMs"], System.Globalization.CultureInfo.InvariantCulture) > 0d);
            Assert.True(double.Parse(lines["AverageFrameTimeMs"], System.Globalization.CultureInfo.InvariantCulture) > 0d);
            Assert.True(double.Parse(lines["FramesPerSecond"], System.Globalization.CultureInfo.InvariantCulture) > 0d);
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
