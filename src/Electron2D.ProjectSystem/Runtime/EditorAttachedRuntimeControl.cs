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
using System.Collections.ObjectModel;

namespace Electron2D.ProjectSystem;

internal enum RuntimeVisibleMode
{
    SeparateWindow,
    EmbeddedViewport
}

internal enum RuntimeStepKind
{
    Frame,
    Physics
}

internal sealed class ProjectWorkspaceRuntimeSession
{
    private readonly List<StructuredDiagnostic> diagnostics = [];
    private readonly RuntimeDebugSession debugSession;

    internal ProjectWorkspaceRuntimeSession(
        RuntimeDebugSession debugSession,
        WorkspaceJob job,
        RuntimeVisibleMode visibleMode,
        bool isProcessIsolated)
    {
        ArgumentNullException.ThrowIfNull(debugSession);
        ArgumentNullException.ThrowIfNull(job);

        this.debugSession = debugSession;
        Job = job;
        VisibleMode = visibleMode;
        IsProcessIsolated = isProcessIsolated;
    }

    public WorkspaceJob Job { get; }

    public string SessionId => debugSession.SessionId;

    public RuntimeDebugSessionKind SessionKind => debugSession.SessionKind;

    public RuntimeDebugSessionState State => debugSession.State;

    public RuntimeVisibleMode VisibleMode { get; }

    public bool IsProcessIsolated { get; }

    public string InputSnapshotId => Job.InputIdentity.InputSnapshotId;

    public ProjectWorkspaceRevision InputWorkspaceRevision => Job.InputIdentity.InputWorkspaceRevision;

    public ProjectWorkspaceRevision InputContentRevision => Job.InputIdentity.InputContentRevision;

    public IReadOnlyDictionary<string, ProjectDocumentRevision> InputDocumentRevisions =>
        new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            Job.InputIdentity.InputDocumentRevisions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));

    public string InputBuildConfigurationHash => Job.InputIdentity.InputBuildConfigurationHash;

    public string ScenePath => debugSession.Scene;

    public int CurrentFrame => debugSession.CurrentFrame;

    public int CurrentPhysicsFrame => debugSession.CurrentPhysicsFrame;

    public IReadOnlyDictionary<string, bool> InputActions => debugSession.InputActions;

    public string? HighlightedNodePath { get; private set; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics => diagnostics.Concat(debugSession.Diagnostics).ToArray();

    public RuntimeDebugMetricsSnapshot GetMetrics()
    {
        return debugSession.GetMetrics();
    }

    public RuntimeDebugSceneTreeSnapshot GetSceneTree()
    {
        return debugSession.GetSceneTree();
    }

    public RuntimeDebugScreenshot CaptureFrame()
    {
        return debugSession.CaptureScreenshot();
    }

    public void Pause()
    {
        debugSession.Pause();
    }

    public void Resume()
    {
        debugSession.Resume();
    }

    public void Stop()
    {
        debugSession.Stop();
    }

    public void Step(RuntimeStepKind kind, int count, double fixedDelta)
    {
        if (kind == RuntimeStepKind.Frame)
        {
            debugSession.StepFrame(count, fixedDelta);
            return;
        }

        debugSession.StepPhysics(count, fixedDelta);
    }

    public void InjectInput(string action, bool pressed)
    {
        debugSession.InjectInput(action, pressed);
    }

    public RuntimeDebugCommandResult HighlightNode(string nodePath)
    {
        var inspected = debugSession.InspectNode(nodePath);
        if (inspected.Succeeded)
        {
            HighlightedNodePath = inspected.Node!.Path;
        }

        return inspected;
    }

    public void ReportProcessCrash(int exitCode, string stderr)
    {
        debugSession.MarkCrashed();
        diagnostics.Add(RuntimeDebugBridge.CreateDiagnostic(
            $"Editor-attached game process crashed with exit code {exitCode}: {Sanitize(stderr)}",
            file: ScenePath));
    }

    private static string Sanitize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var sanitized = value.ReplaceLineEndings(" ").Trim();
        return sanitized.Length <= 240 ? sanitized : sanitized[..240];
    }
}

internal sealed class ProjectWorkspaceRuntimeSessionStore
{
    public ProjectWorkspaceRuntimeSession? ActiveSession { get; private set; }

    public ProjectWorkspaceRuntimeSession StartEditorAttached(
        RuntimeDebugSession debugSession,
        WorkspaceJob job,
        RuntimeVisibleMode visibleMode)
    {
        ArgumentNullException.ThrowIfNull(debugSession);
        ArgumentNullException.ThrowIfNull(job);

        if (ActiveSession is { State: RuntimeDebugSessionState.Running or RuntimeDebugSessionState.Paused })
        {
            throw new InvalidOperationException("An editor-attached runtime session is already active.");
        }

        ActiveSession = new ProjectWorkspaceRuntimeSession(
            debugSession,
            job,
            visibleMode,
            isProcessIsolated: true);
        return ActiveSession;
    }

    public void ClearActiveSession(ProjectWorkspaceRuntimeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (ReferenceEquals(ActiveSession, session))
        {
            ActiveSession = null;
        }
    }
}
