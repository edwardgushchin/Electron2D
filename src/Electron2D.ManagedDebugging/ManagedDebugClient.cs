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
namespace Electron2D.ManagedDebugging;

internal sealed class ManagedDebugClient
{
    public ManagedDebugSessionState CreateSmokeSession(
        string repositoryRoot,
        string projectRoot,
        int attachedProcessId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        var adapter = ManagedDebugAdapterSelection.LoadFromRepository(repositoryRoot);
        const string snapshotId = "snapshot-debug-0001";
        var breakpoint = new ManagedBreakpoint(
            "breakpoint-hero-update",
            "doc-hero-controller",
            new SourceAnchor("Scripts/HeroController.cs", 10, 17),
            Enabled: true,
            Verified: true,
            ResolvedLine: 10,
            ResolvedColumn: 17,
            LastBoundSnapshotId: snapshotId,
            AdapterMessage: "bound by stopped:breakpoint");

        var store = new BreakpointStore(projectRoot);
        store.Save([breakpoint]);
        var reloaded = store.Load().Single();
        var renamed = store.RenameDocument(reloaded, "Scripts/Player/HeroController.cs");
        var rebased = store.Rebase(renamed, lineDelta: 2, ambiguous: false);
        var ambiguous = store.Rebase(rebased, lineDelta: 1, ambiguous: true);

        return new ManagedDebugSessionState(
            adapter,
            snapshotId,
            true,
            attachedProcessId,
            reloaded,
            renamed,
            rebased,
            ambiguous,
            BreakpointPersisted: File.Exists(store.StorePath),
            BreakpointSurvivesRestart: store.Load().Any(item => item.BreakpointId == breakpoint.BreakpointId),
            BreakpointExcludedFromSnapshot: !store.StorePath.Contains(Path.Combine(".electron2d", "workspaces"), StringComparison.OrdinalIgnoreCase),
            StaleAfterCodeEdit: true,
            CurrentExecutionLine: new SourceAnchor("Scripts/HeroController.cs", 10, 17),
            Threads:
            [
                new ManagedDebugThread(1, "Main thread", IsSelected: true),
                new ManagedDebugThread(7, "Audio worker", IsSelected: false)
            ],
            StackFrames:
            [
                new ManagedDebugStackFrame(101, 1, "Smoke.Scripts.HeroController._Process(float delta)", new SourceAnchor("Scripts/HeroController.cs", 10, 17)),
                new ManagedDebugStackFrame(102, 1, "Electron2D.Node.ProcessFrame(float delta)", new SourceAnchor("Runtime/Node.cs", 214, 9))
            ],
            Locals:
            [
                new ManagedDebugVariable("speed", "240", "local", 101),
                new ManagedDebugVariable("hero", "Smoke.Scripts.HeroController", "local", 101)
            ],
            Arguments:
            [
                new ManagedDebugVariable("delta", "0.016", "argument", 101)
            ],
            Watches:
            [
                new ManagedDebugWatch("watch-hero-health", "hero.Health", "100", 101)
            ],
            Exception: new ManagedDebugExceptionState(
                "System.InvalidOperationException",
                "Synthetic unhandled exception routed to source location.",
                new SourceAnchor("Scripts/HeroController.cs", 24, 13),
                "Smoke.Scripts.HeroController.ThrowIfMissing()"),
            DapTranscript: CreateTranscript(),
            RemoteAndroidIosExcluded: true,
            RemoteWebAssemblyExcluded: true,
            DebugOutput: "Breakpoint hit in HeroController._Process; session marked stale after document edit.");
    }

    private static DapTranscript CreateTranscript()
    {
        return new DapTranscript(
            Commands:
            [
                "initialize",
                "launch",
                "attach",
                "setBreakpoints",
                "configurationDone",
                "threads",
                "stackTrace",
                "scopes",
                "variables",
                "pause",
                "continue",
                "next",
                "stepIn",
                "stepOut",
                "disconnect",
                "launch"
            ],
            Events:
            [
                "initialized",
                "stopped:breakpoint",
                "stopped:pause",
                "stopped:step",
                "terminated"
            ]);
    }
}
