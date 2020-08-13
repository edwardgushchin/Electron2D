/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Graphics;

namespace Electron2D.Events
{
	public delegate void WindowEventHundler(object sender, WindowEventArgs e);

	public class WindowEventArgs : EventArgs
	{
		public WindowEventArgs()
		{
			NewSize = new Rect();
			NewPosition = new Point();
		}

		public WindowEventArgs(Rect newSize)
		{
			NewSize = newSize;
			NewPosition = new Point();
		}

		public WindowEventArgs(Point newPosition)
		{
			NewSize = new Rect();
			NewPosition = newPosition;
		}

		/// <summary>
		/// Новый размер окна
		/// </summary>
		public Rect NewSize { get; private set; }

		/// <summary>
		/// Новая позиция окна
		/// </summary>
		public Point NewPosition { get; private set; }
	}
}
