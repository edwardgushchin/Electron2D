namespace Electron2D;

internal sealed partial class NavigationMapIteration
{
    private readonly record struct SamplingRegion(int Start, int End, uint Layers, bool Enabled, double Area);
    private readonly SamplingRegion[] _samplingRegions;
    private readonly double[] _surfaceAreas;
    private static (SamplingRegion[], double[]) PrepareSampling(Cell[] cells)
    {
        var regions = new List<SamplingRegion>(); var areas = new double[cells.Length];
        for (var start = 0; start < cells.Length;)
        {
            if (cells[start].IsLink) { start++; continue; }
            var end = start; double area = 0;
            while (end < cells.Length && !cells[end].IsLink && cells[end].Region == cells[start].Region) { areas[end] = CellArea(cells[end]); area += areas[end++]; }
            if (area > 0) regions.Add(new(start, end, cells[start].Layers, cells[start].Enabled, area));
            start = end;
        }
        return (regions.ToArray(), areas);
    }
    internal Vector2 RandomPoint(uint layers, bool uniformly)
    {
        double total = 0;
        foreach (var region in _samplingRegions) if (region.Enabled && (region.Layers & layers) != 0) total += uniformly ? region.Area : 1;
        if (total == 0) return Vector2.Zero;
        var choice = Random.Shared.NextDouble() * total; var selected = default(SamplingRegion);
        foreach (var region in _samplingRegions)
        {
            if (!region.Enabled || (region.Layers & layers) == 0) continue;
            selected = region; choice -= uniformly ? region.Area : 1; if (choice < 0) break;
        }
        total = uniformly ? selected.Area : selected.End - selected.Start;
        choice = Random.Shared.NextDouble() * total; var index = selected.End - 1;
        for (var i = selected.Start; i < selected.End; i++) { index = i; choice -= uniformly ? _surfaceAreas[i] : 1; if (choice < 0) break; }
        var polygon = Cells[index];
        total = uniformly ? _surfaceAreas[index] : polygon.Vertices.Length - 2;
        choice = Random.Shared.NextDouble() * total; var triangle = polygon.Vertices.Length - 1;
        for (var i = 2; i < polygon.Vertices.Length; i++) { triangle = i; choice -= uniformly ? Math.Abs(Area(polygon.Vertices[0], polygon.Vertices[i - 1], polygon.Vertices[i])) * .5 : 1; if (choice < 0) break; }
        var u = Math.Sqrt(Random.Shared.NextDouble()); var v = Random.Shared.NextDouble();
        var a = polygon.Vertices[0]; var b = polygon.Vertices[triangle - 1]; var c = polygon.Vertices[triangle];
        return new((float)((1 - u) * a.X + u * (1 - v) * b.X + u * v * c.X), (float)((1 - u) * a.Y + u * (1 - v) * b.Y + u * v * c.Y));
    }
    private static double CellArea(Cell cell) { double result = 0; for (var i = 2; i < cell.Vertices.Length; i++) result += Math.Abs(Area(cell.Vertices[0], cell.Vertices[i - 1], cell.Vertices[i])) * .5; return result; }
}
