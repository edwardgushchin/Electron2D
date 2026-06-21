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
namespace Electron2D;

internal static class Area2DOverlapDetector
{
    private const float SegmentBoundsPadding = 0.001f;

    public static Area2DOverlapSnapshot Capture(Area2D area)
    {
        var root = area.GetTree()?.Root;
        if (root is null || !TryGetObjectBounds(area, out var areaBounds))
        {
            return Area2DOverlapSnapshot.Empty;
        }

        var bodies = new List<Node2D>();
        var areas = new List<Area2D>();
        foreach (var candidate in CollectCollisionObjects(root))
        {
            if (ReferenceEquals(candidate, area) ||
                !IsCandidateVisibleToArea(area, candidate) ||
                !TryGetObjectBounds(candidate, out var candidateBounds) ||
                !areaBounds.Intersects(candidateBounds, includeBorders: true))
            {
                continue;
            }

            if (candidate is Area2D candidateArea)
            {
                if (candidateArea.Monitorable)
                {
                    areas.Add(candidateArea);
                }

                continue;
            }

            if (candidate is PhysicsBody2D)
            {
                bodies.Add(candidate);
            }
        }

        return new Area2DOverlapSnapshot(
            bodies.OrderBy(static body => body.GetInstanceId()).ToArray(),
            areas.OrderBy(static candidateArea => candidateArea.GetInstanceId()).ToArray());
    }

    private static IEnumerable<CollisionObject2D> CollectCollisionObjects(Node root)
    {
        if (root is CollisionObject2D collisionObject && IsValidCollisionObject(collisionObject))
        {
            yield return collisionObject;
        }

        foreach (var child in root.GetChildrenSnapshot())
        {
            foreach (var nestedCollisionObject in CollectCollisionObjects(child))
            {
                yield return nestedCollisionObject;
            }
        }
    }

    private static bool IsCandidateVisibleToArea(Area2D area, CollisionObject2D candidate)
    {
        if (!IsValidCollisionObject(candidate))
        {
            return false;
        }

        return (area.CollisionMask & candidate.CollisionLayer) != 0u;
    }

    private static bool IsValidCollisionObject(CollisionObject2D collisionObject)
    {
        return Object.IsInstanceValid(collisionObject) &&
            collisionObject.IsInsideTree() &&
            !collisionObject.IsQueuedForDeletion();
    }

    private static bool TryGetObjectBounds(CollisionObject2D collisionObject, out Rect2 bounds)
    {
        bounds = default;
        var found = false;
        foreach (var shape in CollectActiveShapes(collisionObject))
        {
            if (!TryGetShapeBounds(shape, out var shapeBounds))
            {
                continue;
            }

            bounds = found ? bounds.Merge(shapeBounds) : shapeBounds;
            found = true;
        }

        return found;
    }

    private static IEnumerable<CollisionShape2D> CollectActiveShapes(Node node)
    {
        foreach (var child in node.GetChildrenSnapshot())
        {
            if (child is CollisionObject2D)
            {
                continue;
            }

            if (child is CollisionShape2D { Disabled: false, Shape: not null } shape)
            {
                yield return shape;
            }

            foreach (var nestedShape in CollectActiveShapes(child))
            {
                yield return nestedShape;
            }
        }
    }

    private static bool TryGetShapeBounds(CollisionShape2D collisionShape, out Rect2 bounds)
    {
        bounds = default;
        var shape = collisionShape.Shape;
        if (shape is null || !TryGetLocalShapeBounds(shape, out var localBounds))
        {
            return false;
        }

        bounds = collisionShape.GlobalTransform * localBounds;
        return bounds.HasArea();
    }

    private static bool TryGetLocalShapeBounds(Shape2D shape, out Rect2 bounds)
    {
        switch (shape)
        {
            case RectangleShape2D rectangle:
                bounds = CenteredBounds(rectangle.Size);
                return true;
            case CircleShape2D circle:
                bounds = CenteredBounds(new Vector2(circle.Radius * 2f, circle.Radius * 2f));
                return true;
            case CapsuleShape2D capsule:
                bounds = CenteredBounds(new Vector2(capsule.Radius * 2f, capsule.Height));
                return true;
            case SegmentShape2D segment:
                bounds = BoundsFromPoints(segment.A, segment.B).Grow(SegmentBoundsPadding);
                return true;
            case ConvexPolygonShape2D convexPolygon:
                return TryGetPointBounds(convexPolygon.Points, out bounds);
            case ConcavePolygonShape2D concavePolygon:
                return TryGetPointBounds(concavePolygon.Segments, out bounds);
            default:
                bounds = default;
                return false;
        }
    }

    private static Rect2 CenteredBounds(Vector2 size)
    {
        return new Rect2(size / -2f, size);
    }

    private static bool TryGetPointBounds(IReadOnlyList<Vector2> points, out Rect2 bounds)
    {
        bounds = default;
        if (points.Count == 0)
        {
            return false;
        }

        var min = points[0];
        var max = points[0];
        for (var index = 1; index < points.Count; index++)
        {
            min = min.Min(points[index]);
            max = max.Max(points[index]);
        }

        bounds = new Rect2(min, max - min).Abs();
        return bounds.HasArea();
    }

    private static Rect2 BoundsFromPoints(Vector2 first, Vector2 second)
    {
        return new Rect2(first, Vector2.Zero).Expand(second).Abs();
    }
}

internal readonly record struct Area2DOverlapSnapshot(Node2D[] Bodies, Area2D[] Areas)
{
    public static Area2DOverlapSnapshot Empty { get; } = new([], []);
}
