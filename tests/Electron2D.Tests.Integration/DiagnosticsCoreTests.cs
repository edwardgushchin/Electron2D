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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class DiagnosticsCoreTests
{
    [Fact]
    public void RegistryProvidesStableUniqueDefinitions()
    {
        var definitions = DiagnosticCodeRegistry.All;

        Assert.Equal(
            definitions.Select(definition => definition.Code).Order(StringComparer.Ordinal),
            definitions.Select(definition => definition.Code));
        Assert.Equal(
            definitions.Count,
            definitions.Select(definition => definition.Code).Distinct(StringComparer.Ordinal).Count());

        var malformedProject = DiagnosticCodeRegistry.Get("E2D-PROJECT-0001");
        Assert.Equal(DiagnosticSeverity.Error, malformedProject.Severity);
        Assert.Equal(DiagnosticCategory.Project, malformedProject.Category);
        Assert.Equal(
            "docs/documentation/diagnostics/diagnostics-core.md#e2d-project-0001",
            malformedProject.DocumentationUri);

        Assert.Contains(definitions, definition => definition.Code == "E2D-DIAG-0001");
        Assert.Contains(definitions, definition => definition.Code == "E2D-PROJECT-0002");
        Assert.Contains(definitions, definition => definition.Code == "E2D-PROJECT-0003");
    }

    [Fact]
    public void FactoryRequiresKnownCodeAndMandatoryFields()
    {
        var location = new DiagnosticLocation(
            file: "scenes/main.scene.json",
            line: 12,
            column: 4,
            sceneUid: "scene://main",
            nodePath: "/Root/Player",
            resourceUid: "uid://player-texture");
        var diagnostic = StructuredDiagnostic.Create(
            "E2D-PROJECT-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Scene JSON cannot be parsed.",
            location,
            relatedLocations: [],
            suggestedFixes: []);

        Assert.Equal("E2D-PROJECT-0001", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(DiagnosticCategory.Project, diagnostic.Category);
        Assert.Equal("Scene JSON cannot be parsed.", diagnostic.Message);
        Assert.Same(location, diagnostic.Location);
        Assert.Equal("docs/documentation/diagnostics/diagnostics-core.md#e2d-project-0001", diagnostic.DocumentationUri);

        Assert.Throws<ArgumentException>(() => StructuredDiagnostic.Create(
            "E2D-UNKNOWN-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            "Unknown code must fail closed.",
            location: null,
            relatedLocations: [],
            suggestedFixes: []));
        Assert.Throws<ArgumentException>(() => StructuredDiagnostic.Create(
            "E2D-PROJECT-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            string.Empty,
            location: null,
            relatedLocations: [],
            suggestedFixes: []));
        Assert.Throws<InvalidOperationException>(() => StructuredDiagnostic.Create(
            "E2D-PROJECT-0001",
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Project,
            "Severity must match registry.",
            location: null,
            relatedLocations: [],
            suggestedFixes: []));
    }

    [Fact]
    public void SuggestedFixesRejectUnsafeActions()
    {
        var fix = new DiagnosticSuggestedFix(
            "Add supported version field.",
            [
                DiagnosticFixAction.UpdateJsonProperty(
                    "project.e2project.json",
                    "/version",
                    expectedValue: null,
                    newValue: "1")
            ]);

        Assert.Equal("Add supported version field.", fix.Title);
        var action = Assert.Single(fix.Actions);
        Assert.Equal(DiagnosticFixActionKind.UpdateJsonProperty, action.Kind);
        Assert.Equal("project.e2project.json", action.Path);
        Assert.Equal("/version", action.JsonPointer);
        Assert.Equal("1", action.NewValue);

        Assert.Throws<ArgumentException>(() => DiagnosticFixAction.CreateFile("C:\\temp\\project.e2project.json", "{}"));
        Assert.Throws<ArgumentException>(() => DiagnosticFixAction.CreateFile("../outside.json", "{}"));
        Assert.Throws<ArgumentException>(() => DiagnosticFixAction.ReplaceText(
            ".electron2d/import-cache/generated.json",
            startLine: 1,
            startColumn: 1,
            endLine: 1,
            endColumn: 2,
            expectedText: "{}",
            newText: "{ \"version\": 1 }"));
        Assert.Throws<ArgumentException>(() => DiagnosticFixAction.UpdateJsonProperty(
            "project.e2project.json",
            "version",
            expectedValue: null,
            newValue: "1"));
    }

    [Fact]
    public void JsonSerializationIsStableAndRoundTripsAllFields()
    {
        var diagnostic = StructuredDiagnostic.Create(
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

        var json = DiagnosticJsonSerializer.Serialize(diagnostic);
        var restored = DiagnosticJsonSerializer.Deserialize(json);
        var jsonAgain = DiagnosticJsonSerializer.Serialize(restored);

        Assert.StartsWith(
            "{\n  \"code\": \"E2D-PROJECT-0003\",\n  \"severity\": \"Warning\",\n  \"category\": \"Project\",",
            json,
            StringComparison.Ordinal);
        Assert.Equal(json, jsonAgain);
        Assert.Equal(diagnostic.Code, restored.Code);
        Assert.Equal(diagnostic.Message, restored.Message);
        Assert.NotNull(restored.Location);
        Assert.Equal("project.e2project.json", restored.Location.File);
        Assert.Equal(2, restored.Location.Line);
        Assert.Equal("uid://project-settings", restored.Location.ResourceUid);
        Assert.Equal("Scene references project settings.", Assert.Single(restored.RelatedLocations).Message);
        Assert.Equal("Write current schema version.", Assert.Single(restored.SuggestedFixes).Title);
        Assert.Equal("/version", Assert.Single(Assert.Single(restored.SuggestedFixes).Actions).JsonPointer);
    }
}
