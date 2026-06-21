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

public sealed class ShaderMaterialParameterTests
{
    [Fact]
    public void SupportedUniformsProduceSerializableSnapshot()
    {
        var shader = new Electron2D.Shader();
        shader.TakeOverPath("res://shaders/fill.e2shader");
        var material = new Electron2D.ShaderMaterial
        {
            Shader = shader
        };

        material.SetShaderParameter("enabled", true);
        material.SetShaderParameter("offset", new Electron2D.Vector2(12f, 24f));
        material.SetShaderParameter("threshold", 0.25d);
        material.SetShaderParameter("tint", new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f));
        material.SetShaderParameter("world", new Electron2D.Transform2D(1f, 0f, 0f, 1f, 4f, 8f));

        var snapshot = Electron2D.ShaderMaterialParametersSnapshot.FromMaterial(material);
        var text = Electron2D.ShaderMaterialParameterTextSerializer.Serialize(snapshot);

        Assert.Equal("res://shaders/fill.e2shader", snapshot.ShaderPath);
        Assert.Equal(
            new[] { "enabled", "offset", "threshold", "tint", "world" },
            snapshot.Parameters.Select(parameter => parameter.Name));
        Assert.Contains("\"format\": \"Electron2D.ShaderMaterialParameters\"", text, StringComparison.Ordinal);
        Assert.Contains("\"shader\": \"res://shaders/fill.e2shader\"", text, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"Variant\"", text, StringComparison.Ordinal);
        Assert.Contains("\"type\": \"Color\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TextureSamplerProducesSerializableSnapshot()
    {
        var texture = new Electron2D.RuntimeTexture2D(width: 64, height: 32, hasAlpha: true, hasMipmaps: true, mipmapCount: 3);
        texture.TakeOverPath("res://textures/albedo.png");
        var material = new Electron2D.ShaderMaterial();

        material.SetShaderParameter("albedo_texture", texture);

        var snapshot = Electron2D.ShaderMaterialParametersSnapshot.FromMaterial(material);
        var parameter = Assert.Single(snapshot.Parameters);
        var text = Electron2D.ShaderMaterialParameterTextSerializer.Serialize(snapshot);

        Assert.Equal("albedo_texture", parameter.Name);
        Assert.Equal(Electron2D.ShaderMaterialParameterKind.Texture2D, parameter.Kind);
        Assert.Equal("res://textures/albedo.png", parameter.TextureResourcePath);
        Assert.Equal(64, parameter.TextureWidth);
        Assert.Equal(32, parameter.TextureHeight);
        Assert.True(parameter.TextureHasAlpha);
        Assert.True(parameter.TextureHasMipmaps);
        Assert.Equal(3, parameter.TextureMipmapCount);
        Assert.Contains("\"kind\": \"Texture2D\"", text, StringComparison.Ordinal);
        Assert.Contains("\"resource_path\": \"res://textures/albedo.png\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedParameterValuesFailClosed()
    {
        var material = new Electron2D.ShaderMaterial();

        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("node", new Electron2D.Node());
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("callable", Electron2D.Callable.From(() => { }));
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("rid", default(Electron2D.Rid));
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("string", "not a shader uniform");
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("rect", new Electron2D.Rect2(1f, 2f, 3f, 4f));
        });
    }

    [Fact]
    public void CanvasBuiltInsAreReservedShaderParameterNames()
    {
        var material = new Electron2D.ShaderMaterial();

        Assert.True(Electron2D.CanvasShaderBuiltInRegistry.IsReserved("TIME"));
        Assert.True(Electron2D.CanvasShaderBuiltInRegistry.IsReserved("TEXTURE"));
        Assert.True(Electron2D.CanvasShaderBuiltInRegistry.IsReserved("texture_sdf"));
        Assert.False(Electron2D.CanvasShaderBuiltInRegistry.IsReserved("time"));
        Assert.False(Electron2D.CanvasShaderBuiltInRegistry.IsReserved("texture_sdf_custom"));

        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("TIME", 1.0);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("TEXTURE", new Electron2D.RuntimeTexture2D(1, 1, true));
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter("texture_sdf", 1.0);
        });
    }
}
