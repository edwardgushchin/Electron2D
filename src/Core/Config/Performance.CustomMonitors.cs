using System.Diagnostics;

namespace Electron2D;

public sealed partial class Performance
{
    /// <summary>Describes the display unit of a custom runtime monitor.</summary>
    public enum MonitorType
    {
        /// <summary>A unitless quantity.</summary>
        Quantity = 0,
        /// <summary>A byte count.</summary>
        Memory = 1,
        /// <summary>A duration in seconds.</summary>
        Time = 2,
        /// <summary>A fractional ratio displayed as a percentage; 0.5 represents 50 percent.</summary>
        Percentage = 3,
    }
    /// <summary>An immutable typed identity for one custom monitor registration.</summary>
    /// <typeparam name="TValue">The exact callback result type, without boxing or numeric conversion.</typeparam>
    /// <remarks>Removal invalidates this handle and releases its callback. Registering the same ID again creates
    /// a different identity. Returned mutable values and callback captures remain application-owned.</remarks>
    public sealed class CustomMonitor<TValue>
    {
        private Func<TValue>? _callback;
        internal CustomMonitor(string id, Func<TValue> callback, MonitorType type) { ID = id; Type = type; _callback = callback; }
        /// <summary>Gets the immutable ordinal registration ID.</summary><value>The exact supplied ID, including slash delimiters.</value>
        public string ID { get; }
        /// <summary>Gets the immutable display unit.</summary><value>The unit assigned at registration.</value>
        public MonitorType Type { get; }
        internal Func<TValue>? Callback => _callback;
        internal void Detach() => _callback = null;
    }
    private readonly object _monitorGate = new();
    private readonly Dictionary<string, MonitorRegistration> _customMonitors = new(StringComparer.Ordinal);
    private readonly List<string> _monitorOrder = [];
    private readonly long _monitorEpoch = Stopwatch.GetTimestamp();
    private ulong _monitorModificationTime;
    [ThreadStatic] private static HashSet<object>? _readingMonitors;
    private readonly record struct MonitorRegistration(object Key, Action Detach, MonitorType Type);

