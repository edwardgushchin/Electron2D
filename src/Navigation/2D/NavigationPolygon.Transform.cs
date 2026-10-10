namespace Electron2D;

public sealed partial class NavigationPolygon
{
    internal NavigationPolygon TransformedCopy(bool flipH, bool flipV, bool transpose)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var copy = new NavigationPolygon(); var reverse = flipH ^ flipV ^ transpose;
            Vector2[] Points(Vector2[] input, bool preserveWinding)
            {
                var points = new Vector2[input.Length];
                for (var i = 0; i < input.Length; i++)
                {
                    var p = transpose ? new Vector2(input[i].Y, input[i].X) : input[i]; if (flipH) p.X = -p.X; if (flipV) p.Y = -p.Y;
                    points[i] = p;
                }
                if (reverse && preserveWinding) Array.Reverse(points); return points;
            }
            var polygons = _data.Polygons.Select(p => { var indices = (int[])p.Clone(); if (reverse) Array.Reverse(indices); return indices; }).ToArray();
            copy._data = new(Points(_data.Vertices, polygons.Length == 0), polygons, _data.CellSize);
            copy._outlines = _outlines.Select(p => Points(p, true)).ToArray(); copy._settings = _settings; return copy;
        }
    }
}
