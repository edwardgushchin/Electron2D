using Electron2D.Graphics;

namespace Electron2D.Physics;

public abstract class Collider(string name) : Node(name)
{
    /// <summary>
    /// Проверяет, содержит ли коллайдер точку в мировых координатах
    /// </summary>
    public abstract bool Contains(Vector2 worldPoint);
    
    /// <summary>
    /// Возвращает AABB в мировых координатах
    /// </summary>
    public abstract Bounds Bounds { get; }
    
    /// <summary>
    /// Проверяет пересечение с другим коллайдером
    /// </summary>
    public abstract bool Intersects(Collider other);
    
    /// <summary>
    /// Возвращает вершины коллайдера в мировых координатах (для отладки)
    /// </summary>
    public abstract Vector2[] GetWorldVertices();
    
    public bool ShowDebugOutline { get; set; } = false;
    public Color DebugColor { get; set; } = Color.Green;
}