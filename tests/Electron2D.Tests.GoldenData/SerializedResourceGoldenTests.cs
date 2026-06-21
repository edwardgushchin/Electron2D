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

public sealed class SerializedResourceGoldenTests
{
    [Fact]
    public void SerializedResourceTextSerializerMatchesGoldenText()
    {
        var document = new Electron2D.SerializedResourceDocument(
            uid: 123456789L,
            type: "Electron2D.Tests.GoldenData.GoldenResource",
            path: "res://data/stats.e2res",
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
                    type: "Electron2D.Resource",
                    properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["name"] = Electron2D.SerializedPropertyValue.FromVariant("Stats")
                    })
            ],
            properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
            {
                ["alignment"] = Electron2D.SerializedPropertyValue.FromEnum(Electron2D.HorizontalAlignment.Center),
                ["maybe_lives"] = Electron2D.SerializedPropertyValue.FromNullable(typeof(int), null),
                ["numbers"] = Electron2D.SerializedPropertyValue.FromArray(
                [
                    Electron2D.SerializedPropertyValue.FromVariant(1),
                    Electron2D.SerializedPropertyValue.FromVariant(2)
                ]),
                ["scores"] = Electron2D.SerializedPropertyValue.FromDictionary(
                [
                    new Electron2D.SerializedPropertyDictionaryEntry(
                        Electron2D.SerializedPropertyValue.FromVariant("coins"),
                        Electron2D.SerializedPropertyValue.FromVariant(12))
                ]),
                ["texture"] = Electron2D.SerializedPropertyValue.ExternalResource(1)
            });

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.SerializedResource\",\n" +
            "  \"version\": 1,\n" +
            "  \"uid\": \"uid://21i3v9\",\n" +
            "  \"type\": \"Electron2D.Tests.GoldenData.GoldenResource\",\n" +
            "  \"path\": \"res://data/stats.e2res\",\n" +
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
            "        \"name\": {\n" +
            "          \"kind\": \"Variant\",\n" +
            "          \"value\": {\n" +
            "            \"type\": \"String\",\n" +
            "            \"value\": \"Stats\"\n" +
            "          }\n" +
            "        }\n" +
            "      }\n" +
            "    }\n" +
            "  ],\n" +
            "  \"properties\": {\n" +
            "    \"alignment\": {\n" +
            "      \"kind\": \"Enum\",\n" +
            "      \"type\": \"Electron2D.HorizontalAlignment\",\n" +
            "      \"name\": \"Center\",\n" +
            "      \"value\": 1\n" +
            "    },\n" +
            "    \"maybe_lives\": {\n" +
            "      \"kind\": \"Nullable\",\n" +
            "      \"type\": \"System.Int32\",\n" +
            "      \"value\": null\n" +
            "    },\n" +
            "    \"numbers\": {\n" +
            "      \"kind\": \"Array\",\n" +
            "      \"items\": [\n" +
            "        {\n" +
            "          \"kind\": \"Variant\",\n" +
            "          \"value\": {\n" +
            "            \"type\": \"Int\",\n" +
            "            \"value\": 1\n" +
            "          }\n" +
            "        },\n" +
            "        {\n" +
            "          \"kind\": \"Variant\",\n" +
            "          \"value\": {\n" +
            "            \"type\": \"Int\",\n" +
            "            \"value\": 2\n" +
            "          }\n" +
            "        }\n" +
            "      ]\n" +
            "    },\n" +
            "    \"scores\": {\n" +
            "      \"kind\": \"Dictionary\",\n" +
            "      \"entries\": [\n" +
            "        {\n" +
            "          \"key\": {\n" +
            "            \"kind\": \"Variant\",\n" +
            "            \"value\": {\n" +
            "              \"type\": \"String\",\n" +
            "              \"value\": \"coins\"\n" +
            "            }\n" +
            "          },\n" +
            "          \"value\": {\n" +
            "            \"kind\": \"Variant\",\n" +
            "            \"value\": {\n" +
            "              \"type\": \"Int\",\n" +
            "              \"value\": 12\n" +
            "            }\n" +
            "          }\n" +
            "        }\n" +
            "      ]\n" +
            "    },\n" +
            "    \"texture\": {\n" +
            "      \"kind\": \"Resource\",\n" +
            "      \"scope\": \"External\",\n" +
            "      \"id\": 1\n" +
            "    }\n" +
            "  }\n" +
            "}";

        Assert.Equal(expected, Electron2D.SerializedResourceTextSerializer.Serialize(document));
    }
}
