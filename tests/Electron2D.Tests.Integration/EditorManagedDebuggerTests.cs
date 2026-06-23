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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorManagedDebuggerTests
{
    [Fact]
    public async Task ManagedDebuggerSmokeRunWritesDapBreakpointStateAndVisualAcceptanceArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-managed-debugger-");

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
            startInfo.ArgumentList.Add("--managed-debugger-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor managed debugger smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor managed debugger smoke passed", output);
            Assert.Equal("Electron2D.ManagedDebugging", lines["ManagedDebuggerAssembly"]);
            Assert.Equal("netcoredbg", lines["AdapterId"]);
            Assert.Equal("3.1.3-1062", lines["AdapterReleaseTag"]);
            Assert.Equal("Electron2D.Editor -> Electron2D.ManagedDebugging -> DAP stdio -> netcoredbg -> Electron2D game process", lines["DapBoundary"]);
            Assert.Equal("--interpreter=vscode", lines["AdapterArguments"]);
            Assert.Equal("True", lines["DapInitialize"]);
            Assert.Equal("True", lines["DapLaunch"]);
            Assert.Equal("True", lines["DapAttach"]);
            Assert.Equal("True", lines["DapSetBreakpoints"]);
            Assert.Equal("True", lines["DapStoppedBreakpoint"]);
            Assert.Equal("True", lines["DapThreads"]);
            Assert.Equal("True", lines["DapStackTrace"]);
            Assert.Equal("True", lines["DapScopes"]);
            Assert.Equal("True", lines["DapVariables"]);
            Assert.Equal("True", lines["DapPause"]);
            Assert.Equal("True", lines["DapContinue"]);
            Assert.Equal("True", lines["DapStepInto"]);
            Assert.Equal("True", lines["DapStepOver"]);
            Assert.Equal("True", lines["DapStepOut"]);
            Assert.Equal("editor-managed-disconnect-and-relaunch", lines["RestartStrategy"]);
            Assert.Equal("breakpoint-hero-update", lines["BreakpointId"]);
            Assert.Equal("doc-hero-controller", lines["BreakpointDocumentId"]);
            Assert.Equal("Scripts/HeroController.cs:10:17", lines["BreakpointSourceAnchor"]);
            Assert.Equal("True", lines["BreakpointEnabled"]);
            Assert.Equal("True", lines["BreakpointVerified"]);
            Assert.Equal("10", lines["BreakpointResolvedLine"]);
            Assert.Equal("17", lines["BreakpointResolvedColumn"]);
            Assert.Equal("snapshot-debug-0001", lines["BreakpointLastBoundSnapshotId"]);
            Assert.Equal("bound by stopped:breakpoint", lines["BreakpointAdapterMessage"]);
            Assert.Equal("True", lines["BreakpointPersisted"]);
            Assert.Equal("True", lines["BreakpointSurvivesRestart"]);
            Assert.Equal("True", lines["BreakpointExcludedFromSnapshot"]);
            Assert.Equal("Scripts/Player/HeroController.cs", lines["BreakpointRenamedPath"]);
            Assert.Equal("12", lines["BreakpointRebasedLine"]);
            Assert.Equal("False", lines["AmbiguousBreakpointVerified"]);
            Assert.Equal("True", lines["DebugBuildPortablePdb"]);
            Assert.Equal("snapshot-debug-0001", lines["SnapshotId"]);
            Assert.NotEqual("0", lines["AttachedProcessId"]);
            Assert.Equal("Scripts/HeroController.cs:10:17", lines["CurrentExecutionLine"]);
            Assert.Equal("2", lines["ThreadCount"]);
            Assert.Equal("Smoke.Scripts.HeroController._Process(float delta)", lines["SelectedFrame"]);
            Assert.Equal("delta=0.016", lines["ArgumentValue"]);
            Assert.Equal("speed=240", lines["LocalValue"]);
            Assert.Equal("hero.Health", lines["WatchExpression"]);
            Assert.Equal("100", lines["WatchValue"]);
            Assert.Equal("System.InvalidOperationException", lines["ExceptionType"]);
            Assert.Equal("True", lines["StaleAfterCodeEdit"]);
            Assert.Equal("True", lines["RemoteAndroidIosExcluded"]);
            Assert.Equal("True", lines["RemoteWebAssemblyExcluded"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var statePath = lines["StatePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing managed debugger state artifact: {statePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing managed debugger screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing managed debugger visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.ManagedDebuggerVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-managed-debugger-harness", data.GetProperty("harness").GetString());
            Assert.Equal("Script", data.GetProperty("selectedWorkspace").GetString());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 18);
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
            Assert.True(data.GetProperty("breakpointGutter").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("breakpointGutter").GetProperty("verified").GetBoolean());
            Assert.True(data.GetProperty("currentLineHighlight").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("debuggerControls").GetProperty("allClickable").GetBoolean());
            Assert.True(data.GetProperty("callStack").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("threads").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("variables").GetProperty("localsVisible").GetBoolean());
            Assert.True(data.GetProperty("variables").GetProperty("argumentsVisible").GetBoolean());
            Assert.True(data.GetProperty("watches").GetProperty("evaluationVisible").GetBoolean());
            Assert.True(data.GetProperty("exception").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("stale").GetProperty("visible").GetBoolean());
            Assert.Equal("netcoredbg", data.GetProperty("dap").GetProperty("adapterId").GetString());
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
