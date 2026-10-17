namespace Electron2D;

public sealed partial class ResourceLoader
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<PackedScene, ResourceFileOwnership> _sceneTemplateOwners = new();
    internal static ResourceFileOwnership? RetainSceneTemplateOwner(PackedScene scene) =>
        Runtime._sceneTemplateOwners.TryGetValue(scene, out var owner) ? owner.RetainOwner() : null;
    internal static IDisposable? RetainSceneTemplate(PackedScene scene) => RetainSceneTemplateOwner(scene)?.TransferToLease();
}
