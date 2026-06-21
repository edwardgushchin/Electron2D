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

namespace Electron2D.Tests.Unit;

public sealed class ShaderMaterialPublicApiTests
{
    [Fact]
    public void ShaderMaterialInheritsMaterialAndStoresShader()
    {
        var shader = new Electron2D.Shader
        {
            Code = "shader_type canvas_item;"
        };
        var material = new Electron2D.ShaderMaterial
        {
            Shader = shader
        };

        Assert.True(typeof(Electron2D.Resource).IsAssignableFrom(typeof(Electron2D.Material)));
        Assert.True(typeof(Electron2D.Material).IsAssignableFrom(typeof(Electron2D.ShaderMaterial)));
        Assert.Same(shader, material.Shader);
    }

    [Fact]
    public void MaterialStoresNextPassAndClampsRenderPriorityRange()
    {
        var material = new Electron2D.ShaderMaterial();
        var nextPass = new Electron2D.ShaderMaterial();

        material.NextPass = nextPass;
        material.RenderPriority = 127;

        Assert.Same(nextPass, material.NextPass);
        Assert.Equal(127, material.RenderPriority);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            material.RenderPriority = 128;
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            material.RenderPriority = -129;
        });

        material.RenderPriority = -128;

        Assert.Equal(-128, material.RenderPriority);
    }

    [Fact]
    public void ShaderMaterialStoresParametersCaseSensitivelyAndSupportsNilReset()
    {
        var material = new Electron2D.ShaderMaterial();

        material.SetShaderParameter("Tint", new Electron2D.Color(1f, 0f, 0f, 1f));
        material.SetShaderParameter("tint", new Electron2D.Color(0f, 1f, 0f, 1f));

        Assert.Equal(new Electron2D.Color(1f, 0f, 0f, 1f), material.GetShaderParameter("Tint").AsColor());
        Assert.Equal(new Electron2D.Color(0f, 1f, 0f, 1f), material.GetShaderParameter("tint").AsColor());
        Assert.True(material.GetShaderParameter("missing").IsNil());

        material.SetShaderParameter("tint", default);

        Assert.True(material.GetShaderParameter("tint").IsNil());
        Assert.Equal(new Electron2D.Color(1f, 0f, 0f, 1f), material.GetShaderParameter("Tint").AsColor());
    }

    [Fact]
    public void ShaderMaterialRejectsEmptyParameterName()
    {
        var material = new Electron2D.ShaderMaterial();

        Assert.Throws<ArgumentException>(() =>
        {
            material.SetShaderParameter(default, 1);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            material.GetShaderParameter(default);
        });
    }
}
