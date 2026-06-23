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

internal sealed record ManagedDebugAdapterInfo(
    string AdapterId,
    string ReleaseTag,
    string Boundary,
    IReadOnlyList<string> Arguments,
    string RestartStrategy);

internal sealed record SourceAnchor(
    string Path,
    int Line,
    int Column)
{
    public override string ToString()
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Path}:{Line}:{Column}");
    }
}

internal sealed record ManagedBreakpoint(
    string BreakpointId,
    string DocumentId,
    SourceAnchor SourceAnchor,
    bool Enabled,
    bool Verified,
    int? ResolvedLine,
    int? ResolvedColumn,
    string? LastBoundSnapshotId,
    string AdapterMessage);

internal sealed record ManagedDebugThread(
    int ThreadId,
    string Name,
    bool IsSelected);

internal sealed record ManagedDebugStackFrame(
    int FrameId,
    int ThreadId,
    string Display,
    SourceAnchor Source);

internal sealed record ManagedDebugVariable(
    string Name,
    string Value,
    string Kind,
    int FrameId);

internal sealed record ManagedDebugWatch(
    string WatchId,
    string Expression,
    string Value,
    int FrameId);

internal sealed record ManagedDebugExceptionState(
    string Type,
    string Message,
    SourceAnchor Source,
    string StackTrace);

internal sealed record DapTranscript(
    IReadOnlyList<string> Commands,
    IReadOnlyList<string> Events)
{
    public bool Has(string commandOrEvent)
    {
        return Commands.Contains(commandOrEvent, StringComparer.Ordinal) ||
            Events.Contains(commandOrEvent, StringComparer.Ordinal);
    }
}

internal sealed record ManagedDebugSessionState(
    ManagedDebugAdapterInfo Adapter,
    string SnapshotId,
    bool DebugBuildPortablePdb,
    int AttachedProcessId,
    ManagedBreakpoint Breakpoint,
    ManagedBreakpoint RenamedBreakpoint,
    ManagedBreakpoint RebasedBreakpoint,
    ManagedBreakpoint AmbiguousBreakpoint,
    bool BreakpointPersisted,
    bool BreakpointSurvivesRestart,
    bool BreakpointExcludedFromSnapshot,
    bool StaleAfterCodeEdit,
    SourceAnchor CurrentExecutionLine,
    IReadOnlyList<ManagedDebugThread> Threads,
    IReadOnlyList<ManagedDebugStackFrame> StackFrames,
    IReadOnlyList<ManagedDebugVariable> Locals,
    IReadOnlyList<ManagedDebugVariable> Arguments,
    IReadOnlyList<ManagedDebugWatch> Watches,
    ManagedDebugExceptionState Exception,
    DapTranscript DapTranscript,
    bool RemoteAndroidIosExcluded,
    bool RemoteWebAssemblyExcluded,
    string DebugOutput);
