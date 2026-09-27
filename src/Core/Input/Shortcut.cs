namespace Electron2D;

/// <summary>Groups ordered input-event alternatives for one GUI shortcut.</summary>
/// <remarks>Event references are borrowed. The array is copied on assignment and retrieval; editing an event changes
/// subsequent matching directly, without emitting a shortcut change. Null slots are allowed. Assignment emits Changed
/// even for an equal list. Matching captures an immutable list snapshot and calls event hooks outside the resource lock.
/// Callers must not concurrently mutate or dispose an event while its comparison hook runs.</remarks>
public class Shortcut : Resource
{
    private readonly object _gate = new();
    private InputEvent?[] _events = [];
    private static readonly PropertyDescriptor EventsProperty = new PropertyDescriptor<Shortcut, InputEvent?[]>(
        nameof(Events), resource => resource.Events, (resource, value) => resource.Events = value, _ => []);

    /// <summary>Creates an empty shortcut with no input alternatives.</summary>
    public Shortcut() { }

    /// <summary>Gets or replaces the ordered borrowed input alternatives.</summary>
    /// <value>An empty array initially. Null elements represent empty alternatives.</value>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">An element is an InputEventShortcut, which cannot be a shortcut binding.</exception>
    /// <exception cref="ObjectDisposedException">This shortcut or an assigned event is disposed.</exception>
    /// <exception cref="Exception">A change observer throws after the new list is committed.</exception>
    public InputEvent?[] Events
    {
        get { lock (_gate) { ThrowIfDisposed(); return (InputEvent?[])_events.Clone(); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var copy = (InputEvent?[])value.Clone();
            foreach (var input in copy)
            {
                if (input is InputEventShortcut) throw new ArgumentException("Shortcut bindings cannot contain shortcut events.", nameof(value));
                input?.EnsureUsable();
            }
            lock (_gate) { ThrowIfDisposed(); _events = copy; }
            EmitChanged();
        }
    }

    /// <summary>Tests whether any alternative contains a live event.</summary>
    /// <returns>True for any live event, including an event with no configured key or action.</returns>
    /// <exception cref="ObjectDisposedException">The shortcut is disposed.</exception>
    public bool HasValidEvent()
    {
        foreach (var input in GetEventsSnapshot()) if (input is { IsDisposed: false }) return true;
        return false;
    }

    /// <summary>Matches a direct shortcut identity or the first matching input alternative.</summary>
    /// <param name="event">The live borrowed event to compare using exact event matching.</param>
    /// <returns>True for a shortcut event referring to this resource or a matching alternative.</returns>
    /// <exception cref="ArgumentNullException">The event is null.</exception>
    /// <exception cref="ObjectDisposedException">This shortcut or the compared event is disposed.</exception>
    /// <exception cref="Exception">An event comparison hook throws.</exception>
    public bool MatchesEvent(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event); @event.EnsureUsable();
        var events = GetEventsSnapshot();
        if (@event is InputEventShortcut shortcutEvent && ReferenceEquals(shortcutEvent.Shortcut, this)) return true;
        foreach (var input in events) if (input is { IsDisposed: false } && input.IsMatch(@event)) return true;
        return false;
    }

    /// <summary>Returns the description of the first live alternative.</summary>
    /// <returns>The first event's text, including an empty description, or the literal None when no event is live.</returns>
    /// <exception cref="ObjectDisposedException">The shortcut is disposed.</exception>
    /// <exception cref="Exception">The selected event's description hook throws.</exception>
    public string GetAsText()
    {
        foreach (var input in GetEventsSnapshot()) if (input is { IsDisposed: false }) return input.AsText();
        return "None";
    }

    private InputEvent?[] GetEventsSnapshot() { lock (_gate) { ThrowIfDisposed(); return _events; } }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Shortcut();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var events = Events;
        if (deep) for (var i = 0; i < events.Length; i++) events[i] = (InputEvent?)duplicateSubresource(events[i]);
        ((Shortcut)target).Events = events;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return EventsProperty;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _events = [];
        base.Dispose(disposing);
    }
}
