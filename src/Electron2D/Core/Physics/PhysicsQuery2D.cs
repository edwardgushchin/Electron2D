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
using VariantArray = Electron2D.Collections.Array;
using VariantDictionary = Electron2D.Collections.Dictionary;

namespace Electron2D;

internal static class PhysicsQuery2D
{
    private const float SegmentBoundsPadding = 0.001f;

    public static IEnumerable<CollisionObject2D> CollectCollisionObjects(Node root)
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

    public static bool IsValidCollisionObject(CollisionObject2D collisionObject)
    {
        return Object.IsInstanceValid(collisionObject) &&
            collisionObject.IsInsideTree() &&
            !collisionObject.IsQueuedForDeletion();
    }

    public static bool CollisionMaskMatches(uint collisionMask, CollisionObject2D candidate)
    {
        return (collisionMask & candidate.CollisionLayer) != 0u;
    }

    public static bool TryGetObjectBounds(CollisionObject2D collisionObject, out Rect2 bounds)
    {
        bounds = default;
        var found = false;
        foreach (var shape in CollectActiveShapeBounds(collisionObject))
        {
            bounds = found ? bounds.Merge(shape.Bounds) : shape.Bounds;
            found = true;
        }

        return found;
    }

    public static PhysicsQueryShape[] CollectActiveShapeBounds(CollisionObject2D collisionObject)
    {
        var shapes = new List<PhysicsQueryShape>();
        CollectActiveShapeBounds(collisionObject, shapes);
        return shapes.ToArray();
    }

    public static bool TryGetShapeBounds(Shape2D shape, Transform2D transform, out Rect2 bounds)
    {
        bounds = default;
        if (!TryGetLocalShapeBounds(shape, out var localBounds))
        {
            return false;
        }

        bounds = transform * localBounds;
        return bounds.HasArea();
    }

    public static VariantDictionary CreateObjectResult(CollisionObject2D owner, int shapeIndex)
    {
        var result = new VariantDictionary();
        result.Add(Variant.CreateFrom("collider"), Variant.CreateFrom(owner));
        result.Add(Variant.CreateFrom("collider_id"), Variant.CreateFrom((long)owner.GetInstanceId()));
        result.Add(Variant.CreateFrom("rid"), Variant.CreateFrom(owner.GetRid()));
        result.Add(Variant.CreateFrom("shape"), Variant.CreateFrom((long)shapeIndex));
        return result;
    }

    public static VariantArray CreateObjectResults(IEnumerable<PhysicsQueryShape> shapes, int maxResults)
    {
        var results = new VariantArray();
        if (maxResults == 0)
        {
            return results;
        }

        foreach (var shape in shapes
            .GroupBy(static shape => shape.Owner)
            .Select(static group => group.OrderBy(static shape => shape.ShapeIndex).First())
            .OrderBy(static shape => shape.Owner.GetInstanceId())
            .Take(maxResults))
        {
            results.Add(Variant.CreateFrom(CreateObjectResult(shape.Owner, shape.ShapeIndex)));
        }

        return results;
    }

    public static bool TryIntersectSegmentWithBounds(
        Vector2 from,
        Vector2 to,
        Rect2 bounds,
        bool hitFromInside,
        out float fraction,
        out Vector2 point,
        out Vector2 normal)
    {
        var normalizedBounds = bounds.Abs();
        if (ContainsPointInclusive(normalizedBounds, from))
        {
            if (!hitFromInside)
            {
                fraction = 0f;
                point = Vector2.Zero;
                normal = Vector2.Zero;
                return false;
            }

            fraction = 0f;
            point = from;
            normal = Vector2.Zero;
            return true;
        }

        var direction = to - from;
        var minimum = normalizedBounds.Position;
        var maximum = normalizedBounds.End;
        var minimumFraction = 0f;
        var maximumFraction = 1f;
        var hitNormal = Vector2.Zero;

        if (!ClipAxis(from.X, direction.X, minimum.X, maximum.X, new Vector2(-1f, 0f), new Vector2(1f, 0f), ref minimumFraction, ref maximumFraction, ref hitNormal) ||
            !ClipAxis(from.Y, direction.Y, minimum.Y, maximum.Y, new Vector2(0f, -1f), new Vector2(0f, 1f), ref minimumFraction, ref maximumFraction, ref hitNormal))
        {
            fraction = 0f;
            point = Vector2.Zero;
            normal = Vector2.Zero;
            return false;
        }

        fraction = minimumFraction;
        point = from + (direction * minimumFraction);
        normal = hitNormal;
        return true;
    }

    public static bool ContainsPointInclusive(Rect2 bounds, Vector2 point)
    {
        var normalizedBounds = bounds.Abs();
        return point.X >= normalizedBounds.Position.X &&
            point.X <= normalizedBounds.End.X &&
            point.Y >= normalizedBounds.Position.Y &&
            point.Y <= normalizedBounds.End.Y;
    }

    private static void CollectActiveShapeBounds(Node node, List<PhysicsQueryShape> shapes)
    {
        var shapeIndex = 0;
        foreach (var child in node.GetChildrenSnapshot())
        {
            if (child is CollisionObject2D)
            {
                continue;
            }

            if (child is CollisionShape2D { Disabled: false, Shape: not null } shape &&
                TryGetShapeBounds(shape.Shape, shape.GlobalTransform, out var bounds) &&
                node is CollisionObject2D owner)
            {
                shapes.Add(new PhysicsQueryShape(owner, shape, shapeIndex, bounds));
                shapeIndex++;
                continue;
            }

            CollectActiveShapeBounds(child, shapes);
        }
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
                bounds = new Rect2(segment.A, Vector2.Zero).Expand(segment.B).Abs().Grow(SegmentBoundsPadding);
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

    private static bool ClipAxis(
        float origin,
        float direction,
        float minimum,
        float maximum,
        Vector2 minimumNormal,
        Vector2 maximumNormal,
        ref float minimumFraction,
        ref float maximumFraction,
        ref Vector2 hitNormal)
    {
        if (Mathf.IsZeroApprox(direction))
        {
            return origin >= minimum && origin <= maximum;
        }

        var first = (minimum - origin) / direction;
        var second = (maximum - origin) / direction;
        var firstNormal = minimumNormal;
        var secondNormal = maximumNormal;
        if (first > second)
        {
            (first, second) = (second, first);
            (firstNormal, secondNormal) = (secondNormal, firstNormal);
        }

        if (first > minimumFraction)
        {
            minimumFraction = first;
            hitNormal = firstNormal;
        }

        if (second < maximumFraction)
        {
            maximumFraction = second;
        }

        return minimumFraction <= maximumFraction && maximumFraction >= 0f && minimumFraction <= 1f;
    }
}

internal readonly record struct PhysicsQueryShape(
    CollisionObject2D Owner,
    CollisionShape2D Shape,
    int ShapeIndex,
    Rect2 Bounds);
