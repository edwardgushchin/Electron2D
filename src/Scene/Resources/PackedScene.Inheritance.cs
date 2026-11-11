namespace Electron2D;

internal sealed class SceneInheritance
{
    internal sealed record Origin(
        string Path, string Name, Type RuntimeType, ulong ParentID, ulong OwnerID, int Index,
        HashSet<string> Groups, Dictionary<string, StoredPropertyValue> Properties);
    private sealed record LocalResource(long Revision, Dictionary<string, StoredPropertyValue> Properties);

    internal PackedScene Template { get; }
    internal Dictionary<ulong, Origin> Nodes { get; }
    internal HashSet<SceneConnectionRuntime>? Connections { get; }
    private readonly Dictionary<Resource, LocalResource> _localResources = new(ReferenceEqualityComparer.Instance);

    internal SceneInheritance(PackedScene template, Node root, IEnumerable<Node> nodes)
    {
        if (root is InstancePlaceholder)
            throw new InvalidOperationException("A deferred placeholder cannot serve as an inherited scene root.");
        Template = template;
        Connections = root.SceneInstanceConnections;
        Nodes = nodes.ToDictionary(node => node.InstanceID, node => new Origin(
            root.GetPathTo(node), node.Name, node.GetType(), node.Parent?.InstanceID ?? 0, node.Owner?.InstanceID ?? 0,
            ReferenceEquals(node, root) ? -1 : node.GetIndex(),
            new(node.GetPersistentGroups(), StringComparer.Ordinal),
            PackedScene.CaptureDescriptors(node, root).ToDictionary(p => p.Name, p => CaptureValue(node, p), StringComparer.Ordinal)));
    }

    internal bool Matches(Node node, PropertyDescriptor descriptor, Origin origin) =>
        origin.Properties.TryGetValue(descriptor.Name, out var value) &&
        value.ValueType == descriptor.ValueType && descriptor.MatchesStoredValue(node, value) &&
        (!value.HasResourceReferences || !ResourcesChanged(value, new(ReferenceEqualityComparer.Instance)));

    internal void ReplaceNode(ulong previous, ulong replacement)
    {
        if (!Nodes.Remove(previous, out var origin)) return;
        Nodes.Add(replacement, origin);
        foreach (var id in Nodes.Keys.ToArray())
        {
            var child = Nodes[id];
            if (child.ParentID == previous || child.OwnerID == previous)
                Nodes[id] = child with
                {
                    ParentID = child.ParentID == previous ? replacement : child.ParentID,
                    OwnerID = child.OwnerID == previous ? replacement : child.OwnerID,
                };
        }
    }

    internal HashSet<ulong> ReorderedParents(IEnumerable<Node> selected)
    {
        var previous = new Dictionary<ulong, int>();
        var reordered = new HashSet<ulong>();
        foreach (var node in selected)
        {
            if (!Nodes.TryGetValue(node.InstanceID, out var origin) || node.Parent?.InstanceID != origin.ParentID)
                continue;
            if (previous.TryGetValue(origin.ParentID, out var index) && origin.Index < index)
                reordered.Add(origin.ParentID);
            previous[origin.ParentID] = origin.Index;
        }
        return reordered;
    }

    private StoredPropertyValue CaptureValue(ElectronObject owner, PropertyDescriptor descriptor)
    {
        var value = descriptor.CaptureReloadValue(owner);
        if (value.HasResourceReferences) _ = value.TransformResources(Visit);
        return value;

        Resource Visit(Resource resource)
        {
            if (resource.GetLocalScene() is not null &&
                _localResources.TryAdd(resource, new(resource.ChangeRevision, new(StringComparer.Ordinal))))
                foreach (var property in resource.GetPropertyList().Where(p => p.IsStored))
                    _localResources[resource].Properties.Add(property.Name, CaptureValue(resource, property));
            return resource;
        }
    }

    private bool ResourcesChanged(StoredPropertyValue value, HashSet<Resource> visited)
    {
        if (!value.HasResourceReferences) return false;
        var changed = false;
        _ = value.TransformResources(Visit);
        return changed;

        Resource Visit(Resource resource)
        {
            if (changed || !_localResources.TryGetValue(resource, out var baseline) || !visited.Add(resource))
                return resource;
            if (resource.IsDisposed || resource.ChangeRevision != baseline.Revision) { changed = true; return resource; }
            var properties = resource.GetPropertyList().Where(p => p.IsStored).ToArray();
            changed = properties.Length != baseline.Properties.Count || properties.Any(p =>
                !baseline.Properties.TryGetValue(p.Name, out var stored) || stored.ValueType != p.ValueType ||
                !p.MatchesStoredValue(resource, stored) || ResourcesChanged(stored, visited));
            return resource;
        }
    }
}

public sealed partial class PackedScene
{
    private static PackedSceneData OmitUnchangedInheritedNodes(PackedSceneData data, bool[] keep)
    {
        foreach (var connection in data.Connections)
        {
            keep[connection.PublisherIndex] = true;
            keep[connection.SubscriberIndex] = true;
        }
        for (var index = data.Nodes.Length - 1; index > 0; index--)
        {
            if (!keep[index]) continue;
            var node = data.Nodes[index];
            keep[node.ParentIndex] = true;
            if (node.OwnerIndex >= 0) keep[node.OwnerIndex] = true;
        }

        var remap = new int[keep.Length];
        var nodes = new List<SceneNodeData>();
        for (var index = 0; index < remap.Length; index++)
        {
            if (!keep[index]) continue;
            var node = data.Nodes[index];
            remap[index] = nodes.Count;
            nodes.Add(node with
            {
                ParentIndex = Map(node.ParentIndex),
                OwnerIndex = Map(node.OwnerIndex),
                InstanceRootIndex = Map(node.InstanceRootIndex),
            });
        }
        return new([.. nodes], data.Connections.Select(connection => connection with
        {
            PublisherIndex = Map(connection.PublisherIndex),
            SubscriberIndex = Map(connection.SubscriberIndex),
        }).ToArray());

        int Map(int index) => index < 0 ? -1 : remap[index];
    }
}
