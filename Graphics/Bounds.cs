/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D.Graphics
{
    public struct Bounds
    {
        public Bounds(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public override string ToString()
		{
			return string.Format("({0}:{1}; {2}x{3})", X, Y, Width, Height);
		}
    }
}