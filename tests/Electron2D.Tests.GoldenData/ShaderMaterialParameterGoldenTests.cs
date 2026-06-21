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

public sealed class ShaderMaterialParameterGoldenTests
{
    [Fact]
    public void ShaderMaterialParameterTextSerializerMatchesGoldenText()
    {
        var shader = new Electron2D.Shader();
        shader.TakeOverPath("res://shaders/fill.e2shader");
        var texture = new Electron2D.RuntimeTexture2D(width: 64, height: 32, hasAlpha: true);
        texture.TakeOverPath("res://textures/albedo.png");

        var material = new Electron2D.ShaderMaterial
        {
            Shader = shader
        };
        material.SetShaderParameter("tint", new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f));
        material.SetShaderParameter("albedo_texture", texture);

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.ShaderMaterialParameters\",\n" +
            "  \"version\": 1,\n" +
            "  \"shader\": \"res://shaders/fill.e2shader\",\n" +
            "  \"parameters\": {\n" +
            "    \"albedo_texture\": {\n" +
            "      \"kind\": \"Texture2D\",\n" +
            "      \"type\": \"Electron2D.RuntimeTexture2D\",\n" +
            "      \"resource_path\": \"res://textures/albedo.png\",\n" +
            "      \"resource_scene_unique_id\": \"\",\n" +
            "      \"width\": 64,\n" +
            "      \"height\": 32,\n" +
            "      \"has_alpha\": true,\n" +
            "      \"has_mipmaps\": false,\n" +
            "      \"mipmap_count\": 0\n" +
            "    },\n" +
            "    \"tint\": {\n" +
            "      \"kind\": \"Variant\",\n" +
            "      \"value\": {\n" +
            "        \"type\": \"Color\",\n" +
            "        \"value\": {\n" +
            "          \"r\": 0.25,\n" +
            "          \"g\": 0.5,\n" +
            "          \"b\": 0.75,\n" +
            "          \"a\": 1\n" +
            "        }\n" +
            "      }\n" +
            "    }\n" +
            "  }\n" +
            "}";

        var snapshot = Electron2D.ShaderMaterialParametersSnapshot.FromMaterial(material);

        Assert.Equal(expected, Electron2D.ShaderMaterialParameterTextSerializer.Serialize(snapshot));
    }
}
