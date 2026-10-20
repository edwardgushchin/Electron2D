namespace Electron2D;

public sealed partial class PackedScene
{
    internal static PackedSceneData PrepareMissingSceneSave(PackedSceneData data)
    {
        if (!data.Nodes.Any(node => node.Factory.OriginalScene.Length != 0)) return data;
        var absent = MissingSceneAbsentNodes(data); var map = new int[data.Nodes.Length]; Array.Fill(map, -1);
        var next = 0;
        for (var i = 0; i < map.Length; i++) if (!absent[i]) map[i] = next++;
        using var ordinary = new Node();
        var descriptors = ordinary.GetPropertyList().Where(property => property.IsStored && !property.IsReadOnly).ToDictionary(property => property.Name, StringComparer.Ordinal);
        var nodes = new List<SceneNodeData>();
        for (var i = 0; i < data.Nodes.Length; i++)
        {
            if (absent[i]) continue;
            var node = data.Nodes[i];
            if (node.Factory.OriginalScene.Length != 0)
                node = node with
                {
                    Factory = new SceneFactoryData(CreateRecoveredBase, typeof(Node), 0),
                    Instance = null,
                    InstanceRootIndex = -1,
                    InstancePath = "",
                    InstancePlaceholder = "",
                    DeferredProperties = null,
                    Properties = node.Properties.Where(property => descriptors.TryGetValue(property.Name, out var descriptor) &&
                        ResourceFileTypes.ValueID(descriptor.ValueType) == property.Value.CaptureField(property.Name).Schema).ToArray()
                };
            nodes.Add(node with { ParentIndex = node.ParentIndex < 0 ? -1 : map[node.ParentIndex], OwnerIndex = node.OwnerIndex < 0 ? -1 : map[node.OwnerIndex], InstanceRootIndex = node.InstanceRootIndex < 0 ? -1 : map[node.InstanceRootIndex] });
        }
        var connections = data.Connections.Where(connection => !absent[connection.PublisherIndex] && !absent[connection.SubscriberIndex] &&
            (data.Nodes[connection.PublisherIndex].Factory.OriginalScene.Length == 0 && data.Nodes[connection.SubscriberIndex].Factory.OriginalScene.Length == 0 ||
                connection.Schema is not UnresolvedSceneConnectionSchema && connection.Schema.PublisherType.IsAssignableFrom(nodes[map[connection.PublisherIndex]].Factory.RuntimeType) &&
                connection.Schema.SubscriberType.IsAssignableFrom(nodes[map[connection.SubscriberIndex]].Factory.RuntimeType)))
            .Select(connection => connection with { PublisherIndex = map[connection.PublisherIndex], SubscriberIndex = map[connection.SubscriberIndex] }).ToArray();
        return new(nodes.ToArray(), connections);
    }
    private static Node CreateRecoveredBase() => new();
    internal static bool[] MissingSceneAbsentNodes(PackedSceneData data)
    {
        var absent = new bool[data.Nodes.Length];
        for (var i = 0; i < absent.Length; i++)
        {
            var node = data.Nodes[i];
            absent[i] = node.ParentIndex >= 0 && absent[node.ParentIndex] || node.InstanceRootIndex >= 0 &&
                (absent[node.InstanceRootIndex] || data.Nodes[node.InstanceRootIndex].Factory.OriginalScene.Length != 0);
        }
        return absent;
    }
}
