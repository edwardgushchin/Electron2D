namespace Electron2D;

/// <summary>Configures a typed query over one committed planar navigation map.</summary>
/// <remarks>Caller-owned reusable object. Scalar properties and a query snapshot serialize under its gate;
/// region arrays are copied on set/get and immutable for an in-flight query. Live map identity is resolved at query time.</remarks>
public sealed class NavigationPathQueryParameters : ElectronObject
{
    /// <summary>Supported path search algorithms.</summary>
    public enum PathfindingAlgorithm
    { /// <summary>Weighted A-star corridor search.</summary>
        AStar = 0
    }
    /// <summary>Path corridor output processing modes.</summary>
    public enum PathPostProcessing
    { /// <summary>Funnel corners with crossed-edge provenance.</summary>
        CorridorFunnel = 0, /// <summary>Crossed portal midpoints.</summary>
        EdgeCentered = 1, /// <summary>Search entry positions without geometric postprocessing.</summary>
        None = 2
    }
    /// <summary>Optional parallel point metadata arrays.</summary>
    [Flags]
    public enum PathMetadataFlags
    { /// <summary>No metadata arrays.</summary>
        IncludeNone = 0, /// <summary>Region/link point types.</summary>
        IncludeTypes = 1, /// <summary>Owning region/link RIDs.</summary>
        IncludeRIDs = 2, /// <summary>Logical scene owner identities.</summary>
        IncludeOwners = 4, /// <summary>All supported arrays.</summary>
        IncludeAll = 7
    }
    private readonly object _gate = new();
    private NavigationPathQuerySettings _settings = NavigationPathQuerySettings.Default;
    /// <summary>Creates default A-star/funnel parameters with all metadata and a 4096 polygon search limit.</summary>
    public NavigationPathQueryParameters() { }
    /// <summary>Gets or changes the Map RID resolved at query time; empty/unresolved required identities reject the query.</summary>
    /// <value>Default default; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    public RID Map { get { lock (_gate) { ThrowIfDisposed(); return _settings.Map; } } set { lock (_gate) { ThrowIfDisposed(); _settings = _settings with { Map = value }; } } }
    /// <summary>Gets or changes the finite world-space start point.</summary>
    /// <value>Default Vector2.Zero; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public Vector2 StartPosition { get { lock (_gate) { ThrowIfDisposed(); return _settings.StartPosition; } } set { Point(value); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { StartPosition = value }; } } }
    /// <summary>Gets or changes the finite world-space requested destination.</summary>
    /// <value>Default Vector2.Zero; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public Vector2 TargetPosition { get { lock (_gate) { ThrowIfDisposed(); return _settings.TargetPosition; } } set { Point(value); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { TargetPosition = value }; } } }
    /// <summary>Gets or changes the 32-bit applicable navigation-layer mask.</summary>
    /// <value>Default 1u; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    public uint NavigationLayers { get { lock (_gate) { ThrowIfDisposed(); return _settings.NavigationLayers; } } set { lock (_gate) { ThrowIfDisposed(); _settings = _settings with { NavigationLayers = value }; } } }
    /// <summary>Gets or changes the supported weighted corridor search algorithm.</summary>
    /// <value>Default PathfindingAlgorithm.AStar; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public PathfindingAlgorithm Algorithm { get { lock (_gate) { ThrowIfDisposed(); return _settings.Algorithm; } } set { if (value != PathfindingAlgorithm.AStar) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { Algorithm = value }; } } }
    /// <summary>Gets or changes the corridor output mode.</summary>
    /// <value>Default PathPostProcessing.CorridorFunnel; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public PathPostProcessing PostProcessing { get { lock (_gate) { ThrowIfDisposed(); return _settings.PostProcessing; } } set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { PostProcessing = value }; } } }
    /// <summary>Gets or changes the parallel result point metadata selection.</summary>
    /// <value>Default PathMetadataFlags.IncludeAll; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public PathMetadataFlags MetadataFlags { get { lock (_gate) { ThrowIfDisposed(); return _settings.MetadataFlags; } } set { if ((value & ~PathMetadataFlags.IncludeAll) != 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { MetadataFlags = value }; } } }
    /// <summary>Gets or changes the whether output point decimation is applied.</summary>
    /// <value>Default false; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    public bool SimplifyPath { get { lock (_gate) { ThrowIfDisposed(); return _settings.SimplifyPath; } } set { lock (_gate) { ThrowIfDisposed(); _settings = _settings with { SimplifyPath = value }; } } }
    /// <summary>Gets or changes the finite simplification distance in world units, clamped to zero.</summary>
    /// <value>Default 0f; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float SimplifyEpsilon { get { lock (_gate) { ThrowIfDisposed(); return _settings.SimplifyEpsilon; } } set { Number(value); value = Math.Max(0, value); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { SimplifyEpsilon = value }; } } }
    /// <summary>Gets or changes the finite maximum returned world-space path length, clamped to zero; zero disables clipping.</summary>
    /// <value>Default 0f; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathReturnMaxLength { get { lock (_gate) { ThrowIfDisposed(); return _settings.PathReturnMaxLength; } } set { Number(value); value = Math.Max(0, value); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { PathReturnMaxLength = value }; } } }
    /// <summary>Gets or changes the finite returned-path circle radius about its start, clamped to zero; zero disables clipping.</summary>
    /// <value>Default 0f; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathReturnMaxRadius { get { lock (_gate) { ThrowIfDisposed(); return _settings.PathReturnMaxRadius; } } set { Number(value); value = Math.Max(0, value); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { PathReturnMaxRadius = value }; } } }
    /// <summary>Gets or changes the maximum processed polygon count; zero or negative is unlimited.</summary>
    /// <value>Default 4096; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    public int PathSearchMaxPolygons { get { lock (_gate) { ThrowIfDisposed(); return _settings.PathSearchMaxPolygons; } } set { lock (_gate) { ThrowIfDisposed(); _settings = _settings with { PathSearchMaxPolygons = value }; } } }
    /// <summary>Gets or changes the finite processed-entry distance from projected start, clamped to zero; zero is unlimited.</summary>
    /// <value>Default 0f; captured as one immutable query setting snapshot.</value>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathSearchMaxDistance { get { lock (_gate) { ThrowIfDisposed(); return _settings.PathSearchMaxDistance; } } set { Number(value); value = Math.Max(0, value); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { PathSearchMaxDistance = value }; } } }
    /// <summary>Gets or changes the copied includedregions RID filter array.</summary>
    /// <value>Empty includes every applicable region; otherwise both link endpoint regions must be included. Empty by default; returned arrays are independent copies.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    public RID[] IncludedRegions { get { lock (_gate) { ThrowIfDisposed(); return (RID[])_settings.IncludedRegions.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); var copy = (RID[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { IncludedRegions = copy }; } } }
    /// <summary>Gets or changes the copied excludedregions RID filter array.</summary>
    /// <value>Excluded regions override included regions, including either endpoint region of a link. Empty by default; returned arrays are independent copies.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ObjectDisposedException">This object is disposed.</exception>
    public RID[] ExcludedRegions { get { lock (_gate) { ThrowIfDisposed(); return (RID[])_settings.ExcludedRegions.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); var copy = (RID[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { ExcludedRegions = copy }; } } }
    private static void Number(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Point(Vector2 value) { if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal NavigationPathQuerySettings Snapshot() { lock (_gate) { ThrowIfDisposed(); return _settings; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _settings = NavigationPathQuerySettings.Default; base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(QueryProperties);
    private static readonly PropertyDescriptor[] QueryProperties =
    [
        new PropertyDescriptor<NavigationPathQueryParameters,RID>(nameof(Map),n=>n.Map,(n,v)=>n.Map=v,_=>default),
        new PropertyDescriptor<NavigationPathQueryParameters,Vector2>(nameof(StartPosition),n=>n.StartPosition,(n,v)=>n.StartPosition=v,_=>Vector2.Zero),
        new PropertyDescriptor<NavigationPathQueryParameters,Vector2>(nameof(TargetPosition),n=>n.TargetPosition,(n,v)=>n.TargetPosition=v,_=>Vector2.Zero),
        new PropertyDescriptor<NavigationPathQueryParameters,uint>(nameof(NavigationLayers),n=>n.NavigationLayers,(n,v)=>n.NavigationLayers=v,_=>1u),
        new PropertyDescriptor<NavigationPathQueryParameters,PathfindingAlgorithm>(nameof(Algorithm),n=>n.Algorithm,(n,v)=>n.Algorithm=v,_=>PathfindingAlgorithm.AStar),
        new PropertyDescriptor<NavigationPathQueryParameters,PathPostProcessing>(nameof(PostProcessing),n=>n.PostProcessing,(n,v)=>n.PostProcessing=v,_=>PathPostProcessing.CorridorFunnel),
        new PropertyDescriptor<NavigationPathQueryParameters,PathMetadataFlags>(nameof(MetadataFlags),n=>n.MetadataFlags,(n,v)=>n.MetadataFlags=v,_=>PathMetadataFlags.IncludeAll),
        new PropertyDescriptor<NavigationPathQueryParameters,bool>(nameof(SimplifyPath),n=>n.SimplifyPath,(n,v)=>n.SimplifyPath=v,_=>false),
        new PropertyDescriptor<NavigationPathQueryParameters,float>(nameof(SimplifyEpsilon),n=>n.SimplifyEpsilon,(n,v)=>n.SimplifyEpsilon=v,_=>0f),
        new PropertyDescriptor<NavigationPathQueryParameters,float>(nameof(PathReturnMaxLength),n=>n.PathReturnMaxLength,(n,v)=>n.PathReturnMaxLength=v,_=>0f),
        new PropertyDescriptor<NavigationPathQueryParameters,float>(nameof(PathReturnMaxRadius),n=>n.PathReturnMaxRadius,(n,v)=>n.PathReturnMaxRadius=v,_=>0f),
        new PropertyDescriptor<NavigationPathQueryParameters,int>(nameof(PathSearchMaxPolygons),n=>n.PathSearchMaxPolygons,(n,v)=>n.PathSearchMaxPolygons=v,_=>4096),
        new PropertyDescriptor<NavigationPathQueryParameters,float>(nameof(PathSearchMaxDistance),n=>n.PathSearchMaxDistance,(n,v)=>n.PathSearchMaxDistance=v,_=>0f),
        new PropertyDescriptor<NavigationPathQueryParameters,RID[]>(nameof(IncludedRegions),n=>n.IncludedRegions,(n,v)=>n.IncludedRegions=v,_=>[]),
        new PropertyDescriptor<NavigationPathQueryParameters,RID[]>(nameof(ExcludedRegions),n=>n.ExcludedRegions,(n,v)=>n.ExcludedRegions=v,_=>[]),
    ];
}
internal readonly record struct NavigationPathQuerySettings
{
    internal RID Map { get; init; }
    internal Vector2 StartPosition { get; init; }
    internal Vector2 TargetPosition { get; init; }
    internal uint NavigationLayers { get; init; }
    internal NavigationPathQueryParameters.PathfindingAlgorithm Algorithm { get; init; }
    internal NavigationPathQueryParameters.PathPostProcessing PostProcessing { get; init; }
    internal NavigationPathQueryParameters.PathMetadataFlags MetadataFlags { get; init; }
    internal bool SimplifyPath { get; init; }
    internal float SimplifyEpsilon { get; init; }
    internal float PathReturnMaxLength { get; init; }
    internal float PathReturnMaxRadius { get; init; }
    internal int PathSearchMaxPolygons { get; init; }
    internal float PathSearchMaxDistance { get; init; }
    internal RID[] IncludedRegions { get; init; }
    internal RID[] ExcludedRegions { get; init; }
    internal static NavigationPathQuerySettings Default => new() { MetadataFlags = NavigationPathQueryParameters.PathMetadataFlags.IncludeAll, NavigationLayers = 1, PathSearchMaxPolygons = 4096, IncludedRegions = [], ExcludedRegions = [] };
}
