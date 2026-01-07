namespace Electron2D;

/// <summary>
/// Circle collider
/// </summary>
public sealed class CircleCollider : Collider
{
    /// <summary>
    /// Радиус коллайдера в world units.
    /// </summary>
    public float Radius { get; set; } = 0.5f;

    /// <summary>
    /// Количество сегментов для аппроксимации.
    /// </summary>
    public int Segments { get; set; } = 16;
}