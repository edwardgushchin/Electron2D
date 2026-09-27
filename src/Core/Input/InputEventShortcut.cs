namespace Electron2D;

/// <summary>Requests activation of a particular shortcut resource without simulating its physical bindings.</summary>
/// <remarks>The event is pressed initially and is not an action-binding event. It borrows its shortcut, can be delivered
/// through the ordinary scene shortcut stage, and preserves that reference in shallow event copies.</remarks>
public sealed class InputEventShortcut : InputEvent
{
    private Shortcut? _shortcut;
    private static readonly PropertyDescriptor ShortcutProperty = new PropertyDescriptor<InputEventShortcut, Shortcut?>(
        nameof(Shortcut), input => input.Shortcut, (input, value) => input.Shortcut = value, _ => null, stored: true);

    /// <summary>Creates a pressed shortcut event with no assigned resource.</summary>
    public InputEventShortcut() => PressedState = true;

    /// <summary>Gets or sets the borrowed shortcut to activate.</summary>
    /// <value>Null initially. Every assignment notifies, including equal identities.</value>
    /// <exception cref="ObjectDisposedException">This event or an assigned shortcut is disposed.</exception>
    /// <exception cref="Exception">A change observer throws after assignment.</exception>
    public Shortcut? Shortcut
    {
        get { ThrowIfDisposed(); return _shortcut; }
        set
        {
            ThrowIfDisposed(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value);
            _shortcut = value; EmitInputChanged();
        }
    }

    /// <summary>Returns the translated shortcut-event description.</summary>
    /// <returns>Input Event with Shortcut followed by the shortcut description, or None for a missing or disposed shortcut.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposed.</exception>
    public override string AsText()
    {
        ThrowIfDisposed();
        const string template = "Input Event with Shortcut=%s";
        return _shortcut is { IsDisposed: false } ? FormatTextTemplate(Tr(template), template, _shortcut.GetAsText()) : "None";
    }
    /// <summary>Returns the diagnostic shortcut-event representation.</summary>
    /// <returns>The event type and shortcut description, or None when no live shortcut is assigned.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposed.</exception>
    public override string ToString()
    {
        ThrowIfDisposed(); return _shortcut is { IsDisposed: false } ? "InputEventShortcut: shortcut=" + _shortcut.GetAsText() : "None";
    }
    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventShortcut();
    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target); ((InputEventShortcut)target)._shortcut = _shortcut;
    }
    internal void CopyShortcutResourceTo(InputEventShortcut target, Func<Resource?, Resource?> duplicate) =>
        target._shortcut = (Shortcut?)duplicate(_shortcut);
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return ShortcutProperty;
    }
}
