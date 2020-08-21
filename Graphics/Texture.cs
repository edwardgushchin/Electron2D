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
        internal Bounds DrawRect;

        internal Texture(string path)
        {
            Instance = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(Instance, out uint format, out int access, out int width, out int height);
            DrawRect = new Bounds { X = 0, Y = 0, Width = width, Height = height};
            PixelFormat = format;
            Access = access;
            Width = width;
            Height = height;
        }

        internal IntPtr Instance { get; }

        public int Width { get; }

        public int Height { get; }

        public uint PixelFormat { get; }

        public int Access { get; }

        public void Dispose()
        {
            SDL.SDL_DestroyTexture(Instance);
        }
    }
}