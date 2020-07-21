/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

namespace Electron2D.DebugScene
{
    internal class SpritePositionDebugScene : Scene
    {
        Sprite spaceShip;
        public SpritePositionDebugScene() : base()
		{
			ClearColor = new Color(46, 52, 64);
		}

        protected override void OnLoadScene()
		{
			Debug.Log("SpritePositionDebugScene debug scene loading...", Debug.Sender.Scene);

            ResourceManager.LoadSprite("spaceShips_005", @"Resources\Sprites\space_shooter_demo\Ships\spaceShips_005.png");
            spaceShip = ResourceManager.GetSprite("spaceShips_005");
            spaceShip.Transform.LocalScale = new Vector { X = 0.5, Y = 0.5 };
            spaceShip.Transform.SetPosition(0,0);
            Debug.Log($"size={spaceShip.Size}");

			Debug.Log("SpritePositionDebugScene debug scene loaded.", Debug.Sender.Scene);
        }

        protected override void Update()
		{
            Camera.Transform.Translate(50 * Time.DeltaTime, 0);
            spaceShip.Transform.Degrees += 5 * Time.DeltaTime;
            spaceShip.Draw();
		}

		protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();

			if(e.Key == Keyboard.Keys.F11)
				Settings.Fullscreen = !Settings.Fullscreen;
		}
    }
}