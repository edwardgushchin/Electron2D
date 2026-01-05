using System.Numerics;

namespace Electron2D;

/// <summary>
/// Камера с фиксированным PPU и снапом позиции к пиксельной сетке.
/// Задача: убрать subpixel jitter в pixel-art при движении камеры.
/// </summary>
public sealed class PixelPerfectCamera(string name) : Camera(name)
{
    /// <summary>Сколько экранных пикселей приходится на 1 world-unit по вертикали.</summary>
    public int PixelsPerUnit { get; set; } = 16;

    /// <summary>Снапать позицию камеры к шагу 1/PPU.</summary>
    public bool SnapPosition { get; set; } = true;

    /// <summary>Если true — камера принудительно без вращения (для pixel-art обычно так и надо).</summary>
    public bool EnforceNoRotation { get; set; } = true;

    public PixelSnapMode SnapMode { get; set; } = PixelSnapMode.Round;

    internal float ResolveOrthoSize(int renderHeight)
    {
        var ppu = Math.Max(1, PixelsPerUnit);
        return renderHeight / (2f * ppu);
    }

    internal Vector2 SnapWorldPosition(Vector2 pos, float ppu)
    {
        if (!SnapPosition || !(ppu > 0f))
            return pos;

        var inv = 1f / ppu;

        float Snap(float v) => SnapMode switch
        {
            PixelSnapMode.Floor => MathF.Floor(v * ppu) * inv,
            PixelSnapMode.Ceil  => MathF.Ceiling(v * ppu) * inv,
            _                   => MathF.Round(v * ppu) * inv,
        };

        return new Vector2(Snap(pos.X), Snap(pos.Y));
    }
}