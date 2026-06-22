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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;

namespace Electron2D.Tooling;

internal enum EditorAgentWorkspaceConnectionState
{
    Starting,
    Connected,
    Disconnected,
    HandshakeError,
    TokenExpired
}

internal interface IAgentProcessLauncher
{
    AgentProcessLaunchResult Start(AgentProcessStartPlan plan);
}

internal interface IAgentTokenGenerator
{
    string CreateToken();
}

internal sealed class AgentProcessProfile
{
    public AgentProcessProfile(
        string profileId,
        string displayName,
        string executablePath,
        IReadOnlyList<string> defaultArguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(defaultArguments);

        ProfileId = profileId;
        DisplayName = displayName;
        ExecutablePath = executablePath;
        DefaultArguments = defaultArguments.ToArray();
    }

    public string ProfileId { get; }

    public string DisplayName { get; }

    public string ExecutablePath { get; }

    public IReadOnlyList<string> DefaultArguments { get; }
}

internal sealed class AgentProcessStartPlan
{
    public AgentProcessStartPlan(
        string profileId,
        string executablePath,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        bool useShellExecute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentNullException.ThrowIfNull(environment);

        ProfileId = profileId;
        ExecutablePath = executablePath;
        Arguments = arguments.ToArray();
        WorkingDirectory = workingDirectory;
        Environment = new ReadOnlyDictionary<string, string>(
            environment.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        UseShellExecute = useShellExecute;
    }

    public string ProfileId { get; }

    public string ExecutablePath { get; }

    public IReadOnlyList<string> Arguments { get; }

    public string WorkingDirectory { get; }

    public IReadOnlyDictionary<string, string> Environment { get; }

    public bool UseShellExecute { get; }
}

internal sealed class AgentProcessLaunchResult
{
    private AgentProcessLaunchResult(bool succeeded, int? processId, IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        ProcessId = processId;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public int? ProcessId { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static AgentProcessLaunchResult Success(int processId)
    {
        return new AgentProcessLaunchResult(true, processId, []);
    }

    public static AgentProcessLaunchResult Failure(StructuredDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new AgentProcessLaunchResult(false, null, [diagnostic]);
    }
}

internal sealed class ProcessAgentProcessLauncher : IAgentProcessLauncher
{
    public AgentProcessLaunchResult Start(AgentProcessStartPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var startInfo = new ProcessStartInfo
        {
            FileName = plan.ExecutablePath,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = plan.UseShellExecute
        };
        foreach (var argument in plan.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var pair in plan.Environment)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        try
        {
            var process = Process.Start(startInfo);
            return process is null
                ? AgentProcessLaunchResult.Failure(CreateDiagnostic("Agent process did not return a process handle."))
                : AgentProcessLaunchResult.Success(process.Id);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            return AgentProcessLaunchResult.Failure(CreateDiagnostic($"Agent process launch failed for profile '{plan.ProfileId}'."));
        }
    }

    private static StructuredDiagnostic CreateDiagnostic(string message)
    {
        var definition = DiagnosticCodeRegistry.Get("E2D-AGENT-0003");
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }
}

internal sealed class SecureAgentTokenGenerator : IAgentTokenGenerator
{
    public string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

internal sealed class EditorAgentLaunchRequest
{
    public EditorAgentLaunchRequest(
        string projectRoot,
        EditorSessionDescriptor editorSessionDescriptor,
        string profileId,
        string temporaryRoot,
        DateTimeOffset nowUtc,
        TimeSpan tokenLifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(editorSessionDescriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryRoot);
        if (tokenLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenLifetime), tokenLifetime, "Agent token lifetime must be positive.");
        }

        ProjectRoot = NormalizeProjectRoot(projectRoot);
        EditorSessionDescriptor = editorSessionDescriptor;
        ProfileId = profileId;
        TemporaryRoot = Path.GetFullPath(temporaryRoot);
        NowUtc = nowUtc;
        TokenLifetime = tokenLifetime;
    }

    public string ProjectRoot { get; }

    public EditorSessionDescriptor EditorSessionDescriptor { get; }

    public string ProfileId { get; }

    public string TemporaryRoot { get; }

    public DateTimeOffset NowUtc { get; }

    public TimeSpan TokenLifetime { get; }

    private static string NormalizeProjectRoot(string projectRoot)
    {
        return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

internal sealed class EditorAgentWorkspaceState
{
    public EditorAgentWorkspaceState(
        string agentSessionId,
        string profileId,
        EditorAgentWorkspaceConnectionState connectionState,
        string? route,
        string? mcpConfigurationPath,
        DateTimeOffset startedAtUtc,
        DateTimeOffset tokenExpiresAtUtc,
        string statusText,
        IReadOnlyList<string> diagnosticCodes,
        DateTimeOffset? connectedAtUtc = null,
        DateTimeOffset? disconnectedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(statusText);
        ArgumentNullException.ThrowIfNull(diagnosticCodes);

        AgentSessionId = agentSessionId;
        ProfileId = profileId;
        ConnectionState = connectionState;
        Route = route;
        McpConfigurationPath = mcpConfigurationPath;
        StartedAtUtc = startedAtUtc;
        TokenExpiresAtUtc = tokenExpiresAtUtc;
        StatusText = statusText;
        DiagnosticCodes = diagnosticCodes.ToArray();
        ConnectedAtUtc = connectedAtUtc;
        DisconnectedAtUtc = disconnectedAtUtc;
    }

    public string AgentSessionId { get; }

    public string ProfileId { get; }

    public EditorAgentWorkspaceConnectionState ConnectionState { get; }

    public string? Route { get; }

    public string? McpConfigurationPath { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset TokenExpiresAtUtc { get; }

    public string StatusText { get; }

    public IReadOnlyList<string> DiagnosticCodes { get; }

    public DateTimeOffset? ConnectedAtUtc { get; }

    public DateTimeOffset? DisconnectedAtUtc { get; }
}

internal sealed class EditorAgentLaunchResult
{
    private EditorAgentLaunchResult(
        bool succeeded,
        string agentSessionId,
        string mcpConfigurationPath,
        EditorAgentWorkspaceState workspaceState,
        int? processId,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        AgentSessionId = agentSessionId;
        McpConfigurationPath = mcpConfigurationPath;
        WorkspaceState = workspaceState;
        ProcessId = processId;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public string AgentSessionId { get; }

    public string McpConfigurationPath { get; }

    public EditorAgentWorkspaceState WorkspaceState { get; }

    public int? ProcessId { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static EditorAgentLaunchResult Create(
        bool succeeded,
        string agentSessionId,
        string mcpConfigurationPath,
        EditorAgentWorkspaceState workspaceState,
        int? processId,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        return new EditorAgentLaunchResult(succeeded, agentSessionId, mcpConfigurationPath, workspaceState, processId, diagnostics);
    }
}

internal sealed class EditorAgentInitialState
{
    public EditorAgentInitialState(
        IReadOnlyList<string> openDocuments,
        IReadOnlyList<string> dirtyDocuments,
        IReadOnlyDictionary<string, ProjectDocumentRevision> documentRevisions,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        bool selectionResourceAvailable)
    {
        ArgumentNullException.ThrowIfNull(openDocuments);
        ArgumentNullException.ThrowIfNull(dirtyDocuments);
        ArgumentNullException.ThrowIfNull(documentRevisions);
        ArgumentNullException.ThrowIfNull(diagnostics);

        OpenDocuments = openDocuments.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        DirtyDocuments = dirtyDocuments.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        DocumentRevisions = new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            documentRevisions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        Diagnostics = diagnostics.ToArray();
        SelectionResourceAvailable = selectionResourceAvailable;
    }

    public IReadOnlyList<string> OpenDocuments { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> DocumentRevisions { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public bool SelectionResourceAvailable { get; }
}

internal sealed class EditorAgentHandshakeResult
{
    private readonly ProjectWorkspace? workspace;

    private EditorAgentHandshakeResult(
        bool succeeded,
        EditorAgentWorkspaceState workspaceState,
        EditorSessionConnectionState connectionState,
        ProjectWorkspace? workspace,
        EditorAgentInitialState initialState,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        WorkspaceState = workspaceState;
        ConnectionState = connectionState;
        this.workspace = workspace;
        InitialState = initialState;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public EditorAgentWorkspaceState WorkspaceState { get; }

    public EditorSessionConnectionState ConnectionState { get; }

    public ProjectWorkspace Workspace => workspace ?? throw new InvalidOperationException("Agent handshake result does not contain a workspace.");

    public EditorAgentInitialState InitialState { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static EditorAgentHandshakeResult Create(
        bool succeeded,
        EditorAgentWorkspaceState workspaceState,
        EditorSessionConnectionState connectionState,
        ProjectWorkspace? workspace,
        EditorAgentInitialState initialState,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        return new EditorAgentHandshakeResult(succeeded, workspaceState, connectionState, workspace, initialState, diagnostics);
    }
}

internal sealed class EditorAgentDisconnectResult
{
    public EditorAgentDisconnectResult(EditorAgentWorkspaceConnectionState connectionState, EditorAgentWorkspaceState workspaceState)
    {
        ConnectionState = connectionState;
        WorkspaceState = workspaceState;
    }

    public EditorAgentWorkspaceConnectionState ConnectionState { get; }

    public EditorAgentWorkspaceState WorkspaceState { get; }
}

internal sealed class EditorAgentProcessBootstrapper
{
    private static readonly AgentProcessProfile[] DefaultProfiles =
    [
        new("codex", "Codex", "codex", []),
        new("opencode", "OpenCode", "opencode", []),
        new("claude-code", "Claude Code", "claude", [])
    ];

    private readonly EditorSessionRegistry registry;
    private readonly IAgentProcessLauncher launcher;
    private readonly IAgentTokenGenerator tokenGenerator;
    private readonly Dictionary<string, ActiveAgentSession> sessionsById = new(StringComparer.Ordinal);

    public EditorAgentProcessBootstrapper(EditorSessionRegistry registry)
        : this(registry, new ProcessAgentProcessLauncher(), new SecureAgentTokenGenerator())
    {
    }

    public EditorAgentProcessBootstrapper(
        EditorSessionRegistry registry,
        IAgentProcessLauncher launcher,
        IAgentTokenGenerator tokenGenerator)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(tokenGenerator);

        this.registry = registry;
        this.launcher = launcher;
        this.tokenGenerator = tokenGenerator;
        Profiles = DefaultProfiles;
    }

    public IReadOnlyList<AgentProcessProfile> Profiles { get; }

    public EditorAgentLaunchResult Start(EditorAgentLaunchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = Profiles.FirstOrDefault(profile => string.Equals(profile.ProfileId, request.ProfileId, StringComparison.Ordinal));
        if (profile is null)
        {
            var diagnostic = CreateDiagnostic("E2D-AGENT-0003", $"Agent profile '{request.ProfileId}' is not registered.");
            var unknownProfileState = WorkspaceState(
                agentSessionId: "agent-unregistered",
                request.ProfileId,
                EditorAgentWorkspaceConnectionState.HandshakeError,
                route: null,
                mcpConfigurationPath: null,
                request.NowUtc,
                request.NowUtc,
                "Agent profile is not registered.",
                [diagnostic.Code]);
            return EditorAgentLaunchResult.Create(false, "agent-unregistered", string.Empty, unknownProfileState, null, [diagnostic]);
        }

        if (!PathComparer.Equals(request.ProjectRoot, request.EditorSessionDescriptor.ProjectRoot) ||
            IsPathUnder(request.TemporaryRoot, request.ProjectRoot))
        {
            var diagnostic = CreateDiagnostic("E2D-AGENT-0003", "Agent MCP bootstrap requires matching project root and temporary configuration outside the project.");
            var rejectedState = WorkspaceState(
                agentSessionId: "agent-rejected",
                profile.ProfileId,
                EditorAgentWorkspaceConnectionState.HandshakeError,
                route: null,
                mcpConfigurationPath: null,
                request.NowUtc,
                request.NowUtc,
                "Agent MCP bootstrap was rejected before process start.",
                [diagnostic.Code]);
            return EditorAgentLaunchResult.Create(false, "agent-rejected", string.Empty, rejectedState, null, [diagnostic]);
        }

        var agentSessionId = $"agent-{Guid.NewGuid():N}";
        var token = tokenGenerator.CreateToken();
        var expiresAtUtc = request.NowUtc.Add(request.TokenLifetime);
        var configurationDirectory = Path.Combine(request.TemporaryRoot, agentSessionId);
        Directory.CreateDirectory(configurationDirectory);
        var configurationPath = Path.Combine(configurationDirectory, "mcp-bootstrap.json");
        WriteMcpConfiguration(configurationPath, request, profile, agentSessionId, token, expiresAtUtc);

        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ELECTRON2D_MCP_CONFIG"] = configurationPath,
            ["ELECTRON2D_AGENT_SESSION_ID"] = agentSessionId
        };
        var plan = new AgentProcessStartPlan(
            profile.ProfileId,
            profile.ExecutablePath,
            profile.DefaultArguments,
            request.ProjectRoot,
            environment,
            useShellExecute: false);
        var launch = launcher.Start(plan);
        var connectionState = launch.Succeeded
            ? EditorAgentWorkspaceConnectionState.Starting
            : EditorAgentWorkspaceConnectionState.HandshakeError;
        var state = WorkspaceState(
            agentSessionId,
            profile.ProfileId,
            connectionState,
            route: null,
            configurationPath,
            request.NowUtc,
            expiresAtUtc,
            launch.Succeeded ? "Agent process started; waiting for MCP handshake." : "Agent process failed to start.",
            launch.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray());
        sessionsById[agentSessionId] = new ActiveAgentSession(
            agentSessionId,
            profile,
            request.ProjectRoot,
            request.EditorSessionDescriptor,
            token,
            configurationPath,
            request.NowUtc,
            expiresAtUtc,
            launch.ProcessId,
            state);
        return EditorAgentLaunchResult.Create(
            launch.Succeeded,
            agentSessionId,
            configurationPath,
            state,
            launch.ProcessId,
            launch.Diagnostics);
    }

    public EditorAgentHandshakeResult CompleteHandshake(string agentSessionId, string token, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!sessionsById.TryGetValue(agentSessionId, out var session))
        {
            return FailedHandshake(
                agentSessionId,
                "unknown",
                EditorAgentWorkspaceConnectionState.HandshakeError,
                nowUtc,
                nowUtc,
                "Agent MCP handshake was rejected.",
                CreateDiagnostic("E2D-AGENT-0001", "Agent MCP handshake references an unknown session."));
        }

        if (!string.Equals(session.Token, token, StringComparison.Ordinal))
        {
            return FailedHandshake(
                session,
                EditorAgentWorkspaceConnectionState.HandshakeError,
                nowUtc,
                "Agent MCP handshake token was rejected.",
                CreateDiagnostic("E2D-AGENT-0001", "Agent MCP handshake token was rejected."));
        }

        if (nowUtc > session.TokenExpiresAtUtc)
        {
            return FailedHandshake(
                session,
                EditorAgentWorkspaceConnectionState.TokenExpired,
                nowUtc,
                "Agent MCP handshake token expired.",
                CreateDiagnostic("E2D-AGENT-0002", "Agent MCP handshake token expired."));
        }

        var connection = registry.ConnectToDescriptor(
            EditorSessionAdapterKind.Mcp,
            session.ProjectRoot,
            session.EditorSessionDescriptor,
            session.AgentSessionId,
            nowUtc);
        if (!connection.Succeeded || connection.State != EditorSessionConnectionState.ActiveEditor)
        {
            var diagnostic = CreateDiagnostic("E2D-AGENT-0001", "Agent MCP handshake could not attach to the active Editor route.");
            return FailedHandshake(session, EditorAgentWorkspaceConnectionState.HandshakeError, nowUtc, diagnostic.Message, diagnostic);
        }

        var initialState = new EditorAgentInitialState(
            connection.Workspace.Documents.Documents.Select(document => document.Path).ToArray(),
            connection.Workspace.Revisions.DirtyDocuments,
            connection.Workspace.Revisions.DocumentRevisions,
            connection.Workspace.Diagnostics.GetAllDiagnostics(),
            selectionResourceAvailable: true);
        var state = WorkspaceState(
            session.AgentSessionId,
            session.Profile.ProfileId,
            EditorAgentWorkspaceConnectionState.Connected,
            route: "activeEditor",
            session.ConfigurationPath,
            session.StartedAtUtc,
            session.TokenExpiresAtUtc,
            "Agent connected to active Editor MCP route.",
            [],
            connectedAtUtc: nowUtc);
        sessionsById[session.AgentSessionId] = session.WithWorkspaceState(state);
        return EditorAgentHandshakeResult.Create(
            true,
            state,
            connection.State,
            connection.Workspace,
            initialState,
            connection.Diagnostics);
    }

    public EditorAgentDisconnectResult Disconnect(string agentSessionId, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentSessionId);

        if (!sessionsById.TryGetValue(agentSessionId, out var session))
        {
            var state = WorkspaceState(
                agentSessionId,
                "unknown",
                EditorAgentWorkspaceConnectionState.Disconnected,
                route: null,
                mcpConfigurationPath: null,
                nowUtc,
                nowUtc,
                "Agent session is not active.",
                []);
            return new EditorAgentDisconnectResult(EditorAgentWorkspaceConnectionState.Disconnected, state);
        }

        var disconnected = WorkspaceState(
            session.AgentSessionId,
            session.Profile.ProfileId,
            EditorAgentWorkspaceConnectionState.Disconnected,
            route: session.WorkspaceState.Route,
            session.ConfigurationPath,
            session.StartedAtUtc,
            session.TokenExpiresAtUtc,
            "Agent session disconnected.",
            [],
            disconnectedAtUtc: nowUtc);
        sessionsById[session.AgentSessionId] = session.WithWorkspaceState(disconnected);
        return new EditorAgentDisconnectResult(EditorAgentWorkspaceConnectionState.Disconnected, disconnected);
    }

    private static EditorAgentHandshakeResult FailedHandshake(
        ActiveAgentSession session,
        EditorAgentWorkspaceConnectionState stateKind,
        DateTimeOffset nowUtc,
        string statusText,
        StructuredDiagnostic diagnostic)
    {
        return FailedHandshake(
            session.AgentSessionId,
            session.Profile.ProfileId,
            stateKind,
            session.StartedAtUtc,
            session.TokenExpiresAtUtc,
            statusText,
            diagnostic,
            session.ConfigurationPath);
    }

    private static EditorAgentHandshakeResult FailedHandshake(
        string agentSessionId,
        string profileId,
        EditorAgentWorkspaceConnectionState stateKind,
        DateTimeOffset startedAtUtc,
        DateTimeOffset tokenExpiresAtUtc,
        string statusText,
        StructuredDiagnostic diagnostic,
        string? configurationPath = null)
    {
        var state = WorkspaceState(
            agentSessionId,
            profileId,
            stateKind,
            route: null,
            configurationPath,
            startedAtUtc,
            tokenExpiresAtUtc,
            statusText,
            [diagnostic.Code]);
        return EditorAgentHandshakeResult.Create(
            false,
            state,
            EditorSessionConnectionState.Rejected,
            workspace: null,
            new EditorAgentInitialState([], [], new Dictionary<string, ProjectDocumentRevision>(), [diagnostic], selectionResourceAvailable: false),
            [diagnostic]);
    }

    private static EditorAgentWorkspaceState WorkspaceState(
        string agentSessionId,
        string profileId,
        EditorAgentWorkspaceConnectionState connectionState,
        string? route,
        string? mcpConfigurationPath,
        DateTimeOffset startedAtUtc,
        DateTimeOffset tokenExpiresAtUtc,
        string statusText,
        IReadOnlyList<string> diagnosticCodes,
        DateTimeOffset? connectedAtUtc = null,
        DateTimeOffset? disconnectedAtUtc = null)
    {
        return new EditorAgentWorkspaceState(
            agentSessionId,
            profileId,
            connectionState,
            route,
            mcpConfigurationPath,
            startedAtUtc,
            tokenExpiresAtUtc,
            statusText,
            diagnosticCodes,
            connectedAtUtc,
            disconnectedAtUtc);
    }

    private static void WriteMcpConfiguration(
        string configurationPath,
        EditorAgentLaunchRequest request,
        AgentProcessProfile profile,
        string agentSessionId,
        string token,
        DateTimeOffset expiresAtUtc)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.AgentMcpBootstrap",
            ["version"] = 1,
            ["agentSessionId"] = agentSessionId,
            ["profileId"] = profile.ProfileId,
            ["projectRoot"] = request.ProjectRoot,
            ["expiresAtUtc"] = expiresAtUtc.ToString("O"),
            ["ephemeralToken"] = token,
            ["endpoint"] = new JsonObject
            {
                ["kind"] = request.EditorSessionDescriptor.Endpoint.Kind.ToString(),
                ["address"] = request.EditorSessionDescriptor.Endpoint.Address
            },
            ["mcp"] = new JsonObject
            {
                ["transport"] = "editor-session",
                ["route"] = "activeEditor",
                ["configEnvironmentVariable"] = "ELECTRON2D_MCP_CONFIG"
            }
        };
        File.WriteAllText(configurationPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n"));
    }

    private static bool IsPathUnder(string candidatePath, string rootPath)
    {
        var candidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(root, PathComparerComparison);
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

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static StringComparison PathComparerComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private sealed class ActiveAgentSession
    {
        public ActiveAgentSession(
            string agentSessionId,
            AgentProcessProfile profile,
            string projectRoot,
            EditorSessionDescriptor editorSessionDescriptor,
            string token,
            string configurationPath,
            DateTimeOffset startedAtUtc,
            DateTimeOffset tokenExpiresAtUtc,
            int? processId,
            EditorAgentWorkspaceState workspaceState)
        {
            AgentSessionId = agentSessionId;
            Profile = profile;
            ProjectRoot = projectRoot;
            EditorSessionDescriptor = editorSessionDescriptor;
            Token = token;
            ConfigurationPath = configurationPath;
            StartedAtUtc = startedAtUtc;
            TokenExpiresAtUtc = tokenExpiresAtUtc;
            ProcessId = processId;
            WorkspaceState = workspaceState;
        }

        public string AgentSessionId { get; }

        public AgentProcessProfile Profile { get; }

        public string ProjectRoot { get; }

        public EditorSessionDescriptor EditorSessionDescriptor { get; }

        public string Token { get; }

        public string ConfigurationPath { get; }

        public DateTimeOffset StartedAtUtc { get; }

        public DateTimeOffset TokenExpiresAtUtc { get; }

        public int? ProcessId { get; }

        public EditorAgentWorkspaceState WorkspaceState { get; }

        public ActiveAgentSession WithWorkspaceState(EditorAgentWorkspaceState workspaceState)
        {
            return new ActiveAgentSession(
                AgentSessionId,
                Profile,
                ProjectRoot,
                EditorSessionDescriptor,
                Token,
                ConfigurationPath,
                StartedAtUtc,
                TokenExpiresAtUtc,
                ProcessId,
                workspaceState);
        }
    }
}
