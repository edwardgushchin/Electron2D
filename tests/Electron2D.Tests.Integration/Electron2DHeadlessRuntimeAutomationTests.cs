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
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class Electron2DHeadlessRuntimeAutomationTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HeadlessRunProducesStableArtifactsAndAppliesInputTrace()
    {
        var projectRoot = CreateProjectRoot("artifacts");
        var outputDirectory = Path.Combine(projectRoot, "artifacts", "run-001");
        var result = RunCli(
            "run",
            "--project",
            projectRoot,
            "--scene",
            "scenes/main.scene.json",
            "--frames",
            "4",
            "--fixed-delta",
            "0.25",
            "--input",
            "tests/input/start-game.json",
            "--capture-frame",
            "3",
            "--output",
            "artifacts/run-001",
            "--input-build-configuration-hash",
            "sha256:runtime-test",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);

        var resultPath = Path.Combine(outputDirectory, "result.json");
        var diagnosticsPath = Path.Combine(outputDirectory, "diagnostics.json");
        var runtimeLogPath = Path.Combine(outputDirectory, "runtime.log.jsonl");
        var sceneTreePath = Path.Combine(outputDirectory, "scene-tree-final.json");
        var performancePath = Path.Combine(outputDirectory, "performance.json");
        var framePath = Path.Combine(outputDirectory, "frame-0003.png");

        Assert.True(File.Exists(resultPath), $"Missing {resultPath}");
        Assert.True(File.Exists(diagnosticsPath), $"Missing {diagnosticsPath}");
        Assert.True(File.Exists(runtimeLogPath), $"Missing {runtimeLogPath}");
        Assert.True(File.Exists(sceneTreePath), $"Missing {sceneTreePath}");
        Assert.True(File.Exists(performancePath), $"Missing {performancePath}");
        Assert.True(File.Exists(framePath), $"Missing {framePath}");

        using var envelope = JsonDocument.Parse(result.Output);
        var root = envelope.RootElement;
        Assert.Equal("run", root.GetProperty("command").GetString());
        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("headless", root.GetProperty("route").GetString());
        Assert.Equal("run.headless", root.GetProperty("data").GetProperty("mode").GetString());
        Assert.Equal("result.json", Path.GetFileName(root.GetProperty("data").GetProperty("artifacts").GetProperty("result").GetString()));

        using var resultJson = JsonDocument.Parse(File.ReadAllText(resultPath));
        var run = resultJson.RootElement;
        Assert.Equal(1, run.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("https://electron2d.dev/schemas/runtime/headless-run-result.schema.json", run.GetProperty("$schema").GetString());
        Assert.Equal("scenes/main.scene.json", run.GetProperty("scene").GetString());
        Assert.Equal(4, run.GetProperty("frames").GetInt32());
        Assert.Equal(0.25, run.GetProperty("fixedDelta").GetDouble());
        Assert.Equal(3, run.GetProperty("capturedFrame").GetInt32());
        Assert.Equal("sha256:runtime-test", run.GetProperty("inputBuildConfigurationHash").GetString());
        Assert.Equal(1, run.GetProperty("inputDocumentRevisions").GetProperty("scenes/main.scene.json").GetInt64());
        Assert.False(run.GetProperty("actionStates").GetProperty("move_right").GetBoolean());

        using var sceneTreeJson = JsonDocument.Parse(File.ReadAllText(sceneTreePath));
        var sceneTree = sceneTreeJson.RootElement;
        Assert.Equal(4, sceneTree.GetProperty("finalFrame").GetInt32());
        Assert.Equal("Player", sceneTree.GetProperty("nodes")[0].GetProperty("name").GetString());
        Assert.False(sceneTree.GetProperty("actionStates").GetProperty("move_right").GetBoolean());

        using var performanceJson = JsonDocument.Parse(File.ReadAllText(performancePath));
        var performance = performanceJson.RootElement;
        Assert.Equal(1.0, performance.GetProperty("simulatedSeconds").GetDouble());
        Assert.Equal(250.0, performance.GetProperty("averageFrameTimeMs").GetDouble());
        Assert.Equal(4.0, performance.GetProperty("fps").GetDouble());

        var logLines = File.ReadAllLines(runtimeLogPath);
        Assert.Contains(logLines, line => line.Contains("\"event\":\"runtime.started\"", StringComparison.Ordinal));
        Assert.Contains(logLines, line => line.Contains("\"event\":\"input.action\"", StringComparison.Ordinal) &&
            line.Contains("\"state\":\"pressed\"", StringComparison.Ordinal));
        Assert.Contains(logLines, line => line.Contains("\"event\":\"input.action\"", StringComparison.Ordinal) &&
            line.Contains("\"state\":\"released\"", StringComparison.Ordinal));
        Assert.Contains(logLines, line => line.Contains("\"event\":\"frame.captured\"", StringComparison.Ordinal));
        Assert.Contains(logLines, line => line.Contains("\"event\":\"runtime.completed\"", StringComparison.Ordinal));

        var png = File.ReadAllBytes(framePath);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4).ToArray());
    }

    [Fact]
    public void HeadlessRunArtifactsAreDeterministicForSameInputs()
    {
        var projectRoot = CreateProjectRoot("deterministic");
        var outputDirectory = Path.Combine(projectRoot, "artifacts", "deterministic");

        RunHeadless(projectRoot, outputDirectory);
        var first = HashArtifacts(outputDirectory);

        RunHeadless(projectRoot, outputDirectory);
        var second = HashArtifacts(outputDirectory);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("headless-input-trace.schema.json")]
    [InlineData("headless-run-result.schema.json")]
    [InlineData("headless-run-diagnostics.schema.json")]
    [InlineData("headless-run-scene-tree.schema.json")]
    [InlineData("headless-run-performance.schema.json")]
    public void RuntimeJsonSchemasArePublished(string fileName)
    {
        var schemaPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "schemas", "runtime", fileName));

        Assert.True(File.Exists(schemaPath), $"Missing runtime schema {schemaPath}");
        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var root = schema.RootElement;
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.StartsWith("https://electron2d.dev/schemas/runtime/", root.GetProperty("$id").GetString(), StringComparison.Ordinal);
        Assert.Equal("object", root.GetProperty("type").GetString());
    }

    private static void RunHeadless(string projectRoot, string outputDirectory)
    {
        var result = RunCli(
            "run",
            "--project",
            projectRoot,
            "--scene",
            "scenes/main.scene.json",
            "--frames",
            "4",
            "--fixed-delta",
            "0.25",
            "--input",
            "tests/input/start-game.json",
            "--capture-frame",
            "3",
            "--output",
            outputDirectory,
            "--input-build-configuration-hash",
            "sha256:runtime-test",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
    }

    private static string HashArtifacts(string outputDirectory)
    {
        var files = new[]
        {
            "diagnostics.json",
            "frame-0003.png",
            "performance.json",
            "result.json",
            "runtime.log.jsonl",
            "scene-tree-final.json"
        };
        using var sha = SHA256.Create();
        using var stream = new MemoryStream();
        foreach (var file in files)
        {
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(file);
            stream.Write(nameBytes);
            stream.WriteByte(0);
            stream.Write(File.ReadAllBytes(Path.Combine(outputDirectory, file)));
            stream.WriteByte(0);
        }

        stream.Position = 0;
        return Convert.ToHexString(sha.ComputeHash(stream));
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
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-HeadlessRuntimeTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        Directory.CreateDirectory(Path.Combine(root, "tests", "input"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), SceneText());
        File.WriteAllText(Path.Combine(root, "tests", "input", "start-game.json"), InputTraceText());
        return root;
    }

    private static string SceneText()
    {
        return """
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
                  "value": 10
                }
              }
            }
          ]
        }
        """;
    }

    private static string InputTraceText()
    {
        return """
        {
          "format": "Electron2D.InputTrace",
          "version": 1,
          "events": [
            {
              "frame": 1,
              "action": "move_right",
              "state": "pressed"
            },
            {
              "frame": 3,
              "action": "move_right",
              "state": "released"
            }
          ]
        }
        """;
    }

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
