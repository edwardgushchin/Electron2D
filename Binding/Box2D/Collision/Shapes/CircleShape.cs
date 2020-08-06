using System;
using System.Numerics;
using Electron2D.Binding.Box2D.Collision.Collider;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Collision.Shapes
{
    /// <summary>
    /// A solid circle shape
    /// </summary>
    public class CircleShape : Shape
    {
        /// <summary>
        /// Position
        /// </summary>
        public Vector2 Position;

        public new float Radius
        {
            get => base.Radius;
            set => base.Radius = value;
        }

        public CircleShape()
        {
            ShapeType = ShapeType.Circle;
            Radius = 0.0f;
            Position.SetZero();
        }

        /// <summary>
        /// Implement b2Shape.
        /// </summary>
        public override Shape Clone()
        {
            var clone = new CircleShape {Position = Position, Radius = Radius};
            return clone;
        }

        /// <summary>
        /// @see b2Shape::GetChildCount
        /// </summary>
        public override int GetChildCount()
        {
            return 1;
        }

        /// <summary>
        /// Implement b2Shape.
        /// </summary>
        public override bool TestPoint(in Transform transform, in Vector2 p)
        {
            var center = transform.Position + MathUtils.Mul(transform.Rotation, Position);
            var d = p - center;
            return Vector2.Dot(d, d) <= Radius * Radius;
        }

        /// <summary>
        /// Implement b2Shape.
        /// @note because the circle is solid, rays that start inside do not hit because the normal is
        /// not defined.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <param name="transform"></param>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        public override bool RayCast(
            out RayCastOutput output,
            in RayCastInput input,
            in Transform transform,
            int childIndex)
        {
            output = default;
            var position = transform.Position + MathUtils.Mul(transform.Rotation, Position);
            var s = input.P1 - position;
            var b = Vector2.Dot(s, s) - (Radius * Radius);

            // Solve quadratic equation.
            var r = input.P2 - input.P1;
            var c = Vector2.Dot(s, r);
            var rr = Vector2.Dot(r, r);
            var sigma = (c * c) - (rr * b);

            // Check for negative discriminant and short segment.
            if (sigma < 0.0f || rr < Settings.Epsilon)
            {
                return false;
            }

            // Find the point of intersection of the line with the circle.
            var a = -(c + (float) Math.Sqrt(sigma));

            // Is the intersection point on the segment?
            if (0.0f <= a && a <= input.MaxFraction * rr)
            {
                a /= rr;
                output = new RayCastOutput {Fraction = a, Normal = s + (a * r) };
                output.Normal.Normalize();
                return true;
            }

            return false;
        }

        /// <summary>
        /// @see b2Shape::ComputeAABB
        /// </summary>
        public override void ComputeAABB(
            out AABB aabb,
            in Transform transform,
            int
                childIndex)
        {
            var p = transform.Position + MathUtils.Mul(transform.Rotation, Position);
            aabb = new AABB();
            aabb.LowerBound.Set(p.X - Radius, p.Y - Radius);
            aabb.UpperBound.Set(p.X + Radius, p.Y + Radius);
        }

        /// <summary>
        /// @see b2Shape::ComputeMass
        /// </summary>
        public override void ComputeMass(out MassData massData, float density)
        {
            massData = new MassData {Mass = density * Settings.Pi * Radius * Radius, Center = Position};

            // inertia about the local origin
            massData.RotationInertia = massData.Mass * ((0.5f * Radius * Radius) + Vector2.Dot(Position, Position));
        }
    }
}