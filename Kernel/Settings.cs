/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Diagnostics;

using Electron2D.Graphics;
using Electron2D.Binding.SDL;

namespace Electron2D.Kernel
{
	public static class Settings
	{
		private static Rect _resolution;
		private static bool _fullscreen;
		private static SmoothingType _smoothing;

		static Settings()
		{
			_resolution = new Rect(640, 480);
			_fullscreen = false;
			VSinc = true;
			FPS = 0;
			Resizeble = false;
			DebugInfo = true;
			Smoothing = SmoothingType.Linear;
			Title = "Electron2D Game Engine 0.1 alpha";
		}

		public static Rect Resolution
		{
			get { return _resolution; }
			set
			{
				_resolution = value;
				if(Game.WindowContext != IntPtr.Zero && _resolution != value)
					SDL.SDL_SetWindowSize(Game.WindowContext, (int)_resolution.Width, (int)_resolution.Height);
			}
		}

		public static bool Fullscreen
		{
			get { return _fullscreen; }
			set
			{
				if(Game.WindowContext != IntPtr.Zero)
				{
					if(value != _fullscreen && value)
						SDL.SDL_SetWindowFullscreen(Game.WindowContext, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
					else if(value != _fullscreen && !value)
						SDL.SDL_SetWindowFullscreen(Game.WindowContext, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
				}
				_fullscreen = value;
			}
		}
		public static bool VSinc
		{
			get; set;
		}

		public static SmoothingType Smoothing
		{
			get { return _smoothing; }
			set
			{
				switch(value)
				{
					case SmoothingType.Nearest:
						SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "nearest");
						break;
					case SmoothingType.Linear:
						SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "linear");
						break;
					case SmoothingType.Anisotropic:
						SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "best");
						break;
				}
				_smoothing = value;
			}
		}
		public static bool Resizeble { get; set; }
		public static uint FPS { get; set; }
		public static string Title { get; set; }
		public static bool DebugMode { get { return Debugger.IsAttached; } }
		public static bool DebugInfo { get; set; }
	}
}