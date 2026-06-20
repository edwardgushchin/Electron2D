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
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class CleanRuntimeBaselineTests
{
    [Fact]
    public void RuntimeAssemblyExportsOnlyCurrentGodotLikeBaselineTypes()
    {
        var assembly = Assembly.Load("Electron2D");
        var publicTypeNames = assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Electron2D.AtlasTexture",
                "Electron2D.Callable",
                "Electron2D.Collections.Array",
                "Electron2D.Collections.Dictionary",
                "Electron2D.Color",
                "Electron2D.ConnectFlags",
                "Electron2D.Error",
                "Electron2D.InputEvent",
                "Electron2D.Mathf",
                "Electron2D.Node",
                "Electron2D.NodePath",
                "Electron2D.Object",
                "Electron2D.PackedScene",
                "Electron2D.RandomNumberGenerator",
                "Electron2D.Rect2",
                "Electron2D.Rect2I",
                "Electron2D.RefCounted",
                "Electron2D.RenderingServer",
                "Electron2D.RenderingServer+RenderingFeature",
                "Electron2D.RenderingServer+RenderingProfile",
                "Electron2D.Resource",
                "Electron2D.Rid",
                "Electron2D.SceneTree",
                "Electron2D.StringName",
                "Electron2D.Texture2D",
                "Electron2D.Transform2D",
                "Electron2D.Variant",
                "Electron2D.Variant+Type",
                "Electron2D.Vector2",
                "Electron2D.Vector2I"
            },
            publicTypeNames);

        Assert.Null(assembly.GetType("Electron2D.IComponent"));
        Assert.Null(assembly.GetType("Electron2D.SpriteRenderer"));
        Assert.Null(assembly.GetType("Electron2D.SpriteAnimator"));
        Assert.Null(assembly.GetType("Electron2D.Rigidbody"));
        Assert.Null(assembly.GetType("Electron2D.Collider"));
    }
}
