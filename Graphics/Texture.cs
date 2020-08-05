/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public class Texture : IDisposable
    {
        internal SDL.SDL_Rect Rectangle;

        internal Texture(string path)
        {
            TexturePtr = Image.IMG_LoadTexture(Game.RenderContext, path);
            _ = SDL.SDL_QueryTexture(TexturePtr, out _, out _, out int width, out int height);
            Rectangle = new SDL.SDL_Rect { w = width, h = height };
        }

        internal IntPtr TexturePtr { get; }

        public void Dispose()
        {
            SDL.SDL_DestroyTexture(TexturePtr);
        }
    }
}