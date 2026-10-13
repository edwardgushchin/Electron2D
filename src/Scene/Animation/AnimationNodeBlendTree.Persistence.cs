using System.Globalization;

namespace Electron2D;

public sealed partial class AnimationNodeBlendTree
{
    private string[]? _fileNodeNames, _fileConnections;
    internal override IEnumerable<PropertyDescriptor> GetGraphFileProperties()
    {
        yield return new PropertyDescriptor<AnimationNodeBlendTree, string[]>("NodeNames", n => n.GetFileNodeNames(), (n, v) => n.SetFileNodeNames(v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeBlendTree, AnimationNodeOutput>("OutputDefinition", n => (AnimationNodeOutput)n._nodes["output"].Node, (n, v) => n.SetFileOutput(v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeBlendTree, Vector2>("OutputPosition", n => n.GetNodePosition("output"), (n, v) => n.SetNodePosition("output", v), stored: true);
        foreach (var name in _fileNodeNames ?? GetFileNodeNames())
        {
            var key = name;
            yield return new PropertyDescriptor<AnimationNodeBlendTree, AnimationNode>($"Node[{name}]", n => n.GetNode(key), (n, v) => n.SetFileNode(key, v), stored: true);
            yield return new PropertyDescriptor<AnimationNodeBlendTree, Vector2>($"Position[{name}]", n => n.GetNodePosition(key), (n, v) => n.SetNodePosition(key, v), stored: true);
        }
        yield return new PropertyDescriptor<AnimationNodeBlendTree, string[]>("Connections", n => n.GetFileConnections(), (n, v) => n.SetFileConnections(v), stored: true);
    }
    private void SetFileOutput(AnimationNodeOutput output)
    {
        ArgumentNullException.ThrowIfNull(output); ObjectDisposedException.ThrowIf(output.IsDisposed, output);
        var entry = _nodes["output"]; if (ReferenceEquals(entry.Node, output)) return;
        var old = entry.Node; entry.Node = output; entry.Inputs = new string[output.GetInputCount()]; old.Dispose();
    }
    private string[] GetFileNodeNames()
    {
        var names = GetNodeList().Where(n => n != "output").ToArray();
        if (names.Length > 4095) throw new InvalidDataException("Blend tree exceeds file node budget.");
        return names;
    }
    private void SetFileNodeNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length > 4095 || names.Any(n => string.IsNullOrEmpty(n) || n.Contains('/') || n == "output") || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new InvalidDataException("Invalid blend-tree node names.");
        foreach (var name in GetFileNodeNames()) RemoveNode(name);
        _nodes["output"].Inputs = new string[_nodes["output"].Node.GetInputCount()];
        _fileNodeNames = names.Length == 0 ? null : (string[])names.Clone(); _fileConnections = null; NotifyPropertyListChanged();
    }
    private void SetFileNode(string name, AnimationNode node)
    {
        AddNode(name, node);
        if (_fileNodeNames is not null && _nodes.Count - 1 == _fileNodeNames.Length) _fileNodeNames = null;
    }
    private string[] GetFileConnections()
    {
        var result = new List<string>();
        foreach (var name in GetNodeList()) for (var i = 0; i < _nodes[name].Inputs.Length; i++)
            {
                var source = Connection(name, i); if (source.Length == 0) continue;
                result.Add(name); result.Add(i.ToString(CultureInfo.InvariantCulture)); result.Add(source);
            }
        return result.ToArray();
    }
    private void SetFileConnections(string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length > 12288 || values.Length % 3 != 0 || values.Any(v => v is null)) throw new InvalidDataException("Invalid graph connections.");
        for (var i = 0; i < values.Length; i += 3)
            if (!int.TryParse(values[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port < 0)
                throw new InvalidDataException("Invalid graph input index.");
        _fileConnections = (string[])values.Clone();
    }
    internal override void ValidateGraphFileState()
    {
        base.ValidateGraphFileState();
        if (_fileNodeNames is not null) throw new InvalidDataException("Blend-tree node table is incomplete.");
        if (_fileConnections is not { } values) return;
        foreach (var entry in _nodes.Values) Array.Fill(entry.Inputs, "");
        for (var i = 0; i < values.Length; i += 3)
            ConnectNode(values[i], int.Parse(values[i + 1], CultureInfo.InvariantCulture), values[i + 2]);
        _fileConnections = null;
    }
}
