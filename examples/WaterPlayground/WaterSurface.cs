using System.Runtime.InteropServices;

namespace Electron2D.Examples.WaterPlayground;

/// <summary>Reconstructs a continuous transparent surface from the solved particle density.</summary>
internal sealed class WaterSurface
{
    private const int CellSize = 4, Columns = 289, Rows = 201;
    private readonly float[] _density = new float[Columns * Rows], _scratch = new float[Columns * Rows];
    private readonly List<Vector2> _vertices = new(32768);
    private readonly List<Color> _colors = new(32768);
    internal ReadOnlySpan<Vector2> Vertices => CollectionsMarshal.AsSpan(_vertices);
    internal ReadOnlySpan<Color> Colors => CollectionsMarshal.AsSpan(_colors);
    internal int VertexCount => _vertices.Count;

    internal float Sample(Vector2 p)
    {
        var x = p.X / CellSize; var y = p.Y / CellSize; var ix = (int)MathF.Floor(x); var iy = (int)MathF.Floor(y);
        if (ix < 0 || iy < 0 || ix >= Columns - 1 || iy >= Rows - 1) return 0;
        var u = x - ix; var v = y - iy; var i = iy * Columns + ix;
        return (_density[i] * (1 - u) + _density[i + 1] * u) * (1 - v) + (_density[i + Columns] * (1 - u) + _density[i + Columns + 1] * u) * v;
    }
    internal bool Submerged(Vector2 p, float radius = 18) => Sample(p) > .75f && Sample(p + new Vector2(0, -radius)) > .75f &&
        Sample(p + new Vector2(radius, 0)) > .75f && Sample(p - new Vector2(radius, 0)) > .75f && Sample(p + new Vector2(0, radius)) > .75f;

    internal void Update(WaterSimulation water)
    {
        Array.Clear(_density);
        var weight = water.Spacing * water.Spacing / (CellSize * CellSize);
        for (var n = 0; n < water.ActiveCount; n++)
        {
            var p = water.Positions[n] / CellSize; var x = (int)MathF.Floor(p.X); var y = (int)MathF.Floor(p.Y);
            if (x < 0 || y < 0 || x >= Columns - 1 || y >= Rows - 1) continue;
            var u = p.X - x; var v = p.Y - y; var i = y * Columns + x;
            _density[i] += weight * (1 - u) * (1 - v); _density[i + 1] += weight * u * (1 - v);
            _density[i + Columns] += weight * (1 - u) * v; _density[i + Columns + 1] += weight * u * v;
        }
        for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Columns; x++)
            {
                var i = y * Columns + x;
                _scratch[i] = (_density[y * Columns + Math.Max(0, x - 2)] + 4 * _density[y * Columns + Math.Max(0, x - 1)] +
                    6 * _density[i] + 4 * _density[y * Columns + Math.Min(Columns - 1, x + 1)] + _density[y * Columns + Math.Min(Columns - 1, x + 2)]) / 16;
            }
        for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Columns; x++)
            {
                var i = y * Columns + x;
                _density[i] = (_scratch[Math.Max(0, y - 2) * Columns + x] + 4 * _scratch[Math.Max(0, y - 1) * Columns + x] +
                    6 * _scratch[i] + 4 * _scratch[Math.Min(Rows - 1, y + 1) * Columns + x] + _scratch[Math.Min(Rows - 1, y + 2) * Columns + x]) / 16;
            }
        // The view looks through water in front of the solid toys. Only bridge their immersed silhouette.
        if (water.Duck.IsValid()) CoverToy(water.DuckPose, 78, 60);
        if (water.Boat.IsValid()) CoverToy(water.BoatPose, 88, 36);
        _vertices.Clear(); _colors.Clear();
        for (var y = 0; y < Rows - 1; y++)
            for (var x = 0; x < Columns - 1; x++)
            {
                var i = y * Columns + x;
                if (Full(i))
                {
                    var start = x;
                    while (x + 1 < Columns - 1 && Full(i + 1)) { x++; i++; }
                    var a = new Vector2(start * CellSize, y * CellSize); var b = new Vector2((x + 1) * CellSize, (y + 1) * CellSize);
                    Triangle(a, new(b.X, a.Y), b, 1, 1, 1); Triangle(a, b, new(a.X, b.Y), 1, 1, 1);
                    continue;
                }
                var p = new Vector2(x * CellSize, y * CellSize);
                Clip(p, p + new Vector2(CellSize, 0), p + new Vector2(CellSize, CellSize), _density[i], _density[i + 1], _density[i + Columns + 1]);
                Clip(p, p + new Vector2(CellSize, CellSize), p + new Vector2(0, CellSize), _density[i], _density[i + Columns + 1], _density[i + Columns]);
            }
    }
    private bool Full(int i) => _density[i] >= .3f && _density[i + 1] >= .3f && _density[i + Columns] >= .3f && _density[i + Columns + 1] >= .3f;
    private void CoverToy(Transform pose, float width, float height)
    {
        var center = pose.Origin;
        var left = Math.Clamp((int)((center.X - width) / CellSize), 0, Columns - 1); var right = Math.Clamp((int)((center.X + width) / CellSize), 0, Columns - 1);
        var top = Math.Clamp((int)((center.Y - height) / CellSize), 0, Rows - 1); var bottom = Math.Clamp((int)((center.Y + height) / CellSize), 0, Rows - 1);
        for (var y = top; y <= bottom; y++)
        {
            var depth = Math.Min(_density[y * Columns + left], _density[y * Columns + right]);
            if (depth < .8f) continue;
            for (var x = left; x <= right; x++) _density[y * Columns + x] = Math.Max(_density[y * Columns + x], depth);
        }
    }
    private void Clip(Vector2 a, Vector2 b, Vector2 c, float da, float db, float dc)
    {
        const float edge = .16f;
        if (da < edge && db < edge && dc < edge) return;
        Span<Vector2> points = stackalloc Vector2[3] { a, b, c }; Span<float> density = stackalloc float[3] { da, db, dc };
        Span<Vector2> clipped = stackalloc Vector2[4]; Span<float> values = stackalloc float[4]; var count = 0;
        for (var i = 0; i < 3; i++)
        {
            var j = (i + 1) % 3;
            if (density[i] >= edge) { clipped[count] = points[i]; values[count++] = density[i]; }
            if ((density[i] >= edge) == (density[j] >= edge)) continue;
            clipped[count] = points[i].Lerp(points[j], (edge - density[i]) / (density[j] - density[i])); values[count++] = edge;
        }
        for (var i = 1; i + 1 < count; i++) Triangle(clipped[0], clipped[i], clipped[i + 1], values[0], values[i], values[i + 1]);
    }
    private void Triangle(Vector2 a, Vector2 b, Vector2 c, float da, float db, float dc)
    { _vertices.Add(a); _vertices.Add(b); _vertices.Add(c); _colors.Add(Tint(a, da)); _colors.Add(Tint(b, db)); _colors.Add(Tint(c, dc)); }
    private static Color Tint(Vector2 p, float density)
    {
        var opacity = Math.Clamp((density - .16f) / .14f, 0, 1); opacity = opacity * opacity * (3 - 2 * opacity);
        var depth = p.Y / WaterSimulation.WorldSize.Y;
        return new(.48f - depth * .10f, .81f - depth * .08f, .85f - depth * .06f, opacity * .66f);
    }
}
