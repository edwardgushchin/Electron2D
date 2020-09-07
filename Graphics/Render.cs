/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Kernel;
using Electron2D.Events;
using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public class Render : IDisposable
    {
        private bool _loop;
        private IntPtr _window, _instance;
        public event RenderEventHundler OnPreUpdate;
	    public event RenderEventHundler OnUpdate;
	    public event RenderEventHundler OnPostUpdate;

        public Render()
		{
			_loop = false;

            var sdl = SDLInit();
            var image = ImageInit();
			var font = FontInit();

            if(sdl && image && font)
            {
                CreateWidnow();
			    CreateRenderer();
			    Initialized = true;
                Debug.Log("Initialization of the graphics rendering subsystem completed successfully.",  Debug.Sender.Render);
            }
            else
            {
                Initialized = false;
            }
		}

        private bool SDLInit()
        {
			//SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_HIGHDPI_DISABLED, "2");

			if(SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) != 0)
            {
                Debug.Log("An error occurred while initializing the \"SDL module\": " + SDL.SDL_GetError(), Debug.Sender.Render, Debug.MessageStatus.Error);
                return false;
            }
            return true;
        }

        private bool ImageInit()
        {
            if(Image.IMG_Init( Image.IMG_InitFlags.IMG_INIT_PNG ) != 2)
			{
				Debug.Log("An error occurred while initializing the \"Image module\": " + SDL.SDL_GetError(), Debug.Sender.Render, Debug.MessageStatus.Error);
                return false;
			}
            return true;
        }

		private bool FontInit()
		{
			if(TTFont.TTF_Init() == -1)
			{
				Debug.Log("An error occurred while initializing the \"TTF module\": " + SDL.SDL_GetError(), Debug.Sender.Render, Debug.MessageStatus.Error);
				return false;
			}
			return true;
		}

        internal IntPtr RenderContext => Initialized ? _instance : IntPtr.Zero;

        internal IntPtr WindowContext
        {
            get => Initialized ? _window : IntPtr.Zero;
            set => _window = value;
        }

        public bool Initialized { get; }

        public void Start()
		{
			_loop = true;
			RenderLoop();
			Stop();
		}

		public void Stop()
		{
			_loop = false;
			Dispose();
		}

		private void CreateWidnow()
		{
			var windowFlags = (SDL.SDL_WindowFlags)Settings.Fullscreen;

			if (Settings.Resizeble)
				windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;

			_window = SDL.SDL_CreateWindow(
				Settings.DebugMode ? $"{Settings.Title} [DEBUG]" : Settings.Title,
				SDL.SDL_WINDOWPOS_CENTERED,
				SDL.SDL_WINDOWPOS_CENTERED,
				(int)Settings.Resolution.Width,
				(int)Settings.Resolution.Height,
				windowFlags
			);
		}

        private void CreateRenderer()
		{
			var renderFlags = SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_TARGETTEXTURE;

			if(Settings.VSinc)
				renderFlags |= SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;

			_instance = SDL.SDL_CreateRenderer(_window, -1, renderFlags);
		}

        private void RenderLoop()
		{
			bool cap = Settings.FPS > 0;
		    var timer_fps = new Timer();

		    ulong now_counter = SDL.SDL_GetPerformanceCounter();

            while (_loop)
			{
				timer_fps.Start();

                var last_counter = now_counter;
                now_counter = SDL.SDL_GetPerformanceCounter();

				var deltaTime = (double)(now_counter - last_counter) / SDL.SDL_GetPerformanceFrequency();

				Time.DeltaTime = (float)deltaTime;

				Debug.Log($"DeltaTime: {Time.DeltaTime}");

                SDL.SDL_RenderClear(_instance);

				OnPreUpdate();
				OnUpdate();

				SpriteRenderer.Update();

				SDL.SDL_RenderPresent(_instance);

				OnPostUpdate();

				if(cap && ( timer_fps.GetTicks() < 1000 / Settings.FPS ) )
		        {
					SDL.SDL_Delay( ( 1000 / Settings.FPS  ) - timer_fps.GetTicks() );
		        }
			}
		}

        public void Dispose()
		{
			SDL.SDL_DestroyRenderer(_instance);
			SDL.SDL_DestroyWindow(_window);
		}
    }
}