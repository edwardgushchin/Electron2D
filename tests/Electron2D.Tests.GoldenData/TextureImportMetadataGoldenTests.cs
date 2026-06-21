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

public sealed class TextureImportMetadataGoldenTests
{
    [Fact]
    public void TextureImportMetadataTextSerializerMatchesGoldenText()
    {
        var metadata = new Electron2D.TextureImportMetadata(
            sourcePath: "res://textures/player.png",
            uid: 123456789L,
            format: Electron2D.TextureImageFormat.Png,
            width: 32,
            height: 16,
            hasAlpha: true,
            hasMipmaps: true,
            mipmapCount: 6,
            sampling: new Electron2D.TextureSamplingOptions(
                Electron2D.TextureFilterMode.Nearest,
                Electron2D.TextureRepeatMode.Mirror),
            atlasRegions:
            [
                new Electron2D.TextureAtlasRegionMetadata(
                    "idle",
                    new Electron2D.Rect2(2f, 4f, 16f, 8f),
                    new Electron2D.Rect2(1f, 1f, 2f, 2f),
                    filterClip: true)
            ],
            platformVariants:
            [
                new Electron2D.TexturePlatformVariant("android", "etc2", 80),
                new Electron2D.TexturePlatformVariant("desktop", "rgba8", 100)
            ]);

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.TextureImportMetadata\",\n" +
            "  \"version\": 1,\n" +
            "  \"source\": \"res://textures/player.png\",\n" +
            "  \"uid\": \"uid://21i3v9\",\n" +
            "  \"imageFormat\": \"Png\",\n" +
            "  \"width\": 32,\n" +
            "  \"height\": 16,\n" +
            "  \"hasAlpha\": true,\n" +
            "  \"hasMipmaps\": true,\n" +
            "  \"mipmapCount\": 6,\n" +
            "  \"sampling\": {\n" +
            "    \"filter\": \"Nearest\",\n" +
            "    \"repeat\": \"Mirror\"\n" +
            "  },\n" +
            "  \"atlas\": [\n" +
            "    {\n" +
            "      \"name\": \"idle\",\n" +
            "      \"region\": {\n" +
            "        \"x\": 2,\n" +
            "        \"y\": 4,\n" +
            "        \"width\": 16,\n" +
            "        \"height\": 8\n" +
            "      },\n" +
            "      \"margin\": {\n" +
            "        \"x\": 1,\n" +
            "        \"y\": 1,\n" +
            "        \"width\": 2,\n" +
            "        \"height\": 2\n" +
            "      },\n" +
            "      \"filterClip\": true\n" +
            "    }\n" +
            "  ],\n" +
            "  \"platforms\": [\n" +
            "    {\n" +
            "      \"name\": \"android\",\n" +
            "      \"format\": \"etc2\",\n" +
            "      \"quality\": 80\n" +
            "    },\n" +
            "    {\n" +
            "      \"name\": \"desktop\",\n" +
            "      \"format\": \"rgba8\",\n" +
            "      \"quality\": 100\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        Assert.Equal(expected, Electron2D.TextureImportMetadataTextSerializer.Serialize(metadata));
    }
}
