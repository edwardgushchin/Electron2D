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

internal static class PhysicsBody2DMotion
{
    public static KinematicCollision2D? MoveAndCollide(
        PhysicsBody2D body,
        Vector2 motion,
        bool testOnly,
        float safeMargin,
        bool recoveryAsCollision)
    {
        _ = recoveryAsCollision;
        if (!motion.IsFinite())
        {
            return null;
        }

        var collision = CastMotion(body, motion, MathF.Max(0f, safeMargin));
        if (collision is null)
        {
            if (!testOnly)
            {
                body.Position += motion;
            }

            return null;
        }

        if (!testOnly)
        {
            body.Position += collision.GetTravel();
        }

        return collision;
    }

    public static KinematicCollision2D? CastMotion(PhysicsBody2D body, Vector2 motion, float safeMargin)
    {
        if (!motion.IsFinite())
        {
            return null;
        }

        var tree = body.GetTree();
        if (tree is null)
        {
            return null;
        }

        var movingShapes = PhysicsQuery2D.CollectActiveShapeBounds(body);
        if (movingShapes.Length == 0)
        {
            return null;
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
                if (TrySweepAgainstTarget(movingBounds, movingShape.Shape, motion, targetShape, safeMargin, out var hit) &&
                    hit.Fraction < bestHit.Fraction)
                {
                    bestHit = hit;
                }
            }
        }

        return bestHit.HasHit ? bestHit.ToCollision(body, motion) : null;
    }

    private static IEnumerable<PhysicsQueryShape> CollectStaticTargetShapes(Node root, PhysicsBody2D body)
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
        CollisionShape2D localShape,
        Vector2 motion,
        PhysicsQueryShape targetShape,
        float safeMargin,
        out MotionHit hit)
    {
        var targetBounds = targetShape.Bounds.Abs();
        var expandedTargetBounds = ExpandTargetBounds(targetBounds, movingBounds.Size, safeMargin);
        var from = movingBounds.GetCenter();
        var to = from + motion;
        if (!PhysicsQuery2D.TryIntersectSegmentWithBounds(
            from,
            to,
            expandedTargetBounds,
            hitFromInside: false,
            out var fraction,
            out var position,
            out var normal))
        {
            hit = MotionHit.None;
            return false;
        }

        normal = ResolveTargetNormal(targetShape, motion, normal);
        if (targetShape.Shape.OneWayCollision &&
            !ShouldCollideWithOneWayShape(movingBounds, targetBounds, motion, normal, targetShape.Shape.OneWayCollisionMargin))
        {
            hit = MotionHit.None;
            return false;
        }

        hit = new MotionHit(true, fraction, position, normal, localShape, targetShape);
        return true;
    }

    private static Vector2 ResolveTargetNormal(PhysicsQueryShape targetShape, Vector2 motion, Vector2 boundsNormal)
    {
        if (targetShape.Shape.Shape is not SegmentShape2D segment)
        {
            return boundsNormal;
        }

        var start = targetShape.Shape.GlobalTransform * segment.A;
        var end = targetShape.Shape.GlobalTransform * segment.B;
        var direction = end - start;
        if (direction.IsZeroApprox())
        {
            return boundsNormal;
        }

        var normal = new Vector2(direction.Y, -direction.X).Normalized();
        return normal.Dot(motion) <= 0f ? normal : -normal;
    }

    private static Rect2 ExpandTargetBounds(Rect2 targetBounds, Vector2 movingSize, float safeMargin)
    {
        var halfMovingSize = movingSize / 2f;
        var margin = new Vector2(safeMargin, safeMargin);
        return new Rect2(targetBounds.Position - halfMovingSize - margin, targetBounds.Size + movingSize + (margin * 2f));
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

    private readonly record struct MotionHit(
        bool HasHit,
        float Fraction,
        Vector2 Position,
        Vector2 Normal,
        CollisionShape2D LocalShape,
        PhysicsQueryShape TargetShape)
    {
        public static MotionHit None { get; } = new(
            false,
            float.PositiveInfinity,
            Vector2.Zero,
            Vector2.Zero,
            null!,
            default);

        public KinematicCollision2D ToCollision(PhysicsBody2D body, Vector2 motion)
        {
            var travel = motion * Fraction;
            var remainder = motion - travel;
            return new KinematicCollision2D(
                Position,
                Normal,
                travel,
                remainder,
                TargetShape.Owner,
                TargetShape.Owner.GetRid(),
                TargetShape.Shape,
                TargetShape.ShapeIndex,
                GetColliderVelocity(TargetShape.Owner),
                LocalShape,
                depth: 0f);
        }

        private static Vector2 GetColliderVelocity(CollisionObject2D owner)
        {
            return owner switch
            {
                StaticBody2D staticBody => staticBody.ConstantLinearVelocity,
                RigidBody2D rigidBody => rigidBody.LinearVelocity,
                CharacterBody2D characterBody => characterBody.Velocity,
                _ => Vector2.Zero
            };
        }
    }
}
