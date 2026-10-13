namespace Electron2D;

public sealed partial class AnimationNodeBlendSpace2D
{
    private string[]? _filePointNames;
    internal override IEnumerable<PropertyDescriptor> GetGraphFileProperties()
    {
        yield return new PropertyDescriptor<AnimationNodeBlendSpace2D, string[]>("PointNames", n => n._points.Items.Select(p => p.Name).ToArray(), (n, v) => n.SetFilePointNames(v), stored: true);
        var names = _filePointNames ?? _points.Items.Select(p => p.Name).ToArray();
        for (var i = 0; i < names.Length; i++)
        {
            var index = i; var name = names[i];
            yield return new PropertyDescriptor<AnimationNodeBlendSpace2D, AnimationRootNode>($"Point[{i}].Node", n => n.GetBlendPointNode(index), (n, v) => n.SetFilePointNode(name, v), stored: true);
            yield return new PropertyDescriptor<AnimationNodeBlendSpace2D, Vector2>($"Point[{i}].Position", n => n.GetBlendPointPosition(index), (n, v) => n.SetBlendPointPosition(index, v), stored: true);
        }
        yield return new PropertyDescriptor<AnimationNodeBlendSpace2D, int[]>("Triangles", n => n.GetFileTriangles(), (n, v) => n.SetFileTriangles(v), stored: true);
    }
    private void SetFilePointNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length > 64 || names.Any(n => string.IsNullOrEmpty(n) || n.Contains('/') || n.Contains('.')) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new InvalidDataException("Invalid blend-point names.");
        while (_points.Items.Count != 0) RemoveBlendPoint(_points.Items.Count - 1);
        _filePointNames = names.Length == 0 ? null : (string[])names.Clone(); NotifyPropertyListChanged();
    }
    private void SetFilePointNode(string name, AnimationRootNode node)
    {
        AddBlendPoint(node, default, name: name);
        if (_filePointNames is not null && _points.Items.Count == _filePointNames.Length) _filePointNames = null;
    }
    internal override void ValidateGraphFileState()
    {
        base.ValidateGraphFileState();
        if (_filePointNames is not null) throw new InvalidDataException("Blend-point resource table is incomplete.");
        if (_fileTriangles is not { } triangles) return;
        _triangles.Clear(); _trianglesDirty = false;
        for (var i = 0; i < triangles.Length; i += 3) AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
        _trianglesDirty = false; _fileTriangles = null;
    }
    private int[]? _fileTriangles;
    private int[] GetFileTriangles()
    {
        UpdateTriangles(); return _triangles.SelectMany(t => new[] { t.A, t.B, t.C }).ToArray();
    }
    private void SetFileTriangles(int[] triangles)
    {
        ArgumentNullException.ThrowIfNull(triangles);
        if (triangles.Length > 12288 || triangles.Length % 3 != 0 || triangles.Any(i => i < 0 || i >= _points.Items.Count))
            throw new InvalidDataException("Invalid planar triangle table.");
        _fileTriangles = (int[])triangles.Clone();
    }
}
