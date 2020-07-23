/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;
using System.Collections.Generic;

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
            srcTexture = texture;
            rect = new Rect(texture.Rectangle.w, texture.Rectangle.h);
            if(Parrent == null)
                size = new Rect(texture.Rectangle.w * Transform.LocalScale.X, texture.Rectangle.h * Transform.LocalScale.Y);
            else 
                size = new Rect(texture.Rectangle.w * Parrent.Transform.LocalScale.X, texture.Rectangle.h * Parrent.Transform.LocalScale.Y);
            //

            draw_rect = new SDL.SDL_FRect();

            Debug = false;
        }

        public Entity Parrent { get; set; }

        public bool Debug { get; set; }

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

        public Point Center => new Point(draw_rect.x + (size.Width * Transform.Achor.X), draw_rect.y + (size.Height * Transform.Achor.Y));

        public void Draw()
        {
            var center = new SDL.SDL_FPoint();
            Point point;
            double degrees;

            if(Parrent != null)
            {
                point = (Transform.Position + Parrent.Transform.Position).ConvertToSDLPoint();
                size.Width = rect.Width * (Transform.LocalScale.X * Parrent.Transform.LocalScale.X);
                size.Height = rect.Height * (Transform.LocalScale.Y * Parrent.Transform.LocalScale.Y);
                draw_rect.x = (float)(point.X - (size.Width * Parrent.Transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
                draw_rect.y = (float)(point.Y - (size.Height * Parrent.Transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
                draw_rect.w = (float)size.Width;
                draw_rect.h = (float)size.Height;
                //хуйня какая-то нерабочая
                center.x = (float)(size.Width * Transform.Achor.X + Parrent.Transform.Achor.X);
                center.y = (float)(size.Height * Transform.Achor.Y + Parrent.Transform.Achor.Y);
                degrees = Parrent.Transform.Degrees + Transform.Degrees;
            }
            else
            {
                point = Transform.Position.ConvertToSDLPoint();
                size.Width = rect.Width * Transform.LocalScale.X;
                size.Height = rect.Height * Transform.LocalScale.Y;
                draw_rect.x = (float)(point.X - (size.Width * Transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
                draw_rect.y = (float)(point.Y - (size.Height * Transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
                draw_rect.w = (float)size.Width;
                draw_rect.h = (float)size.Height;
                center.x = (float)(size.Width * Transform.Achor.X);
                center.y = (float)(size.Height * Transform.Achor.Y);
                degrees = Transform.Degrees;
            }
            SDL.SDL_RenderCopyExF(Game.RenderContext, srcTexture.TexturePtr, ref srcTexture.Rectangle, ref draw_rect, degrees, ref center, SDL.SDL_RendererFlip.SDL_FLIP_NONE);

            if(Debug)
            {
                SDL.SDL_SetRenderDrawColor(Game.RenderContext, 255, 0, 0, 0);
                var centerRect = new SDL.SDL_FRect() {x = (float)(point.X - 2), y = (float)(point.Y - 2), w = 4, h = 4};
                SDL.SDL_RenderFillRectF(Game.RenderContext, ref centerRect);
                SDL.SDL_SetRenderDrawColor(Game.RenderContext, 0, 255, 0, 0);
                SDL.SDL_RenderDrawRectF(Game.RenderContext, ref draw_rect);
                var color = SceneManager.GetCurrentScene.ClearColor;
                SDL.SDL_SetRenderDrawColor(Game.RenderContext, color.R, color.G, color.B, color.A);
            }
        }

        public void Draw(Transform transformTo)
        {
            Point point;
            var center = new SDL.SDL_FPoint();
            double degrees;

            if(Parrent != null)
            {
                point = (transformTo.Position + Parrent.Transform.Position).ConvertToSDLPoint();
                size.Width = rect.Width * (transformTo.LocalScale.X * Parrent.Transform.LocalScale.X);
                size.Height = rect.Height * (transformTo.LocalScale.Y * Parrent.Transform.LocalScale.Y);
                draw_rect.x = (float)(point.X - (size.Width * Parrent.Transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
                draw_rect.y = (float)(point.Y - (size.Height * Parrent.Transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
                draw_rect.w = (float)size.Width;
                draw_rect.h = (float)size.Height;
                center.x = (float)(size.Width * Parrent.Transform.Achor.X);
                center.y = (float)(size.Height * Parrent.Transform.Achor.Y);
                degrees = Parrent.Transform.Degrees + transformTo.Degrees;
            }
            else
            {
                point = transformTo.Position.ConvertToSDLPoint();
                size.Width = rect.Width * transformTo.LocalScale.X;
                size.Height = rect.Height * transformTo.LocalScale.Y;
                draw_rect.x = (float)(point.X - (size.Width * transformTo.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
                draw_rect.y = (float)(point.Y - (size.Height * transformTo.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
                draw_rect.w = (float)size.Width;
                draw_rect.h = (float)size.Height;
                center.x = (float)(size.Width * transformTo.Achor.X);
                center.y = (float)(size.Height * transformTo.Achor.Y);
                degrees = transformTo.Degrees;
            }

            SDL.SDL_RenderCopyExF(Game.RenderContext, srcTexture.TexturePtr, ref srcTexture.Rectangle, ref draw_rect, degrees, ref center, SDL.SDL_RendererFlip.SDL_FLIP_NONE);

            if(Debug)
            {
                SDL.SDL_SetRenderDrawColor(Game.RenderContext, 255, 0, 0, 0);
                var centerRect = new SDL.SDL_FRect() {x = (float)point.X - 2, y = (float)point.Y - 2, w = 4, h = 4};
                SDL.SDL_RenderFillRectF(Game.RenderContext, ref centerRect);
                SDL.SDL_SetRenderDrawColor(Game.RenderContext, 0, 255, 0, 0);
                SDL.SDL_RenderDrawRectF(Game.RenderContext, ref draw_rect);
                var color = SceneManager.GetCurrentScene.ClearColor;
                SDL.SDL_SetRenderDrawColor(Game.RenderContext, color.R, color.G, color.B, color.A);
            }
        }
    }
}