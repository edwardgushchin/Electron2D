using SDL3;

namespace Electron2D;

public struct Rect
{
    public float Width;

    public float Height;

    public float X;
    
    public float Y;
    
    public static implicit operator SDL.FRect(Rect r) =>
        new() { X = r.X, Y = r.Y, W = r.Width, H = r.Height };

    public static implicit operator Rect(SDL.FRect r) =>
        new(){ X = r.X, Y = r.Y, Width = r.W, Height = r.H};
}