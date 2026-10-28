namespace Electron2D;

/// <summary>Adds scene-local resource initialization to a live object Script callback set.</summary>
/// <typeparam name="TResource">Compatible resource receiver role.</typeparam>
public class ResourceScriptCallbacks<TResource> : ElectronObjectScriptCallbacks<TResource> where TResource : Resource
{
    /// <summary>Creates an unused resource callback set.</summary>
    public ResourceScriptCallbacks() { }
    /// <summary>Runs after native local-scene setup with restored stored data and the owning scene available.</summary>
    /// <remarks>Uses the ordinary SetupLocalToScene ordering, failure and change-event policy.</remarks>
    public Action<TResource>? SetupLocalToScene { get; init; }
    internal override void Populate(ObjectScriptBinding target, TResource owner)
    {
        base.Populate(target, owner);
        target.SetupLocalToScene = SetupLocalToScene is { } setup ? () => setup(owner) : null;
    }
}
