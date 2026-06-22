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

public sealed class ProjectTextFormatTests
{
    [Fact]
    public void FormatterWritesStableSceneOrderAndKeepsPropertyDiffLocal()
    {
        var formatted = ProjectTextFormatter.FormatText("scenes/main.scene.json", UnformattedScene(speed: 10, damage: 1));
        var formattedAgain = ProjectTextFormatter.FormatText("scenes/main.scene.json", formatted);
        var edited = ProjectTextFormatter.FormatText("scenes/main.scene.json", UnformattedScene(speed: 12, damage: 1));

        Assert.StartsWith(
            "{\n  \"format\": \"Electron2D.SceneFile\",\n  \"version\": 1,\n  \"external\": [],\n  \"internal\": [],\n  \"nodes\": [",
            formatted,
            StringComparison.Ordinal);
        Assert.True(
            formatted.IndexOf("\"damage\"", StringComparison.Ordinal) <
            formatted.IndexOf("\"speed\"", StringComparison.Ordinal));
        Assert.Equal(formatted, formattedAgain);

        var changedLines = formatted.Split('\n').Zip(edited.Split('\n'))
            .Where(pair => !string.Equals(pair.First, pair.Second, StringComparison.Ordinal))
            .ToArray();

        var changedLine = Assert.Single(changedLines);
        Assert.Contains("\"value\": 10", changedLine.First, StringComparison.Ordinal);
        Assert.Contains("\"value\": 12", changedLine.Second, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationAddsCurrentVersionAndPreservesResourceUid()
    {
        var result = ProjectTextMigrationPipeline.MigrateText(
            "data/player.e2res",
            LegacyResourceWithoutVersion(uid: "uid://21i3v9", path: "res://data/player.e2res"));

        Assert.True(result.Changed);
        Assert.Contains("project-text-format/resource-file-v0-to-v1", result.AppliedMigrationIds);
        Assert.Contains("\"version\": 1", result.Text, StringComparison.Ordinal);
        Assert.Contains("\"uid\": \"uid://21i3v9\"", result.Text, StringComparison.Ordinal);

        var snapshot = ProjectDocumentParser.ParseText(
            "data/player.e2res",
            result.Text,
            ProjectDocumentRevisionState.Clean(1));

        Assert.Equal("document://resource/uid://21i3v9", snapshot.Identity.DocumentId);
    }

    [Fact]
    public void ValidatorRejectsAbsolutePathsSecretsAndEditorState()
    {
        var result = ProjectTextValidator.ValidateText(
            "project.e2project.json",
            "{" +
            "\"format\":\"Electron2D.ProjectSettings\"," +
            "\"version\":1," +
            "\"name\":\"Demo\"," +
            "\"cachePath\":\"/home/user/.cache/electron2d\"," +
            "\"apiToken\":\"secret-value\"," +
            "\"selection\":\"Player\"" +
            "}");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "E2D-TEXT-ABSOLUTE-PATH" && error.JsonPath == "$.cachePath");
        Assert.Contains(result.Errors, error => error.Code == "E2D-TEXT-SECRET-FIELD" && error.JsonPath == "$.apiToken");
        Assert.Contains(result.Errors, error => error.Code == "E2D-TEXT-EDITOR-STATE" && error.JsonPath == "$.selection");
    }

    [Fact]
    public void ResourceRenameKeepsDocumentIdentityAndStructuralDiff()
    {
        var beforeText = ProjectTextFormatter.FormatText(
            "data/player.e2res",
            ResourceText(uid: "uid://21i3v9", path: "res://data/player.e2res"));
        var afterText = ProjectTextFormatter.FormatText(
            "data/player.e2res",
            ResourceText(uid: "uid://21i3v9", path: "res://characters/player.e2res"));

        var before = ProjectDocumentParser.ParseText("data/player.e2res", beforeText, ProjectDocumentRevisionState.Clean(1));
        var after = ProjectDocumentParser.ParseText("data/player.e2res", afterText, ProjectDocumentRevisionState.Clean(2));
        var diff = ProjectDocumentStructuralDiff.Compare(before, after);

        Assert.Equal(before.Identity.DocumentId, after.Identity.DocumentId);
        Assert.Contains(diff.Changes, change =>
            change.Kind == ProjectDocumentChangeKind.Renamed &&
            change.ObjectUid.Value == "resource:main" &&
            change.OldValue == "res://data/player.e2res" &&
            change.NewValue == "res://characters/player.e2res");
        Assert.DoesNotContain(diff.Changes, change =>
            change.ObjectUid.Value == "resource:main" &&
            (change.Kind == ProjectDocumentChangeKind.Added || change.Kind == ProjectDocumentChangeKind.Deleted));
    }

    [Fact]
    public void SchemasAreDraft202012AndDescribeKnownFormats()
    {
        foreach (var schemaKind in new[]
        {
            ProjectTextSchemaKind.SceneFile,
            ProjectTextSchemaKind.ResourceFile,
            ProjectTextSchemaKind.ProjectSettings
        })
        {
            using var schema = JsonDocument.Parse(ProjectTextSchemaRegistry.GetSchemaText(schemaKind));
            var root = schema.RootElement;

            Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
            Assert.Equal("object", root.GetProperty("type").GetString());
            Assert.Contains("format", root.GetProperty("required").EnumerateArray().Select(item => item.GetString()));
            Assert.Contains("version", root.GetProperty("required").EnumerateArray().Select(item => item.GetString()));
        }
    }

    private static string UnformattedScene(int speed, int damage)
    {
        return "{" +
            "\"nodes\":[{" +
            "\"properties\":{\"speed\":{\"value\":" + speed.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"type\":\"Int\"},\"damage\":{\"value\":" + damage.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"type\":\"Int\"}}," +
            "\"groups\":[],\"owner\":null,\"parent\":null,\"name\":\"Root\",\"type\":\"Electron2D.Node2D\",\"id\":1" +
            "}]," +
            "\"internal\":[]," +
            "\"format\":\"Electron2D.SceneFile\"," +
            "\"external\":[]," +
            "\"version\":1" +
            "}";
    }

    private static string LegacyResourceWithoutVersion(string uid, string path)
    {
        return "{" +
            "\"format\":\"Electron2D.ResourceFile\"," +
            "\"uid\":\"" + uid + "\"," +
            "\"type\":\"Electron2D.Resource\"," +
            "\"path\":\"" + path + "\"," +
            "\"external\":[]," +
            "\"internal\":[]," +
            "\"properties\":{\"display_name\":{\"type\":\"String\",\"value\":\"Player\"}}" +
            "}";
    }

    private static string ResourceText(string uid, string path)
    {
        return "{" +
            "\"format\":\"Electron2D.ResourceFile\"," +
            "\"version\":1," +
            "\"uid\":\"" + uid + "\"," +
            "\"type\":\"Electron2D.Resource\"," +
            "\"path\":\"" + path + "\"," +
            "\"external\":[]," +
            "\"internal\":[]," +
            "\"properties\":{\"display_name\":{\"type\":\"String\",\"value\":\"Player\"}}" +
            "}";
    }
}
