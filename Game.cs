/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Kernel;
using Electron2D.Events;
using Electron2D.Binding.SDL;
using Electron2D.Graphics;

namespace Electron2D
{
    public sealed class Game
    {
		private static Render render;

        public Game(string Title)
        {
			Settings.Title = Title;

			if(Settings.DebugMode) Console.Title = Settings.Title;

			Debug.Message("Electron2D Game Engine version 0.1");
			Debug.NewLine();
			Debug.Message("Copyright © 2019-2020 Edward Gushchin");
			Debug.Message("Licensed under the Apache License, Version 2.0");
			Debug.NewLine();

			if(Settings.DebugMode)
			{
				Debug.Log("The Electron2D game engine is running in DEBUG mode.", Debug.Sender.Main, Debug.MessageStatus.Warning);
				Debug.NewLine();
			}

			if(!Initialization()) Exit();
        }

		private bool Initialization()
		{
			Debug.Log("Initialization of game engine subsystems...", Debug.Sender.Main);

			Events = new EventPool();
			if(!Events.Initialized) return false;

			render = new Render();
			if(!render.Initialized) return false;

			SceneManager.SetGameContext(this);
			SubscriabeEvents();

			Debug.Log("All subsystems of the game engine have been successfully initialized!", Debug.Sender.Main, Debug.MessageStatus.Success);
			return true;
		}

		public Render Render { get { return render; } }

        internal EventPool Events { get; private set; }

        internal static IntPtr RenderContext => render != null ? render.RenderContext : IntPtr.Zero;
        internal static IntPtr WindowContext
        {
            get => render != null ? render.WindowContext : IntPtr.Zero;
            set => render.WindowContext = value;
        }

        public void Play(Scene scene)
		{
			SceneManager.AddScene(scene);
			SceneManager.LoadScene(0);
			render.Start();
		}

		public void Play(int SceneID)
		{
			SceneManager.LoadScene(SceneID);
			render.Start();
		}

		public void Play()
		{
            if (!SceneManager.LoadScene(0))
            {
                SceneManager.AddScene(new DefaultScene(0));
                SceneManager.LoadScene(0);
            }
            render.Start();
        }

		public void SetIcon(string Path)
		{
			var icon = Image.IMG_Load(Path);
			SDL.SDL_SetWindowIcon(WindowContext, icon);
		}

		public void Exit()
		{
			DescribeEvents();
			render.Stop();
			render.Dispose();
			SDL.SDL_Quit();
		}

		private void SubscriabeEvents()
		{
			render.OnPreUpdate += GameUpdate;
		}

		private void DescribeEvents()
		{
			render.OnPreUpdate -= GameUpdate;
		}

		private void GameUpdate()
		{
			Events.Pool();
		}
    }
}