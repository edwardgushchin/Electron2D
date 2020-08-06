/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Kernel;
using Electron2D.Graphics;
using Electron2D.DebugScene;

namespace Electron2D
{
    internal static class Program
    {
        private static Game TestGame;
        internal static void Main()
        {
            Settings.VSinc = true;
            //Settings.Resizeble = false;
            //Settings.FPS = 60;
            Settings.DebugInfo = false;
            Settings.Fullscreen = false;
            Settings.Resolution = new Size(1920, 1080);
            Settings.Smoothing = SmoothingType.Anisotropic;

            TestGame = new Game("Electron2D - Parralax Background Demo");

            TestGame.SetIcon(@"Resources\\icon.png");
            TestGame.Play(new DefaultScene());
        }
    }
}