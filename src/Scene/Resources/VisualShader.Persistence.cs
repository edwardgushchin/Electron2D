using System.Globalization;

namespace Electron2D;

public sealed partial class VisualShader
{
    private string[]? _fileKeys;
    private int[]? _fileConnections;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
        yield return new PropertyDescriptor<VisualShader, string[]>("GraphNodes", n => n.FileKeys(), (n, v) => n.RestoreKeys(v), stored: true);
        for (var stageIndex = 0; stageIndex < _graphs.Length; stageIndex++)
        {
            var stage = (Type)stageIndex;
            yield return new PropertyDescriptor<VisualShader, VisualShaderNodeOutput>($"Output[{stageIndex}]", n => (VisualShaderNodeOutput)n.GetNode(stage, 0),
                (n, v) => n.RestoreOutput(stage, v), stored: true);
            yield return new PropertyDescriptor<VisualShader, Vector2>($"OutputPosition[{stageIndex}]", n => n.GetNodePosition(stage, 0),
                (n, v) => n.SetNodePosition(stage, 0, v), stored: true);
        }
        foreach (var key in _fileKeys ?? FileKeys())
        {
            var (stage, id) = ParseKey(key);
            yield return new PropertyDescriptor<VisualShader, VisualShaderNode>($"Node[{key}]", n => n.GetNode(stage, id),
                (n, v) => n.RestoreNode(stage, id, v), stored: true);
            yield return new PropertyDescriptor<VisualShader, Vector2>($"Position[{key}]", n => n.GetNodePosition(stage, id),
                (n, v) => n.SetNodePosition(stage, id, v), stored: true);
        }
        yield return new PropertyDescriptor<VisualShader, int[]>("GraphConnections", n => n.FileConnections(), (n, v) => n.RestoreConnections(v), stored: true);
    }
    private string[] FileKeys() => _graphs.SelectMany((g, stage) => g.Nodes.Keys.Where(id => id != 0)
        .Select(id => stage.ToString(CultureInfo.InvariantCulture) + "/" + id.ToString(CultureInfo.InvariantCulture))).ToArray();
    private static (Type Stage, int ID) ParseKey(string key)
    {
        var parts = key.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var stage) || stage is < 0 or > 1 ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 2 || key != stage.ToString(CultureInfo.InvariantCulture) + "/" + id.ToString(CultureInfo.InvariantCulture))
            throw new InvalidDataException("Invalid visual shader node key.");
        return ((Type)stage, id);
    }
    private void RestoreKeys(string[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length > (MaximumNodes - 1) * 2 || keys.Any(k => k is null) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new InvalidDataException("Invalid visual shader node table.");
        foreach (var key in keys) _ = ParseKey(key);
        if (keys.GroupBy(k => ParseKey(k).Stage).Any(g => g.Count() >= MaximumNodes)) throw new InvalidDataException("Visual shader node capacity exceeded.");
        for (var stage = 0; stage < _graphs.Length; stage++)
        {
            foreach (var id in _graphs[stage].Nodes.Keys.Where(id => id != 0).ToArray()) RemoveNode((Type)stage, id);
            _graphs[stage].Connections.Clear();
        }
        _fileKeys = (string[])keys.Clone(); _fileConnections = null;
    }
    private void RestoreNode(Type stage, int id, VisualShaderNode node) => AddNode(stage, node, Vector2.Zero, id);
    private void RestoreOutput(Type stage, VisualShaderNodeOutput output)
    {
        ArgumentNullException.ThrowIfNull(output); ObjectDisposedException.ThrowIf(output.IsDisposed, output);
        var entry = GetEntry(stage, 0); if (ReferenceEquals(entry.Node, output)) return;
        if (_graphs.Any(g => g.Nodes.Values.Any(e => ReferenceEquals(e.Node, output)))) throw new InvalidDataException("Visual shader outputs must be independent.");
        var previous = entry.Node; Unwatch(previous); output.Stage = stage; entry.Node = output; Watch(output); _ownedDefinitions.Add(output);
        if (TakeRetired(previous) is { } retired) retired.Dispose();
    }
    private int[] FileConnections() => _graphs.SelectMany((g, stage) => g.Connections.SelectMany(c => new[] { stage, c.FromNode, c.FromPort, c.ToNode, c.ToPort })).ToArray();
    private void RestoreConnections(int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length % 5 != 0 || values.Length > MaximumNodes * 8 * 2 * 5) throw new InvalidDataException("Invalid visual shader connections.");
        for (var i = 0; i < values.Length; i += 5)
            if (values[i] is < 0 or > 1 || values[i + 1] < 0 || values[i + 2] < 0 || values[i + 3] < 0 || values[i + 4] < 0)
                throw new InvalidDataException("Invalid visual shader connection index.");
        for (var stage = 0; stage < _graphs.Length; stage++)
            if (Enumerable.Range(0, values.Length / 5).Count(i => values[i * 5] == stage) > MaximumNodes * 8)
                throw new InvalidDataException("Visual shader connection capacity exceeded.");
        _fileConnections = (int[])values.Clone();
    }
    internal override void ValidateFileState()
    {
        base.ValidateFileState();
        for (var stage = 0; stage < _graphs.Length; stage++)
            if (!_graphs[stage].Nodes.TryGetValue(0, out var entry) || entry.Node is not VisualShaderNodeOutput output || output.Stage != (Type)stage)
                throw new InvalidDataException("Visual shader output stage mismatch.");
        if (_fileKeys is not null)
        {
            if (!FileKeys().Order(StringComparer.Ordinal).SequenceEqual(_fileKeys.Order(StringComparer.Ordinal)))
                throw new InvalidDataException("Incomplete visual shader node table.");
            _fileKeys = null;
        }
        if (_fileConnections is not { } values) return;
        foreach (var graph in _graphs) graph.Connections.Clear();
        for (var i = 0; i < values.Length; i += 5)
        {
            var graph = GetGraph((Type)values[i]); var edge = new VisualShaderConnection(values[i + 1], values[i + 2], values[i + 3], values[i + 4]);
            if (!PortsExist(graph, edge) || graph.Connections.Contains(edge)) throw new InvalidDataException("Invalid stored visual shader connection.");
            graph.Connections.Add(edge);
        }
        _fileConnections = null;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShader();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        var copy = (VisualShader)target;
        var ownedCopies = new Dictionary<VisualShaderNode, VisualShaderNode>(ReferenceEqualityComparer.Instance);
        for (var stage = 0; stage < _graphs.Length; stage++)
        {
            var to = copy._graphs[stage];
            foreach (var id in to.Nodes.Keys.Where(id => id != 0).ToArray())
            {
                var previous = to.Nodes[id].Node; copy.Unwatch(previous); to.Nodes.Remove(id);
                if (copy.TakeRetired(previous) is { } retired) retired.Dispose();
            }
            if (deep) copy.RestoreOutput((Type)stage, (VisualShaderNodeOutput)forceDuplicateSubresource(_graphs[stage].Nodes[0].Node)!);
            else to.Nodes[0].Node.CopyFromResource(_graphs[stage].Nodes[0].Node);
            to.Nodes[0].Position = _graphs[stage].Nodes[0].Position;
            to.Connections.Clear();
            foreach (var (id, entry) in _graphs[stage].Nodes)
            {
                if (id == 0) continue;
                var node = deep ? (VisualShaderNode)duplicateSubresource(entry.Node)! : entry.Node;
                if (_ownedDefinitions.Contains(entry.Node))
                {
                    if (deep) node = (VisualShaderNode)forceDuplicateSubresource(entry.Node)!;
                    else
                    {
                        if (!ownedCopies.TryGetValue(entry.Node, out node))
                        { node = (VisualShaderNode)entry.Node.Duplicate(); ownedCopies.Add(entry.Node, node); }
                    }
                    copy._ownedDefinitions.Add(node);
                }
                to.Nodes.Add(id, new(node, entry.Position)); copy.Watch(node);
            }
            to.Connections.AddRange(_graphs[stage].Connections);
        }
        copy._fileKeys = null; copy._fileConnections = null;
    }
    /// <inheritdoc />
    protected override void RemapResourceReferences(Func<Resource, Resource> remap)
    {
        base.RemapResourceReferences(remap);
        var replacements = _graphs.SelectMany((g, stage) => g.Nodes.Select(e => (Stage: stage, ID: e.Key, Entry: e.Value,
            Node: remap(e.Value.Node) as VisualShaderNode ?? throw new InvalidDataException("A graph node must remain a visual shader operation.")))).ToArray();
        foreach (var replacement in replacements)
            if (replacement.ID == 0 && replacement.Node is not VisualShaderNodeOutput || replacement.ID != 0 && replacement.Node is VisualShaderNodeOutput)
                throw new InvalidDataException("Invalid visual shader output replacement.");
        var owned = new HashSet<VisualShaderNode>(ReferenceEqualityComparer.Instance);
        foreach (var replacement in replacements)
        {
            if (_ownedDefinitions.Contains(replacement.Entry.Node)) owned.Add(replacement.Node);
            Unwatch(replacement.Entry.Node); replacement.Entry.Node = replacement.Node; Watch(replacement.Node);
            if (replacement.Node is VisualShaderNodeOutput output) output.Stage = (Type)replacement.Stage;
        }
        var retired = _ownedDefinitions.Where(node => !owned.Contains(node) && !_watched.ContainsKey(node)).ToArray();
        _ownedDefinitions.Clear(); foreach (var node in owned) _ownedDefinitions.Add(node);
        List<Exception>? errors = null;
        foreach (var node in retired) try { node.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException(errors);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var node in _watched.Keys) node.Changed -= _childChanged;
            _watched.Clear();
            List<Exception>? errors = null;
            var owned = _ownedDefinitions.ToArray(); _ownedDefinitions.Clear();
            foreach (var graph in _graphs) { graph.Nodes.Clear(); graph.Connections.Clear(); }
            foreach (var node in owned)
                try { node.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            try { base.Dispose(disposing); } catch (Exception error) { (errors ??= []).Add(error); }
            if (errors is { Count: > 0 }) throw new AggregateException(errors);
        }
        else base.Dispose(disposing);
    }
}
