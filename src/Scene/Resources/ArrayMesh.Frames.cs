namespace Electron2D;

public sealed partial class ArrayMesh
{
    /// <summary>Regenerates each surface's tangent channel from its existing normals and primary UVs.</summary>
    /// <remarks>Uses the same MikkTSpace path as SurfaceTool. Empty meshes do nothing. Every surface is
    /// prepared before publication; errors preserve all prior surfaces. Positions, indices, names, materials,
    /// policy flags and exact packed normal words remain unchanged. Success publishes one Changed event.
    /// Structural regeneration may allocate.</remarks>
    /// <exception cref="InvalidOperationException">A surface lacks the required normal/UV data or complete face.</exception>
    /// <exception cref="ObjectDisposedException">The mesh or a borrowed material is disposed.</exception>
    public void RegenNormalMaps()
    {
        lock (Gate)
        {
            ThrowIfDisposed(); if (_surfaces.Count == 0) return;
            var prepared = new List<Surface>(_surfaces.Count);
            foreach (var surface in _surfaces)
            {
                if (surface.Material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(Material));
                using var tool = new SurfaceTool(); tool.CreateFromArrays(surface.Data, surface.Primitive); tool.GenerateTangents();
                var data = tool.CommitToArrays(); data.Validate(surface.Primitive);
                var next = new Surface(data, surface.Primitive, surface.Format) { Name = surface.Name, Material = surface.Material };
                if (data.Normals.Length != 0)
                {
                    var oldStride = surface.FrameWords.Length / data.Vertices.Length; var newStride = next.FrameWords.Length / data.Vertices.Length;
                    for (var i = 0; i < data.Vertices.Length; i++) { var word = surface.FrameWords[i * oldStride]; next.FrameWords[i * newStride] = word; MeshFrameCodec.RestoreWord(data, i * newStride, word); }
                }
                prepared.Add(next);
            }
            _surfaces.Clear(); _surfaces.AddRange(prepared);
        }
        EmitChanged();
    }
}
