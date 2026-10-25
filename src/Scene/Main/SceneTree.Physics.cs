namespace Electron2D;

public sealed partial class SceneTree
{
    // Publish only at cold binding; readers retain a stable world set without per-frame copies.
    private volatile WorldRuntime[] _physicsWorlds = [];
    private readonly List<PhysicsSpace> _sceneTransformSpaces = [];
    private void BeginSceneTransformAuthoring()
    {
        var worlds = _physicsWorlds;
        _sceneTransformSpaces.EnsureCapacity(worlds.Length);
        try
        {
            foreach (var world in worlds)
                if (world.ExistingSpace is { } space) { space.BeginSceneTransformAuthoring(this); _sceneTransformSpaces.Add(space); }
        }
        catch { EndSceneTransformAuthoring(); throw; }
    }
    private void EndSceneTransformAuthoring()
    {
        foreach (var space in _sceneTransformSpaces) space.EndSceneTransformAuthoring();
        _sceneTransformSpaces.Clear();
    }
    private World? _physicsWorld;
    private readonly List<CanvasItem> _worldRebindPending = [];
    private WorldRuntime? _defaultWorldRuntime;
    internal void BindWorld(WorldRuntime runtime)
    {
        EnsureProcessingReadAccess();
        lock (_workGate) if (runtime.Bind(this)) _physicsWorlds = [.. _physicsWorlds, runtime];
    }
    internal World GetPhysicsWorld()
    {
        EnsureProcessingReadAccess();
        if (Root is Viewport view) return view.FindWorld()!;
        lock (_workGate)
        {
            var runtime = _defaultWorldRuntime ??= WorldRuntime.CreateSceneDefault(); BindWorld(runtime);
            return _physicsWorld is { IsDisposed: false } current ? current : _physicsWorld = new(runtime);
        }
    }
    private PhysicsSpace PhysicsSpaceFor(CanvasItem item) { var runtime = item.GetWorld()!.Runtime; BindWorld(runtime); return runtime.Space; }
    internal void EnsurePhysicsParticipationChange(bool releasing = false)
    {
        foreach (var runtime in _physicsWorlds)
            if (releasing) runtime.ExistingSpace?.EnsureReleaseAccess(); else runtime.ExistingSpace?.EnsureQueryAccess();
    }
    internal void EnsureExtensionHierarchyChange()
    {
        foreach (var runtime in _physicsWorlds) runtime.ExistingSpace?.EnsureExtensionHierarchyChange();
    }
    internal void EnsureWorldBindingChange() { foreach (var runtime in _physicsWorlds) runtime.ExistingSpace?.EnsureWorldBindingChange(); }
    internal string ResolveSpatialAudioBus(CanvasItem item, Vector2 position, uint areaMask, string requestedBus)
    { EnsureOwnerThread(); return areaMask == 0 ? requestedBus : item.GetWorld()?.Runtime.ExistingSpace?.FindAudioBusOverride(position, areaMask) ?? requestedBus; }
    internal void RegisterPhysicsBody(PhysicsBody body) { EnsureOwnerThread(); PhysicsServer.BodySetSpace(body.PhysicsRID, PhysicsSpaceFor(body).RID); }
    internal void UnregisterPhysicsBody(PhysicsBody body) { EnsureOwnerThread(); body.Space?.Remove(body); }
    internal void RegisterPhysicsJoint(Joint joint) { EnsureOwnerThread(); PhysicsSpaceFor(joint).Add(joint); }
    internal void UnregisterPhysicsJoint(Joint joint) { EnsureOwnerThread(); joint.AttachmentSpace?.Remove(joint); }
    internal void RegisterPhysicsArea(Area area) { EnsureOwnerThread(); PhysicsServer.AreaSetSpace(area.PhysicsRID, PhysicsSpaceFor(area).RID); }
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
                if (item is PhysicsBody body && !body.PhysicsRemoved && !ReferenceEquals(body.Space, PhysicsSpaceFor(body))) { try { UnregisterPhysicsBody(body); } finally { if (body.Space is null && !body.IsDisposed && ReferenceEquals(body.Tree, this)) RegisterPhysicsBody(body); } }
                else if (item is Area area && !area.PhysicsRemoved && !ReferenceEquals(area.Space, PhysicsSpaceFor(area))) { try { UnregisterPhysicsArea(area); } finally { if (area.Space is null && !area.IsDisposed && ReferenceEquals(area.Tree, this)) RegisterPhysicsArea(area); } }
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
        if (_paused) return;
        foreach (var runtime in _physicsWorlds) if (runtime.ExistingSpace is { } space) try { space.SetDebugContacts(_debugCollisionsHint ? DebugContactLimit : 0); space.Step(delta); } catch (Exception e) { CollectException(ref errors, e); }
    }
    private void ReleasePhysicsWorlds(ref List<Exception>? errors)
    {
        try { _physicsWorld?.Dispose(); } catch (Exception e) { CollectException(ref errors, e); }
        _physicsWorld = null;
        foreach (var runtime in _physicsWorlds) try { try { runtime.ExistingSpace?.SetDebugContacts(0); } finally { runtime.Detach(this); } } catch (Exception e) { CollectException(ref errors, e); }
        _physicsWorlds = []; _worldRebindPending.Clear(); _defaultWorldRuntime = null;
    }
}
