/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

namespace Electron2D.Graphics
{
	public struct Point : IEquatable<Point>
	{
		public Point(float x, float y) : this()
		{
			X = x;
			Y = y;
		}

		public float X { get; set; }
		public float Y { get; set; }

		public static Point Zero => new Point();

		internal Point Rotate(Point center, float degrees)
		{
			float angleInRadians = degrees * (Math.PI / 180);
            float cosTheta = Math.Cos(-angleInRadians);
            float sinTheta = Math.Sin(-angleInRadians);

            return new Point
            {
                X = ((cosTheta * (X - center.X)) - (sinTheta * (Y - center.Y)) + center.X),
                Y = ((sinTheta * (X - center.X)) + (cosTheta * (Y - center.Y)) + center.Y)
            };
		}

		public static Point Lerp(Point value1, Point value2, float amount)
        {
            return new Point(
                value1.X + ((value2.X - value1.X) * amount),
                value1.Y + ((value2.Y - value1.Y) * amount)
			);
        }

		public static Point LerpX(Point value1, Point value2, float y, float amount)
        {
            return new Point(
                value1.X + ((value2.X - value1.X) * amount), y);
        }

		public override bool Equals(object obj)
		{
			if (obj is Point point)
				return Equals(point);
			return false;
		}

		public bool Equals(Point other)
		{
			return X == other.X && Y == other.Y;
		}

		public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Point left, Point right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Point left, Point right)
		{
			return !left.Equals(right);
		}

		public static Point operator +(Point left, Point right)
		{
			return new Point(left.X + right.X, left.Y + right.Y);
		}

		public static Point operator -(Point left, Point right)
		{
			return new Point(left.X - right.X, left.Y - right.Y);
		}

		public static Point operator -(Point point)
		{
			return new Point(-point.X, -point.Y);
		}

		public static Point operator *(Point left, float right)
		{
			return new Point(left.X * right, left.Y * right);
		}

		public static Point Multiply(Point left, float right)
        {
            return left * right;
        }

		public override string ToString()
		{
			return string.Format("({0}:{1})", X, Y);
		}
	}
}
