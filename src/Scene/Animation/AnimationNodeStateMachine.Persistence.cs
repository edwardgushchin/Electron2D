namespace Electron2D;

public sealed partial class AnimationNodeStateMachine
{
    private string[]? _fileStateNames, _fileEdgePairs;
    internal override IEnumerable<PropertyDescriptor> GetGraphFileProperties()
    {
        yield return new PropertyDescriptor<AnimationNodeStateMachine, string[]>("StateNames", n => n.GetFileStateNames(), (n, v) => n.SetFileStateNames(v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeStateMachine, AnimationRootNode>("StartDefinition", n => n._start, (n, v) => n.SetFileBoundary(true, v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeStateMachine, AnimationRootNode>("EndDefinition", n => n._end, (n, v) => n.SetFileBoundary(false, v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeStateMachine, Vector2>("StartPosition", n => n.GetNodePosition("Start"), (n, v) => n.SetNodePosition("Start", v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeStateMachine, Vector2>("EndPosition", n => n.GetNodePosition("End"), (n, v) => n.SetNodePosition("End", v), stored: true);
        yield return new PropertyDescriptor<AnimationNodeStateMachine, Vector2>("GraphOffset", n => n.GetGraphOffset(), (n, v) => n.SetGraphOffset(v), stored: true);
        foreach (var name in _fileStateNames ?? GetFileStateNames())
        {
            var key = name;
            yield return new PropertyDescriptor<AnimationNodeStateMachine, AnimationNode>($"State[{name}]", n => n.GetNode(key), (n, v) => n.SetFileState(key, v), stored: true);
            yield return new PropertyDescriptor<AnimationNodeStateMachine, Vector2>($"Position[{name}]", n => n.GetNodePosition(key), (n, v) => n.SetNodePosition(key, v), stored: true);
        }
        yield return new PropertyDescriptor<AnimationNodeStateMachine, string[]>("EdgePairs", n => n.Edges.SelectMany(e => new[] { e.From, e.To }).ToArray(), (n, v) => n.SetFileEdgePairs(v), stored: true);
        var pairs = _fileEdgePairs ?? Edges.SelectMany(e => new[] { e.From, e.To }).ToArray();
        for (var i = 0; i < pairs.Length; i += 2)
        {
            var index = i / 2; var from = pairs[i]; var to = pairs[i + 1];
            yield return new PropertyDescriptor<AnimationNodeStateMachine, AnimationNodeStateMachineTransition>($"Edge[{index}]", n => n.GetTransition(index), (n, v) => n.SetFileEdge(from, to, v), stored: true);
        }
    }
    private string[] GetFileStateNames()
    {
        var names = GetNodeList().Where(n => n is not ("Start" or "End")).ToArray();
        if (names.Length > 4094 || Edges.Count > 4096) throw new InvalidDataException("State machine exceeds file budget.");
        return names;
    }
    private void SetFileStateNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length > 4094 || names.Any(n => string.IsNullOrEmpty(n) || n.Contains('/') || n is "Start" or "End") || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new InvalidDataException("Invalid state names.");
        while (Edges.Count != 0) RemoveTransitionByIndex(Edges.Count - 1);
        foreach (var name in GetFileStateNames()) RemoveNode(name);
        _fileStateNames = names.Length == 0 ? null : (string[])names.Clone(); _fileEdgePairs = null; NotifyPropertyListChanged();
    }
    private void SetFileState(string name, AnimationNode node)
    {
        AddNode(name, node);
        if (_fileStateNames is not null && States.Count - 2 == _fileStateNames.Length) _fileStateNames = null;
    }
    private void SetFileBoundary(bool start, AnimationRootNode node)
    {
        ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        if (node.GetType() != typeof(AnimationRootNode) || ReferenceEquals(node, start ? _end : _start)) throw new InvalidDataException("Invalid owned boundary definition.");
        var old = start ? _start : _end; if (ReferenceEquals(node, old)) return;
        if (start) _start = node; else _end = node;
        States[start ? "Start" : "End"].Node = node; old.Dispose();
    }
    private void SetFileEdgePairs(string[] pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        if (pairs.Length > 8192 || pairs.Length % 2 != 0 || pairs.Any(p => p is null)) throw new InvalidDataException("Invalid transition pairs.");
        var seen = new HashSet<(string, string)>();
        for (var i = 0; i < pairs.Length; i += 2)
            if (!States.ContainsKey(pairs[i]) || !States.ContainsKey(pairs[i + 1]) || pairs[i] == pairs[i + 1] || pairs[i] == "End" || pairs[i + 1] == "Start" || !seen.Add((pairs[i], pairs[i + 1])))
                throw new InvalidDataException("Invalid or repeated transition edge.");
        while (Edges.Count != 0) RemoveTransitionByIndex(Edges.Count - 1);
        _fileEdgePairs = pairs.Length == 0 ? null : (string[])pairs.Clone(); NotifyPropertyListChanged();
    }
    private void SetFileEdge(string from, string to, AnimationNodeStateMachineTransition transition)
    {
        AddTransition(from, to, transition);
        if (_fileEdgePairs is not null && Edges.Count * 2 == _fileEdgePairs.Length) _fileEdgePairs = null;
    }
    internal override void ValidateGraphFileState()
    {
        base.ValidateGraphFileState();
        if (_fileStateNames is not null || _fileEdgePairs is not null) throw new InvalidDataException("State/transition table is incomplete.");
    }
}
