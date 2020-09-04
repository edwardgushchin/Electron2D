/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

namespace Electron2D
{
    public enum FullscreenType
    {
        Fullscreen = 0x00000001,
        None = 0x00000004,
        Desktop = Fullscreen | 4096
    }
}