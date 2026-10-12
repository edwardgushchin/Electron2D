namespace Electron2D;

public sealed partial class ArrayMesh
{
    internal void AppendCompiled(ArrayMesh draft)
    {
        lock (Gate)
        {
            ThrowIfDisposed(); if (_blendShapeNames.Count != 0 || draft._blendShapeNames.Count != 0 && _surfaces.Count != 0) throw new ArgumentException("Target shape definitions must be empty and shaped imports require an empty target.", nameof(draft));
            foreach (var s in draft._surfaces) if (s.Material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(Material));
            _surfaces.EnsureCapacity(checked(_surfaces.Count + draft._surfaces.Count)); _blendShapeNames.EnsureCapacity(draft._blendShapeNames.Count);
            _surfaces.AddRange(draft._surfaces); _blendShapeNames.AddRange(draft._blendShapeNames); _blendShapeMode = draft._blendShapeMode;
        }
    }
}
