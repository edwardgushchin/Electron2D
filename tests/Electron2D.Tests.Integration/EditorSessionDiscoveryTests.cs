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
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorSessionDiscoveryTests
{
    private static readonly TimeSpan LeaseTimeout = TimeSpan.FromSeconds(30);
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegistryAcceptsWindowsAndUnixEndpointAbstractionsAndRejectsSecrets()
    {
        var registry = new EditorSessionRegistry(LeaseTimeout);
        using var windows = registry.OpenEditorSession(
            CreateProjectRoot("endpoint-windows", SceneText(speed: 10)),
            "editor-windows",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-endpoint-windows"),
            FixedInstant);

        Assert.True(windows.Succeeded);
        Assert.Equal(EditorSessionConnectionState.ActiveEditor, windows.State);
        Assert.Equal(EditorSessionEndpointKind.NamedPipe, windows.Descriptor.Endpoint.Kind);
        Assert.DoesNotContain("token", windows.Descriptor.Endpoint.Address, StringComparison.OrdinalIgnoreCase);

        using var unix = registry.OpenEditorSession(
            CreateProjectRoot("endpoint-unix", SceneText(speed: 10)),
            "editor-unix",
            EditorSessionEndpoint.UnixDomainSocket("/tmp/electron2d-endpoint-unix.sock"),
            FixedInstant);

        Assert.True(unix.Succeeded);
        Assert.Equal(EditorSessionEndpointKind.UnixDomainSocket, unix.Descriptor.Endpoint.Kind);

        var rejected = registry.OpenEditorSession(
            CreateProjectRoot("endpoint-secret", SceneText(speed: 10)),
            "editor-secret",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d?token=secret-value"),
            FixedInstant);

        Assert.False(rejected.Succeeded);
        Assert.Equal(EditorSessionConnectionState.Rejected, rejected.State);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0004");
        Assert.DoesNotContain(
            rejected.Diagnostics,
            diagnostic => diagnostic.Message.Contains("secret-value", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Cli")]
    [InlineData("Mcp")]
    public void CliAndMcpDiscoverActiveSessionAndRouteMutationsToEditorWorkspace(string adapterKindName)
    {
        var adapterKind = Enum.Parse<EditorSessionAdapterKind>(adapterKindName);
        var registry = new EditorSessionRegistry(LeaseTimeout);
        var root = CreateProjectRoot($"active-{adapterKind}", SceneText(speed: 10));
        using var editor = registry.OpenEditorSession(
            root,
            "editor-active",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-active"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-active-scene"));

        var connected = registry.Connect(adapterKind, root, $"client-{adapterKind}", FixedInstant.AddSeconds(1));

        Assert.True(connected.Succeeded);
        Assert.Equal(EditorSessionConnectionState.ActiveEditor, connected.State);
        Assert.Same(editor.Workspace, connected.Workspace);
        Assert.Equal(adapterKind, connected.AdapterKind);
        Assert.Empty(connected.Diagnostics);

        var changed = connected.Tooling.Project.ApplyTextEdit(new ToolingTextEditRequest(
            $"op-{adapterKind}-scene",
            "scene.set-property",
            ToolingApplyMode.WorkspaceOnly,
            "scenes/main.scene.json",
            new ProjectDocumentRevision(1),
            SceneText(speed: 12),
            $"undo-{adapterKind}-scene"),
            new OperationContext(
                $"agent-{adapterKind}",
                PrincipalKind.Agent,
                $"session-{adapterKind}",
                [OperationCapability.TaskWrite],
                "session-discovery-test"));

        Assert.True(changed.Succeeded);
        Assert.Contains("\"value\": 12", editor.Workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.Contains("\"value\": 10", File.ReadAllText(Path.Combine(root, "scenes", "main.scene.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void SecondEditorGetsReadOnlyWorkspaceAndCannotBecomeSecondWriter()
    {
        var registry = new EditorSessionRegistry(LeaseTimeout);
        var root = CreateProjectRoot("second-editor", SceneText(speed: 10));
        using var primary = registry.OpenEditorSession(
            root,
            "editor-primary",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-primary"),
            FixedInstant);

        using var secondary = registry.OpenEditorSession(
            root,
            "editor-secondary",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-secondary"),
            FixedInstant.AddSeconds(1));

        Assert.True(primary.Succeeded);
        Assert.True(secondary.Succeeded);
        Assert.Equal(EditorSessionConnectionState.ReadOnlyEditor, secondary.State);
        Assert.Equal(ProjectWorkspaceOpenMode.EditorReadOnly, secondary.Workspace.OpenMode);
        Assert.False(secondary.Workspace.CommandBus.CanExecuteMutatingCommands);
        Assert.Same(primary.Workspace, registry.Connect(EditorSessionAdapterKind.Cli, root, "cli", FixedInstant.AddSeconds(2)).Workspace);
    }

    [Fact]
    public void StaleSessionCleanupAndGracefulReleaseAllowNewPrimaryOwner()
    {
        var registry = new EditorSessionRegistry(LeaseTimeout);
        var root = CreateProjectRoot("stale-session", SceneText(speed: 10));
        using var stale = registry.OpenEditorSession(
            root,
            "editor-stale",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-stale"),
            FixedInstant);

        var fallback = registry.Connect(EditorSessionAdapterKind.Mcp, root, "mcp-after-stale", FixedInstant.AddSeconds(31));

        Assert.True(fallback.Succeeded);
        Assert.Equal(EditorSessionConnectionState.HeadlessFallback, fallback.State);
        Assert.Equal(ProjectWorkspaceOpenMode.Headless, fallback.Workspace.OpenMode);
        Assert.Contains(fallback.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0003");

        using var replacement = registry.OpenEditorSession(
            root,
            "editor-replacement",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-replacement"),
            FixedInstant.AddSeconds(32));

        Assert.True(replacement.Succeeded);
        Assert.Equal(EditorSessionConnectionState.ActiveEditor, replacement.State);
        Assert.Equal(ProjectWorkspaceOpenMode.EditorPrimary, replacement.Workspace.OpenMode);

        Assert.False(registry.Release(stale.Descriptor.SessionId, stale.Descriptor.OwnerId));
        using var blocked = registry.OpenEditorSession(
            root,
            "editor-blocked",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-blocked"),
            FixedInstant.AddSeconds(33));
        Assert.Equal(EditorSessionConnectionState.ReadOnlyEditor, blocked.State);

        Assert.True(registry.Release(replacement.Descriptor.SessionId, replacement.Descriptor.OwnerId));
        using var afterRelease = registry.OpenEditorSession(
            root,
            "editor-after-release",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-after-release"),
            FixedInstant.AddSeconds(34));
        Assert.Equal(EditorSessionConnectionState.ActiveEditor, afterRelease.State);
    }

    [Fact]
    public void ClosedEditorAndProjectRootMismatchProduceDiagnosticsAndHeadlessFallback()
    {
        var registry = new EditorSessionRegistry(LeaseTimeout);
        var root = CreateProjectRoot("closed-editor", SceneText(speed: 10));
        var otherRoot = CreateProjectRoot("other-project", SceneText(speed: 20));

        var noEditor = registry.Connect(EditorSessionAdapterKind.Cli, root, "cli-headless", FixedInstant);

        Assert.True(noEditor.Succeeded);
        Assert.Equal(EditorSessionConnectionState.HeadlessFallback, noEditor.State);
        Assert.Equal(ProjectWorkspaceOpenMode.Headless, noEditor.Workspace.OpenMode);
        Assert.Contains(noEditor.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0003");

        using var editor = registry.OpenEditorSession(
            root,
            "editor-mismatch",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-mismatch"),
            FixedInstant);

        var mismatch = registry.ConnectToDescriptor(
            EditorSessionAdapterKind.Mcp,
            otherRoot,
            editor.Descriptor,
            "mcp-mismatch",
            FixedInstant.AddSeconds(1));

        Assert.False(mismatch.Succeeded);
        Assert.Equal(EditorSessionConnectionState.Rejected, mismatch.State);
        Assert.Null(mismatch.WorkspaceOrNull);
        Assert.Contains(mismatch.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0003");
    }

    private static string CreateProjectRoot(string name, string sceneText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-EditorSessionDiscoveryTests", name, Guid.NewGuid().ToString("N"));
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
}
