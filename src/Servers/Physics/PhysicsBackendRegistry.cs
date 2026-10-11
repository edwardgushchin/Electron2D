namespace Electron2D;

/// <summary>Retains named implementation factories and captures immutable per-world selection outside factory execution.</summary>
internal sealed class PhysicsBackendRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Registration> _entries = new(StringComparer.Ordinal);
    private Registration _default;
    private int _priority;
    internal PhysicsBackendRegistry()
    {
        _entries.Add("CPU", _default = new("CPU", PhysicsServer.Backend.CPU));
        _entries.Add("GPU", new("GPU", PhysicsServer.Backend.GPU));
    }
    internal void RegisterServer(string name, Func<PhysicsSpace, PhysicsWorldBackend> factory)
    {
        ValidateName(name); ArgumentNullException.ThrowIfNull(factory);
        if (name == "DEFAULT") throw new ArgumentException("DEFAULT identifies the selected registration and cannot be registered.", nameof(name));
        lock (_gate)
            if (!_entries.TryAdd(name, new(name, factory))) throw new ArgumentException("A physics implementation with this name is already registered.", nameof(name));
    }
    internal void SetDefaultServer(string name, int priority)
    {
        ValidateName(name);
        lock (_gate)
        {
            if (!_entries.TryGetValue(name, out var entry)) throw new ArgumentException("The physics implementation is not registered.", nameof(name));
            if (priority <= _priority) return;
            _default = entry; _priority = priority;
        }
    }
    internal Selection Resolve(string name)
    {
        ValidateName(name);
        lock (_gate)
            return new(name, name == "DEFAULT" ? _default : _entries.TryGetValue(name, out var entry) ? entry :
                throw new ArgumentException("The physics implementation is not registered.", nameof(name)));
    }
    private static void ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A physics implementation name cannot be empty.", nameof(name));
    }
    internal readonly record struct Selection(string RequestedName, Registration Registration)
    { }
    internal sealed class Registration
    {
        private static readonly AsyncLocal<Creation?> CurrentCreation = new();
        private sealed record Creation(Registration Registration, Creation? Parent);
        private readonly Func<PhysicsSpace, PhysicsWorldBackend>? _factory;
        internal string Name { get; }
        internal PhysicsServer.Backend? BuiltIn { get; }
        internal Registration(string name, PhysicsServer.Backend kind) { Name = name; BuiltIn = kind; }
        internal Registration(string name, Func<PhysicsSpace, PhysicsWorldBackend> factory) { Name = name; _factory = factory; }
        internal void Create(PhysicsSpace space, bool allowCPUFallback, Action<PhysicsWorldBackend> initialize)
        {
            var previous = CurrentCreation.Value;
            for (var frame = previous; frame is not null; frame = frame.Parent)
                if (ReferenceEquals(frame.Registration, this)) throw new InvalidOperationException("A registered physics factory cannot create itself recursively.");
            CurrentCreation.Value = new(this, previous);
            try { initialize(BuiltIn is { } kind ? PhysicsWorldBackend.Create(space, kind, allowCPUFallback) : _factory!(space)); }
            finally { CurrentCreation.Value = previous; }
        }
    }
}
