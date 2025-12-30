using System.Numerics;

namespace Electron2D;

/// <summary>
/// Геометрия спрайта (опционально). Для FullRect можно не задавать.
/// </summary>
public sealed class SpriteMesh(Vector2[] vertices, Vector2[] uv, ushort[] triangles)
{
    private readonly Vector2[] _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
    private readonly Vector2[] _uv = uv ?? throw new ArgumentNullException(nameof(uv));
    private readonly ushort[] _triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));

    public ReadOnlySpan<Vector2> Vertices => _vertices;
    public ReadOnlySpan<Vector2> Uv => _uv;
    public ReadOnlySpan<ushort> Triangles => _triangles;
}