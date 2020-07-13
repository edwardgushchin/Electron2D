/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Events;
using Electron2D.Graphics;

using System.Collections.Generic;

namespace Electron2D.DebugScene
{
    internal class ParallaxBackgroundDebugScene : Scene
    {
        private readonly ParallaxBackground parallax;
        private Sprite background;

        public ParallaxBackgroundDebugScene() : base()
        {
            parallax = new ParallaxBackground();
        }

        protected override void OnLoadScene()
		{
			Debug.Log("ParallaxBackground debug scene loading...", Debug.Sender.Scene);

            ResourceManager.LoadSprite("bg_layer1", @"Resources\Sprites\parralax_demo\01_Mist.png");
            ResourceManager.LoadSprite("bg_layer2", @"Resources\Sprites\parralax_demo\02_Bushes.png");
            ResourceManager.LoadSprite("bg_layer3", @"Resources\Sprites\parralax_demo\03_Particles.png");
            ResourceManager.LoadSprite("bg_layer4", @"Resources\Sprites\parralax_demo\04_Forest.png");
            ResourceManager.LoadSprite("bg_layer5", @"Resources\Sprites\parralax_demo\05_Particles.png");
            ResourceManager.LoadSprite("bg_layer6", @"Resources\Sprites\parralax_demo\06_Forest.png");
            ResourceManager.LoadSprite("bg_layer7", @"Resources\Sprites\parralax_demo\07_Forest.png");
            ResourceManager.LoadSprite("bg_layer8", @"Resources\Sprites\parralax_demo\08_Forest.png");
            ResourceManager.LoadSprite("bg_layer9", @"Resources\Sprites\parralax_demo\09_Forest.png");
            ResourceManager.LoadSprite("bg_layer10", @"Resources\Sprites\parralax_demo\10_Sky.png");

            background = ResourceManager.GetSprite("bg_layer10");
            parallax.Add(new ParallaxLayer("bg_layer9", 2));
            parallax.Add(new ParallaxLayer("bg_layer8", 4));
            parallax.Add(new ParallaxLayer("bg_layer7", 8));
            parallax.Add(new ParallaxLayer("bg_layer6", 16));
            parallax.Add(new ParallaxLayer("bg_layer5", 24));
            parallax.Add(new ParallaxLayer("bg_layer4", 32));
            parallax.Add(new ParallaxLayer("bg_layer3", 64));
            parallax.Add(new ParallaxLayer("bg_layer2", 96));
            parallax.Add(new ParallaxLayer("bg_layer1", 128));

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
            layers.ForEach(lay => lay.Update());
        }
    }

	internal class ParallaxLayer : GameObject
    {
        private readonly Sprite l1, l2;
        private readonly Transform l1t, l2t;

        public ParallaxLayer(string resourceName, double speed)
        {
            Speed = -speed;

            l1 = ResourceManager.GetSprite(resourceName);
            l2 = ResourceManager.GetSprite(resourceName);

            l1t = new Transform();
            l2t = new Transform(new Point(Settings.Resolution.Width, 0));
        }

        public override void Update()
        {
            l1t.Translate(new Vector(Speed * Time.DeltaTime, 0));
            l2t.Translate(new Vector(Speed * Time.DeltaTime, 0));

            l1.Draw(l1t);
            l2.Draw(l2t);

            if (l1t.Position.X <= -Settings.Resolution.Width)
                l1t.SetPosition(Settings.Resolution.Width + l2t.Position.X, 0);
            if (l2t.Position.X <= -Settings.Resolution.Width)
                l2t.SetPosition(Settings.Resolution.Width + l1t.Position.X, 0);
        }

        public double Speed { get;}
    }
}