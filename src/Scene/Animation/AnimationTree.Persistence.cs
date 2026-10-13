namespace Electron2D;

public partial class AnimationTree
{
    private string[]? _fileParameterSchemas;
    private readonly Dictionary<(string Path, AnimationParameter Key), StoredPropertyValue> _fileParameterValues = [];
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory()
    {
        if (GetType() != typeof(AnimationTree)) return base.CreateSceneInstanceFactory();
        return CreateFileTree;
    }
    private static Node CreateFileTree() => new AnimationTree();
    private IEnumerable<PropertyDescriptor> GetFileParameterProperties()
    {
        yield return new PropertyDescriptor<AnimationTree, string[]>("ParameterSchemas", n => n.GetFileParameterSchemas(), (n, v) => n.SetFileParameterSchemas(v), stored: true);
        var schemas = _fileParameterSchemas ?? GetFileParameterSchemas();
        for (var i = 0; i < schemas.Length; i += 2)
            yield return ResourceFileTypes.AnimationParameterBinding(schemas[i + 1]).Property(schemas[i], i / 2);
    }
    private Dictionary<string, AnimationNode> FileDefinitions()
    {
        var nodes = new Dictionary<string, AnimationNode>(StringComparer.Ordinal);
        if (_root is null) return nodes;
        _root.ValidateFileState(); Visit(_root, "", 0);
        return nodes;
        void Visit(AnimationNode node, string path, int depth)
        {
            if (nodes.Count >= 4096 || depth > 128) throw new InvalidDataException("Graph instance exceeds file budget.");
            nodes.Add(path, node);
            foreach (var child in node.Children()) Visit(child.Value, path.Length == 0 ? child.Key : path + "/" + child.Key, depth + 1);
        }
    }
    private string[] GetFileParameterSchemas()
    {
        if (_fileParameterSchemas is not null) return (string[])_fileParameterSchemas.Clone();
        var schemas = new List<string>();
        foreach (var (path, node) in FileDefinitions().OrderBy(n => n.Key, StringComparer.Ordinal))
            foreach (var parameter in node.Parameters().Where(p => !node.ParameterReadOnly(p)).OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                schemas.Add(path); schemas.Add(ResourceFileTypes.AnimationParameterBinding(parameter).ID);
                if (schemas.Count > 32768) throw new InvalidDataException("Graph parameters exceed file budget.");
            }
        return schemas.ToArray();
    }
    private void SetFileParameterSchemas(string[] schemas)
    {
        EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(schemas);
        if (schemas.Length > 32768 || schemas.Length % 2 != 0 || schemas.Any(s => s is null)) throw new InvalidDataException("Invalid tree parameter schemas.");
        var seen = new HashSet<(string, string)>();
        for (var i = 0; i < schemas.Length; i += 2)
        {
            var binding = ResourceFileTypes.AnimationParameterBinding(schemas[i + 1]);
            if (binding.Parameter.IsReadOnly || !seen.Add((schemas[i], binding.Parameter.Name))) throw new InvalidDataException("Read-only or repeated graph parameter.");
        }
        _fileParameterSchemas = (string[])schemas.Clone(); _fileParameterValues.Clear(); NotifyPropertyListChanged();
    }
    internal T GetFileParameter<T>(string path, AnimationParameter<T> key)
    {
        if (_fileParameterValues.TryGetValue((path, key), out var staged) && staged is StoredPropertyValue<T> value) return value.Resolve(static r => r);
        var definitions = FileDefinitions();
        if (!definitions.TryGetValue(path, out var definition) || !definition.Parameters().Any(p => ReferenceEquals(p, key))) throw new InvalidDataException("Graph parameter does not match its definition.");
        if (_instances.TryGetValue(path, out var instance) && ReferenceEquals(instance.Definition, definition) &&
            instance.Parameters.TryGetValue(key.Name, out var slot) && ReferenceEquals(slot.Parameter, key) && slot is AnimationParameterSlot<T> typed) return typed.Value;
        return definition.ParameterDefault(key);
    }
    internal void SetFileParameter<T>(string path, AnimationParameter<T> key, T value)
    {
        EnsureAnimationMutable();
        if (key.IsReadOnly) throw new InvalidDataException("Read-only graph memory is not authored scene data.");
        if (value is double scalar) Animation.Finite(scalar);
        if (value is float number) Animation.Finite(number);
        if (value is Vector2 position && !position.IsFinite()) throw new InvalidDataException("Graph vector parameter must be finite.");
        _fileParameterValues[(path, key)] = new StoredPropertyValue<T>(StoredPropertyValue<T>.Snapshot(value));
    }
    internal override void ValidateSceneInstance()
    {
        base.ValidateSceneInstance();
        if (_fileParameterSchemas is not { } schemas) return;
        var definitions = FileDefinitions();
        for (var i = 0; i < schemas.Length; i += 2)
        {
            var key = ResourceFileTypes.AnimationParameterBinding(schemas[i + 1]).Parameter;
            if (!definitions.TryGetValue(schemas[i], out var node) || node.ParameterReadOnly(key) || !node.Parameters().Any(p => ReferenceEquals(p, key)) ||
                !_fileParameterValues.ContainsKey((schemas[i], key))) throw new InvalidDataException("Incomplete or incompatible authored parameter.");
        }
        Prepare();
        for (var i = 0; i < schemas.Length; i += 2) ResourceFileTypes.AnimationParameterBinding(schemas[i + 1]).Restore(this, schemas[i]);
        _fileParameterSchemas = null; _fileParameterValues.Clear(); _started = true;
    }
}
