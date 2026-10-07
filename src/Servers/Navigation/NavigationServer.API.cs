namespace Electron2D;

public sealed partial class NavigationServer
{
    /// <summary>Creates a caller-owned, initially inactive navigation map.</summary>
    /// <returns>Creates a caller-owned, initially inactive navigation map.</returns>
    public static RID MapCreate() => Shared.CreateMap();
    /// <summary>Creates a caller-owned enabled navigation region with no map or geometry.</summary>
    /// <returns>Creates a caller-owned enabled navigation region with no map or geometry.</returns>
    public static RID RegionCreate() => Shared.CreateRegion();
    /// <summary>Releases a caller-owned map or region; scene/world identities retain their owning lifetime.</summary>
    /// <exception cref="ArgumentException">The RID is absent or stale.</exception>
    /// <exception cref="InvalidOperationException">The RID is borrowed from a scene/resource owner.</exception>
    /// <param name="rid">Live caller-owned navigation map or region RID.</param>
    public static void FreeRID(RID rid)
    {
        lock (Shared._gate)
        {
            if (Shared._maps.TryGetValue(rid, out var map)) { if (map.Owner is not null) throw new InvalidOperationException("World maps are resource-owned."); Shared.ReleaseWorldMap(rid); return; }
            var region = Shared.Region(rid); if (region.Scene is not null) throw new InvalidOperationException("Scene regions are node-owned."); Shared.ReleaseSceneRegion(rid);
        }
    }
    /// <summary>Returns copied live map identities.</summary>
    /// <returns>Returns copied live map identities.</returns>
    public static RID[] GetMaps() { lock (Shared._gate) return Shared._maps.Keys.ToArray(); }
    /// <summary>Returns copied staged region memberships of a map.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <returns>Returns copied staged region memberships of a map.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static RID[] MapGetRegions(RID map) { lock (Shared._gate) { Shared.Map(map); return Shared._regions.Values.Where(r => r.Map == map).Select(r => r.RID).ToArray(); } }
    /// <summary>Stages map activity; inactive committed maps have no query topology.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <param name="active">True enables map participation; the published change follows synchronization.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void MapSetActive(RID map, bool active) { lock (Shared._gate) { var m = Shared.Map(map); m.Active = active; m.Dirty = true; } }
    /// <summary>Returns the map's staged active setting.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <returns>Returns the map's staged active setting.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static bool MapIsActive(RID map) { lock (Shared._gate) return Shared.Map(map).Active; }
    /// <summary>Returns the latest complete published map iteration, initially zero.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <returns>Returns the latest complete published map iteration, initially zero.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static ulong MapGetIterationID(RID map) { lock (Shared._gate) return Shared.Map(map).IterationID; }
    /// <summary>Stages automatic margin connections between free region edges; exact shared edges remain connected.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <param name="enabled">Staged enabled flag for the region or its optional margin connections.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void MapSetUseEdgeConnections(RID map, bool enabled) { lock (Shared._gate) { var m = Shared.Map(map); m.UseEdgeConnections = enabled; m.Dirty = true; } }
    /// <summary>Returns the staged edge-connection setting.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <returns>Returns the staged edge-connection setting.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static bool MapGetUseEdgeConnections(RID map) { lock (Shared._gate) return Shared.Map(map).UseEdgeConnections; }
    /// <summary>Stages the finite nonnegative overlap matching margin for region edges.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <param name="margin">Finite nonnegative world-space edge overlap distance.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void MapSetEdgeConnectionMargin(RID map, float margin) { Nonnegative(margin); lock (Shared._gate) { var m = Shared.Map(map); m.EdgeMargin = margin; m.Dirty = true; } }
    /// <summary>Returns the staged region-edge connection margin.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <returns>Returns the staged region-edge connection margin.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static float MapGetEdgeConnectionMargin(RID map) { lock (Shared._gate) return Shared.Map(map).EdgeMargin; }
    /// <summary>Projects a finite world point onto the committed map, or returns zero for empty topology.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <param name="point">Finite world-space query position.</param>
    /// <returns>Projects a finite world point onto the committed map, or returns zero for empty topology.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static Vector2 MapGetClosestPoint(RID map, Vector2 point) { Finite(point); lock (Shared._gate) return Shared.Map(map).Iteration.Closest(point).Point; }
    /// <summary>Returns the closest committed region identity, or empty for empty topology.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <param name="point">Finite world-space query position.</param>
    /// <returns>Returns the closest committed region identity, or empty for empty topology.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static RID MapGetClosestPointOwner(RID map, Vector2 point) { Finite(point); lock (Shared._gate) return Shared.Map(map).Iteration.Closest(point).Owner; }
    /// <summary>Returns a copied committed polygon path, projecting endpoints and returning the closest reached destination when disconnected.</summary>
    /// <param name="map">Live map identity.</param><param name="origin">Finite world start.</param><param name="destination">Finite world destination.</param>
    /// <param name="optimize">True uses portal funnel; false retains crossed portal midpoints.</param><param name="navigationLayers">Applicable region-layer bits, one by default.</param>
    /// <returns>Returns a copied committed polygon path, projecting endpoints and returning the closest reached destination when disconnected.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static Vector2[] MapGetPath(RID map, Vector2 origin, Vector2 destination, bool optimize, uint navigationLayers = 1)
    { Finite(origin); Finite(destination); NavigationMapIteration snapshot; lock (Shared._gate) snapshot = Shared.Map(map).Iteration; return snapshot.Path(origin, destination, optimize, navigationLayers); }
    /// <summary>Stages a region's map membership; empty detaches it.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetMap(RID region, RID map) { lock (Shared._gate) Shared.SetRegionMap(region, map); }
    /// <summary>Returns a region's staged map identity.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns a region's staged map identity.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static RID RegionGetMap(RID region) { lock (Shared._gate) return Shared.Region(region).Map; }
    /// <summary>Stages an authored polygon resource version, borrowing the resource and observing later changes.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="polygon">Borrowed authored resource, or null to clear; indexed data is copied by the polygon resource.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetNavigationPolygon(RID region, NavigationPolygon? polygon) { lock (Shared._gate) Shared.SetPolygon(region, polygon); }
    /// <summary>Stages a region's finite world transform.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="transform">Finite invertible local-to-world transform.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetTransform(RID region, Transform transform) { if (!transform.IsFinite() || transform.Determinant() == 0) throw new ArgumentException("Navigation region transforms must be finite and invertible.", nameof(transform)); lock (Shared._gate) { var r = Shared.Region(region); r.Transform = transform; r.Dirty = true; Shared.Dirty(r.Map); } }
    /// <summary>Returns the staged region transform.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns the staged region transform.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static Transform RegionGetTransform(RID region) { lock (Shared._gate) return Shared.Region(region).Transform; }
    /// <summary>Stages a region's enabled flag.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="enabled">Staged enabled flag for the region or its optional margin connections.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetEnabled(RID region, bool enabled) { lock (Shared._gate) { var r = Shared.Region(region); r.Enabled = enabled; r.Dirty = true; Shared.Dirty(r.Map); } }
    /// <summary>Returns the staged region enabled flag.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns the staged region enabled flag.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static bool RegionGetEnabled(RID region) { lock (Shared._gate) return Shared.Region(region).Enabled; }
    /// <summary>Stages automatic edge connections for this region.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="enabled">Staged enabled flag for the region or its optional margin connections.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetUseEdgeConnections(RID region, bool enabled) { lock (Shared._gate) { var r = Shared.Region(region); r.UseEdgeConnections = enabled; r.Dirty = true; Shared.Dirty(r.Map); } }
    /// <summary>Returns this region's staged edge-connection flag.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns this region's staged edge-connection flag.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static bool RegionGetUseEdgeConnections(RID region) { lock (Shared._gate) return Shared.Region(region).UseEdgeConnections; }
    /// <summary>Stages navigation-layer bits.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="layers">Unsigned 32-bit navigation-layer mask.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetNavigationLayers(RID region, uint layers) { lock (Shared._gate) { var r = Shared.Region(region); r.Layers = layers; r.Dirty = true; Shared.Dirty(r.Map); } }
    /// <summary>Returns staged navigation-layer bits.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns staged navigation-layer bits.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static uint RegionGetNavigationLayers(RID region) { lock (Shared._gate) return Shared.Region(region).Layers; }
    /// <summary>Stages finite nonnegative entry cost.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="cost">Finite nonnegative entry cost or travel distance multiplier.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetEnterCost(RID region, float cost) { Nonnegative(cost); lock (Shared._gate) { var r = Shared.Region(region); r.EnterCost = cost; r.Dirty = true; Shared.Dirty(r.Map); } }
    /// <summary>Returns staged region entry cost.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns staged region entry cost.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static float RegionGetEnterCost(RID region) { lock (Shared._gate) return Shared.Region(region).EnterCost; }
    /// <summary>Stages finite nonnegative travel cost per world distance.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="cost">Finite nonnegative entry cost or travel distance multiplier.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetTravelCost(RID region, float cost) { Nonnegative(cost); lock (Shared._gate) { var r = Shared.Region(region); r.TravelCost = cost; r.Dirty = true; Shared.Dirty(r.Map); } }
    /// <summary>Returns staged region travel cost.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns staged region travel cost.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static float RegionGetTravelCost(RID region) { lock (Shared._gate) return Shared.Region(region).TravelCost; }
    /// <summary>Stages the logical owner instance identity used to associate a region with a scene object.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="ownerID">Logical scene owner instance identity; zero means no associated owner.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static void RegionSetOwnerID(RID region, ulong ownerID) { lock (Shared._gate) Shared.Region(region).OwnerID = ownerID; }
    /// <summary>Returns the region's staged logical owner identity.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns the region's staged logical owner identity.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static ulong RegionGetOwnerID(RID region) { lock (Shared._gate) return Shared.Region(region).OwnerID; }
    /// <summary>Returns the region's committed transformed geometry bounds, independently of map activity.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <returns>Returns the region's committed transformed geometry bounds, independently of map activity.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static Rect2 RegionGetBounds(RID region) { lock (Shared._gate) { var r = Shared.Region(region); return r.Iteration.Bounds(region); } }
    /// <summary>Projects a finite point onto this region's committed geometry independently of map activity, or returns zero for no geometry.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="point">Finite world-space query position.</param>
    /// <returns>Projects a finite point onto this region's committed geometry independently of map activity, or returns zero for no geometry.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static Vector2 RegionGetClosestPoint(RID region, Vector2 point)
    {
        Finite(point); lock (Shared._gate) { var r = Shared.Region(region); var best = Vector2.Zero; var distance = double.PositiveInfinity; foreach (var cell in r.Iteration.Cells) if (cell.Region == region) { var p = NavigationMapIteration.Closest(cell, point); var dx = (double)p.X - point.X; var dy = (double)p.Y - point.Y; var d = dx * dx + dy * dy; if (d < distance) { distance = d; best = p; } } return best; }
    }
    /// <summary>Reports whether this region owns the closest committed point, preserving map tie ownership.</summary>
    /// <param name="region">Live navigation region RID.</param>
    /// <param name="point">Finite world-space query position.</param>
    /// <returns>Reports whether this region owns the closest committed point, preserving map tie ownership.</returns>
    /// <exception cref="ArgumentException">A required RID is absent, stale or belongs to another resource kind.</exception>
    public static bool RegionOwnsPoint(RID region, Vector2 point) { Finite(point); lock (Shared._gate) { var r = Shared.Region(region); return r.Map.IsValid() && Shared.Map(r.Map).Iteration.Closest(point).Owner == region; } }
    private static void Nonnegative(float value) { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Finite(Vector2 value) { if (!value.IsFinite()) throw new ArgumentException("Navigation points must be finite.", nameof(value)); }
}
