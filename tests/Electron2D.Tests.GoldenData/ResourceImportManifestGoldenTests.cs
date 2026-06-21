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

public sealed class ResourceImportManifestGoldenTests
{
    [Fact]
    public void ResourceImportManifestTextSerializerMatchesGoldenText()
    {
        var manifest = new Electron2D.ResourceImportManifest(
        [
            new Electron2D.ResourceImportManifestEntry(
                "res://characters/player.e2res",
                123456789L,
                "Electron2D.Resource",
                "Electron2D.ResourceFile",
                "sha256:source",
                ["resources/21i3v9/resource.e2res"],
                [
                    new Electron2D.ResourceImportManifestDependency(
                        "res://textures/player.png",
                        "sha256:dependency")
                ])
        ]);

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.ImportCache\",\n" +
            "  \"version\": 1,\n" +
            "  \"entries\": [\n" +
            "    {\n" +
            "      \"source\": \"res://characters/player.e2res\",\n" +
            "      \"uid\": \"uid://21i3v9\",\n" +
            "      \"type\": \"Electron2D.Resource\",\n" +
            "      \"importer\": \"Electron2D.ResourceFile\",\n" +
            "      \"sourceHash\": \"sha256:source\",\n" +
            "      \"cacheFiles\": [\n" +
            "        \"resources/21i3v9/resource.e2res\"\n" +
            "      ],\n" +
            "      \"dependencies\": [\n" +
            "        {\n" +
            "          \"path\": \"res://textures/player.png\",\n" +
            "          \"hash\": \"sha256:dependency\"\n" +
            "        }\n" +
            "      ]\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        Assert.Equal(expected, Electron2D.ResourceImportManifestTextSerializer.Serialize(manifest));
    }
}
