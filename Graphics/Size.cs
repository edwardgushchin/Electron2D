/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

namespace Electron2D.Graphics
{
	public struct Size : IEquatable<Size>
	{	
		public int Width { get; set; }
		public int Height { get; set; }
		
		public Size(int width, int height) : this()
		{
			Width = width;
			Height = height;
		}
		
		public override bool Equals(object obj)
		{
			if (obj is Size)
				return Equals((Size)obj);
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
		
		public static bool operator ==(Size left, Size right)
		{
			return left.Equals(right);
		}
		
		public static bool operator !=(Size left, Size right)
		{
			return !left.Equals(right);
		}
		
		public override string ToString()
		{
			return string.Format("({0}x{1})", Width, Height);
		}

	}
}
