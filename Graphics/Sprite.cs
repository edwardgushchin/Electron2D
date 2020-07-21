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
        private readonly Texture srcTexture;
        private SDL.SDL_FRect draw_rect;
        private Rect size, rect;

        public Sprite(Texture texture)
        {
            Transform = new Transform(new Point(0, 0));
            Achor = new Point(0.5, 0.5);
            srcTexture = texture;
            rect = new Rect(texture.Rectangle.w, texture.Rectangle.h);
            size = new Rect(texture.Rectangle.w * Transform.LocalScale.X, texture.Rectangle.h * Transform.LocalScale.Y);

            draw_rect = new SDL.SDL_FRect
            {
                w = (float)size.Width,
                h = (float)size.Height
            };
        }

        public byte Alpha
        {
            get
            {
                SDL.SDL_GetTextureAlphaMod(srcTexture.TexturePtr, out byte a);
                return a;
            }
            set => SDL.SDL_SetTextureAlphaMod(srcTexture.TexturePtr, value);
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

        public Point Achor { get; set; }

        public Point Center => new Point(draw_rect.x + (size.Width * Achor.X), draw_rect.y + (size.Height * Achor.Y));


        public void Draw()
        {
            Draw(Transform);
        }

        public void Draw(Transform transformTo)
        {
            var point = (Transform.Position + transformTo.Position).ConvertToSDLPoint();

            size.Width = rect.Width * transformTo.LocalScale.X;
            size.Height = rect.Height * transformTo.LocalScale.Y;
            draw_rect.x = (float)(point.X - (size.Width * Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
            draw_rect.y = (float)(point.Y - (size.Height * Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
            draw_rect.w = (float)size.Width;
            draw_rect.h = (float)size.Height;

            //SDL.SDL_SetRenderDrawColor(Game.RenderContext, 255,0,255,0);
            SDL.SDL_RenderCopyExF(Game.RenderContext, srcTexture.TexturePtr, ref srcTexture.Rectangle, ref draw_rect, Transform.Degrees + transformTo.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
            //SDL.SDL_RenderDrawPointF(Game.RenderContext, (float)Center.X, (float)Center.Y);
            //SDL.SDL_SetRenderDrawColor(Game.RenderContext, 0x00, 0x00, 0x00, 0xFF);
        }
    }
}