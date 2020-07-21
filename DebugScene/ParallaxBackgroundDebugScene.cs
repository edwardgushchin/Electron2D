/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

/*using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

using System.Collections.Generic;
using Electron2D.Binding.SDL;

namespace Electron2D.DebugScene
{
    internal class ParallaxBackgroundDebugScene : Scene
    {
        private readonly ParallaxBackground parallax;
        private Texture background;

        public ParallaxBackgroundDebugScene() : base()
        {
            SDL.SDL_ShowCursor(SDL.SDL_DISABLE);
            parallax = new ParallaxBackground();
        }

        protected override void OnLoadScene()
		{
			Debug.Log("ParallaxBackground debug scene loading...", Debug.Sender.Scene);

            ResourceManager.LoadTexture("bg_layer1", @"Resources\textures\parralax_demo\01_Mist.png");
            //ResourceManager.Gettexture("bg_layer1").Alpha = 150;
            ResourceManager.LoadTexture("bg_layer2", @"Resources\textures\parralax_demo\02_Bushes.png");
            ResourceManager.LoadTexture("bg_layer3", @"Resources\textures\parralax_demo\03_Particles.png");
            ResourceManager.LoadTexture("bg_layer4", @"Resources\textures\parralax_demo\04_Forest.png");
            ResourceManager.LoadTexture("bg_layer5", @"Resources\textures\parralax_demo\05_Particles.png");
            ResourceManager.LoadTexture("bg_layer6", @"Resources\textures\parralax_demo\06_Forest.png");
            ResourceManager.LoadTexture("bg_layer7", @"Resources\textures\parralax_demo\07_Forest.png");
            ResourceManager.LoadTexture("bg_layer8", @"Resources\textures\parralax_demo\08_Forest.png");
            ResourceManager.LoadTexture("bg_layer9", @"Resources\textures\parralax_demo\09_Forest.png");
            ResourceManager.LoadTexture("bg_layer10", @"Resources\textures\parralax_demo\10_Sky.png");

            background = ResourceManager.GetTexture("bg_layer10");
            parallax.Add(new ParallaxLayer("bg_layer9", 2));
            parallax.Add(new ParallaxLayer("bg_layer8", 4));
            parallax.Add(new ParallaxLayer("bg_layer7", 8));
            parallax.Add(new ParallaxLayer("bg_layer6", 16));
            parallax.Add(new ParallaxParticlesLayer("bg_layer5", 24, 0.125));
            parallax.Add(new ParallaxLayer("bg_layer4", 32));
            parallax.Add(new ParallaxParticlesLayer("bg_layer3", 64, 0.25));
            parallax.Add(new ParallaxLayer("bg_layer2", 96));
            parallax.Add(new ParallaxMistLayer("bg_layer1", 128, 5));

			Debug.Log("ParallaxBackground debug scene loaded.", Debug.Sender.Scene);
        }

        protected override void Update()
		{
            background.Draw();
            parallax.Update();
        }

        protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();
		}
    }

    internal class ParallaxBackground : GameObject
    {
        private readonly List<ParallaxLayer> layers;
        private const double layersSpeed = 1;

        public ParallaxBackground()
        {
            layers = new List<ParallaxLayer>();
        }

        public void Add(ParallaxLayer layer)
        {
            layers.Add(layer);
        }

        public override void Update()
        {
            //if (Input.GetKeyDown(Keyboard.Keys.A) || Input.GetKeyDown(Keyboard.Keys.Left))
            //    layers.ForEach(lay => lay.MoveLeft(layersSpeed));

            //if ( Input.GetKeyDown(Keyboard.Keys.D) || Input.GetKeyDown(Keyboard.Keys.Right))
            layers.ForEach(lay => lay.MoveRight(layersSpeed));

            layers.ForEach(lay => lay.Update());
        }
    }

	internal class ParallaxLayer : GameObject
    {
        private readonly Texture texture;

        private readonly double speed;
        private readonly Transform ltcenter, ltright;

        public ParallaxLayer(string resourceName, double speed)
        {
            this.speed = speed;

            texture = ResourceManager.GetTexture(resourceName);

            ltcenter = new Transform();
            ltright = new Transform(new Point(Settings.Resolution.Width, 0));
        }

        public override void Update()
        {
            if (ltcenter.Position.X <= -Settings.Resolution.Width)
                ltcenter.SetPosition(Settings.Resolution.Width + ltright.Position.X, 0);
            else if (ltcenter.Position.X >= Settings.Resolution.Width)
                ltcenter.SetPosition(-Settings.Resolution.Width + ltright.Position.X, 0);

            if (ltright.Position.X <= -Settings.Resolution.Width)
                ltright.SetPosition(Settings.Resolution.Width + ltcenter.Position.X, 0);
            else if (ltright.Position.X >= Settings.Resolution.Width)
                ltright.SetPosition(-Settings.Resolution.Width + ltcenter.Position.X, 0);

            texture.Draw(ltcenter);
            texture.Draw(ltright);
        }

        public virtual void MoveRight(double speed)
        {
            var step = -speed * this.speed * Time.DeltaTime;
            ltcenter.Translate(step, 0);
            ltright.Translate(step, 0);
        }

        public virtual void MoveLeft(double speed)
        {
            var step = speed * this.speed * Time.DeltaTime;
            ltcenter.Translate(step, 0);
            ltright.Translate(step, 0);
        }
    }

    internal class ParallaxMistLayer : ParallaxLayer
    {
        private readonly texture texture;
        private readonly Transform ltcenter, ltright;
        private readonly double force, speed;

        public ParallaxMistLayer(string resourceName, double speed, double force) : base(resourceName, speed)
        {
            this.speed = speed;
            this.force = force;

            texture = ResourceManager.Gettexture(resourceName);
            texture.Alpha = 150;

            ltcenter = new Transform();
            ltright = new Transform(new Point(Settings.Resolution.Width, 0));
        }

        public override void Update()
        {
            if (ltcenter.Position.X <= -Settings.Resolution.Width)
                ltcenter.SetPosition(Settings.Resolution.Width + ltright.Position.X, 0);
            else if (ltcenter.Position.X >= Settings.Resolution.Width)
                ltcenter.SetPosition(-Settings.Resolution.Width + ltright.Position.X, 0);

            if (ltright.Position.X <= -Settings.Resolution.Width)
                ltright.SetPosition(Settings.Resolution.Width + ltcenter.Position.X, 0);
            else if (ltright.Position.X >= Settings.Resolution.Width)
                ltright.SetPosition(-Settings.Resolution.Width + ltcenter.Position.X, 0);

            var step = (force + speed) * Time.DeltaTime;
            ltcenter.Translate(step, 0);
            ltright.Translate(step, 0);

            texture.Draw(ltcenter);
            texture.Draw(ltright);
        }
    }

    internal class ParallaxParticlesLayer : ParallaxLayer
    {
        private readonly texture texture;
        private readonly Transform ltcenter, ltright, ltbottom, ltbottomright;
        private readonly double upspeed, speed;
        public ParallaxParticlesLayer(string resourceName, double speed, double up_speed) : base(resourceName, speed)
        {
            this.speed = speed;
            upspeed = up_speed;

            texture = ResourceManager.Gettexture(resourceName);

            ltcenter = new Transform();
            ltright = new Transform(new Point(Settings.Resolution.Width, 0));
            ltbottom = new Transform(new Point(0, -Settings.Resolution.Height));
            ltbottomright = new Transform(new Point(Settings.Resolution.Width, -Settings.Resolution.Height));
        }

        public override void Update()
        {
            if (ltcenter.Position.X <= -Settings.Resolution.Width)
                ltcenter.SetPosition(Settings.Resolution.Width + ltright.Position.X, ltcenter.Position.Y);
            else if (ltcenter.Position.X >= Settings.Resolution.Width)
                ltcenter.SetPosition(-Settings.Resolution.Width + ltright.Position.X, ltcenter.Position.Y);

            if (ltright.Position.X <= -Settings.Resolution.Width)
                ltright.SetPosition(Settings.Resolution.Width + ltcenter.Position.X, ltright.Position.Y);
            else if (ltright.Position.X >= Settings.Resolution.Width)
                ltright.SetPosition(-Settings.Resolution.Width + ltcenter.Position.X, ltright.Position.Y);

            if (ltbottom.Position.X <= -Settings.Resolution.Width)
                ltbottom.SetPosition(Settings.Resolution.Width + ltbottomright.Position.X, ltbottom.Position.Y);
            else if (ltbottom.Position.X >= Settings.Resolution.Width)
                ltbottom.SetPosition(-Settings.Resolution.Width + ltbottomright.Position.X, ltbottom.Position.Y);

            if (ltbottomright.Position.X <= -Settings.Resolution.Width)
                ltbottomright.SetPosition(Settings.Resolution.Width + ltbottom.Position.X, ltbottomright.Position.Y);
            else if (ltbottomright.Position.X >= Settings.Resolution.Width)
                ltbottomright.SetPosition(-Settings.Resolution.Width + ltbottom.Position.X, ltbottomright.Position.Y);

            if (ltcenter.Position.Y >= Settings.Resolution.Height) {
                ltcenter.SetPosition(ltcenter.Position.X, -Settings.Resolution.Height);
                ltright.SetPosition(ltright.Position.X, -Settings.Resolution.Height);
            }

            if (ltbottom.Position.Y >= Settings.Resolution.Height) {
                ltbottom.SetPosition(ltbottom.Position.X, -Settings.Resolution.Height);
                ltbottomright.SetPosition(ltbottomright.Position.X, -Settings.Resolution.Height);
            }

            var vec = 50 * upspeed * Time.DeltaTime;
            ltcenter.Translate(0, vec);
            ltright.Translate(0, vec);
            ltbottom.Translate(0, vec);
            ltbottomright.Translate(0, vec);

            texture.Draw(ltcenter);
            texture.Draw(ltright);
            texture.Draw(ltbottom);
            texture.Draw(ltbottomright);
        }

        public override void MoveRight(double speed)
        {
            var step = -speed * this.speed * Time.DeltaTime;
            ltcenter.Translate(step, 0);
            ltright.Translate(step, 0);
            ltbottom.Translate(step, 0);
            ltbottomright.Translate(step, 0);
        }

        public override void MoveLeft(double speed)
        {
            var step = speed * this.speed * Time.DeltaTime;
            ltcenter.Translate(step, 0);
            ltright.Translate(step, 0);
            ltbottom.Translate(step, 0);
            ltbottomright.Translate(step, 0);
        }
    }
}*/