/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using Electron2D.Kernel;

namespace Electron2D.Graphics
{
	public struct Point : IEquatable<Point>
	{
		public double X { get; set; }
		public double Y { get; set; }

		public Point(double x, double y) : this()
		{
			X = x;
			Y = y;
		}

		internal static Point ConvertToDecartPoint(double x, double y)
		{
			var newx = x - (Settings.Resolution.Width / 2);
			var newy = (y - (Settings.Resolution.Height / 2)) * -1;

			return new Point(newx, newy);
		}

		internal static Point ConvertToSDLPoint(double x, double y)
		{
			var newx = x + (Settings.Resolution.Width / 2);
			var newy = (y - (Settings.Resolution.Height / 2)) * -1 ;

			return new Point(newx, (uint)newy);
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

		public override string ToString()
		{
			return string.Format("({0}:{1})", X, Y);
		}
	}
}
