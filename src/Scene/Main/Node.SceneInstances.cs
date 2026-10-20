namespace Electron2D;

public partial class Node
{
    internal Dictionary<ulong, string>? SceneInstancePaths;
    internal HashSet<SceneConnectionRuntime>? SceneInstanceConnections;
    internal void RecordSceneInstance(PackedScene scene, IReadOnlyList<Node> nodes)
    {
        SpawnSceneIdentity = scene;
        SceneInstancePaths = nodes.ToDictionary(node => node.InstanceID, node => GetPathTo(node));
    }
    internal void AppendSceneResources(IReadOnlyList<Resource> resources)
    {
        EnsureMutable();
        if (resources.Count == 0) return;
        if (_sharedSceneResourceOwnership is null) _sharedSceneResourceOwnership = new ResourceFileOwnership([.. _ownedSceneResources ?? [], .. resources]);
        else _sharedSceneResourceOwnership.AppendResources(resources);
        (_ownedSceneResources ??= []).AddRange(resources);
    }
}
