namespace Electron2D;

public sealed partial class SceneTree
{
    // ponytail: the distinct world list is tiny for ordinary viewport graphs; index by identity if measured scale requires it.
    private readonly List<WorldRuntime> _physicsWorlds = [];
    private World? _physicsWorld;
    private readonly List<CanvasItem> _worldRebindPending = [];
    private WorldRuntime? _defaultWorldRuntime;
    internal void BindWorld(WorldRuntime runtime) { EnsureOwnerThread(); if (runtime.Bind(this)) _physicsWorlds.Add(runtime); }
    internal World GetPhysicsWorld()
    {
        EnsureOwnerThread();
        if (Root is Viewport view) return view.FindWorld()!;
        var runtime = _defaultWorldRuntime ??= new(sceneOwned: true); BindWorld(runtime);
        return _physicsWorld is { IsDisposed: false } current ? current : _physicsWorld = new(runtime);
    }
    private PhysicsSpace PhysicsSpaceFor(CanvasItem item) { var runtime = item.GetWorld()!.Runtime; BindWorld(runtime); return runtime.Space; }
    internal void EnsurePhysicsParticipationChange() { foreach (var runtime in _physicsWorlds) runtime.ExistingSpace?.EnsureQueryAccess(); }
    internal void EnsureWorldBindingChange() { foreach (var runtime in _physicsWorlds) runtime.ExistingSpace?.EnsureWorldBindingChange(); }
    internal string ResolveSpatialAudioBus(CanvasItem item, Vector2 position, uint areaMask, string requestedBus)
    { EnsureOwnerThread(); return areaMask == 0 ? requestedBus : item.GetWorld()?.Runtime.ExistingSpace?.FindAudioBusOverride(position, areaMask) ?? requestedBus; }
    internal void RegisterPhysicsBody(PhysicsBody body) { EnsureOwnerThread(); PhysicsSpaceFor(body).Add(body); }
    internal void UnregisterPhysicsBody(PhysicsBody body) { EnsureOwnerThread(); body.Space?.Remove(body); }
    internal void RegisterPhysicsJoint(Joint joint) { EnsureOwnerThread(); PhysicsSpaceFor(joint).Add(joint); }
    internal void UnregisterPhysicsJoint(Joint joint) { EnsureOwnerThread(); joint.AttachmentSpace?.Remove(joint); }
    internal void RegisterPhysicsArea(Area area) { EnsureOwnerThread(); PhysicsSpaceFor(area).Add(area); }
    internal void UnregisterPhysicsArea(Area area) { EnsureOwnerThread(); area.Space?.Remove(area); }
    internal void RebindViewportWorld(Viewport viewport)
    {
        var nodes = new List<CanvasItem>(); List<NavigationAgent>? agents = null; Collect(viewport); List<Exception>? errors = null;
        // Remove joints first, then move all colliders, then register joints against their final worlds.
        foreach (var item in nodes) if (item is Joint joint) try { UnregisterPhysicsJoint(joint); } catch (Exception e) { CollectException(ref errors, e); }
        foreach (var item in nodes)
        {
            if (item.IsDisposed || !ReferenceEquals(item.Tree, this)) continue;
            try
            {
                if (item is PhysicsBody body && body.Space is { } old && !ReferenceEquals(old, PhysicsSpaceFor(body))) { try { UnregisterPhysicsBody(body); } finally { if (body.Space is null && !body.IsDisposed && ReferenceEquals(body.Tree, this) && !body.PhysicsRemoved) RegisterPhysicsBody(body); } }
                else if (item is Area area && area.Space is { } oldArea && !ReferenceEquals(oldArea, PhysicsSpaceFor(area))) { try { UnregisterPhysicsArea(area); } finally { if (area.Space is null && !area.IsDisposed && ReferenceEquals(area.Tree, this) && !area.PhysicsRemoved) RegisterPhysicsArea(area); } }
            }
            catch (Exception e) { CollectException(ref errors, e); if (!_worldRebindPending.Contains(item)) _worldRebindPending.Add(item); }
        }
        foreach (var item in nodes) if (item is Joint joint && !joint.IsDisposed && ReferenceEquals(joint.Tree, this)) try { RegisterPhysicsJoint(joint); } catch (Exception e) { CollectException(ref errors, e); }
        foreach (var item in nodes) if (!item.IsDisposed && ReferenceEquals(item.Tree, this)) try { item.DispatchNotification(CanvasItem.NotificationWorldChanged); } catch (Exception e) { CollectException(ref errors, e); }
        if (agents is not null) foreach (var agent in agents) if (!agent.IsDisposed && ReferenceEquals(agent.Tree, this)) try { agent.RebindWorld(); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("World replacement committed with physics callback failures.", errors);
        void Collect(Node node) { for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) { var child = node.GetChild(i, includeInternal: true); if (child is Viewport) continue; if (child is CanvasItem item) nodes.Add(item); if (child is NavigationAgent agent) (agents ??= []).Add(agent); Collect(child); } }
    }
    private void StepPhysicsWorlds(double delta, ref List<Exception>? errors)
    {
        for (var i = _worldRebindPending.Count - 1; i >= 0; i--)
        {
            var item = _worldRebindPending[i];
            if (item.IsDisposed || !ReferenceEquals(item.Tree, this)) { _worldRebindPending.RemoveAt(i); continue; }
            try
            {
                if (item is PhysicsBody body && !body.PhysicsRemoved) { var space = PhysicsSpaceFor(body); if (!ReferenceEquals(body.Space, space)) { try { UnregisterPhysicsBody(body); } finally { if (body.Space is null) RegisterPhysicsBody(body); } } }
                else if (item is Area area && !area.PhysicsRemoved) { var space = PhysicsSpaceFor(area); if (!ReferenceEquals(area.Space, space)) { try { UnregisterPhysicsArea(area); } finally { if (area.Space is null) RegisterPhysicsArea(area); } } }
                _worldRebindPending.RemoveAt(i);
            }
            catch (Exception e) { CollectException(ref errors, e); }
        }
        var count = _physicsWorlds.Count; for (var i = 0; i < count; i++) if (_physicsWorlds[i].ExistingSpace is { } space) try { space.Step(delta); } catch (Exception e) { CollectException(ref errors, e); }
    }
    private void ReleasePhysicsWorlds(ref List<Exception>? errors)
    {
        try { _physicsWorld?.Dispose(); } catch (Exception e) { CollectException(ref errors, e); }
        _physicsWorld = null;
        foreach (var runtime in _physicsWorlds) try { runtime.Detach(this); } catch (Exception e) { CollectException(ref errors, e); }
        _physicsWorlds.Clear(); _worldRebindPending.Clear(); _defaultWorldRuntime = null;
    }
}
