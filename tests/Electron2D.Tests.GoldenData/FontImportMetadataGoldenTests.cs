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

public sealed class FontImportMetadataGoldenTests
{
    [Fact]
    public void FontImportMetadataTextSerializerMatchesGoldenText()
    {
        var metadata = new Electron2D.FontImportMetadata(
            sourcePath: "res://fonts/main.ttf",
            uid: 123456789L,
            format: Electron2D.FontSourceFormat.Ttf,
            familyName: "Electron Sans",
            styleName: "Regular",
            fullName: "Electron Sans Regular",
            postScriptName: "ElectronSans-Regular",
            fallbackFontPaths: ["res://fonts/fallback.ttf"],
            rasterization: new Electron2D.FontRasterizationSettings(
                Electron2D.FontRasterizationMode.Sdf,
                baseSize: 48,
                sdfSpread: 8));

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.FontImportMetadata\",\n" +
            "  \"version\": 1,\n" +
            "  \"source\": \"res://fonts/main.ttf\",\n" +
            "  \"uid\": \"uid://21i3v9\",\n" +
            "  \"fontFormat\": \"Ttf\",\n" +
            "  \"familyName\": \"Electron Sans\",\n" +
            "  \"styleName\": \"Regular\",\n" +
            "  \"fullName\": \"Electron Sans Regular\",\n" +
            "  \"postScriptName\": \"ElectronSans-Regular\",\n" +
            "  \"fallbacks\": [\n" +
            "    \"res://fonts/fallback.ttf\"\n" +
            "  ],\n" +
            "  \"rasterization\": {\n" +
            "    \"mode\": \"Sdf\",\n" +
            "    \"baseSize\": 48,\n" +
            "    \"sdfSpread\": 8\n" +
            "  }\n" +
            "}";

        Assert.Equal(expected, Electron2D.FontImportMetadataTextSerializer.Serialize(metadata));
    }
}
