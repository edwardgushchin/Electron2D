/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Kernel;
using Electron2D.Graphics;

namespace Electron2D
{
    internal static class Program
    {
        private static Game TestGame;
        internal static void Main()
        {
            Settings.VSinc = true;
            //Settings.DebugInfo = true;
            Settings.Resizeble = true;
            Settings.Fullscreen = false;
            Settings.Resolution = new Rect(800, 600);
            Settings.Smoothing = SmoothingType.Anisotropic;

            TestGame = new Game("Electron2D - Crossplatform 2D Game Engine");

            TestGame.SetIcon(@"Resources\\icon.png");
            TestGame.Play();
        }
    }
}