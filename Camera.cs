/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;

namespace Electron2D
{
    public class Camera
    {
        public Camera()
        {
            Transform = new Transform();
        }

        public Size Resolution => Kernel.Settings.Resolution;

        public Transform Transform { get; set; }
    }
}