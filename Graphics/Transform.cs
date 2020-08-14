/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D.Graphics
{
	public class Transform
	{
		private Point _position;
		private Vector _localScale;

		public Transform()
		{
			_position = new Point();
            Achor = new Point(0.5, 0.5);
            Degrees = 0;
			_localScale = new Vector(1, 1);
		}

		public Transform(Point pos)
		{
			_position = pos;
            Achor = new Point(0.5, 0.5);
            Degrees = 0;
			_localScale = new Vector(1, 1);
		}

		public Point Position
		{
			get { return _position; }
			set { _position = value; }
		}

        public double Degrees { get; set; }

        public Vector LocalScale
		{
			get { return _localScale; }
			set { _localScale = value; }
		}

        public Point Achor { get; set; }

        public void Translate(double x, double y)
		{
			_position.X += x;
			_position.Y += y;
		}

		public void TranslateX(double x) => _position.X += x;

		public void TranslateY(double y) => _position.Y += y;

		public void Rotate(double angle)
		{
            Degrees += angle;
		}

		public void Scale(double xscale, double yscale)
		{
			_localScale.X += xscale;
			_localScale.Y += yscale;
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
