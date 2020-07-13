/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

namespace Electron2D
{
    internal class DefaultScene : Scene
    {
		public DefaultScene(int Index) : base(Index)
		{
			Debug.Log("Electron2D demo scene loading...", Debug.Sender.Scene);
			ClearColor = new Color(46, 52, 64);
		}

        protected override void OnLoadScene()
		{
			Debug.Log("Electron2D demo scene loaded.", Debug.Sender.Scene);
        }

        protected override void Update(double deltaTime){}

		protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();

			if(e.Key == Keyboard.Keys.F)
                Kernel.Settings.Fullscreen = !Kernel.Settings.Fullscreen;
		}
    }
}