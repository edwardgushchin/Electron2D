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
		static Settings()
		{
			resolution = new Rect(640, 480);
			fullscreen = false;
			VSinc = true;
			FPS = 0;
			Resizeble = false;
			DebugInfo = true;
			Smoothing = SmoothingType.Linear;
			Title = "Electron2D Game Engine 0.1 alpha";
		}

		private static Rect resolution;
		public static Rect Resolution
		{
			get { return resolution; }
			set
			{
				resolution = value;
				if(Game.WindowContext != IntPtr.Zero && resolution != value)
					SDL.SDL_SetWindowSize(Game.WindowContext, (int)resolution.Width, (int)resolution.Height);
			}
		}

		private static bool fullscreen;
		public static bool Fullscreen
		{
			get { return fullscreen; }
			set
			{
				if(Game.WindowContext != IntPtr.Zero)
				{
					if(value != fullscreen && value)
						SDL.SDL_SetWindowFullscreen(Game.WindowContext, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
					else if(value != fullscreen && !value)
						SDL.SDL_SetWindowFullscreen(Game.WindowContext, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
				}
				fullscreen = value;
			}
		}
		public static bool VSinc
		{
			get; set;
		}

		private static SmoothingType smoothing;
		public static SmoothingType Smoothing
		{
			get { return smoothing; }
			set
			{
				switch(value)
				{
					case SmoothingType.Nearest:
						SDL.SDL_SetHint( SDL.SDL_HINT_RENDER_SCALE_QUALITY, "0");
						break;
					case SmoothingType.Linear:
						SDL.SDL_SetHint( SDL.SDL_HINT_RENDER_SCALE_QUALITY, "1");
						break;
					case SmoothingType.Anisotropic:
						SDL.SDL_SetHint( SDL.SDL_HINT_RENDER_SCALE_QUALITY, "2");
						break;
				}
				smoothing = value;
			}
		}
		public static bool Resizeble { get; set; }
		public static uint FPS { get; set; }
		public static string Title { get; set; }
		public static bool DebugMode { get { return Debugger.IsAttached; } }
		public static bool DebugInfo { get; set; }
	}
}