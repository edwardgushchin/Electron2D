using System.Numerics;

namespace Electron2D;

/// <summary>
/// Предрассчитанное состояние вида/камеры для рендера и отсечения.
/// Держит только «плоские» значения, чтобы минимизировать вычисления на горячем пути.
/// </summary>
internal readonly struct ViewState(
    float halfW,
    float halfH,
    float ppu,
    Vector2 camPos,
    float camRot,
    float cos,
    float sin,
    bool hasRot,
    bool pixelSnap,
    ViewCullRect cull)
{
    #region Instance fields

    /// <summary>Половина ширины view в пикселях (render space).</summary>
    public readonly float HalfW = halfW;

    /// <summary>Половина высоты view в пикселях (render space).</summary>
    public readonly float HalfH = halfH;

    /// <summary>Pixels-per-unit (PPU) для перевода между пикселями и миром.</summary>
    public readonly float Ppu = ppu;

    /// <summary>Позиция камеры в мире.</summary>
    public readonly Vector2 CamPos = camPos;

    /// <summary>Поворот камеры (в радианах).</summary>
    public readonly float CamRot = camRot;

    /// <summary>cos(<see cref="CamRot"/>) (предрассчитан при наличии вращения).</summary>
    public readonly float Cos = cos;

    /// <summary>sin(<see cref="CamRot"/>) (предрассчитан при наличии вращения).</summary>
    public readonly float Sin = sin;

    /// <summary>Есть ли вращение камеры (позволяет пропускать математику поворота).</summary>
    public readonly bool HasRot = hasRot;

    /// <summary>
    /// Если true — рендер может снапать вершины к целым пикселям, чтобы исключить sub-pixel jitter.
    /// Обычно включается вместе с <see cref="PixelPerfectCamera"/>.
    /// </summary>
    public readonly bool PixelSnap = pixelSnap;

    /// <summary>Прямоугольник отсечения (culling) в мировых координатах.</summary>
    public readonly ViewCullRect Cull = cull;

    #endregion
}