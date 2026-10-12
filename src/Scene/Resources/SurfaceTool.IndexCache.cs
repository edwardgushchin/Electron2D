using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class SurfaceTool
{
    /// <summary>Reorders indexed triangles for vertex-cache locality without changing their winding or attributes.</summary>
    /// <remarks>Uses bounded adjacency traversal and accepts a candidate only when a sixteen-entry LRU model
    /// improves. Global triangle order may change. Retained canvas drawing expands indices; native indexed device
    /// consumers can use the copied result. Actual GPU cache sizes and performance are not inferred from this model.</remarks>
    /// <exception cref="InvalidOperationException">The surface is not an indexed complete triangle list.</exception>
    /// <exception cref="ArgumentException">An index does not address captured geometry.</exception>
    public void OptimizeIndicesForCache()
    {
        lock (_gate)
        {
            Mutable(); if (_primitive != Mesh.PrimitiveType.Triangles || _indices.Count == 0 || _indices.Count % 3 != 0) throw new InvalidOperationException("Cache optimization requires indexed complete triangles."); ValidateIndices();
            var source = CollectionsMarshal.AsSpan(_indices); var triangles = source.Length / 3;
            var offsets = new int[checked(_vertices.Count + 1)]; foreach (var vertex in source) offsets[vertex + 1]++;
            for (var i = 1; i < offsets.Length; i++) offsets[i] += offsets[i - 1];
            var cursor = (int[])offsets.Clone(); var adjacency = new int[source.Length]; for (var i = 0; i < source.Length; i++) adjacency[cursor[source[i]]++] = i / 3;
            var visited = new bool[triangles]; var expanded = new bool[_vertices.Count]; var pending = new Stack<int>(checked(source.Length + triangles)); var candidate = new int[source.Length]; var written = 0; var seed = 0;
            while (written != source.Length)
            {
                if (pending.Count == 0) { while (visited[seed]) seed++; pending.Push(seed); }
                var triangle = pending.Pop(); if (visited[triangle]) continue; visited[triangle] = true;
                source.Slice(triangle * 3, 3).CopyTo(candidate.AsSpan(written)); written += 3;
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = source[triangle * 3 + corner]; if (expanded[vertex]) continue; expanded[vertex] = true;
                    for (var i = offsets[vertex + 1] - 1; i >= offsets[vertex]; i--) if (!visited[adjacency[i]]) pending.Push(adjacency[i]);
                }
            }
            if (CacheMisses(candidate) < CacheMisses(source)) _indices = new List<int>(candidate);
        }
    }
    private static long CacheMisses(ReadOnlySpan<int> indices)
    {
        Span<int> cache = stackalloc int[16]; cache.Fill(-1); long misses = 0;
        foreach (var vertex in indices)
        {
            var position = 0; while (position < cache.Length && cache[position] != vertex) position++;
            if (position == cache.Length) { misses++; position--; }
            for (var i = position; i > 0; i--) cache[i] = cache[i - 1]; cache[0] = vertex;
        }
        return misses;
    }
}
