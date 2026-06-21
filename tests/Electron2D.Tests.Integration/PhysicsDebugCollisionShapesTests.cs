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

[Collection(PhysicsServer2DCollection.Name)]
public sealed class PhysicsDebugCollisionShapesTests
{
    [Fact]
    public void DebugCollisionsHintControlsCollisionShapeSnapshot()
    {
        var tree = new Electron2D.SceneTree();
        var body = new Electron2D.StaticBody2D
        {
            Name = "Wall",
            Position = new Electron2D.Vector2(10f, 20f)
        };
        var shape = new Electron2D.CollisionShape2D
        {
            Name = "Shape",
            DebugColor = new Electron2D.Color(1f, 0.2f, 0.1f, 0.9f),
            Shape = new Electron2D.RectangleShape2D { Size = new Electron2D.Vector2(8f, 6f) }
        };
        try
        {
            body.AddChild(shape);
            tree.Root.AddChild(body);

            Assert.Empty(tree.CapturePhysicsDebugShapes());

            tree.DebugCollisionsHint = true;
            var debugShape = Assert.Single(tree.CapturePhysicsDebugShapes());

            Assert.Same(body, debugShape.Owner);
            Assert.Same(shape, debugShape.ShapeNode);
            Assert.Same(shape.Shape, debugShape.Shape);
            Assert.Equal(0, debugShape.ShapeIndex);
            Assert.False(debugShape.Disabled);
            Assert.Equal(shape.DebugColor, debugShape.Color);
            Assert.True(debugShape.Bounds.IsEqualApprox(new Electron2D.Rect2(6f, 17f, 8f, 6f)));
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void DebugCollisionSnapshotIncludesDisabledShapesAndDefaultColor()
    {
        var tree = new Electron2D.SceneTree
        {
            DebugCollisionsHint = true
        };
        var body = new Electron2D.StaticBody2D();
        var disabledShape = new Electron2D.CollisionShape2D
        {
            Disabled = true,
            DebugColor = Electron2D.Color.Transparent,
            Shape = new Electron2D.CircleShape2D { Radius = 4f }
        };
        try
        {
            body.AddChild(disabledShape);
            tree.Root.AddChild(body);

            var debugShape = Assert.Single(tree.CapturePhysicsDebugShapes());

            Assert.True(debugShape.Disabled);
            Assert.Equal(Electron2D.PhysicsDebugShape2D.DefaultColor, debugShape.Color);
            Assert.True(debugShape.Bounds.IsEqualApprox(new Electron2D.Rect2(-4f, -4f, 8f, 8f)));
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void DebugCollisionSnapshotSkipsEmptyShapesAndDoesNotExposeBackendHandles()
    {
        var tree = new Electron2D.SceneTree
        {
            DebugCollisionsHint = true
        };
        var body = new Electron2D.StaticBody2D();
        try
        {
            body.AddChild(new Electron2D.CollisionShape2D());
            tree.Root.AddChild(body);

            Assert.Empty(tree.CapturePhysicsDebugShapes());
            Assert.DoesNotContain(
                typeof(Electron2D.Rid),
                typeof(Electron2D.PhysicsDebugShape2D)
                    .GetProperties()
                    .Select(property => property.PropertyType));
        }
        finally
        {
            tree.Root.Free();
        }
    }
}
