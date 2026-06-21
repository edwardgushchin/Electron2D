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

public sealed class ResourceFileSerializationTests
{
    [Fact]
    public void ResourceFileTextSerializerRoundTripsExternalAndInternalReferences()
    {
        var document = new Electron2D.ResourceFileDocument(
            uid: 123456789L,
            type: "Electron2D.Resource",
            path: "res://characters/player.e2res",
            externalReferences:
            [
                new Electron2D.ResourceFileExternalReference(
                    id: 2,
                    uid: 987654321L,
                    path: "res://textures/player.png",
                    type: "Electron2D.Texture2D"),
                new Electron2D.ResourceFileExternalReference(
                    id: 1,
                    uid: 222333444L,
                    path: "res://audio/jump.wav",
                    type: "Electron2D.AudioStream")
            ],
            internalResources:
            [
                new Electron2D.ResourceFileInternalResource(
                    id: 3,
                    type: "Electron2D.Resource",
                    properties: new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
                    {
                        ["resource_name"] = "Stats",
                        ["health"] = 100
                    })
            ],
            properties: new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
            {
                ["spawn"] = new Electron2D.Vector2(12f, 24f),
                ["display_name"] = "Player"
            });

        var serialized = Electron2D.ResourceFileTextSerializer.Serialize(document);
        var parsed = Electron2D.ResourceFileTextSerializer.Deserialize(serialized);

        Assert.Equal(serialized, Electron2D.ResourceFileTextSerializer.Serialize(parsed));
        Assert.Equal(123456789L, parsed.Uid);
        Assert.Equal("Electron2D.Resource", parsed.Type);
        Assert.Equal("res://characters/player.e2res", parsed.Path);
        Assert.Equal([1, 2], parsed.ExternalReferences.Select(reference => reference.Id).ToArray());
        Assert.Equal("res://audio/jump.wav", parsed.ExternalReferences[0].Path);
        Assert.Equal("uid://3oddl0", parsed.ExternalReferences[0].UidText);
        Assert.Single(parsed.InternalResources);
        Assert.Equal(3, parsed.InternalResources[0].Id);
        Assert.Equal("Stats", parsed.InternalResources[0].Properties["resource_name"].AsString());
        Assert.Equal(100L, parsed.InternalResources[0].Properties["health"].AsInt64());
        Assert.Equal("Player", parsed.Properties["display_name"].AsString());
        Assert.Equal(new Electron2D.Vector2(12f, 24f), parsed.Properties["spawn"].AsVector2());
    }

    [Fact]
    public void ResourceFileTextSerializerRejectsMissingUidBeforeLosingData()
    {
        var exception = Assert.Throws<FormatException>(
            () => Electron2D.ResourceFileTextSerializer.Deserialize("{\"format\":\"Electron2D.ResourceFile\",\"version\":1}"));

        Assert.Contains("uid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
