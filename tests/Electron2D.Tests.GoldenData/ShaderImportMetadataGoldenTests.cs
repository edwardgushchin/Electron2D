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
using System.Text;
using Xunit;

namespace Electron2D.Tests.GoldenData;

public sealed class ShaderImportMetadataGoldenTests
{
    [Fact]
    public void ShaderImportMetadataTextSerializerMatchesGoldenText()
    {
        var metadata = new Electron2D.ShaderImportMetadata(
            sourcePath: "res://shaders/water.e2shader",
            uid: 123456789L,
            requiresRuntimeCompilation: false,
            stages:
            [
                new Electron2D.ShaderImportCompiledStage(
                    Electron2D.CanvasShaderStage.Vertex,
                    Electron2D.CanvasShaderTargetPlatform.Ios,
                    "VSMain",
                    Encoding.UTF8.GetBytes("ios-bytecode"))
            ],
            diagnostics: []);

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.ShaderImportMetadata\",\n" +
            "  \"version\": 1,\n" +
            "  \"source\": \"res://shaders/water.e2shader\",\n" +
            "  \"uid\": \"uid://21i3v9\",\n" +
            "  \"requiresRuntimeCompilation\": false,\n" +
            "  \"stages\": [\n" +
            "    {\n" +
            "      \"stage\": \"Vertex\",\n" +
            "      \"target\": \"Ios\",\n" +
            "      \"entryPoint\": \"VSMain\",\n" +
            "      \"bytecode\": \"aW9zLWJ5dGVjb2Rl\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"diagnostics\": []\n" +
            "}";

        Assert.Equal(expected, Electron2D.ShaderImportMetadataTextSerializer.Serialize(metadata));
    }
}
