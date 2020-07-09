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
		static readonly Dictionary<Keyboard.Keys, bool> keyList;
		
		static Input()
		{
			keyList = new Dictionary<Keyboard.Keys, bool>();
			foreach (var key in Enum.GetValues(typeof(Keyboard.Keys))) {
				keyList[(Keyboard.Keys)key] = false;
			}
		}
		
		public static bool GetKeyDown(Keyboard.Keys key)
		{
			return keyList[key];
		}
		
		internal static void SetKeyDown(Keyboard.Keys key, bool value)
		{
			keyList[key] = value;
		}

		public static Point MousePosition
		{
			get
			{
				int x, y;
				SDL.SDL_GetMouseState(out x, out y);
				return Point.ConvertToDecartPoint(x, y);
			}
		}
	}
}
