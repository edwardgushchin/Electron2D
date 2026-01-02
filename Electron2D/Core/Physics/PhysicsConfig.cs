using System.Numerics;

namespace Electron2D;

/// <summary>
/// Конфигурация физической подсистемы.
/// </summary>
public sealed class PhysicsConfig
{
    /// <summary>
    /// Гравитация в мировых координатах (units/s²).
    /// </summary>
    public Vector2 Gravity { get; set; } = new(0f, -9.81f);

    /// <summary>
    /// Фиксированный шаг симуляции (секунды).
    /// </summary>
    public float FixedDelta { get; set; } = 1f / 60f;
}