    /// <summary>Registers an immediately readable custom monitor and returns its typed identity.</summary>
    /// <typeparam name="TValue">The callback's exact result type.</typeparam>
    /// <param name="id">The unique ordinal ID; empty IDs and slash-delimited IDs retain their exact identities.</param>
    /// <param name="callback">The direct provider; capture typed arguments in a closure when needed.</param>
    /// <param name="type">The display unit, initially Quantity.</param>
    /// <returns>A handle valid until this ID is removed.</returns>
    /// <remarks>Registration allocates and retains the provider. Reads invoke it synchronously on the calling thread,
    /// outside the registry lock. The application owns capture lifetimes and callback thread safety. No value is sampled
    /// at registration, no delegate is serialized and no arbitrary method/member name is invoked.</remarks>
    /// <exception cref="ArgumentNullException">ID or callback is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The display unit is undefined.</exception>
    /// <exception cref="InvalidOperationException">The ID is already registered.</exception>
    public static CustomMonitor<TValue> AddCustomMonitor<TValue>(string id, Func<TValue> callback, MonitorType type = MonitorType.Quantity)
    {
        ArgumentNullException.ThrowIfNull(id); ArgumentNullException.ThrowIfNull(callback);
        if ((uint)type > 3) throw new ArgumentOutOfRangeException(nameof(type));
        var service = Service;
        lock (service._monitorGate)
        {
            if (service._customMonitors.ContainsKey(id)) throw new InvalidOperationException("The custom monitor ID is already registered.");
            var key = new CustomMonitor<TValue>(id, callback, type);
            service._customMonitors.Add(id, new(key, key.Detach, type)); service._monitorOrder.Add(id); service.MarkMonitorModification(); return key;
        }
    }
    /// <summary>Registers a provider with an explicitly captured typed argument.</summary>
    /// <typeparam name="TArgument">The copied argument value or borrowed reference; tuples represent multiple arguments.</typeparam>
    /// <typeparam name="TValue">The callback's exact result type.</typeparam>
    /// <param name="id">The exact unique ordinal ID.</param><param name="callback">The direct typed provider.</param>
    /// <param name="argument">The value captured at registration; referenced objects remain application-owned.</param>
    /// <param name="type">The display unit.</param><returns>A current typed registration identity.</returns>
    /// <exception cref="ArgumentNullException">ID or callback is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The unit is undefined.</exception>
    /// <exception cref="InvalidOperationException">ID is already registered.</exception>
    public static CustomMonitor<TValue> AddCustomMonitor<TArgument, TValue>(string id, Func<TArgument, TValue> callback, TArgument argument, MonitorType type = MonitorType.Quantity)
    { ArgumentNullException.ThrowIfNull(callback); return AddCustomMonitor(id, () => callback(argument), type); }
    /// <summary>Invokes a currently registered provider and returns its exact typed result.</summary>
    /// <typeparam name="TValue">The handle's exact result type.</typeparam><param name="monitor">The identity returned by registration.</param>
    /// <returns>The provider's raw value, including its numeric precision or reference identity.</returns>
    /// <remarks>No clamping, conversion or caching occurs. Numeric display clients apply their own presentation rules.
    /// Removal after lookup does not cancel an in-flight read. Recursive reads of the same identity on one thread reject;
    /// other threads and different monitors may read concurrently. First reads or deeper recursion may allocate guard storage.</remarks>
    /// <exception cref="ArgumentNullException">The handle is null.</exception>
    /// <exception cref="KeyNotFoundException">The identity has been removed or replaced.</exception>
    /// <exception cref="InvalidOperationException">A recursive read cycles back to this identity.</exception>
    /// <exception cref="Exception">The provider throws.</exception>
    public static TValue GetCustomMonitor<TValue>(CustomMonitor<TValue> monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor); var service = Service; Func<TValue> callback;
        lock (service._monitorGate)
        {
            if (!service._customMonitors.TryGetValue(monitor.ID, out var entry) || !ReferenceEquals(entry.Key, monitor)) throw new KeyNotFoundException("The custom monitor identity is no longer registered.");
            callback = monitor.Callback!;
        }
        var reading = _readingMonitors ??= new(ReferenceEqualityComparer.Instance);
        if (!reading.Add(monitor)) throw new InvalidOperationException("A custom monitor cannot recursively read itself.");
        try { return callback(); } finally { reading.Remove(monitor); }
    }
    /// <summary>Reports whether the exact ordinal ID is registered.</summary><param name="id">The non-null ID.</param><returns>True for a current registration.</returns>
    /// <exception cref="ArgumentNullException">ID is null.</exception>
    public static bool HasCustomMonitor(string id) { ArgumentNullException.ThrowIfNull(id); lock (Service._monitorGate) return Service._customMonitors.ContainsKey(id); }
    /// <summary>Removes a registration and releases its provider and captured arguments.</summary><param name="id">The current non-null ID.</param>
    /// <remarks>Does not dispose application objects or wait for already-started reads. Other IDs retain their ordering.</remarks>
    /// <exception cref="ArgumentNullException">ID is null.</exception><exception cref="KeyNotFoundException">ID is absent.</exception>
    public static void RemoveCustomMonitor(string id)
    {
        ArgumentNullException.ThrowIfNull(id); var service = Service;
        lock (service._monitorGate)
        {
            if (!service._customMonitors.Remove(id, out var entry)) throw new KeyNotFoundException("The custom monitor ID is not registered.");
            entry.Detach(); service._monitorOrder.Remove(id); service.MarkMonitorModification();
        }
    }
    /// <summary>Returns an independent name snapshot in registration order.</summary><returns>Exact IDs; removed/readded IDs move to the end.</returns>
    /// <remarks>Concurrent mutation between separate name/type queries may change their alignment.</remarks>
    public static string[] GetCustomMonitorNames() { lock (Service._monitorGate) return Service._monitorOrder.ToArray(); }
    /// <summary>Returns an independent unit snapshot in the same registration order as names.</summary><returns>Typed display units for current registrations.</returns>
    /// <remarks>Each query is atomic. Align separate name/type snapshots using application synchronization; modification ticks are not unique revisions.</remarks>
    public static MonitorType[] GetCustomMonitorTypes()
    { var service = Service; lock (service._monitorGate) { var types = new MonitorType[service._monitorOrder.Count]; for (var i = 0; i < types.Length; i++) types[i] = service._customMonitors[service._monitorOrder[i]].Type; return types; } }
    /// <summary>Gets the last successful registration/removal tick in elapsed microseconds.</summary>
    /// <returns>Zero before the first mutation; a nondecreasing tick relative to retained diagnostics initialization thereafter.</returns>
    /// <remarks>Reads and failed mutations do not change it. Multiple successful changes may share a microsecond.
    /// The process-wide registry persists across native sessions and is usable before a windowed runtime starts.</remarks>
    public static ulong GetMonitorModificationTime() { lock (Service._monitorGate) return Service._monitorModificationTime; }
    private void MarkMonitorModification() => _monitorModificationTime = (ulong)(Stopwatch.GetElapsedTime(_monitorEpoch).Ticks / 10);
}
