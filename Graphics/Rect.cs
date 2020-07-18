/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

namespace Electron2D.Graphics
{
	public struct Rect : IEquatable<Size>
	{
		public double Width { get; set; }
		public double Height { get; set; }

		public Rect(double width, double height) : this()
		{
			Width = width;
			Height = height;
		}

		public override bool Equals(object obj)
		{
			if (obj is Rect rect)
				return Equals(rect);
			return false;
		}

		public bool Equals(Size other)
		{
			return this.Width == other.Width && this.Height == other.Height;
		}

		public override int GetHashCode()
		{
			return (Width+Height).GetHashCode();
		}

		public static bool operator ==(Rect left, Rect right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Rect left, Rect right)
		{
			return !left.Equals(right);
		}

		public override string ToString()
		{
			return string.Format("({0}x{1})", Width, Height);
		}
	}
}
