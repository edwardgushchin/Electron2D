using Electron2D.Components;
using Electron2D.Physics;
using Electron2D.Platform;
using SDL3;

namespace Electron2D.Graphics;

internal class Render : IDisposable
{
    private int _width, _height;
    
    internal readonly IntPtr Handle;
    
    private SDL.FPoint _centerCache;
    
    private SDL.FRect _dstRectCache;

    public Render(Window window)
    {
        Handle = SDL.CreateRenderer(window.Handle, null);
        if (Handle == IntPtr.Zero)
        {
            throw new Exception($"Renderer could not be created! SDL Error: {SDL.GetError()}");
        }

        UpdateRenderSize();
    }
    
    public void UpdateRenderSize()
    {
        SDL.GetRenderOutputSize(Handle, out _width, out _height);
    }

    public (int width, int height) GetWidthAndHeight()
    {
        return (_width, _height);
    }
    
    internal void DrawSprite(Sprite sprite)
    {
        var camera = Camera.ActiveCamera!;
        
        var rotationDegrees = sprite.Transform.GlobalRotation * (180f / Math.PI);

        var relativePos = camera.ConvertWorldToScreen(sprite.Transform.GlobalPosition);

        var cameraZoom = camera.WorldUnit * camera.Zoom;
        
        var dstWidth = (sprite.SourceRect.Width / sprite.PixelsPerUnit) * sprite.Transform.GlobalScale.X * cameraZoom;
        var dstHeight = (sprite.SourceRect.Height / sprite.PixelsPerUnit) * sprite.Transform.GlobalScale.Y * cameraZoom;

        _centerCache.X = dstWidth * sprite.Center.X;
        _centerCache.Y = dstHeight * sprite.Center.Y;

        _dstRectCache.X = relativePos.X - _centerCache.X;
        _dstRectCache.Y = relativePos.Y - _centerCache.Y;
        _dstRectCache.W = dstWidth;
        _dstRectCache.H = dstHeight;
        
        var (screenWidth, screenHeight) = GetWidthAndHeight();

        // Проверяем пересечение с экраном
        if (_dstRectCache.X + _dstRectCache.W < 0 ||
            _dstRectCache.Y + _dstRectCache.H < 0 ||
            _dstRectCache.X > screenWidth ||
            _dstRectCache.Y > screenHeight)
        {
            // Полностью за пределами экрана - ничего не рисуем
            return;
        }
 
        var srcRect = (SDL.FRect)sprite.SourceRect;

        SDL.RenderTextureRotated(Handle, sprite.Texture.Handle, in srcRect, in _dstRectCache, 
            rotationDegrees, in _centerCache, (SDL.FlipMode)sprite.Flip);
        
        if(sprite.ShowDebugRect) DrawRect(_dstRectCache, Color.Green);
    }
    
    private void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderLine(Handle, start.X, start.Y, end.X, end.Y);
    }
    
    private void DrawLines(Vector2[] points, Color color)
    {
        if (points.Length < 2) return;
        
        var sdlPoints = new SDL.FPoint[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            sdlPoints[i] = points[i];
        }
        
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderLines(Handle, sdlPoints, sdlPoints.Length);
    }

    private void DrawRect(Rect rect, Color color)
    {
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderRect(Handle, rect);
    }

    private void DrawRects(Rect[] rect, Color color)
    {
        var sdlRects = new SDL.FRect[rect.Length];

        for (var i = 0; i < rect.Length; i++)
        {
            sdlRects[i] = rect[i];
        }
        
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderRects(Handle, sdlRects, sdlRects.Length);
    }
    
    private void DrawPoint(Vector2 point, Color color)
    {
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        
        // Draw a small cross at the center
        const float size = 3f;
        
        SDL.RenderLine(Handle, point.X - size, point.Y, point.X + size, point.Y);
        SDL.RenderLine(Handle, point.X, point.Y - size, point.X, point.Y + size);
    }


    private void DrawDebugText(string text, Vector2 position, Color color)
    {
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderDebugText(Handle, position.X, position.Y, text);
    }
    
    internal void DrawColliderDebug(Collider collider)
    {
        if (!collider.ShowDebugOutline) return;
        
        var camera = Camera.ActiveCamera!;
        
        var vertices = collider.GetWorldVertices();
        if (vertices.Length < 2) return;

        for (var i = 0; i < vertices.Length; i++)
        {
            var start = camera.ConvertWorldToScreen(vertices[i]);
            var end = camera.ConvertWorldToScreen(i == vertices.Length - 1 ? vertices[0] : vertices[i + 1]);
            DrawLine(start, end, collider.DebugColor);
        }

        var center = camera.ConvertWorldToScreen(collider.Bounds.Center);
        DrawPoint(center, collider.DebugColor);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyRenderer(Handle);
        }
    }
}