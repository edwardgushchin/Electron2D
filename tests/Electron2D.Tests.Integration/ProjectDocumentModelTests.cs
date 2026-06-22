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

public sealed class ProjectDocumentModelTests
{
    [Fact]
    public void ClassifierRecognizesProjectDocumentKindsAndContentForms()
    {
        var scene = ProjectDocumentClassifier.Classify("scenes/main.scene.json", SceneText("Player", 1, 1, includeEnemy: true));
        var resource = ProjectDocumentClassifier.Classify("data/player.e2res", ResourceText(uid: "uid://21i3v9"));
        var settings = ProjectDocumentClassifier.Classify("project.e2project.json", "{\"format\":\"Electron2D.ProjectSettings\",\"version\":1}");
        var code = ProjectDocumentClassifier.Classify("scripts/Player.cs", "public sealed class Player { }");
        var json = ProjectDocumentClassifier.Classify("data/dialogue.json", "{\"line\":\"Hello\"}");
        var text = ProjectDocumentClassifier.Classify("notes/design.txt", "Plain notes");
        var generated = ProjectDocumentClassifier.Classify(".electron2d/import-cache/player.texture.json", "{\"generated\":true}");
        var binary = ProjectDocumentClassifier.ClassifyBinary("assets/player.png", [0x89, 0x50, 0x4E, 0x47]);

        Assert.Equal(ProjectDocumentKind.Scene, scene.Kind);
        Assert.Equal(ProjectDocumentContentKind.Json, scene.ContentKind);
        Assert.Equal(ProjectDocumentKind.Resource, resource.Kind);
        Assert.Equal(ProjectDocumentKind.Settings, settings.Kind);
        Assert.Equal(ProjectDocumentKind.Code, code.Kind);
        Assert.Equal(ProjectDocumentContentKind.CSharp, code.ContentKind);
        Assert.Equal(ProjectDocumentKind.Json, json.Kind);
        Assert.Equal(ProjectDocumentKind.Text, text.Kind);
        Assert.Equal(ProjectDocumentKind.Generated, generated.Kind);
        Assert.True(generated.IsGenerated);
        Assert.Equal(ProjectDocumentKind.BinaryAsset, binary.Kind);
        Assert.True(binary.IsBinary);
    }

    [Fact]
    public void ParserProvidesStableIdentityAndObjectUidRoundTrip()
    {
        var scene = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", 1, 1, includeEnemy: true),
            ProjectDocumentRevisionState.Clean(7));
        var resource = ProjectDocumentParser.ParseText(
            "data/player.e2res",
            ResourceText(uid: "uid://21i3v9"),
            ProjectDocumentRevisionState.Clean(3));
        var settings = ProjectDocumentParser.ParseText(
            "project.e2project.json",
            "{\"format\":\"Electron2D.ProjectSettings\",\"version\":1,\"name\":\"Demo\"}",
            ProjectDocumentRevisionState.Clean(1));

        Assert.Equal("document://scene/res://scenes/main.scene.json", scene.Identity.DocumentId);
        Assert.Equal("document://resource/uid://21i3v9", resource.Identity.DocumentId);
        Assert.Equal("document://settings/res://project.e2project.json", settings.Identity.DocumentId);
        Assert.False(scene.IsDirty);
        Assert.Equal(7, scene.PersistedRevision.Value);
        Assert.Equal(7, scene.InMemoryRevision.Value);

        var serialized = ProjectDocumentStructuralSerializer.Serialize(scene);
        var restored = ProjectDocumentStructuralSerializer.Deserialize(serialized);

