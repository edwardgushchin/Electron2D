/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
	public struct Color : IEquatable<Color>
	{
		#region Equals and GetHashCode implementation
		// The code in this region is useful if you want to use this structure in collections.
		// If you don't need it, you can just remove the region and the ": IEquatable<Color>" declaration.

		public Color(byte R, byte G, byte B)
		{
            this.R = R;
            this.G = G;
            this.B = B;
            A = 0;
		}

		public Color(byte R, byte G, byte B, byte A)
		{
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
		}

		internal SDL.SDL_Color ConvertToSDLColor()
		{
			SDL.SDL_Color color;
            color.r = R;
			color.g = G;
			color.b = B;
			color.a = A;
			return color;
		}

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public static Color Black { get { return new Color(0, 0, 0, 0); } }
		public static Color White { get { return new Color(255, 255, 255, 0); } }
		public static Color Red { get { return new Color(255, 0, 0, 0); } }
		public static Color Lime { get { return new Color(0, 255, 0, 0); } }
		public static Color Blue { get { return new Color(0, 0, 255, 0); } }
		public static Color Yellow { get { return new Color(255, 255, 0, 0); } }
		public static Color Cyan { get { return new Color(0, 255, 255, 0); } }
		public static Color Magenta { get { return new Color(255, 0, 255, 0); } }
		public static Color Silver { get { return new Color(192, 192, 192, 0); } }
		public static Color Gray { get { return new Color(128, 128, 128, 0); } }
	 	public static Color Maroon { get { return new Color(128, 0, 0, 0); } }
	 	public static Color Olive { get { return new Color(128, 128, 0, 0); } }
	 	public static Color Green { get { return new Color(0, 128, 0, 0); } }
	 	public static Color Purple { get { return new Color(128, 0, 128, 0); } }
	 	public static Color Teal { get { return new Color(0, 128, 128, 0); } }
	 	public static Color Navy { get { return new Color(0, 0, 128, 0); } }

		public override bool Equals(object obj)
		{
			if (obj is Color color)
				return Equals(color); // use Equals method below
			else
				return false;
		}

		public bool Equals(Color other)
		{
			// add comparisions for all members here
			return R + G + B + A == other.R + other.G + other.B + other.A;
		}

		public override int GetHashCode()
		{
			// combine the hash codes of all members here (e.g. with XOR operator ^)
			//return (R + G + B + A).GetHashCode();
			return HashCode.Combine(R, G, B, A);
		}

		public static bool operator ==(Color left, Color right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Color left, Color right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
}
