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
using Electron2D.ProjectSystem;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class RuntimeDebugBridgeTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 22, 30, 0, TimeSpan.Zero);

    [Fact]
    public void RuntimeDebugBridgeReadsRemoteSceneTreeAndInspectsProperties()
    {
        var projectRoot = CreateProjectRoot("inspect");
        var start = RuntimeDebugBridge.Start(new RuntimeDebugStartRequest(
            projectRoot,
            "scenes/main.scene.json",
            RuntimeDebugSessionKind.EditorAttachedPreview,
            developmentMode: true,
            "sha256:debug"));

        Assert.True(start.Succeeded);
        Assert.NotNull(start.Session);
        var session = start.Session;
        Assert.Equal(RuntimeDebugSessionKind.EditorAttachedPreview, session.SessionKind);
        Assert.Equal(RuntimeDebugSessionState.Running, session.State);

        var sceneTree = session.GetSceneTree();
        Assert.Equal("scenes/main.scene.json", sceneTree.Scene);
        Assert.Contains(sceneTree.Nodes, node => node.Path == "/Root");
        var player = Assert.Single(sceneTree.Nodes, node => node.Path == "/Root/Player");
        Assert.Equal("Electron2D.Node2D", player.Type);
        Assert.Equal("/Root", player.ParentPath);
        Assert.Equal(12, player.Properties["speed"]!["value"]!.GetValue<int>());

        var inspected = session.InspectNode("/Root/Player");

        Assert.True(inspected.Succeeded);
        Assert.Equal("/Root/Player", inspected.Node!.Path);
        Assert.Equal("Electron2D.Node2D", inspected.Node.Type);
        Assert.Equal(12, inspected.Node.Properties["speed"]!["value"]!.GetValue<int>());
    }

    [Fact]
    public void RuntimeDebugBridgePauseStepInputAndScreenshotAreDeterministic()
    {
        var projectRoot = CreateProjectRoot("deterministic");
        var session = StartSession(projectRoot);

        session.Pause();
        session.StepFrame(count: 2, fixedDelta: 0.25);
        session.StepPhysics(count: 1, fixedDelta: 0.5);
        session.InjectInput("jump", pressed: true);
        var screenshot = session.CaptureScreenshot();
        var screenshotAgain = session.CaptureScreenshot();

        Assert.Equal(RuntimeDebugSessionState.Paused, session.State);
        Assert.Equal(2, session.CurrentFrame);
        Assert.Equal(1, session.CurrentPhysicsFrame);
        Assert.True(session.InputActions["jump"]);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, screenshot.Bytes.Take(4).ToArray());
        Assert.Equal(screenshot.Sha256, screenshotAgain.Sha256);

        var metrics = session.GetMetrics();
        Assert.Equal(2, metrics.CurrentFrame);
        Assert.Equal(1, metrics.CurrentPhysicsFrame);
        Assert.Equal(0.5, metrics.SimulatedSeconds);
        Assert.Equal(0.25, metrics.LastFrameDelta);
        Assert.Equal(0.5, metrics.LastPhysicsDelta);
        Assert.Equal(4.0, metrics.Fps);
    }

    [Fact]
    public void RuntimeDebugBridgeFailsClosedForProductionModeMissingNodeAndMutation()
    {
        var projectRoot = CreateProjectRoot("fail-closed");
        var production = RuntimeDebugBridge.Start(new RuntimeDebugStartRequest(
            projectRoot,
            "scenes/main.scene.json",
            RuntimeDebugSessionKind.HeadlessPreview,
            developmentMode: false,
            "sha256:debug"));

        Assert.False(production.Succeeded);
        Assert.Null(production.Session);
        Assert.Equal("E2D-RUNTIME-0001", Assert.Single(production.Diagnostics).Code);

        var session = StartSession(projectRoot);
        var missing = session.InspectNode("/Root/Missing");

        Assert.False(missing.Succeeded);
        var missingDiagnostic = Assert.Single(missing.Diagnostics);
        Assert.Equal("E2D-RUNTIME-0001", missingDiagnostic.Code);
        Assert.Equal("/Root/Missing", missingDiagnostic.Location!.NodePath);

        var mutation = session.TrySetNodeProperty("/Root/Player", "speed", JsonValue.Create(24)!);

        Assert.False(mutation.Succeeded);
        Assert.Equal("E2D-RUNTIME-0001", Assert.Single(mutation.Diagnostics).Code);
        Assert.Equal(12, session.InspectNode("/Root/Player").Node!.Properties["speed"]!["value"]!.GetValue<int>());
    }

    [Fact]
    public void CliRunDebugReturnsBridgeEnvelopeAndScreenshotMetadata()
    {
        var projectRoot = CreateProjectRoot("cli");
        var screenshotPath = Path.Combine(projectRoot, "artifacts", "debug", "frame.png");
        var result = RunCli(
            "run",
            "debug",
            "--project",
            projectRoot,
            "--scene",
            "scenes/main.scene.json",
            "--session-kind",
            "editor",
            "--step-frames",
            "2",
            "--step-physics",
            "1",
            "--fixed-delta",
            "0.25",
            "--physics-delta",
            "0.5",
            "--input-action",
            "jump=pressed",
            "--inspect-node",
            "/Root/Player",
            "--screenshot",
            "artifacts/debug/frame.png",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.True(File.Exists(screenshotPath), $"Missing {screenshotPath}");
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        Assert.Equal("run debug", root.GetProperty("command").GetString());
        Assert.Equal("headless", root.GetProperty("route").GetString());
        Assert.Equal("runtime.debugBridge", root.GetProperty("data").GetProperty("mode").GetString());
        Assert.Equal("EditorAttachedPreview", root.GetProperty("data").GetProperty("session").GetProperty("sessionKind").GetString());
        Assert.Equal(2, root.GetProperty("data").GetProperty("metrics").GetProperty("currentFrame").GetInt32());
        Assert.Equal(1, root.GetProperty("data").GetProperty("metrics").GetProperty("currentPhysicsFrame").GetInt32());
        Assert.Equal("/Root/Player", root.GetProperty("data").GetProperty("inspectedNode").GetProperty("path").GetString());
        Assert.Equal("/Root/Player", root.GetProperty("data").GetProperty("sceneTree").GetProperty("nodes")[1].GetProperty("path").GetString());
        Assert.Equal("artifacts/debug/frame.png", root.GetProperty("data").GetProperty("screenshot").GetProperty("path").GetString());
        Assert.True(root.GetProperty("data").GetProperty("screenshot").GetProperty("sha256").GetString()!.Length > 0);
    }

    private static RuntimeDebugSession StartSession(string projectRoot)
    {
        var start = RuntimeDebugBridge.Start(new RuntimeDebugStartRequest(
            projectRoot,
            "scenes/main.scene.json",
            RuntimeDebugSessionKind.HeadlessPreview,
            developmentMode: true,
            "sha256:debug"));

        Assert.True(start.Succeeded);
        Assert.NotNull(start.Session);
        return start.Session;
    }

    private static CliRunResult RunCli(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(args, output, error, CliExecutionContext.ForTests(FixedInstant));

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
    }

    private static string CreateProjectRoot(string name)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-RuntimeDebugBridgeTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), SceneText());
        return root;
    }

    private static string SceneText()
    {
        return """
        {
          "format": "Electron2D.SceneFile",
          "version": 1,
          "nodes": [
            {
              "id": 1,
              "type": "Electron2D.Node",
              "name": "Root",
              "parent": null,
              "properties": {
                "visible": {
                  "type": "Bool",
                  "value": true
                }
              }
            },
            {
              "id": 2,
              "type": "Electron2D.Node2D",
              "name": "Player",
              "parent": 1,
              "properties": {
                "speed": {
                  "type": "Int",
                  "value": 12
                }
              }
            }
          ]
        }
        """;
    }

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
