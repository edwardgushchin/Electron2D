using System;

namespace Electron2D;

#region PhysicsSystem

/// <summary>
/// Система физики (на текущем этапе — заглушка).
/// </summary>
internal sealed class PhysicsSystem
{
    #region Public API

    public void Initialize(PhysicsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
    }

    public void Shutdown()
    {
    }

    public void Step(float fixedDeltaTime, SceneTree sceneTree)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);

        // TODO: Интеграция Box2D.
        // Сейчас оставлено как заглушка: компонент Rigidbody уже готов к versioned-sync.
        _ = fixedDeltaTime; // подавляем предупреждение о неиспользуемом параметре, пока Step не реализован
    }

    #endregion
}

#endregion