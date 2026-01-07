using System.Numerics;

namespace Electron2D;

public class Collider : IComponent
{
    private Node? _owner;

    /// <summary>
    /// Локальный сдвиг коллайдера относительно <see cref="Node.Transform"/>.
    /// </summary>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Включает режим сенсора (только события без физического отклика).
    /// </summary>
    public bool IsTrigger { get; set; }

    internal Node? Owner => _owner;

    public void OnAttach(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    public void OnDetach()
    {
        _owner = null;
    }
}