namespace Electron2D;

/// <summary>Immutable typed details delivered when an agent reaches a path waypoint.</summary>
/// <remarks>Missing query metadata is represented by null. Owner resolves live scene navigation owners;
/// arbitrary caller-assigned owner IDs remain available as OwnerID with no managed owner.</remarks>
public readonly record struct NavigationWaypoint
{
    /// <summary>Gets the finite world-space waypoint.</summary><value>Path position at delivery.</value>
    public Vector2 Position { get; init; }
    /// <summary>Gets the optional owning primitive kind.</summary><value>Null when type metadata was omitted.</value>
    public NavigationPathQueryResult.PathSegmentType? Type { get; init; }
    /// <summary>Gets the optional owning primitive identity.</summary><value>Null when RID metadata was omitted.</value>
    public RID? RID { get; init; }
    /// <summary>Gets the optional logical owner identity.</summary><value>Null when owner metadata was omitted; zero denotes no assigned owner.</value>
    public ulong? OwnerID { get; init; }
    /// <summary>Gets the live scene navigation owner.</summary><value>Null for omitted, unknown or disposed owners.</value>
    public ElectronObject? Owner { get; init; }
    /// <summary>Gets the link endpoint nearest this waypoint.</summary><value>Null unless type and live NavigationLink owner metadata are available.</value>
    public Vector2? LinkEntryPosition { get; init; }
    /// <summary>Gets the opposite link endpoint.</summary><value>Null unless type and live NavigationLink owner metadata are available.</value>
    public Vector2? LinkExitPosition { get; init; }
}
