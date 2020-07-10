/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/


namespace Electron2D.Graphics
{
    public enum SmoothingType {
        // nearest pixel sampling
        Nearest,
        // linear filtering (supported by OpenGL and Direct3D)
        Linear,
        // anisotropic filtering (supported by Direct3D)
        Anisotropic
    }
}