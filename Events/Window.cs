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
			NewSize = new Size();
			NewPosition = new Point();
		}

		public WindowEventArgs(Size newSize)
		{
			NewSize = newSize;
			NewPosition = new Point();
		}

		public WindowEventArgs(Point newPosition)
		{
			NewSize = new Size();
			NewPosition = newPosition;
		}

		/// <summary>
		/// Новый размер окна
		/// </summary>
		public Size NewSize { get; private set; }

		/// <summary>
		/// Новая позиция окна
		/// </summary>
		public Point NewPosition { get; private set; }
	}
}
