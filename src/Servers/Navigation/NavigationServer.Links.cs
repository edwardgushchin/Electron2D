namespace Electron2D;

public sealed partial class NavigationServer
{
    private NavigationLinkState Link(RID rid) => _links.TryGetValue(rid, out var link) && (link.Scene is null || link.Scene.TryGetTarget(out var node) && !node.IsDisposed) ? link : throw new ArgumentException("The RID does not identify a live navigation link.", nameof(rid));
    internal RID CreateLink(NavigationLink? node = null) { lock (_gate) { var rid = RID.Allocate(); _links.Add(rid, new(rid, node)); return rid; } }
    internal void ReleaseSceneLink(RID rid) { lock (_gate) { if (_links.Remove(rid, out var link)) Dirty(link.Map); } }
    private void DirtyLink(NavigationLinkState link) { link.Dirty = true; Dirty(link.Map); }
    internal void StageSceneLinkEndpoints(RID rid, Vector2 start, Vector2 end)
    { Finite(start); Finite(end); lock (_gate) { var link = Link(rid); if (link.Start == start && link.End == end) return; link.Start = start; link.End = end; DirtyLink(link); } }
    /// <summary>Creates a caller-owned enabled, bidirectional link with no map and zero endpoints.</summary>
    /// <returns>A new link RID requiring FreeRID.</returns>
    public static RID LinkCreate() => Shared.CreateLink();
    /// <summary>Returns copied staged link memberships of the live map.</summary>
    /// <param name="map">Live navigation map RID.</param>
    /// <returns>Independent link identity array, including disabled links.</returns>
    /// <exception cref="ArgumentException">The map RID is absent, stale or of another kind.</exception>
    public static RID[] MapGetLinks(RID map) { lock (Shared._gate) { Shared.Map(map); return Shared._links.Values.Where(l => l.Map == map).Select(l => l.RID).ToArray(); } }
    /// <summary>Stages the finite nonnegative endpoint attachment radius, initially four world units.</summary>
    /// <param name="map">Live navigation map RID.</param><param name="radius">World distance; attachment is strictly inside this radius. Zero prevents attachment.</param>
    /// <exception cref="ArgumentOutOfRangeException">Radius is nonfinite or negative.</exception>
    /// <exception cref="ArgumentException">The map RID is absent, stale or of another kind.</exception>
    public static void MapSetLinkConnectionRadius(RID map, float radius) { Nonnegative(radius); lock (Shared._gate) { var value = Shared.Map(map); if (value.LinkRadius == radius) return; value.LinkRadius = radius; value.Dirty = true; } }
    /// <summary>Returns the map's staged endpoint attachment radius.</summary>
    /// <param name="map">Live navigation map RID.</param><returns>Finite nonnegative world-space distance.</returns>
    /// <exception cref="ArgumentException">The map RID is absent, stale or of another kind.</exception>
    public static float MapGetLinkConnectionRadius(RID map) { lock (Shared._gate) return Shared.Map(map).LinkRadius; }
    /// <summary>Returns the link's committed version counter, initially zero.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Counter increments on changed attached link synchronization; wraps from uint.MaxValue to one.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static ulong LinkGetIterationID(RID link) { lock (Shared._gate) return Shared.Link(link).IterationID; }
    /// <summary>Stages a link map assignment; empty detaches it.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="map">Live map RID, or empty to detach.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or of another kind.</exception>
    public static void LinkSetMap(RID link, RID map) { lock (Shared._gate) { var value = Shared.Link(link); if (map.IsValid()) Shared.Map(map); if (value.Map == map) return; Shared.Dirty(value.Map); value.Map = map; Shared.DirtyLink(value); } }
    /// <summary>Returns the link's staged map assignment.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Assigned map, or empty when detached.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static RID LinkGetMap(RID link) { lock (Shared._gate) return Shared.Link(link).Map; }
    /// <summary>Stages whether the link participates in committed routes; initially true.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="enabled">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static void LinkSetEnabled(RID link, bool enabled) { lock (Shared._gate) { var value = Shared.Link(link); if (value.Enabled == enabled) return; value.Enabled = enabled; Shared.DirtyLink(value); } }
    /// <summary>Returns whether the link participates in committed routes from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static bool LinkGetEnabled(RID link) { lock (Shared._gate) return Shared.Link(link).Enabled; }
    /// <summary>Stages whether traversal also permits end-to-start travel; initially true.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="bidirectional">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static void LinkSetBidirectional(RID link, bool bidirectional) { lock (Shared._gate) { var value = Shared.Link(link); if (value.Bidirectional == bidirectional) return; value.Bidirectional = bidirectional; Shared.DirtyLink(value); } }
    /// <summary>Returns whether traversal also permits end-to-start travel from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static bool LinkIsBidirectional(RID link) { lock (Shared._gate) return Shared.Link(link).Bidirectional; }
    /// <summary>Stages the finite world-space start endpoint; initially Vector2.Zero.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="position">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind; endpoint is nonfinite.</exception>
    public static void LinkSetStartPosition(RID link, Vector2 position) { Finite(position); lock (Shared._gate) { var value = Shared.Link(link); if (value.Start == position) return; value.Start = position; Shared.DirtyLink(value); } }
    /// <summary>Returns the finite world-space start endpoint from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static Vector2 LinkGetStartPosition(RID link) { lock (Shared._gate) return Shared.Link(link).Start; }
    /// <summary>Stages the finite world-space end endpoint; initially Vector2.Zero.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="position">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind; endpoint is nonfinite.</exception>
    public static void LinkSetEndPosition(RID link, Vector2 position) { Finite(position); lock (Shared._gate) { var value = Shared.Link(link); if (value.End == position) return; value.End = position; Shared.DirtyLink(value); } }
    /// <summary>Returns the finite world-space end endpoint from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static Vector2 LinkGetEndPosition(RID link) { lock (Shared._gate) return Shared.Link(link).End; }
    /// <summary>Stages the unsigned 32-bit navigation-layer mask; initially 1.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="layers">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static void LinkSetNavigationLayers(RID link, uint layers) { lock (Shared._gate) { var value = Shared.Link(link); if (value.Layers == layers) return; value.Layers = layers; Shared.DirtyLink(value); } }
    /// <summary>Returns the unsigned 32-bit navigation-layer mask from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static uint LinkGetNavigationLayers(RID link) { lock (Shared._gate) return Shared.Link(link).Layers; }
    /// <summary>Stages finite nonnegative cost paid when entering the link; initially 0.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="cost">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Cost is nonfinite or negative.</exception>
    public static void LinkSetEnterCost(RID link, float cost) { Nonnegative(cost); lock (Shared._gate) { var value = Shared.Link(link); if (value.EnterCost == cost) return; value.EnterCost = cost; Shared.DirtyLink(value); } }
    /// <summary>Returns finite nonnegative cost paid when entering the link from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static float LinkGetEnterCost(RID link) { lock (Shared._gate) return Shared.Link(link).EnterCost; }
    /// <summary>Stages finite nonnegative distance multiplier inside the link; initially 1.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="cost">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Cost is nonfinite or negative.</exception>
    public static void LinkSetTravelCost(RID link, float cost) { Nonnegative(cost); lock (Shared._gate) { var value = Shared.Link(link); if (value.TravelCost == cost) return; value.TravelCost = cost; Shared.DirtyLink(value); } }
    /// <summary>Returns finite nonnegative distance multiplier inside the link from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static float LinkGetTravelCost(RID link) { lock (Shared._gate) return Shared.Link(link).TravelCost; }
    /// <summary>Stages the logical scene owner instance identity; initially 0.</summary>
    /// <param name="link">Live navigation link RID.</param><param name="ownerID">New staged value.</param>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static void LinkSetOwnerID(RID link, ulong ownerID) { lock (Shared._gate) { var value = Shared.Link(link); if (value.OwnerID == ownerID) return; value.OwnerID = ownerID; Shared.DirtyLink(value); } }
    /// <summary>Returns the logical scene owner instance identity from staged link settings.</summary>
    /// <param name="link">Live navigation link RID.</param><returns>Current staged value.</returns>
    /// <exception cref="ArgumentException">The link RID is absent, stale or of another kind.</exception>
    public static ulong LinkGetOwnerID(RID link) { lock (Shared._gate) return Shared.Link(link).OwnerID; }
}
internal sealed class NavigationLinkState(RID rid, NavigationLink? scene)
{
    internal readonly RID RID = rid;
    internal readonly WeakReference<NavigationLink>? Scene = scene is null ? null : new(scene);
    internal RID Map;
    internal bool Enabled = true, Bidirectional = true, Dirty = true;
    internal Vector2 Start, End;
    internal uint Layers = 1, IterationID;
    internal float EnterCost, TravelCost = 1;
    internal ulong OwnerID;
}
