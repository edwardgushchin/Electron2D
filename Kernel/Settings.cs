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
			resolution = new Size(640, 480);
			fullscreen = false;
			VSinc = true;
			FPS = 0;
			Resizeble = false;
			DebugInfo = true;
			Title = "Electron2D Game Engine 0.1";
		}
		
		static Size resolution;
		public static Size Resolution 
		{ 
			get { return resolution; }
			set
			{
				
				resolution = value;
				if(Game.WindowContext != IntPtr.Zero && resolution != value)
					SDL.SDL_SetWindowSize(Game.WindowContext, resolution.Width, resolution.Height);
			}
		}

		static bool fullscreen;
		public static bool Fullscreen 
		{ 
			get { return fullscreen; }
			set
			{
				if(Game.WindowContext != IntPtr.Zero)
				{
					if(value != fullscreen && value == true)
						SDL.SDL_SetWindowFullscreen(Game.WindowContext, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
					else if(value != fullscreen && value == false)
						SDL.SDL_SetWindowFullscreen(Game.WindowContext, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
				}	
				fullscreen = value;
				
			}
		}
		public static bool VSinc 
		{ 
			get; set;
		}
		public static bool Resizeble { get; set; }
		public static uint FPS { get; set; }
		public static string Title { get; set; }
		public static bool DebugMode { get { return Debugger.IsAttached; } }

		public static bool DebugInfo { get; set; }
	}
}