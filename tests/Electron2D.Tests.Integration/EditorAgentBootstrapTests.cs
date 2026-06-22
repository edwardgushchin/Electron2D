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
using Electron2D.ProjectSystem;
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorAgentBootstrapTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 23, 1, 40, 0, TimeSpan.Zero);

    [Fact]
    public void EditorAgentProfilesCreateTemporaryMcpConfigAndStartInProjectRootWithoutTokenLeaks()
    {
        var projectRoot = CreateProjectRoot("bootstrap-config", SceneText(speed: 10));
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "Electron2D-AgentBootstrapTests", "temp", Guid.NewGuid().ToString("N"));
        var registry = new EditorSessionRegistry(TimeSpan.FromMinutes(5));
        var launcher = new CapturingAgentProcessLauncher();
        var bootstrapper = new EditorAgentProcessBootstrapper(
            registry,
            launcher,
            new DeterministicAgentTokenGenerator("test-token-value"));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-agent-bootstrap",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-agent-bootstrap"),
            FixedInstant);

        var launch = bootstrapper.Start(new EditorAgentLaunchRequest(
            projectRoot,
            editor.Descriptor,
            "codex",
            temporaryRoot,
            FixedInstant,
            TimeSpan.FromMinutes(10)));

        Assert.True(launch.Succeeded);
        Assert.Equal(EditorAgentWorkspaceConnectionState.Starting, launch.WorkspaceState.ConnectionState);
        Assert.Equal("codex", launch.WorkspaceState.ProfileId);
        Assert.Equal(projectRoot, launcher.LastPlan!.WorkingDirectory);
        Assert.False(launcher.LastPlan.UseShellExecute);
        Assert.Equal(temporaryRoot, Path.GetDirectoryName(Path.GetDirectoryName(launch.McpConfigurationPath)));
        Assert.False(IsPathUnder(launch.McpConfigurationPath, projectRoot));
        Assert.Equal(launch.McpConfigurationPath, launcher.LastPlan.Environment["ELECTRON2D_MCP_CONFIG"]);
        Assert.Equal(launch.AgentSessionId, launcher.LastPlan.Environment["ELECTRON2D_AGENT_SESSION_ID"]);
        Assert.DoesNotContain("test-token-value", string.Join(" ", launcher.LastPlan.Arguments), StringComparison.Ordinal);
        Assert.DoesNotContain("test-token-value", string.Join(" ", launcher.LastPlan.Environment.Values), StringComparison.Ordinal);
        Assert.DoesNotContain("test-token-value", launch.WorkspaceState.StatusText, StringComparison.Ordinal);
        Assert.All(bootstrapper.Profiles.Select(profile => profile.ProfileId), profileId =>
            Assert.Contains(profileId, new[] { "codex", "opencode", "claude-code" }));

        using var json = JsonDocument.Parse(File.ReadAllText(launch.McpConfigurationPath));
        var root = json.RootElement;
        Assert.Equal("Electron2D.AgentMcpBootstrap", root.GetProperty("format").GetString());
        Assert.Equal(launch.AgentSessionId, root.GetProperty("agentSessionId").GetString());
        Assert.Equal(projectRoot, root.GetProperty("projectRoot").GetString());
        Assert.Equal(@"\\.\pipe\electron2d-agent-bootstrap", root.GetProperty("endpoint").GetProperty("address").GetString());
        Assert.Equal("test-token-value", root.GetProperty("ephemeralToken").GetString());

        foreach (var file in Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("test-token-value", File.ReadAllText(file), StringComparison.Ordinal);
            Assert.DoesNotContain(launch.McpConfigurationPath, File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void HandshakeConnectsToActiveEditorAndReadsWorkspaceStateBeforeTokenExpiry()
    {
        var projectRoot = CreateProjectRoot("handshake", SceneText(speed: 10));
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "Electron2D-AgentBootstrapTests", "temp", Guid.NewGuid().ToString("N"));
        var registry = new EditorSessionRegistry(TimeSpan.FromMinutes(5));
        var bootstrapper = new EditorAgentProcessBootstrapper(
            registry,
            new CapturingAgentProcessLauncher(),
            new DeterministicAgentTokenGenerator("handshake-token"));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-agent-handshake",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-agent-handshake"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-agent-handshake-scene"));
        editor.Workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-human-dirty-agent",
            ProjectWorkspaceActorKind.Human,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-human-dirty-agent",
            [WorkspaceTransactionDocumentEdit.ReplaceText("scenes/main.scene.json", new ProjectDocumentRevision(1), SceneText(speed: 12))]));
        editor.Workspace.Diagnostics.SetDiagnostics(
            "script",
            [CreateDiagnostic("Agent handshake diagnostic.")],
            ProjectWorkspaceOperationContext.ForTest("set-agent-handshake-diagnostics"));
        var launch = bootstrapper.Start(new EditorAgentLaunchRequest(
            projectRoot,
            editor.Descriptor,
            "opencode",
            temporaryRoot,
            FixedInstant,
            TimeSpan.FromMinutes(10)));

        var handshake = bootstrapper.CompleteHandshake(
            launch.AgentSessionId,
            "handshake-token",
            FixedInstant.AddSeconds(5));

        Assert.True(handshake.Succeeded);
        Assert.Equal(EditorAgentWorkspaceConnectionState.Connected, handshake.WorkspaceState.ConnectionState);
        Assert.Equal("activeEditor", handshake.WorkspaceState.Route);
        Assert.Equal(EditorSessionConnectionState.ActiveEditor, handshake.ConnectionState);
        Assert.Contains("scenes/main.scene.json", handshake.InitialState.OpenDocuments);
        Assert.Contains("scenes/main.scene.json", handshake.InitialState.DirtyDocuments);
        Assert.Equal(2, handshake.InitialState.DocumentRevisions["scenes/main.scene.json"].Value);
        Assert.Single(handshake.InitialState.Diagnostics);
        Assert.True(handshake.InitialState.SelectionResourceAvailable);
        Assert.Same(editor.Workspace, handshake.Workspace);
    }

    [Fact]
    public void InvalidOrExpiredAgentTokenUpdatesWorkspaceStateWithoutEchoingSecret()
    {
        var projectRoot = CreateProjectRoot("token-expiry", SceneText(speed: 10));
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "Electron2D-AgentBootstrapTests", "temp", Guid.NewGuid().ToString("N"));
        var registry = new EditorSessionRegistry(TimeSpan.FromMinutes(5));
        var bootstrapper = new EditorAgentProcessBootstrapper(
            registry,
            new CapturingAgentProcessLauncher(),
            new DeterministicAgentTokenGenerator("short-lived-token"));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-agent-expiry",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-agent-expiry"),
            FixedInstant);
        var launch = bootstrapper.Start(new EditorAgentLaunchRequest(
            projectRoot,
            editor.Descriptor,
            "claude-code",
            temporaryRoot,
            FixedInstant,
            TimeSpan.FromSeconds(1)));

        var rejected = bootstrapper.CompleteHandshake(
            launch.AgentSessionId,
            "wrong-token-value",
            FixedInstant.AddMilliseconds(500));
        var expired = bootstrapper.CompleteHandshake(
            launch.AgentSessionId,
            "short-lived-token",
            FixedInstant.AddSeconds(2));
        var disconnected = bootstrapper.Disconnect(launch.AgentSessionId, FixedInstant.AddSeconds(3));

        Assert.False(rejected.Succeeded);
        Assert.Equal(EditorAgentWorkspaceConnectionState.HandshakeError, rejected.WorkspaceState.ConnectionState);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "E2D-AGENT-0001");
        Assert.DoesNotContain(rejected.Diagnostics, diagnostic => diagnostic.Message.Contains("wrong-token-value", StringComparison.Ordinal));
        Assert.False(expired.Succeeded);
        Assert.Equal(EditorAgentWorkspaceConnectionState.TokenExpired, expired.WorkspaceState.ConnectionState);
        Assert.Contains(expired.Diagnostics, diagnostic => diagnostic.Code == "E2D-AGENT-0002");
        Assert.DoesNotContain(expired.Diagnostics, diagnostic => diagnostic.Message.Contains("short-lived-token", StringComparison.Ordinal));
        Assert.Equal(EditorAgentWorkspaceConnectionState.Disconnected, disconnected.ConnectionState);
        Assert.True(registry.Connect(EditorSessionAdapterKind.Mcp, projectRoot, "mcp-after-disconnect", FixedInstant.AddSeconds(4)).Succeeded);
    }

    private static string CreateProjectRoot(string name, string sceneText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-AgentBootstrapTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), sceneText);
        return root;
    }

    private static string SceneText(int speed)
    {
        return $$"""
        {
          "format": "Electron2D.SceneFile",
          "version": 1,
          "external": [],
          "internal": [],
          "nodes": [
            {
              "id": 1,
              "type": "Electron2D.Node2D",
              "name": "Player",
              "parent": null,
              "owner": null,
              "groups": [],
              "properties": {
                "speed": {
                  "type": "Int",
                  "value": {{speed}}
                }
              }
            }
          ]
        }
        """;
    }

    private static StructuredDiagnostic CreateDiagnostic(string message)
    {
        var definition = DiagnosticCodeRegistry.Get("E2D-TOOLING-0003");
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static bool IsPathUnder(string candidatePath, string rootPath)
    {
        var candidate = Path.GetFullPath(candidatePath);
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private sealed class CapturingAgentProcessLauncher : IAgentProcessLauncher
    {
        public AgentProcessStartPlan? LastPlan { get; private set; }

        public AgentProcessLaunchResult Start(AgentProcessStartPlan plan)
        {
            LastPlan = plan;
            return AgentProcessLaunchResult.Success(processId: 4242);
        }
    }

    private sealed class DeterministicAgentTokenGenerator : IAgentTokenGenerator
    {
        private readonly string token;

        public DeterministicAgentTokenGenerator(string token)
        {
            this.token = token;
        }

        public string CreateToken()
        {
            return token;
        }
    }
}
