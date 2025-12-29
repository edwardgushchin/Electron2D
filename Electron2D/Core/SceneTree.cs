using System.Runtime.InteropServices;

namespace Electron2D;

public sealed class SceneTree
{
    private readonly List<Node> _freeQueue; // предвыделяем — никаких аллокаций при QueueFree в кадре
    private readonly GroupIndex _groups = new();

    public SceneTree(Node root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));

        _freeQueue = new List<Node>(64);

        Root.InternalEnterTree(this);
        Root.InternalReady();
    }

    public Node Root { get; }

    public ReadOnlySpan<Node> GetNodesInGroup(string group) => _groups.GetNodes(group);

    internal int RegisterInGroup(string group, Node node) => _groups.Add(group, node);

    internal void UnregisterFromGroup(string group, int index, Node removedNode)
        => _groups.Remove(group, index, removedNode);

    internal void QueueFree(Node node) => _freeQueue.Add(node);

    public void FlushFreeQueue()
    {
        if (_freeQueue.Count == 0) return;

        for (var i = 0; i < _freeQueue.Count; i++)
            _freeQueue[i].InternalFreeImmediate();

        _freeQueue.Clear();
    }

    private sealed class GroupIndex
    {
        private readonly Dictionary<string, List<Node>> _map = new(StringComparer.Ordinal);

        public ReadOnlySpan<Node> GetNodes(string group)
        {
            if (string.IsNullOrWhiteSpace(group)) return ReadOnlySpan<Node>.Empty;
            return _map.TryGetValue(group, out var list)
                ? CollectionsMarshal.AsSpan(list)
                : ReadOnlySpan<Node>.Empty;
        }

        public int Add(string group, Node node)
        {
            if (!_map.TryGetValue(group, out var list))
            {
                list = new List<Node>(8);
                _map[group] = list;
            }

            var index = list.Count;
            list.Add(node);
            return index;
        }

        public void Remove(string group, int index, Node removedNode)
        {
            if (!_map.TryGetValue(group, out var list)) return;
            var last = list.Count - 1;
            if ((uint)index > (uint)last) return;

            if (index != last)
            {
                var swapped = list[last];
                list[index] = swapped;
                swapped.InternalUpdateGroupIndex(group, index);
            }

            list.RemoveAt(last);

            if (list.Count == 0)
                _map.Remove(group);

            removedNode.InternalUpdateGroupIndex(group, -1);
        }
    }
}
