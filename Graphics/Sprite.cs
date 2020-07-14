/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public class Sprite : IDisposable
    {
        private readonly IntPtr sprite;
        private SDL.SDL_Rect scr_rect;
        private SDL.SDL_FRect draw_rect;

        private Size size;

        public Sprite(string path)
        {
            Path = path;
            Transform = new Transform(new Point(0, 0));
            sprite = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(sprite, out var format, out var access, out int width, out int height);
            size = new Size(width, height);

            

            draw_rect = new SDL.SDL_FRect
            {
                w = size.Width,
                h = size.Height
            };

            scr_rect = new SDL.SDL_Rect
            {
                x = 0,
                y = 0,
                w = size.Width,
                h = size.Height
            };
        }

        public byte Alpha
        {
            get
            {
                SDL.SDL_GetTextureAlphaMod(sprite, out byte a);
                return a;
            }
            set => SDL.SDL_SetTextureAlphaMod(sprite, value);
        }

        public Transform Transform { get; }

        public Size Size { get; }

        /*private void Resize()
        {
            int ratio;
            if (width > height)
                ratio = width / Settings.Resolution.Width;
            else
                ratio = height / Settings.Resolution.Height;

            var newWidth  = ratio * Settings.Resolution.Width;
            var newHeight = ratio * Settings.Resolution.Height;


            draw_rect = new SDL.SDL_FRect
            {
                w = newWidth,//newWidth;
                h = newHeight//newHeight;
            };
            //draw_rect.x = (float)Transform.Position.X;
            //draw_rect.y = (float)Transform.Position.Y;
        }*/

        public string Path { get; }

        private Point Pivot => new Point(size.Width / 2, size.Height / 2);

        public void Draw()
        {
            Draw(Transform);
        }

        public void Draw(Transform transform)
        {
            var point = transform.Position.ConvertToSDLPoint();
            draw_rect.x = (float)(point.X + Pivot.X);
            draw_rect.y = (float)(point.Y - Pivot.Y);
            SDL.SDL_RenderCopyExF(Game.RenderContext, sprite, ref scr_rect, ref draw_rect, Transform.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
        }

        public void Dispose()
        {
            //SDL.SDL_FreeSurface(sprite);
            SDL.SDL_free(sprite);
        }
    }
}