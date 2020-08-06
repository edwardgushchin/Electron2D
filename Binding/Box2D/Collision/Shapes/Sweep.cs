using System;
using System.Diagnostics;
using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Collision.Shapes
{
    /// <summary>
    /// This describes the motion of a body/shape for TOI computation.
    /// Shapes are defined with respect to the body origin, which may
    /// no coincide with the center of mass. However, to support dynamics
    /// we must interpolate the center of mass position.
    /// </summary>
    public struct Sweep
    {
        /// <summary>
        /// Get the interpolated transform at a specific time.
        /// @param beta is a factor in [0,1], where 0 indicates alpha0.
        /// 获取特定时间的位置插值
        /// </summary>
        /// <param name="xf">位置</param>
        /// <param name="beta"></param>
        public void GetTransform(out Transform xf, float beta)
        {
            var position = C0 + (beta * (C - C0));
            var angle = A0 + (beta * (A - A0));
            xf = new Transform(position, angle);

            // Shift to origin
            xf.Position -= MathUtils.Mul(xf.Rotation, LocalCenter);
        }

        /// <summary>
        /// Advance the sweep forward, yielding a new initial state.
        /// @param alpha the new initial time.
        /// </summary>
        public void Advance(float alpha)
        {
            System.Diagnostics.Debug.Assert(Alpha0 < 1.0f);
            var beta = (alpha - Alpha0) / (1.0f - Alpha0);
            C0 += beta * (C - C0);
            A0 += beta * (A - A0);
            Alpha0 = alpha;
        }

        /// <summary>
        /// Normalize the angles.
        /// </summary>
        public void Normalize()
        {
            const float twoPi = 2.0f * Settings.Pi;
            var d = twoPi * (float)Math.Floor(A0 / twoPi);
            A0 -= d;
            A -= d;
        }

        /// <summary>
        /// local center of mass position
        /// </summary>
        public Vector2 LocalCenter;

        /// <summary>
        /// center world positions
        /// </summary>
        public Vector2 C0, C;

        /// <summary>
        /// world angles
        /// </summary>
        public float A0, A;

        /// <summary>
        /// Fraction of the current time step in the range [0,1]
        /// c0 and a0 are the positions at alpha0.
        /// </summary>
        public float Alpha0;
    }
}