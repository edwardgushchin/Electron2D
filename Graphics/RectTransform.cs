/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D.Graphics
{
	public class RectTransform
    {
        public RectTransform(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;

            Degrees = 0;
            Scale = new Point(1, 1);
            Achor = new Achors();
        }

        public Achors Achor;

        public struct Achors
        {
            public bool Top { get; set; }
            public bool Left { get; set; }
            public bool Right { get; set; }
            public bool Bottom { get; set; }

            public Achors(AchorsPreset Achor)
            {
                if (Achor == AchorsPreset.TopLeft)
                {
                    Top = true;
                    Left = true;
                    Right = false;
                    Bottom = false;
                }
                else if (Achor == AchorsPreset.TopRight)
                {
                    Top = true;
                    Left = false;
                    Right = true;
                    Bottom = false;
                }
                else if (Achor == AchorsPreset.BottomLeft)
                {
                    Top = false;
                    Left = true;
                    Right = false;
                    Bottom = true;
                }
                else if (Achor == AchorsPreset.BottomRight)
                {
                    Top = false;
                    Left = false;
                    Right = true;
                    Bottom = true;
                }
                else if (Achor == AchorsPreset.MiddleLeft)
                {
                    Top = true;
                    Left = true;
                    Right = false;
                    Bottom = true;
                }
                else if (Achor == AchorsPreset.MiddleRight)
                {
                    Top = true;
                    Left = false;
                    Right = true;
                    Bottom = true;
                }
                else if (Achor == AchorsPreset.MiddleTop)
                {
                    Top = true;
                    Left = true;
                    Right = true;
                    Bottom = false;
                }
                else if (Achor == AchorsPreset.MiddleBottom)
                {
                    Top = false;
                    Left = true;
                    Right = true;
                    Bottom = true;
                }
                else if (Achor == AchorsPreset.Center)
                {
                    Top = true;
                    Left = true;
                    Right = true;
                    Bottom = true;
                }
                else if (Achor == AchorsPreset.None)
                {
                    Top = false;
                    Left = false;
                    Right = false;
                    Bottom = false;
                }
                else
                {
                    Top = false;
                    Left = false;
                    Right = false;
                    Bottom = false;
                }
            }
        }

        public double Degrees { get; private set; }

		// disable once ConvertToAutoProperty
		public Point Scale
		{
            get; private set;
		}

        public void Rotate(double deg)
		{
			Degrees += deg;
		}

        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
    }
}