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

using VariantArray = Electron2D.Collections.Array;
using VariantDictionary = Electron2D.Collections.Dictionary;

namespace Electron2D.Tests.Integration;

[Collection(PhysicsServer2DCollection.Name)]
public sealed class PhysicsDirectSpaceState2DTests
{
    [Fact]
    public void RayCast2DHitsNearestBodyAndHonorsMaskAndExcludeParent()
    {
        var tree = new Electron2D.SceneTree();
        var body = CreateBody("Body", new Electron2D.Vector2(40f, 0f), collisionLayer: 0b0010u);
        var ray = new Electron2D.RayCast2D
        {
            Name = "Ray",
            TargetPosition = new Electron2D.Vector2(100f, 0f),
            CollisionMask = 0b0010u,
            CollideWithBodies = true,
            CollideWithAreas = false,
            HitFromInside = false
        };

        tree.Root.AddChild(ray);
        tree.Root.AddChild(body);

        ray.ForceRaycastUpdate();

        Assert.True(ray.IsColliding());
        Assert.Same(body, ray.GetCollider());
        Assert.Equal(body.GetRid(), ray.GetColliderRid());
        Assert.Equal(0, ray.GetColliderShape());
        AssertVectorEqual(new Electron2D.Vector2(30f, 0f), ray.GetCollisionPoint());
        AssertVectorEqual(new Electron2D.Vector2(-1f, 0f), ray.GetCollisionNormal());

        ray.CollisionMask = 0b0100u;
        ray.ForceRaycastUpdate();

        Assert.False(ray.IsColliding());
        Assert.Null(ray.GetCollider());

        ray.CollisionMask = 0b0010u;
        ray.Reparent(body);
        ray.ExcludeParent = true;
        ray.ForceRaycastUpdate();

        Assert.False(ray.IsColliding());
        Assert.Null(ray.GetCollider());
    }

    [Fact]
    public void DirectSpaceStateRayPointAndShapeQueriesReturnFilteredResultsAndNoHit()
    {
        var tree = new Electron2D.SceneTree();
        var sensor = CreateArea("Sensor", new Electron2D.Vector2(0f, 0f), collisionLayer: 0b0001u);
        var body = CreateBody("Body", new Electron2D.Vector2(50f, 0f), collisionLayer: 0b0010u);

        tree.Root.AddChild(sensor);
        tree.Root.AddChild(body);
        var state = sensor.GetWorld2D().DirectSpaceState;

        var rayHit = state.IntersectRay(new Electron2D.PhysicsRayQueryParameters2D
        {
            From = new Electron2D.Vector2(-20f, 0f),
            To = new Electron2D.Vector2(80f, 0f),
            CollisionMask = 0b0010u,
            CollideWithBodies = true,
            CollideWithAreas = true,
            Exclude = [sensor.GetRid()]
        });

        Assert.Same(body, GetObject(rayHit, "collider"));
        Assert.Equal(body.GetRid(), GetRid(rayHit, "rid"));
        AssertVectorEqual(new Electron2D.Vector2(40f, 0f), GetVector2(rayHit, "position"));

        var pointResults = state.IntersectPoint(new Electron2D.PhysicsPointQueryParameters2D
        {
            Position = new Electron2D.Vector2(0f, 0f),
            CollisionMask = 0b0001u,
            CollideWithBodies = false,
            CollideWithAreas = true
        });

        Assert.Single(pointResults);
        Assert.Same(sensor, GetObject(GetDictionary(pointResults, 0), "collider"));

        var shapeResults = state.IntersectShape(new Electron2D.PhysicsShapeQueryParameters2D
        {
            Shape = new Electron2D.RectangleShape2D { Size = new Electron2D.Vector2(20f, 20f) },
            Transform = new Electron2D.Transform2D(0f, new Electron2D.Vector2(50f, 0f)),
            CollisionMask = 0b0010u,
            CollideWithBodies = true,
            CollideWithAreas = false
        });

        Assert.Single(shapeResults);
        Assert.Same(body, GetObject(GetDictionary(shapeResults, 0), "collider"));

        Assert.Empty(state.IntersectPoint(new Electron2D.PhysicsPointQueryParameters2D
        {
            Position = new Electron2D.Vector2(300f, 300f),
            CollisionMask = uint.MaxValue,
            CollideWithBodies = true,
            CollideWithAreas = true
        }));
        Assert.Empty(state.IntersectShape(new Electron2D.PhysicsShapeQueryParameters2D
        {
            CollisionMask = uint.MaxValue,
            CollideWithBodies = true,
            CollideWithAreas = true
        }));
        Assert.Empty(state.IntersectPoint(new Electron2D.PhysicsPointQueryParameters2D(), maxResults: 0));
        Assert.Contains("maxResults", Assert.Throws<ArgumentOutOfRangeException>(
            () => state.IntersectPoint(new Electron2D.PhysicsPointQueryParameters2D(), maxResults: -1)).Message);
    }

    private static Electron2D.StaticBody2D CreateBody(string name, Electron2D.Vector2 position, uint collisionLayer)
    {
        var body = new Electron2D.StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = collisionLayer
        };
        body.AddChild(CreateShape());
        return body;
    }

    private static Electron2D.Area2D CreateArea(string name, Electron2D.Vector2 position, uint collisionLayer)
    {
        var area = new Electron2D.Area2D
        {
            Name = name,
            Position = position,
            CollisionLayer = collisionLayer
        };
        area.AddChild(CreateShape());
        return area;
    }

    private static Electron2D.CollisionShape2D CreateShape()
    {
        return new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.RectangleShape2D { Size = new Electron2D.Vector2(20f, 20f) }
        };
    }

    private static VariantDictionary GetDictionary(VariantArray array, int index)
    {
        return Assert.IsType<VariantDictionary>(array.ToArray()[index].Obj);
    }

    private static Electron2D.ElectronObject? GetObject(VariantDictionary dictionary, string key)
    {
        return Assert.IsAssignableFrom<Electron2D.ElectronObject>(dictionary[Electron2D.Variant.CreateFrom(key)].Obj);
    }

    private static Electron2D.Rid GetRid(VariantDictionary dictionary, string key)
    {
        return Assert.IsType<Electron2D.Rid>(dictionary[Electron2D.Variant.CreateFrom(key)].Obj);
    }

    private static Electron2D.Vector2 GetVector2(VariantDictionary dictionary, string key)
    {
        return Assert.IsType<Electron2D.Vector2>(dictionary[Electron2D.Variant.CreateFrom(key)].Obj);
    }

    private static void AssertVectorEqual(Electron2D.Vector2 expected, Electron2D.Vector2 actual)
    {
        Assert.True(actual.IsEqualApprox(expected), $"Expected {expected}, actual {actual}.");
    }
}
