/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

namespace Electron2D
{
    internal class DefaultScene : Scene
    {
        private readonly Animator _playerAnimator, _oposumAnimator, _eagleAnimator;
        private readonly Animation _playerRunAnimation, _playerIdleAnimation, _playerCrouchAnimation, _oposumWalkAnimation, _eagleAnimation;

        private readonly Sprite _middleLayer1, _middleLayer2, _ground_middle, _dirt_middle, _house, _tree, _bush;
        private readonly SpriteSheet _tiles;
        private bool _flipX, _oposum_flipX;
        public DefaultScene()
        {
            Debug.Log("Electron2D demo scene loading...", Debug.Sender.Scene);

            ClearColor = new Color(46, 52, 64);
            Camera.Size = 5;
            _oposum_flipX = false;

            new Sprite(ResourceManager.LoadTexture("background", @"Resources\Sprites\sannyland\back.png"), Point.Zero, 0, 15);
            new Sprite(ResourceManager.LoadTexture("rock", @"Resources\Sprites\sannyland\rock.png"), new Point(1.3, -3), 5, 15);
            new Sprite(ResourceManager.LoadTexture("shrooms", @"Resources\Sprites\sannyland\shrooms.png"), new Point(2, -3), 6, 15);
            new Sprite(ResourceManager.LoadTexture("sign", @"Resources\Sprites\sannyland\sign.png"), new Point(-3.5, -2.82), 6, 15);
            _tiles = ResourceManager.LoadTextureAtlas("tileset", @"Resources\Sprites\sannyland\tileset.xml", 15);
            _house = new Sprite(ResourceManager.LoadTexture("house", @"Resources\Sprites\sannyland\house.png"), new Point(7,0.1), 5, 15);
            _tree = new Sprite(ResourceManager.LoadTexture("tree", @"Resources\Sprites\sannyland\tree.png"), new Point(3,-0.4), 4, 15);
            _bush = new Sprite(ResourceManager.LoadTexture("bush", @"Resources\Sprites\sannyland\bush.png"), new Point(-7,-2.57), 4, 15);

            var middle = ResourceManager.LoadTexture("middle", @"Resources\Sprites\sannyland\middle.png");

            _middleLayer1 = new Sprite(middle, new Point(-4, -11), 1, 15);
            _middleLayer2 = new Sprite(middle, new Point(_middleLayer1.Transform.Position.X + _middleLayer1.Size.Width, -11), 1, 15);

            _ground_middle = _tiles.Sprite["ground_middle"];
            _dirt_middle = _tiles.Sprite["dirt_middle"];

            _playerAnimator = new Animator(new Point(-5,-2.5), 10, 15);
            _oposumAnimator = new Animator(new Point(5, -2.55), 9, 14);
            _eagleAnimator = new Animator(new Point(20, 5), 9, 15);

            _playerIdleAnimation = new Animation("idle", 10);
            _playerIdleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("idle_1", @"Resources\Sprites\sannyland\player\idle\player-idle-1.png")));
            _playerIdleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("idle_2", @"Resources\Sprites\sannyland\player\idle\player-idle-2.png")));
            _playerIdleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("idle_3", @"Resources\Sprites\sannyland\player\idle\player-idle-3.png")));
            _playerIdleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("idle_4", @"Resources\Sprites\sannyland\player\idle\player-idle-4.png")));

            _eagleAnimation = new Animation("run", 10);
            _eagleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("eagle_run_1", @"Resources\Sprites\sannyland\eagle\eagle-attack-1.png")));
            _eagleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("eagle_run_2", @"Resources\Sprites\sannyland\eagle\eagle-attack-2.png")));
            _eagleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("eagle_run_3", @"Resources\Sprites\sannyland\eagle\eagle-attack-3.png")));
            _eagleAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("eagle_run_4", @"Resources\Sprites\sannyland\eagle\eagle-attack-4.png")));

            _playerRunAnimation = new Animation("run", 10);
            _playerRunAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("run_1", @"Resources\Sprites\sannyland\player\run\player-run-1.png")));
            _playerRunAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("run_2", @"Resources\Sprites\sannyland\player\run\player-run-2.png")));
            _playerRunAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("run_3", @"Resources\Sprites\sannyland\player\run\player-run-3.png")));
            _playerRunAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("run_4", @"Resources\Sprites\sannyland\player\run\player-run-4.png")));
            _playerRunAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("run_5", @"Resources\Sprites\sannyland\player\run\player-run-5.png")));
            _playerRunAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("run_6", @"Resources\Sprites\sannyland\player\run\player-run-6.png")));

            _playerCrouchAnimation = new Animation("crouch", 5);
            _playerCrouchAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("crouch_1", @"Resources\Sprites\sannyland\player\crouch\player-crouch-1.png")));
            _playerCrouchAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("crouch_2", @"Resources\Sprites\sannyland\player\crouch\player-crouch-2.png")));

            _oposumWalkAnimation = new Animation("walk", 5);
            _oposumWalkAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("opossum_1", @"Resources\Sprites\sannyland\opossum\opossum-1.png")));
            _oposumWalkAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("opossum_2", @"Resources\Sprites\sannyland\opossum\opossum-2.png")));
            _oposumWalkAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("opossum_3", @"Resources\Sprites\sannyland\opossum\opossum-3.png")));
            _oposumWalkAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("opossum_4", @"Resources\Sprites\sannyland\opossum\opossum-4.png")));
            _oposumWalkAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("opossum_5", @"Resources\Sprites\sannyland\opossum\opossum-5.png")));
            _oposumWalkAnimation.AddSprite(new Sprite(ResourceManager.LoadTexture("opossum_6", @"Resources\Sprites\sannyland\opossum\opossum-6.png")));

            _playerAnimator.Add(_playerIdleAnimation);
            _playerAnimator.Add(_playerRunAnimation);
            _playerAnimator.Add(_playerCrouchAnimation);
            _eagleAnimator.Add(_eagleAnimation);

            _oposumAnimator.Add(_oposumWalkAnimation);

            Debug.Log("Electron2D demo scene loaded.", Debug.Sender.Scene);
        }

        protected override void OnLoadScene()
        {
            var grounds = new System.Collections.Generic.List<Sprite>();

            for (double i = -12; i <= 13; i++)
            {
                var sprite1 = new Sprite(_ground_middle.Texture, _ground_middle.PackageBounds, 2, 15);
                var sprite2 = new Sprite(_dirt_middle.Texture, _dirt_middle.PackageBounds, 2, 15);

                sprite1.Transform.Position = new Point(i, -4);
                sprite1.Visible = true;

                sprite2.Transform.Position = new Point(i, -5);
                sprite2.Visible = true;

                grounds.Add(sprite1);
                grounds.Add(sprite2);
            }
        }

        protected override void PreUpdate()
        {
            //s1.Transform.Position = new Point(-5, -4);
        }

        protected override void Update()
        {
            //if (Input.GetKeyDown(Keyboard.Keys.F))
            //    Settings.Fullscreen = !Settings.Fullscreen;

            //if (Input.GetKeyDown(Keyboard.Keys.W))
            //    Camera.Transform.TranslateY(Camera.Size * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.A))
            //    if (Camera.Transform.Position.X > -3.79) Camera.Transform.TranslateX(-Camera.Size * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.S))
            //    Camera.Transform.TranslateY(-Camera.Size * Time.DeltaTime);

            //if (Input.GetKeyDown(Keyboard.Keys.D))
            //    if (Camera.Transform.Position.X < 6.07) Camera.Transform.TranslateX(Camera.Size * Time.DeltaTime);

            if (Input.GetKeyDown(Keyboard.Keys.Right))
            {
                _playerAnimator.Transform.TranslateX(5 * Time.DeltaTime);
                _flipX = false;
                _playerAnimator.Play("run", _flipX);
                
                // if (Camera.Transform.Position.X <= 6.07)
                //    Camera.Transform.Position = new Point(_playerAnimator.Transform.Position.X, 0);
            }
            else if (Input.GetKeyDown(Keyboard.Keys.Left))
            {
                _playerAnimator.Transform.TranslateX(-5 * Time.DeltaTime);
                _flipX = true;
                _playerAnimator.Play("run", _flipX);
                //if (Camera.Transform.Position.X >= -3.79)
                 //   Camera.Transform.Position = new Point(_playerAnimator.Transform.Position.X, 0);
            }
            else if (Input.GetKeyDown(Keyboard.Keys.Down))
            {
                _playerAnimator.Play("crouch", _flipX);
            }
            else
            {
                _playerAnimator.Play("idle", _flipX);
            }

            if(_oposumAnimator.Transform.Position.X <= 5)
            {
                _oposum_flipX = true;
                //_oposumAnimator.Transform.TranslateX(1 * Time.DeltaTime);
            }
            if(_oposumAnimator.Transform.Position.X >= 12)
            {
                _oposum_flipX = false;
                //_oposumAnimator.Transform.TranslateX(-1 * Time.DeltaTime);
            }
            if(_oposum_flipX)
                _oposumAnimator.Transform.TranslateX(1 * Time.DeltaTime);
            else
                _oposumAnimator.Transform.TranslateX(-1 * Time.DeltaTime);
            _oposumAnimator.Play("walk", _oposum_flipX);

            _eagleAnimator.Transform.TranslateX(-3 * Time.DeltaTime);
            _eagleAnimator.Play("run");

            if(_eagleAnimator.Transform.Position.X < -20)
                _eagleAnimator.Transform.Position = new Point(20, 5);


            //var cameraOffsetPosition = _flipX ? new Point(_playerAnimator.Transform.Position.X - 3, 0) : new Point(_playerAnimator.Transform.Position.X + 3, 0);
            //if (Camera.Transform.Position.X < 6.07 || Camera.Transform.Position.X > -3.79) Camera.Transform.Position = cameraOffsetPosition;
            //if (Camera.Transform.Position.X > -3.79) Camera.Transform.Position = new Point(_playerAnimator.Transform.Position.X - 3, 0);
            //if (Camera.Transform.Position.X >= -3.79 || Camera.Transform.Position.X <= 6.07 )
            //    Camera.Transform.Position = new Point(_playerAnimator.Transform.Position.X, 0);

            Camera.Transform.Position = new Point(Math.Clamp(_playerAnimator.Transform.Position.X, -3.5, 3.9), 0);
        }

        protected override void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.Log($"Screen: {Input.ScreenMousePosition}, World: {Input.WorldMousePosition}");
        }

        protected override void OnKeyDown(object sender, KeyboardEventArgs e)
        {
           if (e.Key == Keyboard.Keys.Escape)
                SceneManager.ExitGame();
        }

        protected override void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //if (e.Wheel == Mouse.Wheel.Down) Camera.Size += .1;

            //if (e.Wheel == Mouse.Wheel.Up && Camera.Size > .1) Camera.Size -= .1;

            //Debug.Log($"Camera Size: {Camera.Size}");
        }
    }
}