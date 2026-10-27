using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Owns function-loaded extension modules and their ordered initialization lifecycle.</summary>
/// <remarks>Mutation belongs to the first mutating caller. Reads use synchronized access and independent address snapshots.
/// Library-host runtime services are ready through Scene; a load captures Editor when the host's editor hint is set.
/// Registered-class replacement and file-based module loading are separate workflows.</remarks>
public sealed class ExtensionManager : ElectronObject
{
    /// <summary>Reports the outcome of an extension registry operation.</summary>
    public enum LoadStatus
    {
        /// <summary>The operation completed.</summary>
        Ok = 0,
        /// <summary>The entry declined loading.</summary>
        Failed = 1,
        /// <summary>The exact path is already registered.</summary>
        AlreadyLoaded = 2,
        /// <summary>The exact path has no registered module.</summary>
        NotLoaded = 3,
        /// <summary>A replacement requires restarting its host.</summary>
        NeedsRestart = 4
    }
    internal static readonly ExtensionManager Service = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, Extension> _modules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pending = new(StringComparer.Ordinal);
    private int _owner;
    private EventPublisher<Action<Extension>> _loaded, _unloading;
    private ExtensionManager() { }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The extension manager has permanent process lifetime.");
    /// <summary>Occurs after initialization, before path/registry publication.</summary>
    /// <remarks>Observer failures propagate after publication. A handler can load another path; reentrant mutation of the same path rejects.</remarks>
    public static event Action<Extension>? ExtensionLoaded { add => Service._loaded.Add(value); remove => Service._loaded.Remove(value); }
    /// <summary>Occurs before reverse deinitialization while the module is still registered.</summary>
    /// <remarks>Observer failures do not skip deinitialization or registry retirement.</remarks>
    public static event Action<Extension>? ExtensionUnloading { add => Service._unloading.Add(value); remove => Service._unloading.Remove(value); }

    /// <summary>Loads an exact module address through a typed entry function.</summary>
    /// <param name="path">Ordinal module address; it is not interpreted as a file or assembly name.</param>
    /// <param name="initialize">Entry receiving its fresh closed resource; null output declines loading.</param>
    /// <returns>Ok, AlreadyLoaded, or Failed when the entry declines.</returns>
    /// <remarks>First load calls every available level from Core, independently of the minimum replacement level.
    /// Callback/validation failures propagate after rollback. A failed level is included in reverse cleanup.
    /// A successful load retains its resource; unload before explicitly disposing a borrowed result.</remarks>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the mutation owner or the same address is transitioning.</exception>
    /// <exception cref="Exception">An entry, lifecycle callback, path publication or observer fails.</exception>
    public static LoadStatus LoadExtensionFromFunction(string path, Func<Extension, ExtensionInitialization?> initialize)
    {
        ArgumentNullException.ThrowIfNull(path); ArgumentNullException.ThrowIfNull(initialize);
        var owner = Service; owner.EnsureOwner();
        lock (owner._gate)
        {
            if (owner._modules.ContainsKey(path)) return LoadStatus.AlreadyLoaded;
            if (!owner._pending.Add(path)) throw new InvalidOperationException("The extension address is already transitioning.");
        }
        var highestLevel = Engine.IsEditorHint ? ExtensionInitializationLevel.Editor : ExtensionInitializationLevel.Scene;
        Extension? extension = null;
        var published = false; List<Exception>? errors = null;
        try
        {
            extension = new Extension { Managed = true };
            var entry = initialize(extension);
            if (entry is null) return LoadStatus.Failed;
            extension.Open(entry.Value);
            extension.InitializeTo(highestLevel);
            var handlers = owner.BeginEventEmission(owner._loaded.Handlers);
            try { handlers?.Invoke(extension); } catch (Exception error) { (errors ??= []).Add(error); }
            extension.ResourcePath = path;
            lock (owner._gate) owner._modules.Add(path, extension);
            published = true;
            ThrowErrors(errors);
            return LoadStatus.Ok;
        }
        catch (Exception error) when (!published)
        {
            (errors ??= []).Add(error);
            return LoadStatus.Failed;
        }
        finally
        {
            if (!published && extension is not null)
            {
                extension.Deinitialize(ref errors); Volatile.Write(ref extension.Managed, false);
                try { extension.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            lock (owner._gate) owner._pending.Remove(path);
            if (!published) ThrowErrors(errors);
        }
    }

    /// <summary>Gets the borrowed registered resource for an exact module address.</summary>
    /// <returns>The resource, or null when not loaded.</returns>
    /// <param name="path">The exact module address.</param>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    public static Extension? GetExtension(string path) { ArgumentNullException.ThrowIfNull(path); lock (Service._gate) return Service._modules.GetValueOrDefault(path); }
    /// <summary>Reports whether an exact module address is registered.</summary>
    /// <param name="path">The exact module address.</param><returns>True only after successful publication.</returns>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    public static bool IsExtensionLoaded(string path) => GetExtension(path) is not null;
    /// <summary>Copies the currently registered module addresses.</summary>
    /// <returns>An independent string-array snapshot.</returns>
    public static string[] GetLoadedExtensions() { lock (Service._gate) return Service._modules.Keys.ToArray(); }

    /// <summary>Deinitializes and unregisters one module without disposing a retained resource.</summary>
    /// <param name="path">The exact module address.</param><returns>Ok or NotLoaded.</returns>
    /// <remarks>Every initialized level is retired despite callback failures; the registry removal commits before errors propagate.</remarks>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="InvalidOperationException">The caller is foreign or this address is transitioning.</exception>
    /// <exception cref="Exception">An observer or deinitialization callback fails.</exception>
    public static LoadStatus UnloadExtension(string path)
    {
        ArgumentNullException.ThrowIfNull(path); var owner = Service; owner.EnsureOwner(); Extension extension;
        lock (owner._gate)
        {
            if (owner._pending.Contains(path)) throw new InvalidOperationException("The extension address is already transitioning.");
            if (!owner._modules.TryGetValue(path, out extension!)) return LoadStatus.NotLoaded;
            owner._pending.Add(path);
        }
        List<Exception>? errors = null;
        try
        {
            var handlers = owner.BeginEventEmission(owner._unloading.Handlers);
            try { handlers?.Invoke(extension); } catch (Exception error) { (errors ??= []).Add(error); }
            extension.Deinitialize(ref errors);
        }
        finally
        {
            lock (owner._gate) { owner._modules.Remove(path); owner._pending.Remove(path); }
            Volatile.Write(ref extension.Managed, false);
        }
        ThrowErrors(errors); return LoadStatus.Ok;
    }
    private void EnsureOwner()
    {
        var caller = Environment.CurrentManagedThreadId;
        if (Interlocked.CompareExchange(ref _owner, caller, 0) is var previous && previous != 0 && previous != caller)
            throw new InvalidOperationException("Extension mutation requires its loading thread.");
    }
    private static void ThrowErrors(List<Exception>? errors)
    {
        if (errors is { Count: 1 }) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors is not null) throw new AggregateException("Extension lifecycle callbacks failed.", errors);
    }
}
