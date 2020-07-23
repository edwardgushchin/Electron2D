/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D.Graphics
{
	public class Transform
	{
		private Point position, achor;
		private Vector localScale;
		private double degrees;

		public Transform()
		{
			position = new Point();
			achor = new Point(0.5, 0.5);
            degrees = 0;
			localScale = new Vector(1, 1);
		}

		public Transform(Point pos)
		{
			position = pos;
			achor = new Point(0.5, 0.5);
            degrees = 0;
			localScale = new Vector(1, 1);
		}

		public Point Position
		{
			get { return position; }
			set { position = value; }
		}

        public double Degrees
		{
			get { return degrees; }
			set { degrees = value; }
		}

        public Vector LocalScale
		{
			get { return localScale; }
			set { localScale = value; }
		}

		public Point Achor
		{
			get { return achor; }
			set { achor = value; }
		}

		public void Translate(double x, double y)
		{
			position.X += x;
			position.Y += y;
		}

		public void Rotate(double deg)
		{
            degrees += deg;
		}

		public void Scale(double xscale, double yscale)
		{
			localScale.X += xscale;
			localScale.Y += yscale;
		}

		public void SetPosition(double x, double y)
		{
			Position = new Point(x, y);
		}

		public void SetPosition(Point point)
		{
			Position = point;
		}

		public override string ToString()
		{
			return $"X: {Position.X}; Y: {Position.Y};";
		}
	}
}
