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
using Electron2D.ProjectSystem;

namespace Electron2D.Tooling;

internal enum EditorSessionEndpointKind
{
    NamedPipe,
    UnixDomainSocket
}

internal enum EditorSessionAdapterKind
{
    Cli,
    Mcp
}

internal enum EditorSessionConnectionState
{
    ActiveEditor,
    ReadOnlyEditor,
    HeadlessFallback,
    Rejected
}

internal sealed class EditorSessionEndpoint
{
    private EditorSessionEndpoint(EditorSessionEndpointKind kind, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        Kind = kind;
        Address = address;
    }

    public EditorSessionEndpointKind Kind { get; }

    public string Address { get; }

    public static EditorSessionEndpoint NamedPipe(string address)
    {
        return new EditorSessionEndpoint(EditorSessionEndpointKind.NamedPipe, address);
    }

    public static EditorSessionEndpoint UnixDomainSocket(string address)
    {
        return new EditorSessionEndpoint(EditorSessionEndpointKind.UnixDomainSocket, address);
    }
}

internal sealed class EditorSessionDescriptor
{
    public EditorSessionDescriptor(
        string sessionId,
        string ownerId,
        string projectRoot,
        EditorSessionEndpoint endpoint,
        ProjectWorkspaceOpenMode openMode,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset lastHeartbeatUtc,
        DateTimeOffset leaseExpiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(endpoint);

        SessionId = sessionId;
        OwnerId = ownerId;
        ProjectRoot = projectRoot;
        Endpoint = endpoint;
        OpenMode = openMode;
        RegisteredAtUtc = registeredAtUtc;
        LastHeartbeatUtc = lastHeartbeatUtc;
        LeaseExpiresAtUtc = leaseExpiresAtUtc;
    }

    public string SessionId { get; }

    public string OwnerId { get; }

    public string ProjectRoot { get; }

    public EditorSessionEndpoint Endpoint { get; }

    public ProjectWorkspaceOpenMode OpenMode { get; }

    public DateTimeOffset RegisteredAtUtc { get; }

    public DateTimeOffset LastHeartbeatUtc { get; }

    public DateTimeOffset LeaseExpiresAtUtc { get; }

    public bool IsStale(DateTimeOffset nowUtc)
    {
        return nowUtc > LeaseExpiresAtUtc;
    }

    public EditorSessionDescriptor WithHeartbeat(DateTimeOffset heartbeatUtc, TimeSpan leaseTimeout)
    {
        if (heartbeatUtc < LastHeartbeatUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(heartbeatUtc), heartbeatUtc, "Editor session heartbeat must be monotonic.");
        }

        return new EditorSessionDescriptor(
            SessionId,
            OwnerId,
            ProjectRoot,
            Endpoint,
            OpenMode,
            RegisteredAtUtc,
            heartbeatUtc,
            heartbeatUtc.Add(leaseTimeout));
    }
}

internal sealed class EditorSessionOpenResult : IDisposable
{
    private readonly ProjectWorkspace? workspace;
    private readonly EditorSessionDescriptor? descriptor;
    private readonly bool disposeWorkspace;
    private bool disposed;

    private EditorSessionOpenResult(
        bool succeeded,
        EditorSessionConnectionState state,
        ProjectWorkspace? workspace,
        EditorSessionDescriptor? descriptor,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        bool disposeWorkspace)
    {
        Succeeded = succeeded;
        State = state;
        this.workspace = workspace;
        this.descriptor = descriptor;
        Diagnostics = diagnostics.ToArray();
        this.disposeWorkspace = disposeWorkspace;
    }

    public bool Succeeded { get; }

    public EditorSessionConnectionState State { get; }

    public ProjectWorkspace Workspace => workspace ?? throw new InvalidOperationException("Editor session result does not contain a workspace.");

    public ProjectWorkspace? WorkspaceOrNull => workspace;

