/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;

namespace Electron2D.Kernel
{
    public static class Profiler
    {
        public static int DrawCalls => SpriteRenderer.DrawCalls;

        public static int TextureCache => ResourceManager.TextureCacheCount;

        public static int SpriteCache => SpriteRenderer.SpriteCacheCount;
    }
}