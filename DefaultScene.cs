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
        private readonly Animator _playerAnimator;
        private readonly Animation _playerWalkAnimation, _playerIdleAnimation;
        public DefaultScene()
        {
            Debug.Log("Electron2D demo scene loading...", Debug.Sender.Scene);
            ClearColor = new Color(46, 52, 64);

            var atlas = ResourceManager.LoadTextureAtlas("spritesheet_complete", @"Resources\Sprites\platformer\Spritesheets\spritesheet_players.xml");

            _playerAnimator = new Animator();

            _playerWalkAnimation = new Animation("walk", 25);
            _playerWalkAnimation.AddSprite(atlas.Sprite["alienBeige_walk1"]);
            _playerWalkAnimation.AddSprite(atlas.Sprite["alienBeige_walk2"]);

			//_playerIdleAnimation = new Animation("idle", 30);
			//_playerIdleAnimation.AddSprite(atlas.Sprite["alienBeige_front"]);

            //_sprite1 = new Sprite(ResourceManager.LoadTexture("_sprite1", @"Resources\Sprites\platformer\PNG\Tiles\boxExplosive.png"), Point.Zero, 0);
            //_sprite2 = new Sprite(ResourceManager.LoadTexture("_sprite2", @"Resources\Sprites\platformer\PNG\Tiles\boxCrate_single.png"), Point.Zero, 1);

            _playerAnimator.Add(_playerWalkAnimation);
			//_playerAnimator.Add(_playerIdleAnimation);
            ////_alienBeige_walk1 = atlas1.Sprite["alienBeige_walk2"];
            //_alienBeige_walk2 = atlas1.Sprite["alienBeige_fron2"];

            Debug.Log("Electron2D demo scene loaded.", Debug.Sender.Scene);
        }

        protected override void OnLoadScene()
        {

        }

        protected override void Update()
        {
            if (Input.GetKeyDown(Keyboard.Keys.F))
                Settings.Fullscreen = !Settings.Fullscreen;

            if (Input.GetKeyDown(Keyboard.Keys.W))
                Camera.Transform.TranslateY(Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.A))
                Camera.Transform.TranslateX(-Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.S))
                Camera.Transform.TranslateY(-Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.D))
                Camera.Transform.TranslateX(Camera.Size * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.Left))
                //_sprite2.Transform.TranslateX(-2 * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.Right))
			_playerAnimator.Play("walk");
			//Debug.Log("asd");
			//else _playerAnimator.Play("idle");
            //_sprite2.Transform.TranslateX(2 * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.Up))
                //_sprite2.Transform.TranslateY(2 * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.Down))
                //_sprite2.Transform.TranslateY(-2 * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.Num4))
                //_sprite2.Transform.Rotate(30 * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.Num6))
                //_sprite2.Transform.Rotate(-30 * Time.DeltaTime);

            //Debug.DrawGrid();

            //Debug.Log($"Draw Calls: {Profiler.DrawCalls}, Texture Cache: {Profiler.TextureCache}, Sprite Cache: {Profiler.SpriteCache}, Time: {(Time.DeltaTime * 1000).ToString("0.00")}ms, FPS: {(int)(1.0f/Time.DeltaTime)}");
        }

        protected override void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.Log($"Screen: {Input.ScreenMousePosition}, World: {Input.WorldMousePosition}");
        }

        protected override void OnKeyDown(object sender, KeyboardEventArgs e)
        {
            if (e.Key == Keyboard.Keys.Escape)
                SceneManager.ExitGame();

            if (e.Key == Keyboard.Keys.Equals)
            {
                //_sprite2.Layer++;
                //Debug.Log(_sprite2.Layer.ToString());
            }

            if (e.Key == Keyboard.Keys.Minus)
            {
                //_sprite2.Layer--;
                //Debug.Log(_sprite2.Layer.ToString());
            }

            //Debug.Log(e.Key.ToString());
        }

        protected override void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Wheel == Mouse.Wheel.Down) Camera.Size++;

            if (e.Wheel == Mouse.Wheel.Up && Camera.Size > 1) Camera.Size--;

            //Debug.Log($"Camera Size: {Camera.Size}");
        }
    }
}