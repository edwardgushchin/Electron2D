namespace Electron2D;

public partial class Extension
{
    internal Func<Extension, ExtensionInitialization?>? Entry;
    private List<ExtensionInstanceState>? _replacementState;
    private int _replacementHighest;
    internal void DiscardReplacement() => _replacementState = null;
    internal ExtensionManager.LoadStatus Replace(out List<Exception>? observerErrors)
    {
        observerErrors = null;
        EnsureNoClassCreation();
        using var operation = ExtensionBindings.Protect(this, _replacementState);
        if (_replacementState is null)
        {
            List<ExtensionInstanceState> states;
            _retiring = true;
            try { states = ExtensionBindings.Capture(this, 0); }
            finally { _retiring = false; }
            _replacementHighest = _availableLevel;
            _replacementState = states;
        }
        List<Exception>? errors = null;
        var restored = false; var needsRestart = false;
        try
        {
            _retiring = true;
            if (_open) ExtensionManager.EmitUnloading(this, ref errors);
            ExtensionBindings.Clear(_replacementState, ref errors);
            Deinitialize(ref errors);
            Volatile.Write(ref _open, false); _initialize = _deinitialize = null;
            ExtensionManager.ThrowErrors(errors);
            var entry = Entry!(this);
            if (entry is null) return ExtensionManager.LoadStatus.Failed;
            Open(entry.Value);
            if ((int)entry.Value.MinimumLevel < Math.Min(_replacementHighest, (int)ExtensionInitializationLevel.Scene))
            { needsRestart = true; return ExtensionManager.LoadStatus.NeedsRestart; }
            InitializeTo((ExtensionInitializationLevel)_replacementHighest, (int)entry.Value.MinimumLevel);
            var surviving = ExtensionBindings.Rebind(_replacementState);
            using var restoration = ExtensionBindings.Protect(this, surviving);
            ExtensionBindings.Restore(surviving);
            _replacementState = null; restored = true;
            ExtensionBindings.Notify(surviving, ref errors);
            observerErrors = errors;
            return ExtensionManager.LoadStatus.Ok;
        }
        catch (Exception error) when (!restored)
        {
            if (errors is null) errors = [error];
            return ExtensionManager.LoadStatus.Failed;
        }
        finally
        {
            _retiring = false;
            if (!restored && !needsRestart)
            {
                ExtensionBindings.Clear(_replacementState!, ref errors);
                Deinitialize(ref errors);
                Volatile.Write(ref _open, false); _initialize = _deinitialize = null;
                ExtensionManager.ThrowErrors(errors);
            }
        }
    }
}

public sealed partial class ExtensionManager
{
    private EventPublisher<Action> _reloaded;
    private bool _reloadingBatch;
    /// <summary>Occurs once after an explicit changed-address batch has attempted its loaded modules.</summary>
    /// <remarks>Individual successful replacements send ExtensionLoaded; this batch signal follows every attempt, including failures.</remarks>
    public static event Action? ExtensionsReloaded { add => Service._reloaded.Add(value); remove => Service._reloaded.Remove(value); }

    /// <summary>Reopens the retained function entry and replaces its registered class bindings in place.</summary>
    /// <param name="path">The exact loaded module address.</param>
    /// <returns>Ok, NotLoaded, Failed for a declined entry, or NeedsRestart when the reopened entry requires an earlier initialization level.</returns>
    /// <remarks>Stored state is captured before retirement. All surviving bindings then all compatible stored values are
    /// restored before instance notifications and ExtensionLoaded. Removed classes remain cleared. Capture failure leaves
    /// the old module untouched; later failure leaves a closed registered resource with state retained for explicit retry.
    /// A NeedsRestart entry stays open without initialization; explicit retry retains the captured state.
    /// Loaded-observer/notification errors propagate after a restored replacement commits. This invokes the original typed entry;
    /// editing source files alone does not replace CLR code or trigger this workflow.</remarks>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="InvalidOperationException">The caller is foreign or the module is already transitioning/constructing an instance.</exception>
    /// <exception cref="Exception">Capture, callbacks, restoration or observers fail.</exception>
    public static LoadStatus ReloadExtension(string path)
    {
        ArgumentNullException.ThrowIfNull(path); var owner = Service; owner.EnsureOwner(); Extension extension;
        lock (owner._gate)
        {
            if (owner._pending.Contains(path)) throw new InvalidOperationException("The extension address is already transitioning.");
            if (!owner._modules.TryGetValue(path, out extension!)) return LoadStatus.NotLoaded;
            if (!owner._pending.Add(path)) throw new InvalidOperationException("The extension address is already transitioning.");
        }
        try
        {
            var result = extension.Replace(out var errors);
            if (result == LoadStatus.Ok)
                EmitLoaded(extension, ref errors);
            ThrowErrors(errors);
            return result;
        }
        finally { lock (owner._gate) owner._pending.Remove(path); }
    }

    /// <summary>Attempts an explicit host-supplied set of changed module addresses and sends one batch-completion signal.</summary>
    /// <param name="paths">Changed addresses; duplicates are attempted once in first-occurrence order.</param>
    /// <returns>An independent ordinal result map, including NotLoaded for unknown addresses.</returns>
    /// <remarks>All addresses are validated before attempts. Each replacement commits independently. Callback failures
    /// do not skip later addresses; original errors propagate after the batch signal. A nonempty set of loaded addresses
    /// emits ExtensionsReloaded even if a replacement fails or needs a restart. Empty/unknown-only sets emit nothing.
    /// The host supplies change detection; this operation does not install a file watcher or reload ordinary CLR scripts.</remarks>
    /// <exception cref="ArgumentNullException">The sequence or an address is null.</exception>
    /// <exception cref="InvalidOperationException">The caller is foreign or a batch is already active.</exception>
    /// <exception cref="Exception">Enumeration, replacements or observers fail.</exception>
    public static Dictionary<string, LoadStatus> ReloadExtensions(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths); var owner = Service; owner.EnsureOwner();
        if (owner._reloadingBatch) throw new InvalidOperationException("An extension reload batch is already active.");
        var addresses = paths.Distinct(StringComparer.Ordinal).ToArray();
        foreach (var address in addresses) ArgumentNullException.ThrowIfNull(address);
        Dictionary<string, LoadStatus> results = new(StringComparer.Ordinal);
        List<Exception>? errors = null; var attempted = false;
        owner._reloadingBatch = true;
        try
        {
            foreach (var address in addresses)
            {
                if (!IsExtensionLoaded(address)) { results.Add(address, LoadStatus.NotLoaded); continue; }
                attempted = true;
                try { results.Add(address, ReloadExtension(address)); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            if (attempted)
                try { owner.BeginEventEmission(owner._reloaded.Handlers)?.Invoke(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            ThrowErrors(errors); return results;
        }
        finally { owner._reloadingBatch = false; }
    }
}
