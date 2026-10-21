namespace Electron2D;

public abstract partial class ElectronObject
{
    private static readonly AsyncLocal<ConstructionScope?> FactoryConstruction = new();

    /// <summary>Initializes a new engine object with its independent process-local identity.</summary>
    /// <remarks>Allocates a shared weak identity for lifetime diagnostics without retaining this object or invoking
    /// derived code. Committed disposal or collection removes it from diagnostic counts.</remarks>
    protected ElectronObject()
    {
        _identityReference = new(this);
        ObjectCensus.Register(_identityReference);
        for (var scope = FactoryConstruction.Value; scope is not null; scope = scope.Parent) scope.Register(this);
    }

    // Closed scopes may outlive their invocation in captured contexts; release every recorded object.
    internal sealed class ConstructionScope(ConstructionScope? parent, bool scene)
    {
        internal readonly ConstructionScope? Parent = parent;
        internal readonly bool Scene = scene;
        private HashSet<ElectronObject>? _created = new(ReferenceEqualityComparer.Instance);
        internal void Register(ElectronObject value) { lock (this) _created?.Add(value); }
        internal bool Contains(ElectronObject? value) { lock (this) return value is not null && _created?.Contains(value) == true; }
        internal bool IsActive { get { lock (this) return _created is not null; } }
        internal void Close() { lock (this) _created = null; }
    }

    internal static ConstructionScope? ConstructionContext
    {
        get => FactoryConstruction.Value;
        set { if (!ReferenceEquals(FactoryConstruction.Value, value)) FactoryConstruction.Value = value; }
    }

    internal static bool IsConstructingScene
    {
        get
        {
            for (var scope = ConstructionContext; scope is not null; scope = scope.Parent)
                if (scope.Scene && scope.IsActive) return true;
            return false;
        }
    }

    internal static T InvokeConstructionFactory<T>(Func<T> factory, out bool created, bool scene = false) where T : ElectronObject
    {
        var previous = ConstructionContext;
        var scope = new ConstructionScope(previous, scene);
        ConstructionContext = scope;
        try
        {
            var value = factory();
            created = scope.Contains(value);
            return value;
        }
        finally { scope.Close(); ConstructionContext = previous; }
    }
}
