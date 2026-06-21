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

internal static class RigidBody2DMotion
{
    public static Vector2 ResolveMotion(RigidBody2D body, Vector2 motion, out Vector2 collisionNormal)
    {
        collisionNormal = Vector2.Zero;
        if (!motion.IsFinite())
        {
            return Vector2.Zero;
        }

        var tree = body.GetTree();
        if (tree is null)
        {
            return motion;
        }

        var movingShapes = PhysicsQuery2D.CollectActiveShapeBounds(body);
        if (movingShapes.Length == 0)
        {
            return motion;
        }

        var bestHit = MotionHit.None;
        foreach (var movingShape in movingShapes)
        {
            var movingBounds = movingShape.Bounds.Abs();
            if (!movingBounds.HasArea())
            {
                continue;
            }

            foreach (var targetShape in CollectStaticTargetShapes(tree.Root, body))
            {
                if (TrySweepAgainstTarget(movingBounds, motion, targetShape, out var hit) &&
                    hit.Fraction < bestHit.Fraction)
                {
                    bestHit = hit;
                }
            }
        }

        if (!bestHit.HasHit)
        {
            return motion;
        }

        collisionNormal = bestHit.Normal;
        return motion * bestHit.Fraction;
    }

    private static IEnumerable<PhysicsQueryShape> CollectStaticTargetShapes(Node root, RigidBody2D body)
    {
        foreach (var target in PhysicsQuery2D.CollectCollisionObjects(root))
        {
            if (ReferenceEquals(target, body) ||
                target is not StaticBody2D ||
                !PhysicsQuery2D.CollisionMaskMatches(body.CollisionMask, target))
            {
                continue;
            }

            foreach (var targetShape in PhysicsQuery2D.CollectActiveShapeBounds(target))
            {
                yield return targetShape;
            }
        }
    }

    private static bool TrySweepAgainstTarget(
        Rect2 movingBounds,
        Vector2 motion,
        PhysicsQueryShape targetShape,
        out MotionHit hit)
    {
        var targetBounds = targetShape.Bounds.Abs();
        var expandedTargetBounds = ExpandTargetBounds(targetBounds, movingBounds.Size);
        var from = movingBounds.GetCenter();
        var to = from + motion;
        if (!PhysicsQuery2D.TryIntersectSegmentWithBounds(
            from,
            to,
            expandedTargetBounds,
            hitFromInside: false,
            out var fraction,
            out _,
            out var normal))
        {
            hit = MotionHit.None;
            return false;
        }

        if (targetShape.Shape.OneWayCollision &&
            !ShouldCollideWithOneWayShape(movingBounds, targetBounds, motion, normal, targetShape.Shape.OneWayCollisionMargin))
        {
            hit = MotionHit.None;
            return false;
        }

        hit = new MotionHit(true, fraction, normal);
        return true;
    }

    private static Rect2 ExpandTargetBounds(Rect2 targetBounds, Vector2 movingSize)
    {
        var halfMovingSize = movingSize / 2f;
        return new Rect2(targetBounds.Position - halfMovingSize, targetBounds.Size + movingSize);
    }

    private static bool ShouldCollideWithOneWayShape(
        Rect2 movingBounds,
        Rect2 targetBounds,
        Vector2 motion,
        Vector2 normal,
        float margin)
    {
        if (motion.Y <= 0f || normal.Y >= -0.5f)
        {
            return false;
        }

        var oneWayMargin = MathF.Max(0f, margin);
        return movingBounds.End.Y <= targetBounds.Position.Y + oneWayMargin;
    }

    private readonly record struct MotionHit(bool HasHit, float Fraction, Vector2 Normal)
    {
        public static MotionHit None { get; } = new(false, float.PositiveInfinity, Vector2.Zero);
    }
}
