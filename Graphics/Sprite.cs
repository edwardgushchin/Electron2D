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
            size = new Rect(texture.Rectangle.w * Transform.LocalScale.X, texture.Rectangle.h * Transform.LocalScale.Y);

            draw_rect = new SDL.SDL_FRect();

            Debug = false;
        }

        //public Sprite Parrent { get; set; }

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

        /*&public void Draw()
        {
            var center = new SDL.SDL_FPoint();
            var point = Transform.Position.ConvertToSDLPoint();

            size.Width = rect.Width * Transform.LocalScale.X;
            size.Height = rect.Height * Transform.LocalScale.Y;
            draw_rect.x = (float)(point.X - (size.Width * Transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
            draw_rect.y = (float)(point.Y - (size.Height * Transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
            draw_rect.w = (float)size.Width;
            draw_rect.h = (float)size.Height;
            center.x = (float)(size.Width * Transform.Achor.X);
            center.y = (float)(size.Height * Transform.Achor.Y);

            SDL.SDL_RenderCopyExF(Game.RenderContext, srcTexture.TexturePtr, ref srcTexture.Rectangle, ref draw_rect, Transform.Degrees, ref center, SDL.SDL_RendererFlip.SDL_FLIP_NONE);

            if(Debug) DrawDebug(point, Transform);
        }*/

        internal void Draw(Transform transformTo)
        {
            var point = transformTo.Position.ConvertToSDLPoint();
            var center = new SDL.SDL_FPoint();
            size.Width = rect.Width * transformTo.LocalScale.X;
            size.Height = rect.Height * transformTo.LocalScale.Y;
            draw_rect.x = (float)(point.X - (size.Width * transformTo.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X);
            draw_rect.y = (float)(point.Y - (size.Height * transformTo.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y);
            draw_rect.w = (float)size.Width;
            draw_rect.h = (float)size.Height;
            center.x = (float)(size.Width * transformTo.Achor.X);
            center.y = (float)(size.Height * transformTo.Achor.Y);

            SDL.SDL_RenderCopyExF(Game.RenderContext, srcTexture.TexturePtr, ref srcTexture.Rectangle, ref draw_rect, transformTo.Degrees, ref center, SDL.SDL_RendererFlip.SDL_FLIP_NONE);

            if(Debug) DrawDebug(point, transformTo);
        }

        private void DrawDebug(Point point, Transform transform)
        {
            SDL.SDL_SetRenderDrawColor(Game.RenderContext, 255, 0, 0, 0);
            var centerRect = new SDL.SDL_FRect() {x = (float)point.X - 2, y = (float)point.Y - 2, w = 4, h = 4};
            SDL.SDL_RenderFillRectF(Game.RenderContext, ref centerRect);
            DrawSpriteContainer(transform);
            var color = SceneManager.GetCurrentScene.ClearColor;
            SDL.SDL_SetRenderDrawColor(Game.RenderContext, color.R, color.G, color.B, color.A);
        }

        private void DrawSpriteContainer(Transform transform)
        {
            SDL.SDL_SetRenderDrawColor(Game.RenderContext, 0, 255, 0, 0);

            var x1 = new Point(
                transform.Position.X - (size.Width * transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X,
                transform.Position.Y + (size.Height * transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y
            ).Rotate(transform.Position, transform.Degrees).ConvertToSDLPoint();

            var x2 = new Point(
                transform.Position.X + (size.Width * transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X,
                transform.Position.Y + (size.Height * transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y
            ).Rotate(transform.Position, transform.Degrees).ConvertToSDLPoint();

            var x3 = new Point(
                transform.Position.X + (size.Width * transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X,
                transform.Position.Y - (size.Height * transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y
            ).Rotate(transform.Position, transform.Degrees).ConvertToSDLPoint();

            var x4 = new Point(
                transform.Position.X - (size.Width * transform.Achor.X) - SceneManager.GetCurrentScene.Camera.Transform.Position.X,
                transform.Position.Y - (size.Height * transform.Achor.Y) - SceneManager.GetCurrentScene.Camera.Transform.Position.Y
            ).Rotate(transform.Position, transform.Degrees).ConvertToSDLPoint();

            var points = new SDL.SDL_FPoint[] {
                new SDL.SDL_FPoint() { x = (float)x1.X, y = (float)x1.Y },
                new SDL.SDL_FPoint() { x = (float)x2.X, y = (float)x2.Y },
                new SDL.SDL_FPoint() { x = (float)x3.X, y = (float)x3.Y },
                new SDL.SDL_FPoint() { x = (float)x4.X, y = (float)x4.Y },
                new SDL.SDL_FPoint() { x = (float)x1.X, y = (float)x1.Y }
            };

            SDL.SDL_RenderDrawLinesF(Game.RenderContext, points, 5);
        }
    }
}