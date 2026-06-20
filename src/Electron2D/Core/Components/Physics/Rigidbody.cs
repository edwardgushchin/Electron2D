using System.Numerics;

namespace Electron2D;

public sealed class Rigidbody : IComponent
{
    #region Instance fields
    private Node? _owner;
    private int _lastWorldVersion = -1;
    private Vector2 _pendingForce;
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
    
    /// <summary>
    /// Тип физического тела.
    /// </summary>
    public PhysicsBodyType BodyType { get; set; } = PhysicsBodyType.Dynamic;
    #endregion

    #region Public API
    /// <summary>
    /// Добавляет силу к телу.
    /// </summary>
    /// <param name="force">Вектор силы в мировых координатах.</param>
    /// <remarks>
    /// Сила будет применена на ближайшем физическом шаге, когда Rigidbody синхронизирован с физическим миром.
    /// </remarks>
    public void AddForce(Vector2 force)
    {
        _pendingForce += force;
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
    internal Node? Owner => _owner;

    internal bool NeedsTransformSync
    {
        get
        {
            var owner = _owner;
            if (owner is null)
                return false;

            return owner.Transform.WorldVersion != _lastWorldVersion;
        }
    }

    internal void MarkTransformSynced()
    {
        var owner = _owner;
        if (owner is null) return;
        
        _lastWorldVersion = owner.Transform.WorldVersion;
    }
    
    internal Vector2 ConsumePendingForce()
    {
        var force = _pendingForce;
        _pendingForce = Vector2.Zero;
        return force;
    }
    #endregion

    #region Private helpers
    private static bool IsValidMass(float mass) =>
        mass > 0f && !float.IsNaN(mass) && !float.IsInfinity(mass);
    #endregion
}
