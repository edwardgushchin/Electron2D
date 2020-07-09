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
    internal class ParallaxBackgroundDebugScene : Scene
    {	
		Sprite  mist_011, mist_012, bushes_021, 
                bushes_022, particles_031, particles_032, 
                forest_041, forest_042, particles051, 
                particles052, forest_061, forest_062, 
                forest_071, forest_072, forest_081, 
                forest_082, forest_091, forest_092, background;

        bool layer1, layer2, layer3, layer4, layer5, layer6, layer7, layer8, layer9, layer10;
        public ParallaxBackgroundDebugScene() : base()
		{
			layer1 = layer2 = layer3 = layer4 = layer5 = layer6 = layer7 = layer8 = layer9 = layer10 = true;
		}

        protected override void OnLoadScene()
		{
			Debug.Log("ParallaxBackground debug scene loading...", Debug.Sender.Scene);

            mist_011 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\01_Mist.png");
            mist_012 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\01_Mist.png");
            mist_012.Transform.SetPosition(Settings.Resolution.Width, 0);

            bushes_021 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\02_Bushes.png");
            bushes_022 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\02_Bushes.png");
            bushes_022.Transform.SetPosition(Settings.Resolution.Width, 0);

            particles_031 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\03_Particles.png");
            particles_032 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\03_Particles.png");
            particles_032.Transform.SetPosition(Settings.Resolution.Width, 0);

            forest_041 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\04_Forest.png");
            forest_042 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\04_Forest.png");
            forest_042.Transform.SetPosition(Settings.Resolution.Width, 0);

            particles051 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\05_Particles.png");
            particles052 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\05_Particles.png");
            particles052.Transform.SetPosition(Settings.Resolution.Width, 0);

            forest_061 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\06_Forest.png");
            forest_062 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\06_Forest.png");
            forest_062.Transform.SetPosition(Settings.Resolution.Width, 0);

            forest_071 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\07_Forest.png");
            forest_072 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\07_Forest.png");
            forest_072.Transform.SetPosition(Settings.Resolution.Width, 0);

            forest_081 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\08_Forest.png");
            forest_082 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\08_Forest.png");
            forest_082.Transform.SetPosition(Settings.Resolution.Width, 0);

            forest_091 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\09_Forest.png");
            forest_092 = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\09_Forest.png");
            forest_092.Transform.SetPosition(Settings.Resolution.Width, 0);

            background = ResourceManager.LoadSprite(@"Resources\Sprites\parralax_demo\10_Sky.png");
            

			Debug.Log("ParallaxBackground debug scene loaded.", Debug.Sender.Scene);
        }

        void layerDraw(Sprite l1, Sprite l2, double speed)
        {
            l1.Transform.Translate(speed, 0);
            l2.Transform.Translate(speed, 0);

            l1.Draw();
            l2.Draw();

            if (l1.Transform.Position.X <= -Settings.Resolution.Width)
                l1.Transform.SetPosition(Settings.Resolution.Width + l2.Transform.Position.X, 0);
            if (l2.Transform.Position.X <= -Settings.Resolution.Width)
                l2.Transform.SetPosition(Settings.Resolution.Width + l1.Transform.Position.X, 0);  
            
        }

        double speed_09 = -2.0, speed_08 = -4.0, speed_07 = -8.0, speed_06 = -16.0;
        double speed_05 = -24.0, speed_04 = -32.0, speed_03 = -64.0, speed_02 = -96.0, speed_01 = -128.0;
        protected override void Update(double deltaTime)
		{
            if(layer1) background.Draw();
            if(layer2) layerDraw(forest_091, forest_092, speed_09 * deltaTime);
            if(layer3) layerDraw(forest_081, forest_082, speed_08 * deltaTime);
            if(layer4) layerDraw(forest_071, forest_072, speed_07 * deltaTime);
            if(layer5) layerDraw(forest_061, forest_062, speed_06 * deltaTime);
            if(layer6) layerDraw(particles051, particles052, speed_05 * deltaTime);
            if(layer7) layerDraw(forest_041, forest_042, speed_04 * deltaTime);
            if(layer8) layerDraw(particles_031, particles_032, speed_03 * deltaTime);
            if(layer9) layerDraw(bushes_021, bushes_022, speed_02 * deltaTime);
            if(layer10) layerDraw(mist_011, mist_012, speed_01 * deltaTime);

            Debug.Log($"x: = {forest_092.Transform.Position}, deltaTime = {deltaTime}, speed = {speed_09 * deltaTime}");
        }

        protected override void OnKeyDown(object sender, KeyboardEventArgs e)
		{			
			if(e.Key == Keyboard.Keys.Escape)
				SceneManager.ExitGame();
		}
    }
}