        Assert.Equal(scene.Identity.DocumentId, restored.Identity.DocumentId);
        Assert.Equal(
            scene.Objects.Select(projectObject => projectObject.Uid.Value).OrderBy(uid => uid, StringComparer.Ordinal),
            restored.Objects.Select(projectObject => projectObject.Uid.Value).OrderBy(uid => uid, StringComparer.Ordinal));
        Assert.Contains(restored.Objects, projectObject => projectObject.Uid.Value == "scene-node:2" && projectObject.Name == "Player");
        Assert.Contains(resource.Objects, projectObject => projectObject.Uid.Value == "resource:main");
        Assert.Contains(resource.Objects, projectObject => projectObject.Uid.Value == "resource-internal:1");
    }

    [Fact]
    public void RevisionStateTracksDirtyAndPersistedTransitions()
    {
        var clean = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", 1, 1, includeEnemy: true),
            ProjectDocumentRevisionState.Clean(11));

        var dirty = clean.WithInMemoryRevision(clean.InMemoryRevision.Next());
        var saved = dirty.MarkPersisted();

        Assert.True(dirty.IsDirty);
        Assert.Equal(11, dirty.PersistedRevision.Value);
        Assert.Equal(12, dirty.InMemoryRevision.Value);
        Assert.False(saved.IsDirty);
        Assert.Equal(12, saved.PersistedRevision.Value);
        Assert.Equal(12, saved.InMemoryRevision.Value);
    }

    [Fact]
    public void StructuralDiffDetectsNonOverlappingPropertyChanges()
    {
        var before = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", parent: 1, owner: 1, includeEnemy: true, speed: 10, damage: 1),
            ProjectDocumentRevisionState.Clean(1));
        var speedAfter = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", parent: 1, owner: 1, includeEnemy: true, speed: 12, damage: 1),
            ProjectDocumentRevisionState.Clean(2));
        var damageAfter = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", parent: 1, owner: 1, includeEnemy: true, speed: 10, damage: 2),
            ProjectDocumentRevisionState.Clean(2));
        var conflictingAfter = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", parent: 1, owner: 1, includeEnemy: true, speed: 14, damage: 1),
            ProjectDocumentRevisionState.Clean(2));

        var speedChange = Assert.Single(ProjectDocumentStructuralDiff.Compare(before, speedAfter).Changes);
        var damageChange = Assert.Single(ProjectDocumentStructuralDiff.Compare(before, damageAfter).Changes);
        var conflictingChange = Assert.Single(ProjectDocumentStructuralDiff.Compare(before, conflictingAfter).Changes);

        Assert.Equal(ProjectDocumentChangeKind.PropertyChanged, speedChange.Kind);
        Assert.Equal("scene-node:2", speedChange.ObjectUid.Value);
        Assert.Equal("speed", speedChange.PropertyPath);
        Assert.True(ProjectDocumentStructuralDiff.AreNonOverlappingPropertyChanges(speedChange, damageChange));
        Assert.False(ProjectDocumentStructuralDiff.AreNonOverlappingPropertyChanges(speedChange, conflictingChange));
    }

    [Fact]
    public void StructuralDiffUsesUidForRenameMoveDeletionAndAddition()
    {
        var before = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Player", parent: 1, owner: 1, includeEnemy: true),
            ProjectDocumentRevisionState.Clean(4));
        var after = ProjectDocumentParser.ParseText(
            "scenes/main.scene.json",
            SceneText("Hero", parent: null, owner: 1, includeEnemy: false, includeCamera: true),
            ProjectDocumentRevisionState.Clean(5));

        var diff = ProjectDocumentStructuralDiff.Compare(before, after);

        Assert.Contains(diff.Changes, change =>
            change.Kind == ProjectDocumentChangeKind.Renamed &&
            change.ObjectUid.Value == "scene-node:2" &&
            change.OldValue == "Player" &&
            change.NewValue == "Hero");
        Assert.Contains(diff.Changes, change =>
            change.Kind == ProjectDocumentChangeKind.Moved &&
            change.ObjectUid.Value == "scene-node:2" &&
            change.OldValue == "scene-node:1" &&
            change.NewValue is null);
        Assert.Contains(diff.Changes, change =>
            change.Kind == ProjectDocumentChangeKind.Deleted &&
            change.ObjectUid.Value == "scene-node:3");
        Assert.Contains(diff.Changes, change =>
            change.Kind == ProjectDocumentChangeKind.Added &&
            change.ObjectUid.Value == "scene-node:4");
        Assert.DoesNotContain(diff.Changes, change =>
            change.ObjectUid.Value == "scene-node:2" &&
            (change.Kind == ProjectDocumentChangeKind.Added || change.Kind == ProjectDocumentChangeKind.Deleted));
    }

    private static string SceneText(
        string playerName,
        int? parent,
        int? owner,
        bool includeEnemy,
        bool includeCamera = false,
        int speed = 10,
        int damage = 1)
    {
        var parentText = parent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var ownerText = owner?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var extraNodes = includeEnemy
            ? ",{\"id\":3,\"type\":\"Electron2D.Node2D\",\"name\":\"Enemy\",\"parent\":1,\"owner\":1,\"groups\":[],\"properties\":{}}"
            : string.Empty;
        extraNodes += includeCamera
            ? ",{\"id\":4,\"type\":\"Electron2D.Camera2D\",\"name\":\"Camera\",\"parent\":1,\"owner\":1,\"groups\":[],\"properties\":{}}"
            : string.Empty;

        return "{" +
            "\"format\":\"Electron2D.SceneFile\"," +
            "\"version\":1," +
            "\"external\":[]," +
            "\"internal\":[]," +
            "\"nodes\":[" +
            "{\"id\":1,\"type\":\"Electron2D.Node2D\",\"name\":\"Root\",\"parent\":null,\"owner\":null,\"groups\":[],\"properties\":{}}," +
            "{\"id\":2,\"type\":\"Electron2D.Sprite2D\",\"name\":\"" + playerName + "\",\"parent\":" + parentText + ",\"owner\":" + ownerText + ",\"groups\":[],\"properties\":{" +
            "\"speed\":{\"type\":\"Int\",\"value\":" + speed.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
            "\"damage\":{\"type\":\"Int\",\"value\":" + damage.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}" +
            "}}" +
            extraNodes +
            "]}";
    }

    private static string ResourceText(string uid)
    {
        return "{" +
            "\"format\":\"Electron2D.ResourceFile\"," +
            "\"version\":1," +
            "\"uid\":\"" + uid + "\"," +
            "\"type\":\"Electron2D.Resource\"," +
            "\"path\":\"res://data/player.e2res\"," +
            "\"external\":[]," +
            "\"internal\":[{\"id\":1,\"type\":\"Electron2D.Resource\",\"properties\":{\"resource_name\":{\"type\":\"String\",\"value\":\"Stats\"}}}]," +
            "\"properties\":{\"display_name\":{\"type\":\"String\",\"value\":\"Player\"}}" +
            "}";
    }
}
