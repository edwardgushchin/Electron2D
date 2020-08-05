/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Collections.Generic;

using Electron2D.Inputs;
using Electron2D.Graphics;
using Electron2D.Binding.SDL;

namespace Electron2D
{
	public static class Input
	{
		private static readonly Dictionary<Keyboard.Keys, bool> keyList;
		private static readonly Dictionary<Mouse.Button, bool> mouseList;

		static Input()
		{
			keyList = new Dictionary<Keyboard.Keys, bool>();
			mouseList = new Dictionary<Mouse.Button, bool>();

			foreach (var key in Enum.GetValues(typeof(Keyboard.Keys))) {
				keyList[(Keyboard.Keys)key] = false;
			}

			foreach (var button in Enum.GetValues(typeof(Mouse.Button))) {
				mouseList[(Mouse.Button)button] = false;
			}
		}

		public static bool GetKeyDown(Keyboard.Keys key)
		{
			return keyList[key];
		}

		public static bool GetMouseButtonDown(Mouse.Button button)
		{
			return mouseList[button];
		}

		internal static void SetKeyDown(Keyboard.Keys key, bool value)
		{
			keyList[key] = value;
		}

		internal static void SetMouseButtonDown(Mouse.Button button, bool value)
		{
			mouseList[button] = value;
		}

		public static Point MousePosition
		{
			get
			{
                SDL.SDL_GetMouseState(out int x, out int y);
                return new Point(x, y).ConvertToDecartPoint();
			}
		}
	}
}
