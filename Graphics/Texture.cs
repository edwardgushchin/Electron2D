/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    internal class Texture : IDisposable
    {
        private readonly IntPtr texture;
        private SDL.SDL_Rect rect;

        public Texture(string path)
        {
            texture = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(texture, out var format, out var access, out int width, out int height);
            rect = new SDL.SDL_Rect { w = width, h = height };
        }

        public SDL.SDL_Rect Rectangle => rect;

        public void Dispose()
        {
            SDL.SDL_DestroyTexture(texture);
        }
    }
}