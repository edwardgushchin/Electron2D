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
        private Rect size, rect;

        public Sprite(string path)
        {
            Path = path;
            Transform = new Transform(new Point(0, 0));
            //sprite = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(sprite, out var format, out var access, out int width, out int height);
            rect = new Rect(width, height);
            size = new Rect(width * Transform.LocalScale.X, height * Transform.LocalScale.Y);

            draw_rect = new SDL.SDL_FRect
            {
                w = (float)size.Width,
                h = (float)size.Height
            };

            scr_rect = new SDL.SDL_Rect
            {
                w = (int)rect.Width,
                h = (int)rect.Height
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

        public Transform Transform { get; set; }

        public Rect Size { get { return size; } }

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

            size.Width = rect.Width * transform.LocalScale.X;
            size.Height = rect.Height * transform.LocalScale.Y;
            draw_rect.x = (float)(point.X - Pivot.X - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
            draw_rect.y = (float)(point.Y - Pivot.Y - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
            draw_rect.w = (float)size.Width;
            draw_rect.h = (float)size.Height;
            SDL.SDL_RenderCopyExF(Game.RenderContext, sprite, ref scr_rect, ref draw_rect, Transform.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
        }

        public void Dispose()
        {
            //SDL.SDL_FreeSurface(sprite);
            SDL.SDL_free(sprite);
        }
    }
}