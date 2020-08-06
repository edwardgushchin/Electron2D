using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D.Binding.Box2D.Common
{
    /// <summary>
    /// Rotation
    /// </summary>
    public struct Rotation
    {
        /// <summary>
        /// Sine and cosine
        /// </summary>
        public float Sin;

        public float Cos;

        public Rotation(float sin, float cos)
        {
            Sin = sin;
            Cos = cos;
        }

        /// <summary>
        /// Initialize from an angle in radians
        /// </summary>
        public Rotation(float angle)
        {
            // TODO_ERIN optimize
            Sin = (float) Math.Sin(angle);
            Cos = (float) Math.Cos(angle);
        }

        /// <summary>
        /// Set using an angle in radians.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(float angle)
        {
            // TODO_ERIN optimize
            Sin = (float) Math.Sin(angle);
            Cos = (float) Math.Cos(angle);
        }

        /// <summary>
        /// Set to the identity rotation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIdentity()
        {
            Sin = 0.0f;
            Cos = 1.0f;
        }

        /// <summary>
        /// Get the angle in radians
        /// </summary>
        public float Angle => (float) Math.Atan2(Sin, Cos);

        /// <summary>
        /// Get the x-axis
        /// </summary>
        public Vector2 GetXAxis()
        {
            return new Vector2(Cos, Sin);
        }

        /// <summary>
        /// Get the u-axis
        /// </summary>
        public Vector2 GetYAxis()
        {
            return new Vector2(-Sin, Cos);
        }
    }
}