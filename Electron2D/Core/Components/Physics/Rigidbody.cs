using System.Numerics;

namespace Electron2D;

public sealed class Rigidbody : IComponent
{
    #region Instance fields
    private Node? _owner;
    private int _lastWorldVersion = -1;
    #endregion

    #region Properties
    /// <summary>
    /// Масса тела.
    /// </summary>
    /// <remarks>
    /// Должна быть конечной и строго больше 0. Проверка выполняется в момент установки значения.
    /// </remarks>
    public float Mass
    {
        get;
        set
        {
            // Предсказуемый контракт: ошибку лучше ловить в момент установки.
            if (!IsValidMass(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Mass must be finite and > 0.");

            field = value;
        }
    } = 1f;
    #endregion

    #region Public API
    /// <summary>
    /// Добавляет силу к телу.
    /// </summary>
    /// <param name="force">Вектор силы в мировых координатах.</param>
    /// <remarks>
    /// Текущая реализация является заглушкой: до подключения физического backend'а метод не оказывает эффекта.
    /// </remarks>
    public void AddForce(Vector2 force)
    {
        // Заглушка: пока нет backend'а физики, метод ничего не делает.
        // Важно: не храним накопленные силы здесь, чтобы не вводить ложный контракт.
    }

    /// <summary>
    /// Вызывается при присоединении компонента к узлу.
    /// </summary>
    /// <param name="owner">Узел-владелец компонента.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    public void OnAttach(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
        _lastWorldVersion = -1; // Гарантируем синхронизацию после attach.
    }

    /// <summary>
    /// Вызывается при отсоединении компонента от узла.
    /// </summary>
    public void OnDetach()
    {
        _owner = null;
        _lastWorldVersion = -1;
    }
    #endregion

    #region Internal helpers
    internal void SyncToPhysicsWorldIfNeeded()
    {
        var owner = _owner;
        if (owner is null)
            return;

        // Пока нет реальной синхронизации с физическим backend'ом:
        // фиксируем последнюю "виденную" версию мира, чтобы в будущем добавить guard
        // (например: owner.Transform.WorldVersion != _lastWorldVersion).
        _lastWorldVersion = owner.Transform.WorldVersion;
    }
    #endregion

    #region Private helpers
    private static bool IsValidMass(float mass) =>
        mass > 0f && !float.IsNaN(mass) && !float.IsInfinity(mass);
    #endregion
}
