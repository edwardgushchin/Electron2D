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
    /// <summary>Supplies a borrowed RID through the base Resource implementation.</summary>
    /// <remarks>Native resource overrides keep their own identities; this callback neither acquires nor frees an RID.</remarks>
    public Func<TResource, RID>? GetRID { get; init; }
    /// <summary>Clears script-owned transient state after the native reset hook.</summary>
    /// <remarks>Also runs when data copying resets its target, before stored restoration or code replacement.</remarks>
    public Action<TResource>? ResetState { get; init; }
    /// <summary>Handles a committed raw path-cache assignment after native handling.</summary>
    /// <remarks>Equal raw assignments also invoke this callback. Ordinary registered path changes do not.</remarks>
    public Action<TResource, string>? SetPathCache { get; init; }
    internal override void Populate(ObjectScriptBinding target, TResource owner)
    {
        base.Populate(target, owner);
        target.SetupLocalToScene = SetupLocalToScene is { } setup ? () => setup(owner) : null;
        target.GetRID = GetRID is { } rid ? () => rid(owner) : null;
        target.ResetState = ResetState is { } reset ? () => reset(owner) : null;
        target.SetPathCache = SetPathCache is { } path ? value => path(owner, value) : null;
    }
}
