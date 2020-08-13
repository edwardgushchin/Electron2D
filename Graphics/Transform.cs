/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D.Graphics
{
	public class Transform
	{
		private Point position;
		private Vector localScale;

		public Transform()
		{
			position = new Point();
            Achor = new Point(0.5, 0.5);
            Degrees = 0;
			localScale = new Vector(1, 1);
		}

		public Transform(Point pos)
		{
			position = pos;
            Achor = new Point(0.5, 0.5);
            Degrees = 0;
			localScale = new Vector(1, 1);
		}

		public Point Position
		{
			get { return position; }
			set { position = value; }
		}

        public double Degrees { get; set; }

        public Vector LocalScale
		{
			get { return localScale; }
			set { localScale = value; }
		}

        public Point Achor { get; set; }

        public void Translate(double x, double y)
		{
			position.X += x;
			position.Y += y;
		}

		public void TranslateX(double x) => position.X += x;

		public void TranslateY(double y) => position.Y += y;

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
			Position = new Point(x, y);
		}

		public void SetPosition(Point point)
		{
			Position = point;
		}

		public override string ToString()
		{
			return $"X: {Position.X}; Y: {Position.Y}; Angle: {Degrees};";
		}
	}
}
