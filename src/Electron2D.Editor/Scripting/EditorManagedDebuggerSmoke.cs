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
using System.Text.Json.Nodes;
using Electron2D.ManagedDebugging;

namespace Electron2D.Editor.Scripting;

internal static class EditorManagedDebuggerSmoke
{
    public static EditorManagedDebuggerSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);
        var projectRoot = Path.Combine(fullWorkRoot, "SmokeProject");
        Directory.CreateDirectory(projectRoot);

        var client = new ManagedDebugClient();
        var state = client.CreateSmokeSession(FindRepositoryRoot(), projectRoot, Environment.ProcessId);

        var statePath = Path.Combine(fullWorkRoot, "managed-debugger.state.json");
        File.WriteAllText(
            statePath,
            WriteState(state).ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        var visual = EditorManagedDebuggerVisualHarness.WriteArtifacts(
            state,
            Path.Combine(fullWorkRoot, "visual"));

        return new EditorManagedDebuggerSmokeResult(
            state,
            statePath,
            visual.ScreenshotPath,
            visual.AnalysisPath,
            visual.TextOverflowCount,
            visual.ClickableControlCount,
            visual.ForbiddenUiMatchCount,
            visual.ScreenshotReviewed);
    }

    private static JsonObject WriteState(ManagedDebugSessionState state)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ManagedDebuggerState",
            ["adapterId"] = state.Adapter.AdapterId,
            ["adapterReleaseTag"] = state.Adapter.ReleaseTag,
            ["dapBoundary"] = state.Adapter.Boundary,
            ["adapterArguments"] = string.Join(' ', state.Adapter.Arguments),
            ["snapshotId"] = state.SnapshotId,
            ["debugBuildPortablePdb"] = state.DebugBuildPortablePdb,
            ["attachedProcessId"] = state.AttachedProcessId,
            ["breakpointId"] = state.Breakpoint.BreakpointId,
            ["breakpointDocumentId"] = state.Breakpoint.DocumentId,
            ["breakpointSourceAnchor"] = state.Breakpoint.SourceAnchor.ToString(),
            ["breakpointPersisted"] = state.BreakpointPersisted,
            ["breakpointSurvivesRestart"] = state.BreakpointSurvivesRestart,
            ["breakpointExcludedFromSnapshot"] = state.BreakpointExcludedFromSnapshot,
            ["currentExecutionLine"] = state.CurrentExecutionLine.ToString(),
            ["threadCount"] = state.Threads.Count,
            ["selectedFrame"] = state.StackFrames.First().Display,
            ["staleAfterCodeEdit"] = state.StaleAfterCodeEdit,
            ["debugOutput"] = state.DebugOutput
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "data", "debugging")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        while (workingDirectory is not null)
        {
            if (Directory.Exists(Path.Combine(workingDirectory.FullName, "data", "debugging")) &&
                File.Exists(Path.Combine(workingDirectory.FullName, "src", "Electron2D.sln")))
            {
                return workingDirectory.FullName;
            }

            workingDirectory = workingDirectory.Parent;
        }

        throw new InvalidOperationException("Electron2D repository root was not found.");
    }
}
