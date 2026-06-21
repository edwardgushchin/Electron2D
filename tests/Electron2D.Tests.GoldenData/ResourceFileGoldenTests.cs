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

namespace Electron2D.Tests.GoldenData;

public sealed class ResourceFileGoldenTests
{
    [Fact]
    public void ResourceFileTextSerializerMatchesGoldenText()
    {
        var document = new Electron2D.ResourceFileDocument(
            uid: 123456789L,
            type: "Electron2D.Resource",
            path: "res://characters/player.e2res",
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
                new Electron2D.ResourceFileInternalResource(
                    id: 1,
                    type: "Electron2D.Resource",
                    properties: new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
                    {
                        ["resource_name"] = "Stats"
                    })
            ],
            properties: new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
            {
                ["health"] = 100,
                ["display_name"] = "Player",
                ["spawn"] = new Electron2D.Vector2(12f, 24f)
            });

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.ResourceFile\",\n" +
            "  \"version\": 1,\n" +
            "  \"uid\": \"uid://21i3v9\",\n" +
            "  \"type\": \"Electron2D.Resource\",\n" +
            "  \"path\": \"res://characters/player.e2res\",\n" +
            "  \"external\": [\n" +
            "    {\n" +
            "      \"id\": 1,\n" +
            "      \"uid\": \"uid://gc0uy9\",\n" +
            "      \"path\": \"res://textures/player.png\",\n" +
            "      \"type\": \"Electron2D.Texture2D\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"internal\": [\n" +
            "    {\n" +
            "      \"id\": 1,\n" +
            "      \"type\": \"Electron2D.Resource\",\n" +
            "      \"properties\": {\n" +
            "        \"resource_name\": {\n" +
            "          \"type\": \"String\",\n" +
            "          \"value\": \"Stats\"\n" +
            "        }\n" +
            "      }\n" +
            "    }\n" +
            "  ],\n" +
            "  \"properties\": {\n" +
            "    \"display_name\": {\n" +
            "      \"type\": \"String\",\n" +
            "      \"value\": \"Player\"\n" +
            "    },\n" +
            "    \"health\": {\n" +
            "      \"type\": \"Int\",\n" +
            "      \"value\": 100\n" +
            "    },\n" +
            "    \"spawn\": {\n" +
            "      \"type\": \"Vector2\",\n" +
            "      \"value\": {\n" +
            "        \"x\": 12,\n" +
            "        \"y\": 24\n" +
            "      }\n" +
            "    }\n" +
            "  }\n" +
            "}";

        Assert.Equal(expected, Electron2D.ResourceFileTextSerializer.Serialize(document));
    }
}
