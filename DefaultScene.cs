/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/
using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

namespace Electron2D
{
    internal class DefaultScene : Scene
    {
		private readonly Sprite _sprite1, _sprite2;
		public DefaultScene()
		{
			Debug.Log("Electron2D demo scene loading...", Debug.Sender.Scene);
			ClearColor = new Color(46, 52, 64);

            //_sprite1 = new Sprite(ResourceManager.LoadTexture("_sprite1", @"Resources\Sprites\platformer\PNG\Tiles\boxExplosive.png"), new Point(), 0);
			_sprite2 = new Sprite(ResourceManager.LoadTexture("_sprite2", @"Resources\Sprites\platformer\PNG\Tiles\boxCrate_single.png"), new Point(), 1);

			Debug.Log("Electron2D demo scene loaded.", Debug.Sender.Scene);
        }

        protected override void Update()
		{
			if(Input.GetKeyDown(Keyboard.Keys.F))
                Settings.Fullscreen = !Settings.Fullscreen;

			if (Input.GetKeyDown(Keyboard.Keys.W))
                Camera.Transform.TranslateY(Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.A))
                Camera.Transform.TranslateX(-Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.S))
                Camera.Transform.TranslateY(-Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.D))
                Camera.Transform.TranslateX(Camera.Size * Time.DeltaTime);

			if (Input.GetKeyDown(Keyboard.Keys.Left))
				_sprite2.Transform.TranslateX(-2 * Time.DeltaTime);

			if (Input.GetKeyDown(Keyboard.Keys.Right))
				_sprite2.Transform.TranslateX(2 * Time.DeltaTime);

			if (Input.GetKeyDown(Keyboard.Keys.Up))
				_sprite2.Transform.TranslateY(2 * Time.DeltaTime);

			if (Input.GetKeyDown(Keyboard.Keys.Down))
				_sprite2.Transform.TranslateY(-2 * Time.DeltaTime);

			if (Input.GetKeyDown(Keyboard.Keys.Num4))
				_sprite2.Transform.Rotate(-30 * Time.DeltaTime);

			if (Input.GetKeyDown(Keyboard.Keys.Num6))
				_sprite2.Transform.Rotate(30 * Time.DeltaTime);


			Debug.DrawGrid();
		}

		protected override void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			Debug.Log($"World: {Camera.ConvertWorldToScreen(Input.MousePosition)}, Screen: {Input.MousePosition}");
		}

		protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();

			if(e.Key == Keyboard.Keys.Equals)
			{
				_sprite2.Layer++;
				//Debug.Log(_sprite2.Layer.ToString());
			}

			if(e.Key == Keyboard.Keys.Minus)
			{
				_sprite2.Layer--;
				//Debug.Log(_sprite2.Layer.ToString());
			}

			//Debug.Log(e.Key.ToString());
		}

		protected override void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if(e.Wheel == Mouse.Wheel.Down) Camera.Size++;

			if(e.Wheel == Mouse.Wheel.Up && Camera.Size > 1) Camera.Size--;

			Debug.Log($"Camera Size: {Camera.Size}");
		}
    }
}