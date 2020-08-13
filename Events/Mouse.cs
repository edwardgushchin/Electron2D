/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Inputs;
using Electron2D.Graphics;

namespace Electron2D.Events
{
	/// <summary>
	/// Description of Mouse.
	/// </summary>
	public delegate void MouseButtonEventHundler(object sender, MouseButtonEventArgs e);

	public class MouseButtonEventArgs : EventArgs
	{
		public MouseButtonEventArgs(Mouse.Button button, Point position, byte click)
		{
			Button = button;
			//Position = position.ConvertToDecartPoint();
			Position = SceneManager.GetCurrentScene.Camera.ConvertWorldToScreen(position);
			Click = click;
		}

		public Point Position { get; private set; }

        public Mouse.Button Button { get; }

        public byte Click { get; }
    }
	public delegate void MouseMotionEventHundler(object sender, MouseMotionEventArgs e);

	public class MouseMotionEventArgs : EventArgs
	{
		public MouseMotionEventArgs(Point position, int xrel, int yrel)
		{
			Position = SceneManager.GetCurrentScene.Camera.ConvertWorldToScreen(position);
			XRel = xrel;
			YRel = yrel;
		}

		public Point Position { get; private set; }

        public int XRel { get; }
        public int YRel { get; }
    }

	public delegate void MouseWheelEventHundler(object sender, MouseWheelEventArgs e );

	public class MouseWheelEventArgs : EventArgs
	{
		public MouseWheelEventArgs(Point whell)
		{
			if (whell.X > 0) Wheel = Mouse.Wheel.Right;
			else if (whell.X < 0) Wheel = Mouse.Wheel.Left;
			else if (whell.Y > 0) Wheel = Mouse.Wheel.Up;
			else if (whell.Y < 0) Wheel = Mouse.Wheel.Down;
		}

        public Mouse.Wheel Wheel { get; }
    }
}
