namespace Electron2D;

/// <summary>Adds activation and signalled-toggle hooks to a live Control Script callback set.</summary>
/// <typeparam name="TButton">Compatible button receiver.</typeparam>
/// <remarks>Install a fresh set in the owner's synchronous Bind. Each callback runs before the native CLR hook
/// and signal. Signal blocking preserves these direct callbacks. Committed state, reentrant interaction guards,
/// error aggregation and automatic retirement remain the ordinary button/binding contract.</remarks>
public class BaseButtonScriptCallbacks<TButton> : ControlScriptCallbacks<TButton> where TButton : BaseButton
{
    /// <summary>Creates an unused button callback set.</summary>
    public BaseButtonScriptCallbacks() { }
    /// <summary>Handles an activation before the native pressed hook and Pressed signal.</summary>
    public Action<TButton>? Pressed { get; init; }
    /// <summary>Handles a signalled toggle's committed value before the native hook and Toggled signal.</summary>
    /// <remarks>Silent assignment does not invoke this callback. A newer interaction or disposal skips later old phases.</remarks>
    public Action<TButton, bool>? Toggled { get; init; }

    internal override ObjectScriptBinding CreateBinding() => new ButtonScriptBinding();
    internal override void Populate(ObjectScriptBinding binding, TButton owner)
    {
        base.Populate(binding, owner);
        var target = (ButtonScriptBinding)binding;
        target.Pressed = Pressed is { } pressed ? () => pressed(owner) : null;
        target.Toggled = Toggled is { } toggled ? value => toggled(owner, value) : null;
    }
}

internal sealed class ButtonScriptBinding : ControlScriptBinding
{
    internal Action? Pressed;
    internal Action<bool>? Toggled;
}
