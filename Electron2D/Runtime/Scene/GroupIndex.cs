using System.Runtime.InteropServices;

namespace Electron2D;

sealed class GroupIndex
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