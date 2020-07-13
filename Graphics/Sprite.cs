/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public class Sprite
    {
        private readonly IntPtr sprite;
        private SDL.SDL_Rect scr_rect;
        private SDL.SDL_FRect draw_rect;
        //private readonly int access;
        //uint format;
        public Sprite(string path)
        {
            Path = path;
            Transform = new Transform(new Point(0, 0));
            sprite = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(sprite, out var format, out var access, out int width, out int height); // get the width and height of the texture

            draw_rect = new SDL.SDL_FRect
            {
                w = width,
                h = height
            };

            scr_rect = new SDL.SDL_Rect
            {
                x = 0,
                y = 0,
                w = width,
                h = height
            };
        }

        public Transform Transform
        {
            get; set;
        }

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

        public void Draw()
        {
            draw_rect.x = (float)Transform.Position.X;
            draw_rect.y = (float)Transform.Position.Y;
            SDL.SDL_RenderCopyExF(Game.RenderContext, sprite, ref scr_rect, ref draw_rect, Transform.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
        }

        public void Draw(Transform transform)
        {
            draw_rect.x = (float)transform.Position.X;
            draw_rect.y = (float)transform.Position.Y;
            SDL.SDL_RenderCopyExF(Game.RenderContext, sprite, ref scr_rect, ref draw_rect, Transform.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
        }
    }
}