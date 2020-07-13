/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

namespace Electron2D.Graphics
{
    public struct Vector
    {
        public Vector(double x, double y) : this()
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public override bool Equals(object obj)
		{
			if (obj is Vector vector)
				return Equals(vector);
			return false;
		}

        public bool Equals(Vector other) => X == other.X && Y == other.Y;

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

		public static bool operator ==(Vector left, Vector right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Vector left, Vector right)
		{
			return !left.Equals(right);
		}

        public static Vector operator +(Vector left, Vector right)
        {
            return new Vector(left.X + right.X, left.Y + right.Y);
        }

        public static Vector operator -(Vector left, Vector right)
        {
            return new Vector(left.X - right.X, left.Y - right.Y);
        }
    }
}