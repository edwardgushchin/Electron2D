/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public class Sprite
    {
        private readonly Texture _texture;
        private SDL.SDL_FRect _draw_rect;
        private Rect _size, _rect;

        public Sprite(Texture texture)
        {
            Transform = new Transform();
            PixelPerUnit = 100;

            _texture = texture;
            _rect = new Rect(texture.Width, texture.Height);
            _size = new Rect(texture.Width * Transform.LocalScale.X, texture.Height * Transform.LocalScale.Y);
            _draw_rect = new SDL.SDL_FRect();

            Debug = false;
        }

        public Sprite(Texture texture, int pixelPerUnit)
        {
            Transform = new Transform();
            PixelPerUnit = pixelPerUnit;

            _texture = texture;
            _rect = new Rect(texture.Width, texture.Height);
            _size = new Rect(texture.Width * Transform.LocalScale.X, texture.Height * Transform.LocalScale.Y);
            _draw_rect = new SDL.SDL_FRect();

            Debug = false;
        }

        public int PixelPerUnit { get; }

        public bool Debug { get; set; }

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

        public Rect Size { get { return _size; } }

        public string Path { get; }

        public Point Center => new Point(_draw_rect.x + (_size.Width * Transform.Achor.X), _draw_rect.y + (_size.Height * Transform.Achor.Y));

        internal void Draw() => Draw(Transform);

        internal void Draw(Transform transformTo)
        {
            var point = Camera.MainCamera.ConvertWorldToScreen(transformTo.Position - Camera.MainCamera.Transform.Position);

            var center = new SDL.SDL_FPoint();

            _size.Width = _rect.Width * transformTo.LocalScale.X;
            _size.Height = _rect.Height * transformTo.LocalScale.Y;

            var unit = Camera.MainCamera.WorldUnit;

            _draw_rect.w = (float)(unit * (_size.Width / PixelPerUnit));
            _draw_rect.h = (float)(unit * (_size.Height / PixelPerUnit));

            _draw_rect.x = (float)(point.X - (_draw_rect.w * transformTo.Achor.X));
            _draw_rect.y = (float)(point.Y - (_draw_rect.h * transformTo.Achor.Y));

            center.x = (float)(_draw_rect.w * transformTo.Achor.X);
            center.y = (float)(_draw_rect.h * transformTo.Achor.Y);

            SDL.SDL_RenderCopyExF(Game.RenderContext, _texture.Instance, ref _texture.SdlRect, ref _draw_rect, transformTo.Degrees, ref center, SDL.SDL_RendererFlip.SDL_FLIP_NONE);

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
    }
}