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

/// <summary>
/// Provides direct 2D physics queries for a <see cref="World2D" />.
/// </summary>
///
/// <remarks>
/// Electron2D 0.1.0 Preview implements a managed AABB query baseline over
/// active <see cref="CollisionShape2D" /> nodes. The result format follows the
/// Godot-like dynamic dictionary/array surface and does not expose backend
/// handles.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Call it on the main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public sealed class PhysicsDirectSpaceState2D : Object
{
    private readonly Node? root;

    internal PhysicsDirectSpaceState2D(Node? root)
    {
        this.root = root;
    }

    /// <summary>
    /// Intersects a ray segment with the current 2D physics shapes.
    /// </summary>
    /// <param name="parameters">Ray query parameters.</param>
    /// <returns>
    /// A dictionary with hit data, or an empty dictionary when the ray hits nothing.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters" /> is <c>null</c>.</exception>
    public VariantDictionary IntersectRay(PhysicsRayQueryParameters2D parameters)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(parameters);
        if (root is null)
        {
            return new VariantDictionary();
        }

        var exclude = parameters.Exclude.ToHashSet();
        var hit = EnumerateQueryShapes(parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, exclude)
            .Select(shape => CreateRayHit(parameters, shape))
            .Where(static hit => hit is not null)
            .Select(static hit => hit!.Value)
            .OrderBy(static hit => hit.Fraction)
            .ThenBy(static hit => hit.Shape.Owner.GetInstanceId())
            .FirstOrDefault();

        if (hit.Shape.Owner is null)
        {
            return new VariantDictionary();
        }

        var result = PhysicsQuery2D.CreateObjectResult(hit.Shape.Owner, hit.Shape.ShapeIndex);
        result.Add(Variant.CreateFrom("position"), Variant.CreateFrom(hit.Point));
        result.Add(Variant.CreateFrom("normal"), Variant.CreateFrom(hit.Normal));
        return result;
    }

    /// <summary>
    /// Finds physics objects whose active shapes contain a point.
    /// </summary>
    /// <param name="parameters">Point query parameters.</param>
    /// <param name="maxResults">Maximum number of result dictionaries to return.</param>
    /// <returns>An array of dictionaries describing matching objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxResults" /> is negative.</exception>
    public VariantArray IntersectPoint(PhysicsPointQueryParameters2D parameters, int maxResults = 32)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(parameters);
        ValidateMaxResults(maxResults);
        if (root is null || maxResults == 0)
        {
            return new VariantArray();
        }

        var exclude = parameters.Exclude.ToHashSet();
        var shapes = EnumerateQueryShapes(parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, exclude)
            .Where(shape => PhysicsQuery2D.ContainsPointInclusive(shape.Bounds, parameters.Position));
        return PhysicsQuery2D.CreateObjectResults(shapes, maxResults);
    }

    /// <summary>
    /// Finds physics objects whose active shapes intersect a query shape.
    /// </summary>
    /// <param name="parameters">Shape query parameters.</param>
    /// <param name="maxResults">Maximum number of result dictionaries to return.</param>
    /// <returns>An array of dictionaries describing matching objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxResults" /> is negative.</exception>
    public VariantArray IntersectShape(PhysicsShapeQueryParameters2D parameters, int maxResults = 32)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(parameters);
        ValidateMaxResults(maxResults);
        if (root is null || maxResults == 0 || parameters.Shape is null)
        {
            return new VariantArray();
        }

        var queryTransform = parameters.Transform;
        if (!PhysicsQuery2D.TryGetShapeBounds(parameters.Shape, queryTransform, out var queryBounds))
        {
            return new VariantArray();
        }

        if (parameters.Margin > 0f)
        {
            queryBounds = queryBounds.Grow(parameters.Margin);
        }

        if (!parameters.Motion.IsZeroApprox())
        {
            queryBounds = queryBounds.Merge(new Rect2(queryBounds.Position + parameters.Motion, queryBounds.Size));
        }

        var exclude = parameters.Exclude.ToHashSet();
        var shapes = EnumerateQueryShapes(parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, exclude)
            .Where(shape => queryBounds.Intersects(shape.Bounds, includeBorders: true));
        return PhysicsQuery2D.CreateObjectResults(shapes, maxResults);
    }

    private IEnumerable<PhysicsQueryShape> EnumerateQueryShapes(
        uint collisionMask,
        bool collideWithBodies,
        bool collideWithAreas,
        IReadOnlySet<Rid> exclude)
    {
        if (root is null)
        {
            yield break;
        }

        foreach (var collisionObject in PhysicsQuery2D.CollectCollisionObjects(root))
        {
            if (!PhysicsQuery2D.CollisionMaskMatches(collisionMask, collisionObject) ||
                exclude.Contains(collisionObject.GetRid()) ||
                (collisionObject is Area2D && !collideWithAreas) ||
                (collisionObject is PhysicsBody2D && !collideWithBodies))
            {
                continue;
            }

            foreach (var shape in PhysicsQuery2D.CollectActiveShapeBounds(collisionObject))
            {
                yield return shape;
            }
        }
    }

    private static PhysicsRayHit? CreateRayHit(PhysicsRayQueryParameters2D parameters, PhysicsQueryShape shape)
    {
        return PhysicsQuery2D.TryIntersectSegmentWithBounds(
            parameters.From,
            parameters.To,
            shape.Bounds,
            parameters.HitFromInside,
            out var fraction,
            out var point,
            out var normal)
            ? new PhysicsRayHit(shape, fraction, point, normal)
            : null;
    }

    private static void ValidateMaxResults(int maxResults)
    {
        if (maxResults < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), maxResults, "maxResults must be greater than or equal to zero.");
        }
    }

    private readonly record struct PhysicsRayHit(
        PhysicsQueryShape Shape,
        float Fraction,
        Vector2 Point,
        Vector2 Normal);
}
