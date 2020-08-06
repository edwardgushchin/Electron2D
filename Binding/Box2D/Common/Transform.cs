using System;
using System.Numerics;

namespace Electron2D.Binding.Box2D.Common
{
    public struct Transform : IFormattable
    {
        public Vector2 Position;

        public Rotation Rotation;

        /// <summary>
        /// Initialize using a position vector and a rotation.
        /// </summary>
        public Transform(Vector2 position, Rotation rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Transform(Vector2 position, float angle)
        {
            Position = position;
            Rotation = new Rotation(angle);
        }

        /// <summary>
        /// Set this to the identity transform.
        /// </summary>
        public void SetIdentity()
        {
            Position = Vector2.Zero;
            Rotation.SetIdentity();
        }

        /// <summary>
        /// Set this based on the position and angle.
        /// </summary>
        public void Set(Vector2 position, float angle)
        {
            Position = position;
            Rotation.Set(angle);
        }

        /// <inheritdoc />
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        public new string ToString()
        {
            return $"({Position.X},{Position.Y}), Cos:{Rotation.Cos}, Sin:{Rotation.Sin})";
        }
    }
}