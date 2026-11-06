namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Identifies an absent status indicator.</summary>
    public const int InvalidIndicatorId = -1;
    /// <summary>Creates an operating-system notification-area icon for this display session.</summary>
    /// <param name="icon">Borrowed readable texture, or null for no image.</param>
    /// <param name="tooltip">Nonnull tooltip without NUL; Windows keeps at most 127 UTF16 units.</param>
    /// <param name="callback">Receives the pressed button and desktop position on the next owner event pump.</param>
    /// <returns>A positive process-unique identity.</returns>
    /// <remarks>Resource changes refresh on the owner pump. Native right-click menus take precedence over the callback.
    /// Unsupported drivers reject creation. Callbacks and icon sources are borrowed, not disposed.</remarks>
    /// <exception cref="NotSupportedException">The active display has no notification host or the texture has no readable image.</exception>
    /// <exception cref="IOException">Native creation failed.</exception>
    /// <exception cref="ArgumentException">Tooltip text contains NUL.</exception>
    /// <exception cref="ArgumentNullException">Tooltip text is null.</exception>
    /// <exception cref="InvalidOperationException">The session is absent, preparing, retiring, or accessed off its owner thread.</exception>
    public static int CreateStatusIndicator(Texture? icon, string tooltip, Action<MouseButton, Vector2i>? callback = null) => RequireService().CreateStatusIndicatorCore(icon, tooltip, callback);
    /// <summary>Replaces the native image transactionally, retaining the old image on failure.</summary>
    /// <param name="id">A live indicator in this session.</param><param name="icon">Borrowed readable texture, or null to clear the image.</param>
    /// <exception cref="ArgumentException">The identity is absent, retired or from another session.</exception>
    /// <exception cref="IOException">The native replacement failed; the previous image remains owned.</exception>
    /// <exception cref="NotSupportedException">The texture has no readable image or exceeds the native pixel budget.</exception>
    /// <exception cref="ObjectDisposedException">The supplied texture is disposed.</exception>
    public static void StatusIndicatorSetIcon(int id, Texture? icon) => RequireService().SetStatusIcon(id, icon);
    /// <summary>Updates native tooltip text.</summary>
    /// <param name="id">A live indicator.</param><param name="tooltip">Nonnull text without NUL; Windows truncates to 127 UTF16 units without splitting a surrogate pair.</param>
    /// <exception cref="ArgumentException">The identity is invalid or text contains NUL.</exception>
    /// <exception cref="ArgumentNullException">Tooltip text is null.</exception>
    /// <exception cref="IOException">Native modification failed; previous text remains retained.</exception>
    public static void StatusIndicatorSetTooltip(int id, string tooltip) => RequireService().SetStatusTooltip(id, tooltip);
    /// <summary>Binds a borrowed root native menu to right-button presses.</summary>
    /// <param name="id">A live indicator.</param><param name="menuRID">A live unattached root in this session, or zero to detach.</param>
    /// <remarks>A bound root cannot become a submenu. Freeing the menu clears its bindings without deleting indicators.</remarks>
    /// <exception cref="ArgumentException">An indicator or nonzero menu identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The menu is attached as a submenu or native preparation/tracking forbids rebinding.</exception>
    public static void StatusIndicatorSetMenu(int id, RID menuRID) => RequireService().SetStatusMenu(id, menuRID);
    /// <summary>Replaces the typed activation callback for subsequently accepted native presses.</summary>
    /// <param name="id">A live indicator.</param><param name="callback">Borrowed button/desktop-position callback, or null.</param>
    /// <exception cref="ArgumentException">The identity is absent, retired or from another session.</exception>
    public static void StatusIndicatorSetCallback(int id, Action<MouseButton, Vector2i>? callback) { var owner = RequireService(); owner.StatusWritable(); owner.StatusAt(id).Callback = callback; }
    /// <summary>Gets the actual notification icon's desktop rectangle.</summary>
    /// <param name="id">A live indicator.</param><returns>Native bounds, or zero when the Shell cannot locate the icon.</returns>
    /// <remarks>Hidden icons may return their overflow activation rectangle. Access requires the opening thread.</remarks>
    /// <exception cref="ArgumentException">The identity is absent, retired or from another session.</exception>
    public static Rect2 StatusIndicatorGetRect(int id) { var owner = RequireService(); owner.EnsureOwner(); return owner.GetStatusRect(owner.StatusAt(id)); }
    /// <summary>Retires an indicator, its native icon, resource watchers and menu binding.</summary>
    /// <param name="id">A live indicator.</param>
    /// <remarks>Queued presses become ineligible. Native removal failure is reported after managed retirement.</remarks>
    /// <exception cref="ArgumentException">The identity is absent, retired or from another session.</exception>
    /// <exception cref="IOException">The Shell did not confirm removal; managed ownership has still been retired.</exception>
    public static void DeleteStatusIndicator(int id) { var owner = RequireService(); owner.StatusWritable(); owner.DeleteStatus(owner.StatusAt(id)); }

    private static int _nextStatusID;
    private readonly Dictionary<int, StatusIndicator> _statusIndicators = [];
    private readonly Queue<StatusPress> _statusPresses = [];
    private bool _preparingStatus, _dropStatusInput, _recreateStatus;
    private Exception? _statusNativeFailure;
    private sealed class StatusIndicator(int id)
    {
        internal readonly int ID = id;
        internal bool Alive = true, Registered;
        internal Texture? Source;
        internal Action<Resource>? Changed;
        internal Action<ElectronObject>? Disposed;
        internal int Dirty;
        internal nint Icon;
        internal string Tooltip = "";
        internal Action<MouseButton, Vector2i>? Callback;
        internal NativeMenu? MenuOwner;
        internal RID Menu;
    }
    private readonly record struct StatusPress(StatusIndicator Indicator, MouseButton Button, Vector2i Position, Rect2 Anchor, Action<MouseButton, Vector2i>? Callback, NativeMenu? MenuOwner, RID Menu);
    private void StatusWritable() { EnsureOwner(); if (_retiringNativeWindow || _preparingStatus) throw new InvalidOperationException("The status-indicator owner is preparing or retiring."); }
    private StatusIndicator StatusAt(int id) => _statusIndicators.TryGetValue(id, out var value) ? value : throw new ArgumentException("The indicator is absent, retired or belongs to another display session.", nameof(id));
    private static void StatusText(string text) { ArgumentNullException.ThrowIfNull(text); if (text.Contains('\0')) throw new ArgumentException("Native tooltip text cannot contain NUL.", nameof(text)); }
    private int CreateStatusIndicatorCore(Texture? icon, string tooltip, Action<MouseButton, Vector2i>? callback)
    {
        StatusWritable(); StatusText(tooltip); if (!HasStatusIndicatorHost()) throw new NotSupportedException("This display has no active status-indicator host.");
        if (icon is { IsDisposed: true }) throw new ObjectDisposedException(nameof(icon));
        var id = checked(_nextStatusID + 1); _nextStatusID = id;
        var value = new StatusIndicator(id) { Tooltip = tooltip, Callback = callback };
        _statusIndicators.EnsureCapacity(_statusIndicators.Count + 1);
        try
        {
            WatchStatusIcon(value, icon); value.Icon = PrepareStatusIcon(icon); EnsureStatusHook();
            if (!NotifyStatus(value, 0, 7)) throw StatusFailure("create a status indicator");
            value.Registered = true;
            if (!SetStatusVersion(value)) throw StatusFailure("select the status-indicator notification protocol");
            _statusIndicators.Add(id, value); return id;
        }
        catch
        {
            if (value.Registered) NotifyStatus(value, 2, 0);
            RetireStatus(value); throw;
        }
    }
    private void SetStatusIcon(int id, Texture? source)
    {
        StatusWritable(); var value = StatusAt(id); if (source is { IsDisposed: true }) throw new ObjectDisposedException(nameof(source));
        var replacement = PrepareStatusIcon(source); var previous = value.Icon;
        Action<Resource>? changed = null; Action<ElectronObject>? disposed = null;
        try
        {
            SubscribeStatus(value, source, out changed, out disposed);
            value.Icon = replacement;
            if (!NotifyStatus(value, 1, 2)) { value.Icon = previous; throw StatusFailure("replace a status-indicator icon"); }
        }
        catch { value.Icon = previous; if (source is not null) { source.Changed -= changed; source.Disposed -= disposed; } if (replacement != 0) StatusWin.DestroyIcon(replacement); throw; }
        UnwatchStatus(value); value.Source = source; value.Changed = changed; value.Disposed = disposed;
        if (previous != 0) StatusWin.DestroyIcon(previous);
    }
    private void SetStatusTooltip(int id, string tooltip)
    {
        StatusWritable(); StatusText(tooltip); var value = StatusAt(id); var previous = value.Tooltip; value.Tooltip = tooltip;
        try { if (!NotifyStatus(value, 1, 4)) throw StatusFailure("update a status-indicator tooltip"); }
        catch { value.Tooltip = previous; throw; }
    }
    private void SetStatusMenu(int id, RID menu)
    {
        StatusWritable(); var value = StatusAt(id); if (value.Menu == menu) return;
        var owner = menu.IsValid() ? GetNativeMenus() : null; owner?.AcquireStatusMenu(menu);
        value.MenuOwner?.ReleaseStatusMenu(value.Menu); value.MenuOwner = owner; value.Menu = menu;
    }
    internal void NativeStatusMenuRetired(RID menu)
    {
        foreach (var value in _statusIndicators.Values) if (value.Menu == menu) { value.Menu = default; value.MenuOwner = null; }
    }
    private static void SubscribeStatus(StatusIndicator value, Texture? source, out Action<Resource>? changed, out Action<ElectronObject>? disposed)
    {
        changed = null; disposed = null; if (source is null) return;
        changed = _ => Interlocked.Exchange(ref value.Dirty, 1); disposed = _ => Interlocked.Exchange(ref value.Dirty, 1);
        source.Changed += changed;
        try { source.Disposed += disposed; } catch { source.Changed -= changed; throw; }
    }
    private static void WatchStatusIcon(StatusIndicator value, Texture? source)
    { SubscribeStatus(value, source, out var changed, out var disposed); value.Source = source; value.Changed = changed; value.Disposed = disposed; }
    private static void UnwatchStatus(StatusIndicator value)
    { if (value.Source is { } source) { source.Changed -= value.Changed; source.Disposed -= value.Disposed; } value.Changed = null; value.Disposed = null; }
    private static void RetireStatus(StatusIndicator value)
    {
        value.Alive = false; UnwatchStatus(value); value.Source = null; value.Callback = null;
        value.MenuOwner?.ReleaseStatusMenu(value.Menu); value.MenuOwner = null; value.Menu = default;
        if (value.Icon != 0) StatusWin.DestroyIcon(value.Icon); value.Icon = 0;
    }
    private void DeleteStatus(StatusIndicator value)
    {
        var removed = NotifyStatus(value, 2, 0); _statusIndicators.Remove(value.ID); RetireStatus(value);
        if (!removed) throw StatusFailure("remove a status indicator (managed ownership was retired)");
    }
    private nint PrepareStatusIcon(Texture? source)
    {
        if (source is null || source.IsDisposed) return 0;
        _preparingStatus = true; Interlocked.Increment(ref _pendingOwnedNativeDialogs); Interlocked.Increment(ref _pendingNativeDialogs);
        try { return CopyStatusIcon(source); }
        finally { Interlocked.Decrement(ref _pendingNativeDialogs); Interlocked.Decrement(ref _pendingOwnedNativeDialogs); _preparingStatus = false; }
    }
    private void RefreshStatusIndicators(ref List<Exception>? failures)
    {
        if (_statusNativeFailure is { } native) { _statusNativeFailure = null; (failures ??= []).Add(native); }
        foreach (var value in _statusIndicators.Values)
        {
            if (_recreateStatus) value.Registered = false;
            var dirty = Interlocked.Exchange(ref value.Dirty, 0) != 0;
            if (!dirty && value.Registered) continue;
            var previous = value.Icon; nint replacement = previous;
            try
            {
                if (dirty) replacement = PrepareStatusIcon(value.Source);
                value.Icon = replacement;
                var restoring = !value.Registered;
                if (restoring)
                {
                    if (!NotifyStatus(value, 0, 7) && !NotifyStatus(value, 1, 7)) throw StatusFailure("restore a status indicator");
                    if (!SetStatusVersion(value)) throw StatusFailure("restore status-indicator notifications");
                    value.Registered = true;
                }
                else if (!NotifyStatus(value, 1, 2)) throw StatusFailure("refresh a changed status-indicator icon");
                if (dirty && previous != 0) StatusWin.DestroyIcon(previous);
            }
            catch (Exception error)
            {
                value.Icon = previous;
                if (dirty) { if (replacement != previous && replacement != 0) StatusWin.DestroyIcon(replacement); Interlocked.Exchange(ref value.Dirty, 1); }
                (failures ??= []).Add(error);
            }
        }
        _recreateStatus = false;
    }
    private void DrainStatusPresses(bool dropInput, ref List<Exception>? failures)
    {
        if (_statusNativeFailure is { } native) { _statusNativeFailure = null; (failures ??= []).Add(native); }
        var count = _statusPresses.Count;
        for (var i = 0; i < count && _statusPresses.Count != 0; i++)
        {
            var press = _statusPresses.Dequeue(); if (dropInput || !press.Indicator.Alive) continue;
            try
            {
                if (press.Button == MouseButton.Right && press.Menu.IsValid() && press.MenuOwner is { } menu)
                {
                    if (menu.HasStatusMenu(press.Menu))
                    {
                        if (press.Anchor.HasArea()) menu.PresentStatusMenu(press.Menu, (Vector2i)press.Anchor.GetCenter(), this);
                        continue;
                    }
                }
                press.Callback?.Invoke(press.Button, press.Position);
            }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
    }
    private void RetireStatusIndicators()
    {
        List<Exception>? failures = null;
        foreach (var value in _statusIndicators.Values)
        {
            if (!NotifyStatus(value, 2, 0)) (failures ??= []).Add(StatusFailure("retire a status indicator"));
            RetireStatus(value);
        }
        _statusIndicators.Clear(); _statusPresses.Clear(); _statusNativeFailure = null;
        ReleaseStatusHook();
        if (failures is not null) throw new AggregateException("Status-indicator retirement failed.", failures);
    }
    private static IOException StatusFailure(string operation) => new("The Windows notification area could not " + operation + ".");
}
