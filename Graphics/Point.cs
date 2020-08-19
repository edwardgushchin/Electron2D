/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

namespace Electron2D.Graphics
{
	public struct Point : IEquatable<Point>
	{
		public Point(double x, double y) : this()
		{
			X = x;
			Y = y;
		}

		public double X { get; set; }
		public double Y { get; set; }

		public static Point Zero => new Point();

		internal Point Rotate(Point center, double degrees)
		{
			double angleInRadians = degrees * (Math.PI / 180);
            double cosTheta = Math.Cos(-angleInRadians);
            double sinTheta = Math.Sin(-angleInRadians);

            return new Point
            {
                X = (double) ((cosTheta * (X - center.X)) - (sinTheta * (Y - center.Y)) + center.X),
                Y = (double) ((sinTheta * (X - center.X)) + (cosTheta * (Y - center.Y)) + center.Y)
            };
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

		public static Point operator *(Point left, double right)
		{
			return new Point(left.X * right, left.Y * right);
		}

		public static Point Multiply(Point left, double right)
        {
            return left * right;
        }

		public override string ToString()
		{
			return string.Format("({0}:{1})", X, Y);
		}
	}
}
