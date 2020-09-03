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
        private Texture _texture;
        private Bounds _bounds;
        private SDL.SDL_FRect _draw_rect;
        private Rect _size;
        private int _layer;

        public Sprite(Texture texture)
        {
            SpriteInit(texture, Point.Zero, texture.DrawRect, false, 0, 100, true);
        }

        public Sprite(Texture texture, Point position)
        {
            SpriteInit(texture, position, texture.DrawRect, false, 0, 100, true);
        }

        public Sprite(Texture texture, Point position, int layer)
        {
            SpriteInit(texture, position, texture.DrawRect, false, layer, 100, true);
        }

        public Sprite(Texture texture, Point position, int layer, int pixelPerUnit)
        {
            SpriteInit(texture, position, texture.DrawRect, false, layer, pixelPerUnit, true);
        }

        public Sprite(Texture texture, Bounds bounds, int layer, int pixelPerUnit)
        {
            SpriteInit(texture, Point.Zero, bounds, true, layer, pixelPerUnit, false);
        }

        private void SpriteInit(Texture texture, Point position, Bounds bounds, bool package, int layer, int pixelPerUnit, bool visible)
        {
            Transform = new Transform(position);
            PixelPerUnit = pixelPerUnit;
            Package = package;
            Visible = visible;
            FlipX = false;

            _layer = layer;
            _texture = texture;
            _bounds = bounds;
            _size = new Rect(texture.Width * Transform.LocalScale.X, texture.Height * Transform.LocalScale.Y);
            _draw_rect = new SDL.SDL_FRect();

            Debug = false;

            SpriteRenderer.Add(this);
        }

        public int PixelPerUnit { get; set; }

        public bool Debug { get; set; }

        public bool Package { get; private set; }

        public bool Visible { get; set; }

        public bool FlipX { get; set; }

        public Bounds PackageBounds => _bounds;

        public byte Alpha
        {
            get
            {
                SDL.SDL_GetTextureAlphaMod(_texture.Instance, out byte a);
                return a;
            }
            set => SDL.SDL_SetTextureAlphaMod(_texture.Instance, value);
        }

        public Transform Transform { get; set; }

        public Rect Size { get { return new Rect(_size.Width / PixelPerUnit, _size.Height / PixelPerUnit); } }

        public string Path { get; }

        public Texture Texture => _texture;

        public Point Center => new Point(_draw_rect.x + (_size.Width * Transform.Achor.X), _draw_rect.y + (_size.Height * Transform.Achor.Y));

        public int Layer
        {
            get => _layer;
            set
            {
                _layer = value;
                SpriteRenderer.Sort();
            }
        }

        internal void Draw() => Draw(Transform);

        private void Draw(Transform transformTo)
        {
            var point = Camera.MainCamera.ConvertWorldToScreen(transformTo.Position - Camera.MainCamera.Transform.Position);

            var center = new SDL.SDL_FPoint();

            _size.Width = _bounds.Width * transformTo.LocalScale.X;
            _size.Height = _bounds.Height * transformTo.LocalScale.Y;

            var unit = Camera.MainCamera.WorldUnit;

            _draw_rect.w = (float)(unit * (_size.Width / PixelPerUnit));
            _draw_rect.h = (float)(unit * (_size.Height / PixelPerUnit));

            _draw_rect.x = (float)(point.X - (_draw_rect.w * transformTo.Achor.X));
            _draw_rect.y = (float)(point.Y - (_draw_rect.h * transformTo.Achor.Y));

            center.x = Convert.ToSingle(_draw_rect.w * transformTo.Achor.X);
            center.y = Convert.ToSingle(_draw_rect.h * transformTo.Achor.Y);

            var flip = FlipX ? SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL : SDL.SDL_RendererFlip.SDL_FLIP_NONE;

            SDL.SDL_RenderCopyExF(Game.RenderContext, _texture.Instance, ref _bounds.SDLRect, ref _draw_rect, transformTo.Degrees, ref center, flip);

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

            var x = SceneManager.GetCurrentScene.Camera.Transform.Position.X;
            var y = SceneManager.GetCurrentScene.Camera.Transform.Position.Y;

            var x1 = SceneManager.GetCurrentScene.Camera.ConvertWorldToScreen(new Point(
                transform.Position.X - (_size.Width * transform.Achor.X / PixelPerUnit) - x,
                transform.Position.Y + (_size.Height * transform.Achor.Y / PixelPerUnit) - y
            ).Rotate(transform.Position, transform.Degrees));

            var x2 = SceneManager.GetCurrentScene.Camera.ConvertWorldToScreen(new Point(
                transform.Position.X + (_size.Width * transform.Achor.X / PixelPerUnit) - x,
                transform.Position.Y + (_size.Height * transform.Achor.Y / PixelPerUnit) - y
            ).Rotate(transform.Position, transform.Degrees));

            var x3 = SceneManager.GetCurrentScene.Camera.ConvertWorldToScreen(new Point(
                transform.Position.X + (_size.Width * transform.Achor.X / PixelPerUnit) - x,
                transform.Position.Y - (_size.Height * transform.Achor.Y / PixelPerUnit) - y
            ).Rotate(transform.Position, transform.Degrees));

            var x4 = SceneManager.GetCurrentScene.Camera.ConvertWorldToScreen(new Point(
                transform.Position.X - (_size.Width  * transform.Achor.X / PixelPerUnit) - x,
                transform.Position.Y - (_size.Height * transform.Achor.Y / PixelPerUnit) - y
            ).Rotate(transform.Position, transform.Degrees));

            var points = new SDL.SDL_FPoint[] {
                new SDL.SDL_FPoint() { x = (float)x1.X, y = (float)x1.Y },
                new SDL.SDL_FPoint() { x = (float)x2.X, y = (float)x2.Y },
                new SDL.SDL_FPoint() { x = (float)x3.X, y = (float)x3.Y },
                new SDL.SDL_FPoint() { x = (float)x4.X, y = (float)x4.Y },
                new SDL.SDL_FPoint() { x = (float)x1.X, y = (float)x1.Y }
            };

            SDL.SDL_RenderDrawLinesF(Game.RenderContext, points, 5);
        }

        public void Destroy()
        {
            SpriteRenderer.Remove(this);
        }

        public void Dispose()
        {
            Destroy();
        }
    }
}