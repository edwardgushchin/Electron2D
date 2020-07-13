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
        private bool renderLoop;
        private IntPtr renderWindow;
		private IntPtr renderer;
        public event RenderEventHundler OnPreUpdate;
	    public event RenderEventHundler OnUpdate;
	    public event RenderEventHundler OnPostUpdate;

        public Render()
		{
			renderLoop = false;

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
			SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_HIGHDPI_DISABLED, "2");

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

        internal IntPtr RenderContext
		{
			get
			{
				return Initialized ? renderer : IntPtr.Zero;
			}
		}

		internal IntPtr WindowContext
		{
			get
			{
				return Initialized ? renderWindow : IntPtr.Zero;
			}
			set
			{
				renderWindow = value;
			}
		}

        public bool Initialized { get; }

        public void Start()
		{
			renderLoop = true;
			RenderLoop();
			Stop();
		}

		public void Stop()
		{
			renderLoop = false;
			Dispose();
		}

		//public void Clear(Color Color)
		//{
		//	SDL.SDL_SetRenderDrawColor(renderer, Color.R, Color.G, Color.B, Color.A);
		//	SDL.SDL_RenderClear(renderer);
		//}

		private void CreateWidnow()
		{
			var windowFlags = Settings.Fullscreen ?
				SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN : Settings.Resizeble ?
				SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE : SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN;

			renderWindow = SDL.SDL_CreateWindow(

					Settings.DebugMode ? $"{Settings.Title} [DEBUG]" : Settings.Title, SDL.SDL_WINDOWPOS_CENTERED,
					SDL.SDL_WINDOWPOS_CENTERED,
					Settings.Resolution.Width,
					Settings.Resolution.Height,
					windowFlags);
		}

        private void CreateRenderer()
		{
			if(Settings.VSinc) {
				renderer = SDL.SDL_CreateRenderer(
					renderWindow, -1,
					SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC |
					SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
					SDL.SDL_RendererFlags.SDL_RENDERER_TARGETTEXTURE
				);
			}
			else
			{
				renderer = SDL.SDL_CreateRenderer(
					renderWindow, -1,
					SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
					SDL.SDL_RendererFlags.SDL_RENDERER_TARGETTEXTURE
				);
			}
		}

        private void RenderLoop()
		{
			bool cap = Settings.FPS > 0;
		    var timer_fps = new Timer();

		    ulong now_counter = SDL.SDL_GetPerformanceCounter();

            while (renderLoop)
			{
				timer_fps.Start();

                ulong last_counter = now_counter;
                now_counter = SDL.SDL_GetPerformanceCounter();

				Time.DeltaTime = (double)(now_counter - last_counter) / SDL.SDL_GetPerformanceFrequency();

                SDL.SDL_RenderClear(renderer);

				OnPreUpdate();
				OnUpdate();

				SDL.SDL_RenderPresent(renderer);

				OnPostUpdate();

				if(cap && ( timer_fps.GetTicks() < 1000 / Settings.FPS ) )
		        {
					SDL.SDL_Delay( ( 1000 / Settings.FPS  ) - timer_fps.GetTicks() );
		        }
			}
		}

        public void Dispose()
		{
			SDL.SDL_DestroyRenderer(renderer);
			SDL.SDL_DestroyWindow(renderWindow);
		}
    }
}