    public EditorSessionDescriptor Descriptor => descriptor ?? throw new InvalidOperationException("Editor session result does not contain a descriptor.");

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static EditorSessionOpenResult Active(ProjectWorkspace workspace, EditorSessionDescriptor descriptor)
    {
        return new EditorSessionOpenResult(true, EditorSessionConnectionState.ActiveEditor, workspace, descriptor, [], disposeWorkspace: true);
    }

    public static EditorSessionOpenResult ReadOnly(ProjectWorkspace workspace, EditorSessionDescriptor descriptor, StructuredDiagnostic diagnostic)
    {
        return new EditorSessionOpenResult(true, EditorSessionConnectionState.ReadOnlyEditor, workspace, descriptor, [diagnostic], disposeWorkspace: true);
    }

    public static EditorSessionOpenResult Rejected(StructuredDiagnostic diagnostic)
    {
        return new EditorSessionOpenResult(false, EditorSessionConnectionState.Rejected, null, null, [diagnostic], disposeWorkspace: false);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (disposeWorkspace)
        {
            workspace?.Dispose();
        }
    }
}

internal sealed class EditorSessionConnectResult : IDisposable
{
    private readonly ProjectWorkspace? workspace;
    private readonly ProjectToolingHost? tooling;
    private readonly bool disposeWorkspace;
    private bool disposed;

    private EditorSessionConnectResult(
        bool succeeded,
        EditorSessionConnectionState state,
        EditorSessionAdapterKind adapterKind,
        ProjectWorkspace? workspace,
        ProjectToolingHost? tooling,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        bool disposeWorkspace)
    {
        Succeeded = succeeded;
        State = state;
        AdapterKind = adapterKind;
        this.workspace = workspace;
        this.tooling = tooling;
        Diagnostics = diagnostics.ToArray();
        this.disposeWorkspace = disposeWorkspace;
    }

    public bool Succeeded { get; }

    public EditorSessionConnectionState State { get; }

    public EditorSessionAdapterKind AdapterKind { get; }

    public ProjectWorkspace Workspace => workspace ?? throw new InvalidOperationException("Editor session connection does not contain a workspace.");

    public ProjectWorkspace? WorkspaceOrNull => workspace;

    public ProjectToolingHost Tooling => tooling ?? throw new InvalidOperationException("Editor session connection does not contain a tooling host.");

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static EditorSessionConnectResult Active(
        EditorSessionAdapterKind adapterKind,
        ProjectWorkspace workspace)
    {
        return new EditorSessionConnectResult(
            true,
            EditorSessionConnectionState.ActiveEditor,
            adapterKind,
            workspace,
            new ProjectToolingHost(workspace),
            diagnostics: [],
            disposeWorkspace: false);
    }

    public static EditorSessionConnectResult Headless(
        EditorSessionAdapterKind adapterKind,
        ProjectWorkspace workspace,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        return new EditorSessionConnectResult(
            true,
            EditorSessionConnectionState.HeadlessFallback,
            adapterKind,
            workspace,
            new ProjectToolingHost(workspace),
            diagnostics,
            disposeWorkspace: true);
    }

    public static EditorSessionConnectResult Rejected(
        EditorSessionAdapterKind adapterKind,
        StructuredDiagnostic diagnostic)
    {
        return new EditorSessionConnectResult(
            false,
            EditorSessionConnectionState.Rejected,
            adapterKind,
            null,
            null,
            [diagnostic],
            disposeWorkspace: false);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (disposeWorkspace)
        {
            workspace?.Dispose();
        }
    }
}

internal sealed class EditorSessionRegistry
{
    private readonly Dictionary<string, ActiveEditorSession> sessionsByProjectRoot;
    private readonly TimeSpan leaseTimeout;

