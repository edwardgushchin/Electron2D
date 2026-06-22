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
using Electron2D.Testing;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class Electron2DSceneVisualTestingTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SceneTestRunnerLoadsSceneAdvancesFramesAndWritesVisualArtifacts()
    {
        var projectRoot = CreateProjectRoot("runner");
        var outputDirectory = Path.Combine(projectRoot, "artifacts", "scene-tests");

        var result = SceneTestRunner.Run(new SceneTestRunRequest(
            projectRoot,
            "tests/electron2d.scene-tests.json",
            outputDirectory,
            "sha256:scene-tests",
            FixedInstant));

        Assert.True(result.Succeeded);
        Assert.Equal(1, Assert.Single(result.Tests).FramesAdvanced);
        Assert.Equal("player_exists", Assert.Single(result.Tests).Name);
        Assert.Empty(result.Diagnostics);

        var resultPath = Path.Combine(outputDirectory, "result.json");
        var diagnosticsPath = Path.Combine(outputDirectory, "diagnostics.json");
        var eventsPath = Path.Combine(outputDirectory, "events.jsonl");
        var screenshotPath = Path.Combine(outputDirectory, "screenshots", "player_exists-frame-0001.png");
        var diffPath = Path.Combine(outputDirectory, "pixel-diff", "player_exists-diff.png");

        Assert.True(File.Exists(resultPath), $"Missing {resultPath}");
        Assert.True(File.Exists(diagnosticsPath), $"Missing {diagnosticsPath}");
        Assert.True(File.Exists(eventsPath), $"Missing {eventsPath}");
        Assert.True(File.Exists(screenshotPath), $"Missing {screenshotPath}");
        Assert.True(File.Exists(diffPath), $"Missing {diffPath}");

        using var resultJson = JsonDocument.Parse(File.ReadAllText(resultPath));
        var root = resultJson.RootElement;
        Assert.Equal("https://electron2d.dev/schemas/testing/scene-test-result.schema.json", root.GetProperty("$schema").GetString());
        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("tests/electron2d.scene-tests.json", root.GetProperty("suite").GetString());
        Assert.Equal("sha256:scene-tests", root.GetProperty("inputBuildConfigurationHash").GetString());
        Assert.Equal(1, root.GetProperty("inputDocumentRevisions").GetProperty("scenes/main.scene.json").GetInt64());
        var visual = root.GetProperty("tests")[0].GetProperty("visual");
        Assert.True(visual.GetProperty("passed").GetBoolean());
        Assert.Equal(0, visual.GetProperty("differenceRatio").GetDouble());

        var events = File.ReadAllLines(eventsPath);
        Assert.Contains(events, line => line.Contains("\"event\":\"test.suiteStarted\"", StringComparison.Ordinal));
        Assert.Contains(events, line => line.Contains("\"event\":\"test.frameAdvanced\"", StringComparison.Ordinal));
        Assert.Contains(events, line => line.Contains("\"event\":\"test.visualCompared\"", StringComparison.Ordinal));
        Assert.Contains(events, line => line.Contains("\"event\":\"test.suiteCompleted\"", StringComparison.Ordinal));
    }

    [Fact]
    public void CliTestJsonRunsProjectSceneTestsAndReturnsStableEnvelope()
    {
        var projectRoot = CreateProjectRoot("cli");
        var result = RunCli(
            "test",
            "--project",
            projectRoot,
            "--format",
            "json",
            "--output",
            "artifacts/tests",
            "--input-build-configuration-hash",
            "sha256:cli-test");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);

        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("test", root.GetProperty("command").GetString());
        Assert.Equal("headless", root.GetProperty("route").GetString());
        Assert.Equal("test.scene", root.GetProperty("data").GetProperty("mode").GetString());
        Assert.True(root.GetProperty("data").GetProperty("succeeded").GetBoolean());
        Assert.Equal("player_exists", root.GetProperty("data").GetProperty("tests")[0].GetProperty("name").GetString());
        Assert.True(File.Exists(Path.Combine(projectRoot, "artifacts", "tests", "result.json")));
    }

    [Theory]
    [InlineData("scene-test-suite.schema.json")]
    [InlineData("scene-test-result.schema.json")]
    [InlineData("scene-test-diagnostics.schema.json")]
    [InlineData("scene-test-events.schema.json")]
    public void TestingJsonSchemasArePublished(string fileName)
    {
        var schemaPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schemas", "testing", fileName));

        Assert.True(File.Exists(schemaPath), $"Missing testing schema {schemaPath}");
        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var root = schema.RootElement;
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.StartsWith("https://electron2d.dev/schemas/testing/", root.GetProperty("$id").GetString(), StringComparison.Ordinal);
        Assert.Equal("object", root.GetProperty("type").GetString());
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
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-SceneVisualTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        Directory.CreateDirectory(Path.Combine(root, "tests", "references"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), SceneText());
        File.WriteAllText(Path.Combine(root, "tests", "electron2d.scene-tests.json"), SuiteText());
        File.WriteAllBytes(Path.Combine(root, "tests", "references", "player-frame.png"), SceneTestRunner.DeterministicFramePng);
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

    private static string SuiteText()
    {
        return """
        {
          "format": "Electron2D.SceneTestSuite",
          "version": 1,
          "tests": [
            {
              "name": "player_exists",
              "scene": "scenes/main.scene.json",
              "frames": 1,
              "fixedDelta": 0.5,
              "assertNodes": [
                {
                  "path": "/Player",
                  "type": "Electron2D.Node2D"
                }
              ],
              "assertProperties": [
                {
                  "node": "/Player",
                  "property": "speed",
                  "equals": 10
                }
              ],
              "visual": {
                "captureFrame": 1,
                "reference": "tests/references/player-frame.png",
                "tolerance": 0
              }
            }
          ]
        }
        """;
    }

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
