namespace Electron2D;

public partial class AnimationNode
{
    private IEnumerable<PropertyDescriptor> GetBaseFileProperties()
    {
        yield return new PropertyDescriptor<AnimationNode, string[]>("InputNames", n => n._inputs.ToArray(), (n, v) => n.SetFileInputs(v), stored: true);
        yield return new PropertyDescriptor<AnimationNode, string[]>("FilterPaths", n => n._filters.Order(StringComparer.Ordinal).ToArray(), (n, v) => n.SetFileFilters(v), stored: true);
    }
    internal virtual IEnumerable<PropertyDescriptor> GetGraphFileProperties() => [];
    private void SetFileInputs(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length > 1024 || names.Any(n => n is null || n.Contains('/') || n.Contains('.')) || this is AnimationRootNode && names.Length != 0)
            throw new InvalidDataException("Invalid graph input table.");
        while (_inputs.Count != 0) RemoveInput(_inputs.Count - 1);
        foreach (var name in names) if (!AddInput(name)) throw new InvalidDataException("Graph input cannot be restored.");
        NotifyPropertyListChanged();
    }
    private void SetFileFilters(string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Length > 4096 || paths.Any(p => p is null) || paths.Distinct(StringComparer.Ordinal).Count() != paths.Length)
            throw new InvalidDataException("Invalid graph filter paths.");
        _filters.Clear(); foreach (var path in paths) _filters.Add(path); EmitGraphChanged();
    }
    internal override void ValidateFileState()
    {
        base.ValidateFileState();
        var seen = new HashSet<AnimationNode>(ReferenceEqualityComparer.Instance);
        var stack = new HashSet<AnimationNode>(ReferenceEqualityComparer.Instance);
        Visit(this, 0);
        void Visit(AnimationNode node, int depth)
        {
            ObjectDisposedException.ThrowIf(node.IsDisposed, node);
            if (depth > 128 || !stack.Add(node)) throw new InvalidDataException("Animation graph is cyclic or exceeds file depth.");
            if (seen.Add(node))
            {
                if (seen.Count > 4096) throw new InvalidDataException("Animation graph exceeds file node budget.");
                node.ValidateGraphFileState();
                foreach (var child in node.Children()) Visit(child.Value, depth + 1);
            }
            stack.Remove(node);
        }
    }
    internal virtual void ValidateGraphFileState()
    {
        if (_inputs.Count > 1024 || _filters.Count > 4096) throw new InvalidDataException("Graph input/filter count exceeds file budget.");
    }
}
