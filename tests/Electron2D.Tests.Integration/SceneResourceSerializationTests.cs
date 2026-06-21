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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SceneResourceSerializationTests
{
    [Fact]
    public void SceneFileTextSerializerPreservesValuesAndReferencesAfterModifySaveLoad()
    {
        var scene = new Electron2D.SceneFileDocument(
            externalReferences:
            [
                new Electron2D.ResourceFileExternalReference(
                    id: 1,
                    uid: 987654321L,
                    path: "res://textures/player.png",
                    type: "Electron2D.Texture2D")
            ],
            internalResources:
            [
                new Electron2D.SerializedResourceEntry(
                    id: 1,
                    type: typeof(PlayerStatsResource).FullName!,
                    properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["display_name"] = Electron2D.SerializedPropertyValue.FromVariant("Stats")
                    })
            ],
            nodes:
            [
                new Electron2D.SceneFileNode(
                    id: 1,
                    type: "Electron2D.Node2D",
                    name: "Root",
                    parentId: null,
                    ownerId: null,
                    persistentGroups: ["level"],
                    properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["state"] = Electron2D.SerializedPropertyValue.FromDictionary(
                        [
                            new Electron2D.SerializedPropertyDictionaryEntry(
                                Electron2D.SerializedPropertyValue.FromVariant("spawn"),
                                Electron2D.SerializedPropertyValue.FromVariant(new Electron2D.Vector2(8f, 16f))),
                            new Electron2D.SerializedPropertyDictionaryEntry(
                                Electron2D.SerializedPropertyValue.FromVariant("frames"),
                                Electron2D.SerializedPropertyValue.FromArray(
                                [
                                    Electron2D.SerializedPropertyValue.FromVariant(1),
                                    Electron2D.SerializedPropertyValue.FromVariant(2)
                                ])),
                            new Electron2D.SerializedPropertyDictionaryEntry(
                                Electron2D.SerializedPropertyValue.FromVariant("alignment"),
                                Electron2D.SerializedPropertyValue.FromEnum(Electron2D.HorizontalAlignment.Center)),
                            new Electron2D.SerializedPropertyDictionaryEntry(
                                Electron2D.SerializedPropertyValue.FromVariant("maybe_lives"),
                                Electron2D.SerializedPropertyValue.FromNullable(typeof(int), Electron2D.SerializedPropertyValue.FromVariant(3)))
                        ])
                    }),
                new Electron2D.SceneFileNode(
                    id: 2,
                    type: "Electron2D.Sprite2D",
                    name: "Player",
                    parentId: 1,
                    ownerId: 1,
                    persistentGroups: ["persisted"],
                    properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["texture"] = Electron2D.SerializedPropertyValue.ExternalResource(1),
                        ["stats"] = Electron2D.SerializedPropertyValue.InternalResource(1)
                    })
            ]);

        var loaded = Electron2D.SceneFileTextSerializer.Deserialize(
            Electron2D.SceneFileTextSerializer.Serialize(scene));
        var player = loaded.Nodes.Single(node => node.Id == 2);
        var modifiedPlayerProperties = player.Properties.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        modifiedPlayerProperties["flip_h"] = Electron2D.SerializedPropertyValue.FromVariant(true);
        var modifiedNodes = loaded.Nodes
            .Select(node => node.Id == 2 ? node.WithProperties(modifiedPlayerProperties) : node)
            .ToArray();
        var modified = new Electron2D.SceneFileDocument(
            loaded.ExternalReferences,
            loaded.InternalResources,
            modifiedNodes);

        var reloaded = Electron2D.SceneFileTextSerializer.Deserialize(
            Electron2D.SceneFileTextSerializer.Serialize(modified));

        var reloadedPlayer = reloaded.Nodes.Single(node => node.Id == 2);
        Assert.True(reloadedPlayer.Properties["flip_h"].VariantValue.AsBool());
        Assert.Equal(Electron2D.SerializedResourceReferenceScope.External, reloadedPlayer.Properties["texture"].ReferenceScope);
        Assert.Equal(1, reloadedPlayer.Properties["texture"].ReferenceId);
        Assert.Equal(Electron2D.SerializedResourceReferenceScope.Internal, reloadedPlayer.Properties["stats"].ReferenceScope);
        Assert.Equal(1, reloadedPlayer.Properties["stats"].ReferenceId);
        Assert.Equal("res://textures/player.png", Assert.Single(reloaded.ExternalReferences).Path);

        var state = reloaded.Nodes.Single(node => node.Id == 1).Properties["state"];
        var alignment = state.DictionaryEntries.Single(entry => entry.Key.VariantValue.AsString() == "alignment").Value;
        var maybeLives = state.DictionaryEntries.Single(entry => entry.Key.VariantValue.AsString() == "maybe_lives").Value;

        Assert.Equal(Electron2D.SerializedPropertyValueKind.Dictionary, state.Kind);
        Assert.Equal("Center", alignment.EnumName);
        Assert.Equal(1L, alignment.EnumValue);
        Assert.True(maybeLives.NullableValue is not null);
        Assert.Equal(3L, maybeLives.NullableValue!.VariantValue.AsInt64());
    }

    [Fact]
    public void ResourceObjectSerializerRoundTripsCustomResourceValues()
    {
        var resource = new PlayerStatsResource
        {
            DisplayName = "Player",
            Alignment = Electron2D.HorizontalAlignment.Right,
            OptionalLives = 7,
            Tags = ["hero", "player"],
            Scores = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["coins"] = 12,
                ["gems"] = 2
            }
        };

        var document = Electron2D.ResourceObjectSerializer.Capture(resource, "res://data/player_stats.e2res");
        var parsed = Electron2D.SerializedResourceTextSerializer.Deserialize(
            Electron2D.SerializedResourceTextSerializer.Serialize(document));
        var restored = Assert.IsType<PlayerStatsResource>(Electron2D.ResourceObjectSerializer.Instantiate(parsed));

        Assert.Equal("Player", restored.DisplayName);
        Assert.Equal(Electron2D.HorizontalAlignment.Right, restored.Alignment);
        Assert.Equal(7, restored.OptionalLives);
        Assert.Equal(["hero", "player"], restored.Tags);
        Assert.Equal(12, restored.Scores["coins"]);
        Assert.Equal(2, restored.Scores["gems"]);

        restored.OptionalLives = null;
        var restoredDocument = Electron2D.ResourceObjectSerializer.Capture(restored, "res://data/player_stats.e2res");

        Assert.Null(restoredDocument.Properties["OptionalLives"].NullableValue);
    }

    private sealed class PlayerStatsResource : Electron2D.Resource
    {
        public string DisplayName { get; set; } = string.Empty;

        public Electron2D.HorizontalAlignment Alignment { get; set; }

        public int? OptionalLives { get; set; }

        public string[] Tags { get; set; } = [];

        public Dictionary<string, int> Scores { get; set; } = new(StringComparer.Ordinal);
    }
}