    public EditorSessionRegistry(TimeSpan leaseTimeout)
    {
        if (leaseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseTimeout), leaseTimeout, "Editor session lease timeout must be positive.");
        }

        this.leaseTimeout = leaseTimeout;
        sessionsByProjectRoot = new Dictionary<string, ActiveEditorSession>(PathComparer);
    }

    public EditorSessionOpenResult OpenEditorSession(
        string projectRoot,
        string ownerId,
        EditorSessionEndpoint endpoint,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(endpoint);

        var normalizedRoot = NormalizeProjectRoot(projectRoot);
        if (!TryValidateEndpoint(endpoint, out var endpointDiagnostic))
        {
            return EditorSessionOpenResult.Rejected(endpointDiagnostic);
        }

        CleanupStaleSession(normalizedRoot, nowUtc);
        if (sessionsByProjectRoot.TryGetValue(normalizedRoot, out var existing))
        {
            var readOnlyWorkspace = CreateWorkspace(
                normalizedRoot,
                ownerId,
                ProjectWorkspaceOpenMode.EditorReadOnly,
                nowUtc,
                release: null);
            var diagnostic = CreateSessionDiagnostic(
                $"Project '{normalizedRoot}' already has an active Editor owner '{existing.Descriptor.OwnerId}'. Opened read-only workspace.");
            return EditorSessionOpenResult.ReadOnly(readOnlyWorkspace, existing.Descriptor, diagnostic);
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var descriptor = new EditorSessionDescriptor(
            sessionId,
            ownerId,
            normalizedRoot,
            endpoint,
            ProjectWorkspaceOpenMode.EditorPrimary,
            nowUtc,
            nowUtc,
            nowUtc.Add(leaseTimeout));
        var workspace = CreateWorkspace(
            normalizedRoot,
            ownerId,
            ProjectWorkspaceOpenMode.EditorPrimary,
            nowUtc,
            () => Release(sessionId, ownerId));
        sessionsByProjectRoot[normalizedRoot] = new ActiveEditorSession(descriptor, workspace);
        return EditorSessionOpenResult.Active(workspace, descriptor);
    }

    public EditorSessionConnectResult Connect(
        EditorSessionAdapterKind adapterKind,
        string projectRoot,
        string clientId,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var normalizedRoot = NormalizeProjectRoot(projectRoot);
        var cleanupDiagnostics = CleanupStaleSession(normalizedRoot, nowUtc);
        if (sessionsByProjectRoot.TryGetValue(normalizedRoot, out var existing))
        {
            return EditorSessionConnectResult.Active(adapterKind, existing.Workspace);
        }

        var diagnostics = cleanupDiagnostics.Count == 0
            ? [CreateSessionDiagnostic($"No active Editor session was found for project '{normalizedRoot}'. Using explicit headless fallback.")]
            : cleanupDiagnostics;
        return EditorSessionConnectResult.Headless(
            adapterKind,
            ProjectWorkspace.CreateHeadless(normalizedRoot, clientId),
            diagnostics);
    }

    public EditorSessionConnectResult ConnectToDescriptor(
        EditorSessionAdapterKind adapterKind,
        string requestedProjectRoot,
        EditorSessionDescriptor descriptor,
        string clientId,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var normalizedRoot = NormalizeProjectRoot(requestedProjectRoot);
        if (!PathComparer.Equals(normalizedRoot, descriptor.ProjectRoot))
        {
            return EditorSessionConnectResult.Rejected(
                adapterKind,
                CreateSessionDiagnostic(
                    $"Editor session descriptor belongs to project '{descriptor.ProjectRoot}', not requested project '{normalizedRoot}'."));
        }

        return Connect(adapterKind, normalizedRoot, clientId, nowUtc);
    }

    public bool Heartbeat(string sessionId, string ownerId, DateTimeOffset heartbeatUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        foreach (var pair in sessionsByProjectRoot.ToArray())
        {
            var session = pair.Value;
            if (string.Equals(session.Descriptor.SessionId, sessionId, StringComparison.Ordinal) &&
                string.Equals(session.Descriptor.OwnerId, ownerId, StringComparison.Ordinal))
            {
                sessionsByProjectRoot[pair.Key] = session.WithDescriptor(session.Descriptor.WithHeartbeat(heartbeatUtc, leaseTimeout));
                session.Workspace.OwnerLease.Touch(heartbeatUtc);
                return true;
            }
        }

        return false;
    }

    public bool Release(string sessionId, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        foreach (var pair in sessionsByProjectRoot.ToArray())
        {
            var session = pair.Value;
            if (string.Equals(session.Descriptor.SessionId, sessionId, StringComparison.Ordinal) &&
                string.Equals(session.Descriptor.OwnerId, ownerId, StringComparison.Ordinal))
            {
                sessionsByProjectRoot.Remove(pair.Key);
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<StructuredDiagnostic> CleanupStaleSession(string normalizedRoot, DateTimeOffset nowUtc)
    {
        if (!sessionsByProjectRoot.TryGetValue(normalizedRoot, out var existing) ||
            !existing.Descriptor.IsStale(nowUtc))
        {
            return [];
        }

        sessionsByProjectRoot.Remove(normalizedRoot);
        return
        [
            CreateSessionDiagnostic(
                $"Active Editor session '{existing.Descriptor.SessionId}' for project '{normalizedRoot}' expired at {existing.Descriptor.LeaseExpiresAtUtc:O}. Using explicit headless fallback until a new Editor registers.")
        ];
    }

    private ProjectWorkspace CreateWorkspace(
        string projectRoot,
        string ownerId,
        ProjectWorkspaceOpenMode openMode,
        DateTimeOffset nowUtc,
        Action? release)
    {
        var lease = new ProjectWorkspaceOwnerLease(projectRoot, ownerId, openMode, leaseTimeout, nowUtc);
        return ProjectWorkspace.Create(projectRoot, openMode, lease, release);
    }

    private static bool TryValidateEndpoint(EditorSessionEndpoint endpoint, out StructuredDiagnostic diagnostic)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Address))
        {
            diagnostic = CreateEndpointDiagnostic("Editor session endpoint address is empty.");
            return false;
        }

        var address = endpoint.Address;
        if (ContainsSecretLikeText(address))
        {
            diagnostic = CreateEndpointDiagnostic("Editor session endpoint address contains secret-like material and was rejected.");
            return false;
        }

        var isLocal = endpoint.Kind switch
        {
            EditorSessionEndpointKind.NamedPipe => address.StartsWith(@"\\.\pipe\", StringComparison.OrdinalIgnoreCase),
            EditorSessionEndpointKind.UnixDomainSocket => Path.IsPathRooted(address),
            _ => false
        };
        if (!isLocal)
        {
            diagnostic = CreateEndpointDiagnostic("Editor session endpoint is not a supported local named pipe or Unix domain socket.");
            return false;
        }

        diagnostic = null!;
        return true;
    }

    private static bool ContainsSecretLikeText(string value)
    {
        return value.Contains('?', StringComparison.Ordinal) ||
            value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("api_key", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProjectRoot(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static StructuredDiagnostic CreateSessionDiagnostic(string message)
    {
        return CreateDiagnostic("E2D-TOOLING-0003", message);
    }

    private static StructuredDiagnostic CreateEndpointDiagnostic(string message)
    {
        return CreateDiagnostic("E2D-TOOLING-0004", message);
    }

    private static StructuredDiagnostic CreateDiagnostic(string code, string message)
    {
        var definition = DiagnosticCodeRegistry.Get(code);
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private sealed class ActiveEditorSession
    {
        public ActiveEditorSession(EditorSessionDescriptor descriptor, ProjectWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(workspace);

            Descriptor = descriptor;
            Workspace = workspace;
        }

        public EditorSessionDescriptor Descriptor { get; }

        public ProjectWorkspace Workspace { get; }

        public ActiveEditorSession WithDescriptor(EditorSessionDescriptor descriptor)
        {
            return new ActiveEditorSession(descriptor, Workspace);
        }
    }
}
