/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;

using Electron2D.Graphics;
using Electron2D.Binding.SDL;

namespace Electron2D.DebugScene
{
    internal class MouseCursorTouchDebugScene : Scene
    {	
		public MouseCursorTouchDebugScene() : base()
		{
			Debug.Log("MouseCursorTouch debug scene loading...", Debug.Sender.Scene);
			ClearColor = new Color(46, 52, 64);
		}

        protected override void OnLoadScene()
		{
			SetCursor(ResourceManager.LoadSprite(@"Resources\cursor.png"));

			Debug.Log("MouseCursorTouch debug scene loaded.", Debug.Sender.Scene);
        }

        protected override void Update(double deltaTime)
		{
			
		}
		
		protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{			
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();

			if(e.Key == Keyboard.Keys.F11) 
				Kernel.Settings.Fullscreen = Kernel.Settings.Fullscreen ? false : true;
		}
    }
}