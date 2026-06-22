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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class DiagnosticsAdapterTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CliDiagnosticJsonPreservesLocationRelatedLocationsAndSuggestedFixes()
    {
        var diagnostic = CreateSampleDiagnostic();
        var json = Electron2DCommandLine.WriteDiagnostics([diagnostic]);
        var item = Assert.Single(json);

        Assert.Equal("E2D-PROJECT-0003", item!["code"]!.GetValue<string>());
        Assert.Equal("project.e2project.json", item["location"]!["file"]!.GetValue<string>());
        Assert.Equal(2, item["location"]!["line"]!.GetValue<int>());
        Assert.Equal("uid://project-settings", item["location"]!["resourceUid"]!.GetValue<string>());
        Assert.Equal("Scene references project settings.", item["relatedLocations"]![0]!["message"]!.GetValue<string>());
        Assert.Equal("Write current schema version.", item["suggestedFixes"]![0]!["title"]!.GetValue<string>());
        Assert.Equal("UpdateJsonProperty", item["suggestedFixes"]![0]!["actions"]![0]!["kind"]!.GetValue<string>());
        Assert.Equal("/version", item["suggestedFixes"]![0]!["actions"]![0]!["jsonPointer"]!.GetValue<string>());
    }

    [Fact]
    public void DiagnosticStreamEventsUseFullPayloadForFakeConsumers()
    {
        var diagnostic = CreateSampleDiagnostic();
        var payload = DiagnosticStreamEventJsonSerializer.WriteEvent(
            "diagnostics.updated",
            "fake.consumer",
            FixedInstant,
            [diagnostic]);
        var line = payload.ToJsonString();

        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("diagnostics.updated", root.GetProperty("event").GetString());
        Assert.Equal("fake.consumer", root.GetProperty("producer").GetString());
        Assert.Equal("2026-06-22T19:00:00.0000000+00:00", root.GetProperty("timestampUtc").GetString());
        Assert.Equal("E2D-PROJECT-0003", root.GetProperty("diagnostics")[0].GetProperty("code").GetString());
        Assert.Equal("project.e2project.json", root.GetProperty("diagnostics")[0].GetProperty("location").GetProperty("file").GetString());
    }

    [Fact]
    public void SarifSerializerWritesRulesResultsLocationsAndPreservedPayload()
    {
        var diagnostic = CreateSampleDiagnostic();
        var sarif = DiagnosticSarifSerializer.WriteRun("Electron2D", "https://electron2d.dev", [diagnostic]);

        Assert.Equal("https://json.schemastore.org/sarif-2.1.0.json", sarif["$schema"]!.GetValue<string>());
        Assert.Equal("2.1.0", sarif["version"]!.GetValue<string>());
        var run = sarif["runs"]![0]!;
        Assert.Equal("Electron2D", run["tool"]!["driver"]!["name"]!.GetValue<string>());
        Assert.Equal("E2D-PROJECT-0003", run["tool"]!["driver"]!["rules"]![0]!["id"]!.GetValue<string>());

        var result = run["results"]![0]!;
        Assert.Equal("E2D-PROJECT-0003", result["ruleId"]!.GetValue<string>());
        Assert.Equal("warning", result["level"]!.GetValue<string>());
        Assert.Equal("project.e2project.json", result["locations"]![0]!["physicalLocation"]!["artifactLocation"]!["uri"]!.GetValue<string>());
        Assert.Equal(2, result["locations"]![0]!["physicalLocation"]!["region"]!["startLine"]!.GetValue<int>());
        Assert.Equal("E2D-PROJECT-0003", result["properties"]!["electron2dDiagnostic"]!["code"]!.GetValue<string>());
        Assert.Equal("UpdateJsonProperty", result["properties"]!["electron2dSuggestedFixes"]![0]!["actions"]![0]!["kind"]!.GetValue<string>());
    }

    [Fact]
    public void CliValidateSarifReturnsSarifEnvelope()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-DiagnosticsAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectRoot);
        var result = RunCli(
            "validate",
            "--project",
            projectRoot,
            "--format",
            "sarif");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        Assert.Equal("https://json.schemastore.org/sarif-2.1.0.json", root.GetProperty("$schema").GetString());
        Assert.Equal("2.1.0", root.GetProperty("version").GetString());
        Assert.Equal("Electron2D", root.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("electron2d-diagnostic.schema.json")]
    [InlineData("diagnostic-stream-event.schema.json")]
    public void DiagnosticAdapterJsonSchemasArePublished(string fileName)
    {
        var schemaPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schemas", "diagnostics", fileName));

        Assert.True(File.Exists(schemaPath), $"Missing diagnostics schema {schemaPath}");
        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var root = schema.RootElement;
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.StartsWith("https://electron2d.dev/schemas/diagnostics/", root.GetProperty("$id").GetString(), StringComparison.Ordinal);
        Assert.Equal("object", root.GetProperty("type").GetString());
    }

    private static StructuredDiagnostic CreateSampleDiagnostic()
    {
        return StructuredDiagnostic.Create(
            "E2D-PROJECT-0003",
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Project,
            "Project file can be migrated safely.",
            new DiagnosticLocation(
                file: "project.e2project.json",
                line: 2,
                column: 3,
                sceneUid: null,
                nodePath: null,
                resourceUid: "uid://project-settings"),
            relatedLocations:
            [
                new DiagnosticRelatedLocation(
                    new DiagnosticLocation(
                        file: "scenes/main.scene.json",
                        line: 7,
                        column: 9,
                        sceneUid: "scene://main",
                        nodePath: "/Root",
                        resourceUid: null),
                    "Scene references project settings.")
            ],
            suggestedFixes:
            [
                new DiagnosticSuggestedFix(
                    "Write current schema version.",
                    [
                        DiagnosticFixAction.UpdateJsonProperty(
                            "project.e2project.json",
                            "/version",
                            expectedValue: null,
                            newValue: "1")
                    ])
            ]);
    }

    private static CliRunResult RunCli(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(args, output, error, CliExecutionContext.ForTests(FixedInstant));

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
    }

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
