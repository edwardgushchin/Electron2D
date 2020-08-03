/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

namespace Electron2D.DebugScene
{
    internal class SpritePositionDebugScene : Scene
    {
        //Sprite spaceShip;
        public SpritePositionDebugScene() : base()
		{
			ClearColor = new Color(46, 52, 64);
		}

        /*protected override void OnLoadScene()
		{
			Debug.Log("SpritePositionDebugScene debug scene loading...", Debug.Sender.Scene);

            ResourceManager.LoadTexture("spaceShips_005", @"Resources\Sprites\space_shooter_demo\Ships\spaceShips_005.png");
            spaceShip = new Sprite(ResourceManager.GetTexture("spaceShips_005"));
            spaceShip.Transform.LocalScale = new Vector { X = 0.5, Y = 0.5 };
            spaceShip.Debug = true;
            Debug.Log($"size={spaceShip.Size}");

			Debug.Log("SpritePositionDebugScene debug scene loaded.", Debug.Sender.Scene);
        }

        protected override void Update()
		{
            //Camera.Transform.Translate(50 * Time.DeltaTime, 0);
            //spaceShip.Transform.Degrees += 5 * Time.DeltaTime;

            var speed = 500 * Time.DeltaTime;
            var left = -Settings.Resolution.Width / 2 + spaceShip.Size.Height / 2;
            var top = Settings.Resolution.Height / 2 - spaceShip.Size.Width / 2;
            var bottom = -Settings.Resolution.Height / 2 + spaceShip.Size.Width / 2;

            if (Input.GetKeyDown(Keyboard.Keys.A))
            {
                var moveLeft = Math.Clamp(spaceShip.Transform.Position.X - speed, left, 0);
                spaceShip.Transform.Position = new Point(moveLeft, spaceShip.Transform.Position.Y);
            }

            if (Input.GetKeyDown(Keyboard.Keys.D))
            {
                var moveRight = Math.Clamp(spaceShip.Transform.Position.X + speed, left, 0);
                spaceShip.Transform.Position = new Point(moveRight, spaceShip.Transform.Position.Y);
            }

            if (Input.GetKeyDown(Keyboard.Keys.W))
            {
                var moveUp = Math.Clamp(spaceShip.Transform.Position.Y + speed, bottom, top);
                spaceShip.Transform.Position = new Point(spaceShip.Transform.Position.X, moveUp);
            }

            if (Input.GetKeyDown(Keyboard.Keys.S))
            {
                var moveBottom = Math.Clamp(spaceShip.Transform.Position.Y - speed, bottom, top);
                spaceShip.Transform.Position = new Point(spaceShip.Transform.Position.X, moveBottom);
            }

            spaceShip.Transform.Degrees = GetAngle() - 90;
            //spaceShip.Draw();
		}

        private double GetAngle()
        {
            return -Math.Atan2(Input.MousePosition.Y - spaceShip.Transform.Position.Y, Input.MousePosition.X - spaceShip.Transform.Position.X) / Math.PI * 180;
        }

		protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();

			if(e.Key == Keyboard.Keys.F11)
				Settings.Fullscreen = !Settings.Fullscreen;
		}*/
    }
}