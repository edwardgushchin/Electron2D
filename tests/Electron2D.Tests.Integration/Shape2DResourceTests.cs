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
public sealed class Shape2DResourceTests
{
    [Fact]
    public void ConcreteShapesCreateExpectedPhysicsServerRids()
    {
        ResetBackend();

        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Rectangle,
            Electron2D.PhysicsServer2D.ShapeGetType(new Electron2D.RectangleShape2D().GetRid()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Circle,
            Electron2D.PhysicsServer2D.ShapeGetType(new Electron2D.CircleShape2D().GetRid()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Capsule,
            Electron2D.PhysicsServer2D.ShapeGetType(new Electron2D.CapsuleShape2D().GetRid()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Segment,
            Electron2D.PhysicsServer2D.ShapeGetType(new Electron2D.SegmentShape2D().GetRid()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.ConvexPolygon,
            Electron2D.PhysicsServer2D.ShapeGetType(new Electron2D.ConvexPolygonShape2D().GetRid()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.ConcavePolygon,
            Electron2D.PhysicsServer2D.ShapeGetType(new Electron2D.ConcavePolygonShape2D().GetRid()));
    }

    [Fact]
    public void ShapePropertiesRejectInvalidGeometryWithClearMessages()
    {
        var rectangle = new Electron2D.RectangleShape2D();
        var circle = new Electron2D.CircleShape2D();
        var capsule = new Electron2D.CapsuleShape2D();
        var segment = new Electron2D.SegmentShape2D();
        var convex = new Electron2D.ConvexPolygonShape2D();
        var concave = new Electron2D.ConcavePolygonShape2D();

        Assert.Contains("RectangleShape2D.Size", Assert.Throws<ArgumentOutOfRangeException>(
            () => { rectangle.Size = new Electron2D.Vector2(0f, 2f); }).Message);
        Assert.Contains("CircleShape2D.Radius", Assert.Throws<ArgumentOutOfRangeException>(
            () => { circle.Radius = 0f; }).Message);
        Assert.Contains("CapsuleShape2D.Height", Assert.Throws<ArgumentOutOfRangeException>(
            () => { capsule.Height = 1f; }).Message);
        Assert.Contains("SegmentShape2D", Assert.Throws<ArgumentException>(
            () => { segment.B = segment.A; }).Message);
        Assert.Contains("ConvexPolygonShape2D.Points", Assert.Throws<ArgumentException>(
            () =>
            {
                convex.Points =
            [
                new Electron2D.Vector2(0f, 0f),
                new Electron2D.Vector2(1f, 0f),
                new Electron2D.Vector2(2f, 0f)
            ];
            }).Message);
        Assert.Contains("ConcavePolygonShape2D.Segments", Assert.Throws<ArgumentException>(
            () => { concave.Segments = [Electron2D.Vector2.Zero]; }).Message);
    }

    [Fact]
    public void ConcavePolygonShape2DIsAllowedOnlyUnderStaticBody2D()
    {
        var tree = new Electron2D.SceneTree();
        var staticBody = new Electron2D.StaticBody2D();
        var rigidBody = new Electron2D.RigidBody2D();
        var validShape = new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.ConcavePolygonShape2D()
        };
        var invalidShape = new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.ConcavePolygonShape2D()
        };

        staticBody.AddChild(validShape);
        rigidBody.AddChild(invalidShape);

        tree.Root.AddChild(staticBody);
        var exception = Assert.Throws<InvalidOperationException>(() => tree.Root.AddChild(rigidBody));

        Assert.Contains(nameof(Electron2D.ConcavePolygonShape2D), exception.Message);
        Assert.Contains(nameof(Electron2D.StaticBody2D), exception.Message);
    }

    [Fact]
    public void ShapeResourcesRoundTripThroughAotSafeResourceSerialization()
    {
        AssertShapeRoundTrip(
            new Electron2D.RectangleShape2D { Size = new Electron2D.Vector2(12f, 8f) },
            restored => Assert.Equal(new Electron2D.Vector2(12f, 8f), ((Electron2D.RectangleShape2D)restored).Size));
        AssertShapeRoundTrip(
            new Electron2D.CircleShape2D { Radius = 7f },
            restored => Assert.Equal(7f, ((Electron2D.CircleShape2D)restored).Radius));
        AssertShapeRoundTrip(
            new Electron2D.CapsuleShape2D { Radius = 4f, Height = 12f },
            restored =>
            {
                var capsule = (Electron2D.CapsuleShape2D)restored;
                Assert.Equal(4f, capsule.Radius);
                Assert.Equal(12f, capsule.Height);
            });
        AssertShapeRoundTrip(
            new Electron2D.SegmentShape2D { A = new Electron2D.Vector2(-1f, 2f), B = new Electron2D.Vector2(3f, 4f) },
            restored =>
            {
                var segment = (Electron2D.SegmentShape2D)restored;
                Assert.Equal(new Electron2D.Vector2(-1f, 2f), segment.A);
                Assert.Equal(new Electron2D.Vector2(3f, 4f), segment.B);
            });
        AssertShapeRoundTrip(
            new Electron2D.ConvexPolygonShape2D
            {
                Points =
                [
                    new Electron2D.Vector2(0f, 0f),
                    new Electron2D.Vector2(4f, 0f),
                    new Electron2D.Vector2(2f, 3f)
                ]
            },
            restored => Assert.Equal(3, ((Electron2D.ConvexPolygonShape2D)restored).Points.Length));
        AssertShapeRoundTrip(
            new Electron2D.ConcavePolygonShape2D
            {
                Segments =
                [
                    new Electron2D.Vector2(0f, 0f),
                    new Electron2D.Vector2(1f, 0f),
                    new Electron2D.Vector2(1f, 0f),
                    new Electron2D.Vector2(1f, 1f)
                ]
            },
            restored => Assert.Equal(4, ((Electron2D.ConcavePolygonShape2D)restored).Segments.Length));
    }

    private static void AssertShapeRoundTrip(Electron2D.Shape2D shape, Action<Electron2D.Shape2D> assertRestored)
    {
        var document = Electron2D.ResourceObjectSerializer.Capture(shape, $"res://shapes/{shape.GetType().Name}.e2res");
        var serialized = Electron2D.SerializedResourceTextSerializer.Serialize(document);
        var parsed = Electron2D.SerializedResourceTextSerializer.Deserialize(serialized);
        var restored = Assert.IsAssignableFrom<Electron2D.Shape2D>(Electron2D.ResourceObjectSerializer.Instantiate(parsed));

        Assert.IsType(shape.GetType(), restored);
        assertRestored(restored);
    }

    private static void ResetBackend()
    {
        Electron2D.PhysicsServer2D.SetBackend(new Electron2D.ManagedPhysicsServer2DBackend());
    }
}
