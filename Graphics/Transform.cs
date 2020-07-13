/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D.Graphics
{
	public class Transform
	{
		private Point position, localScale;

		public Transform()
		{
			position = new Point();
            Degrees = 0;
			localScale = new Point(1, 1);
		}

		public Transform(Point pos)
		{
			position = pos;
            Degrees = 0;
			localScale = new Point(1, 1);
		}

		// disable once ConvertToAutoProperty
		public Point Position
		{
			get { return position; }
			private set { position = value; }
		}

        // disable once ConvertToAutoProperty
        public double Degrees { get; set; }

        // disable once ConvertToAutoProperty
        public Point LocalScale
		{
			get { return localScale; }
			set { localScale = value; }
		}

		public void Translate(double x, double y)
		{
			position.X += x;
			position.Y += y;
		}

		public void Rotate(double deg)
		{
            Degrees += deg;
		}

		public void Scale(double xscale, double yscale)
		{
			localScale.X += xscale;
			localScale.Y += yscale;
		}

		public void SetPosition(double x, double y)
		{
			position = new Point(x, y);
		}

		public override string ToString()
		{
			return $"X: {position.X}; Y: {position.Y};";
		}
	}
}
