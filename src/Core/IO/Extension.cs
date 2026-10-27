namespace Electron2D;

/// <summary>Identifies ordered runtime initialization stages.</summary>
public enum ExtensionInitializationLevel
{
    /// <summary>Core object and resource services.</summary>
    Core = 0,
    /// <summary>Runtime servers.</summary>
    Servers = 1,
    /// <summary>Scene services.</summary>
    Scene = 2,
    /// <summary>Editor-host services.</summary>
    Editor = 3
}

/// <summary>Describes the typed initialization callbacks of one executable extension module.</summary>
/// <remarks>Closures retain caller-defined module state. Initialization is cold setup; callback ownership and cleanup
/// remain with the module. The minimum level is reload metadata, not a filter on first-load callbacks.</remarks>
public readonly struct ExtensionInitialization
{
    /// <summary>Creates a validated module entry result.</summary>
    /// <param name="minimumLevel">Earliest level required when replacing the module.</param>
    /// <param name="initialize">Callback invoked in ascending level order.</param>
    /// <param name="deinitialize">Callback invoked in descending level order, including rollback of a failed initialization.</param>
    /// <exception cref="ArgumentOutOfRangeException">The minimum level is invalid.</exception>
    /// <exception cref="ArgumentNullException">Either callback is null.</exception>
    public ExtensionInitialization(ExtensionInitializationLevel minimumLevel,
        Action<ExtensionInitializationLevel> initialize, Action<ExtensionInitializationLevel> deinitialize)
    {
        if (minimumLevel is < ExtensionInitializationLevel.Core or > ExtensionInitializationLevel.Editor)
            throw new ArgumentOutOfRangeException(nameof(minimumLevel));
        ArgumentNullException.ThrowIfNull(initialize); ArgumentNullException.ThrowIfNull(deinitialize);
        MinimumLevel = minimumLevel; Initialize = initialize; Deinitialize = deinitialize;
    }
    /// <summary>Gets the earliest module replacement level.</summary>
    public ExtensionInitializationLevel MinimumLevel { get; }
    /// <summary>Gets the ascending initialization callback.</summary>
    public Action<ExtensionInitializationLevel> Initialize { get; }
    /// <summary>Gets the descending cleanup callback.</summary>
    public Action<ExtensionInitializationLevel> Deinitialize { get; }
}

/// <summary>Retains the executable entry and initialization state of one extension module.</summary>
/// <remarks>The manager owns a registered module. After unloading, a retained resource remains open until disposed;
/// duplication and archive construction create a closed resource and never copy executable callbacks.</remarks>
public partial class Extension : Resource
{
    private Action<ExtensionInitializationLevel>? _initialize, _deinitialize;
    private ExtensionInitializationLevel _minimum;
    private bool _open;
    private int _highest = -1;
    private int _availableLevel = -1;
    private int _initializedLevels;
    private int _registeringLevel = -1;
    internal bool Managed;
    /// <summary>Creates a closed extension resource without invoking a module entry.</summary>
    public Extension() { }
    /// <summary>Reports whether an executable module entry is retained.</summary>
    /// <returns>True after successful entry validation, including after manager unloading while this resource is retained.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool IsLibraryOpen() { ThrowIfDisposed(); return Volatile.Read(ref _open); }
    /// <summary>Gets the minimum replacement level advertised by the open entry.</summary>
    /// <returns>The entry's declared minimum level.</returns>
    /// <exception cref="InvalidOperationException">No entry is open.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public ExtensionInitializationLevel GetMinimumLibraryInitializationLevel()
    {
        ThrowIfDisposed();
        if (!Volatile.Read(ref _open)) throw new InvalidOperationException("The extension has no open module entry.");
        return _minimum;
    }
    internal void Open(ExtensionInitialization initialization)
    {
        if (initialization.Initialize is null || initialization.Deinitialize is null)
            throw new ArgumentException("An extension entry must provide both lifecycle callbacks.", nameof(initialization));
        _minimum = initialization.MinimumLevel;
        _initialize = initialization.Initialize; _deinitialize = initialization.Deinitialize;
        Volatile.Write(ref _open, true);
    }
    internal void InitializeTo(ExtensionInitializationLevel level, int minimum = 0)
    {
        _availableLevel = (int)level;
        for (var next = minimum; next <= (int)level; next++)
        {
            _highest = Math.Max(_highest, next); _initializedLevels |= 1 << next;
            _registeringLevel = next;
            try { _initialize!((ExtensionInitializationLevel)next); }
            finally { _registeringLevel = -1; }
        }
    }
    internal void Deinitialize(ref List<Exception>? errors, int minimum = 0)
    {
        _retiring = true;
        try
        {
            for (var index = _highest; index >= minimum; index--)
            {
                if ((_initializedLevels & (1 << index)) == 0) continue;
                _initializedLevels &= ~(1 << index);
                try { _deinitialize!((ExtensionInitializationLevel)index); }
                catch (Exception error) { (errors ??= []).Add(error); }
                RetireClasses(index, ref errors);
            }
            while (_highest >= 0 && (_initializedLevels & (1 << _highest)) == 0) _highest--;
        }
        finally { _retiring = false; }
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance()
    {
        if (GetType() != typeof(Extension)) return base.CreateDuplicateInstance();
        return new Extension();
    }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        // Executable callbacks are module ownership, not stored Resource state.
        if (GetType() != typeof(Extension)) base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
    }
    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        base.ValidateDisposal();
        if (Volatile.Read(ref Managed)) throw new InvalidOperationException("Unload a managed extension before disposing its borrowed resource.");
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        Volatile.Write(ref _open, false); _initialize = _deinitialize = null;
        Entry = null; _replacementState = null;
        base.Dispose(disposing);
    }
}
