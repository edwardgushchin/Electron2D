namespace Electron2D;

/// <summary>Provides process-wide discovery and selection of text implementations.</summary>
/// <remarks>Registration retains an implementation without transferring disposal ownership. Callbacks run after
/// committed changes and outside the registry lock. Interface snapshots are independent ordered arrays.</remarks>
public sealed class TextServerManager : ElectronObject
{
    internal static TextServerManager Service { get; } = new();
    private readonly object _gate = new();
    private readonly TextServer _builtin = new TextServer.Builtin();
    private readonly List<TextServer> _interfaces;
    private TextServer? _primary;
    private int _readers;
    [ThreadStatic] private static TextServer? _consumerProvider;
    [ThreadStatic] private static TextServer? _creatingConsumerBuffer;
    internal static TextServer? CallbackProvider => !TextServer.IsNativeBackendCall && (_consumerProvider ?? Volatile.Read(ref Service._primary)) is TextServerExtension provider ? provider : null;
    internal static bool IsCreatingConsumerBuffer(TextServer provider) => ReferenceEquals(_creatingConsumerBuffer, provider);
    internal static BufferCreation BeginBufferCreation(TextServer provider)
    { var previous = _creatingConsumerBuffer; _creatingConsumerBuffer = provider; return new(previous); }
    internal readonly struct BufferCreation(TextServer? previous) : IDisposable
    { public void Dispose() => _creatingConsumerBuffer = previous; }
    internal static bool IsConsumerAdmitted(TextServer provider) => ReferenceEquals(_consumerProvider, provider);
    private long _generation;
    internal static TextServer NativeInterface => Service._builtin;
    internal static long ContentGeneration => Volatile.Read(ref Service._generation);
    internal static bool UsesCallbacks => (_consumerProvider ?? Volatile.Read(ref Service._primary)) is TextServerExtension;
    internal static ConsumerRead BeginConsumerRead(TextServer? owner = null)
    {
        lock (Service._gate)
        {
            var provider = owner ?? _consumerProvider ?? Service._primary ?? (TextServer.IsNativeBackendCall ? Service._builtin : throw new InvalidOperationException("No primary text implementation is selected."));
            ObjectDisposedException.ThrowIf(provider.IsDisposed, provider);
            provider.AcquireConsumerAdmission(); Service._readers++;
            var previous = _consumerProvider; _consumerProvider = provider;
            return new(provider, Service._generation, previous);
        }
    }
    internal static ConsumerReservation ReserveConsumer(TextServer provider)
    {
        lock (Service._gate)
        { provider.AcquireConsumerAdmission(); Service._readers++; return new(provider); }
    }
    internal readonly struct ConsumerReservation(TextServer provider) : IDisposable
    {
        public void Dispose() { lock (Service._gate) Service._readers--; provider.ReleaseConsumerAdmission(); }
    }
    internal readonly struct ConsumerRead(TextServer provider, long generation, TextServer? previous) : IDisposable
    {
        internal TextServer Provider => provider;
        internal long Generation => generation;
        public void Dispose() { if (provider is null) return; _consumerProvider = previous; lock (Service._gate) Service._readers--; provider.ReleaseConsumerAdmission(); }
    }
    private TextServerManager() { _primary = _builtin; _interfaces = [_builtin]; }
    /// <summary>Returns the primary text implementation.</summary><returns>The selected interface.</returns>
    /// <exception cref="InvalidOperationException">The primary interface was explicitly cleared.</exception>
    public static TextServer GetPrimaryInterface() => Volatile.Read(ref Service._primary) ?? throw new InvalidOperationException("No primary text implementation is selected.");
    /// <summary>Returns the number of registered interfaces.</summary><returns>The current registry size.</returns>
    public static int GetInterfaceCount() { lock (Service._gate) return Service._interfaces.Count; }
    /// <summary>Returns an interface by its current index.</summary><param name="index">Zero-based index.</param><returns>The registered implementation.</returns>
    public static TextServer GetInterface(int index)
    {
        lock (Service._gate) return (uint)index < (uint)Service._interfaces.Count ? Service._interfaces[index] : throw new ArgumentOutOfRangeException(nameof(index));
    }
    /// <summary>Returns registered interfaces in current index order.</summary><returns>A caller-owned array; each object supplies its own name.</returns>
    public static TextServer[] GetInterfaces() { lock (Service._gate) return Service._interfaces.ToArray(); }
    /// <summary>Finds the first registered interface with the exact name.</summary><param name="name">Case-sensitive name.</param><returns>The implementation, or null.</returns>
    public static TextServer? FindInterface(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var implementation in GetInterfaces()) if (!implementation.IsDisposed && implementation.GetName() == name) return implementation;
        return null;
    }
    /// <summary>Appends an implementation unless its exact object is already registered.</summary>
    /// <param name="implementation">Live caller-owned text implementation.</param>
    /// <remarks>Duplicate object registration is silent. Duplicate names retain their index order.
    /// An event failure leaves the registration committed.</remarks>
    /// <exception cref="ArgumentNullException">The implementation is null.</exception>
    /// <exception cref="ObjectDisposedException">The implementation is disposed.</exception>
    public static void AddInterface(TextServer implementation)
    {
        ArgumentNullException.ThrowIfNull(implementation); implementation.EnsureRegistryAvailable();
        var name = implementation.GetName();
        lock (Service._gate)
        {
            implementation.EnsureRegistryAvailable();
            if (Service._interfaces.Contains(implementation)) return;
            Service._interfaces.Add(implementation);
        }
        Service.BeginEventEmission(Service._publisherAdded.Handlers)?.Invoke(name);
    }
    /// <summary>Removes a registered nonprimary implementation without disposing it.</summary>
    /// <param name="implementation">Registered implementation.</param>
    /// <remarks>Cleanup runs before removal, outside registry locks and without new consumer admission.
    /// Cleanup failure still commits removal and delivers the event; later indices shift down. Callback failures do not restore the entry.</remarks>
    /// <exception cref="ArgumentNullException">The implementation is null.</exception>
    /// <exception cref="InvalidOperationException">It is primary or absent from the registry.</exception>
    public static void RemoveInterface(TextServer implementation)
    {
        ArgumentNullException.ThrowIfNull(implementation); var name = implementation.GetName();
        lock (Service._gate)
        {
            if (Service._readers != 0) throw new InvalidOperationException("Text implementations cannot retire during an active consumer read.");
            if (ReferenceEquals(Service._primary, implementation)) throw new InvalidOperationException("The primary text implementation cannot be removed.");
            if (!Service._interfaces.Contains(implementation)) throw new InvalidOperationException("The text implementation is not registered.");
            implementation.BeginUnregister();
        }
        List<Exception>? errors = null;
        try
        {
            try { implementation.CleanupBeforeUnregister(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            lock (Service._gate) Service._interfaces.Remove(implementation);
        }
        finally { implementation.EndUnregister(); }
        try { Service.BeginEventEmission(Service._publisherRemoved.Handlers)?.Invoke(name); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Text implementation removal committed with callback failures.", errors);
    }
    /// <summary>Selects a live implementation, or explicitly clears selection.</summary>
    /// <param name="implementation">The implementation; registration is independent of selection. Null clears it.</param>
    /// <remarks>A nonnull assignment delivers the ordinary text-server-changed system notification to an active main loop.
    /// Notification failure leaves the selection committed. Existing provider-owned identities retain their creator.</remarks>
    /// <exception cref="ObjectDisposedException">The implementation is disposed.</exception>
    /// <exception cref="InvalidOperationException">The active scene owner is unavailable on this thread.</exception>
    public static void SetPrimaryInterface(TextServer? implementation)
    {
        if (implementation is not null) implementation.EnsureRegistryAvailable();
        var loop = Engine.MainLoop;
        if (loop is SceneTree tree) tree.EnsureOwnerThread();
        TextServer? previous;
        lock (Service._gate)
        {
            if (implementation is not null) implementation.EnsureRegistryAvailable();
            if (Service._readers != 0) throw new InvalidOperationException("The text implementation cannot change during an active consumer read.");
            previous = Service._primary;
            Volatile.Write(ref Service._primary, implementation);
            Interlocked.Increment(ref Service._generation);
        }
        List<Exception>? errors = null;
        try { if (implementation is not null) CanvasRasterScale.TrackProvider(implementation); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { TextLayout.RetireProviderLayouts(previous); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { Font.InvalidateTextProviderCaches(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { if (implementation is not null) loop?.Notify(MainLoop.NotificationTextServerChanged); }
        catch (Exception error) { Node.CollectException(ref errors, error); }
        if (errors is not null) throw new AggregateException("Text provider selection committed with notification failures.", errors);
    }
    /// <summary>Occurs after an implementation has been registered.</summary>
    public static event Action<string>? InterfaceAdded { add => Service._publisherAdded.Add(value); remove => Service._publisherAdded.Remove(value); }
    private EventPublisher<Action<string>> _publisherAdded;
    /// <summary>Occurs after an implementation has been removed.</summary>
    public static event Action<string>? InterfaceRemoved { add => Service._publisherRemoved.Add(value); remove => Service._publisherRemoved.Remove(value); }
    private EventPublisher<Action<string>> _publisherRemoved;
    internal static bool IsRetained(TextServer implementation)
    { lock (Service._gate) return ReferenceEquals(Service._primary, implementation) || Service._interfaces.Contains(implementation); }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process-wide TextServerManager cannot be disposed.");
}
