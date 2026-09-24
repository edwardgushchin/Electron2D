using System.Text.Json.Serialization;

namespace Electron2D;

/// <summary>Stores one versioned, typed project input-action definition.</summary>
/// <remarks>Use this as the value of a <see cref="ProjectSetting{T}"/> named <c>input/&lt;action&gt;</c>.
/// Version one is the only supported schema; unknown JSON members and unsupported versions fail before the live action map changes.</remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class InputActionSettings
{
    /// <summary>Creates an empty version-one input action.</summary>
    public InputActionSettings() { }

    /// <summary>Gets the serialized schema version.</summary>
    /// <value>One for the supported schema.</value>
    public int Version { get; init; } = 1;

    /// <summary>Gets the analog threshold in the closed range zero through one.</summary>
    /// <value>The requested deadzone; validated when the map loads.</value>
    public float Deadzone { get; init; } = InputMap.DefaultDeadzone;

    /// <summary>Gets the ordered typed bindings. The project setting stores a serialized snapshot.</summary>
    /// <value>At most 32 bindings; validated when the map loads.</value>
    public InputBindingSettings[] Bindings { get; init; } = [];

    internal void Validate()
    {
        if (Version != 1)
            throw new InvalidDataException($"Unsupported input-action schema version {Version}.");
        if (!float.IsFinite(Deadzone) || Deadzone < 0f || Deadzone > 1f)
            throw new InvalidDataException("An input-action deadzone must be finite and between zero and one.");
        if (Bindings is null || Bindings.Length > Input.MaxEventsPerAction)
            throw new InvalidDataException("An input action must have at most 32 bindings and a non-null binding list.");
    }
}

/// <summary>Identifies the concrete event represented by a serialized action binding.</summary>
public enum InputBindingKind
{
    /// <summary>Keyboard key and modifiers.</summary>
    Key,
    /// <summary>Mouse button or wheel direction and modifiers.</summary>
    MouseButton,
    /// <summary>Controller button.</summary>
    JoypadButton,
    /// <summary>Controller axis and signed direction.</summary>
    JoypadMotion,
    /// <summary>Named synthetic action.</summary>
    Action,
}

/// <summary>Stores one typed action-binding event without an engine-object value in project settings.</summary>
/// <remarks>Only the properties used to identify an action binding are persisted. Unknown properties and invalid
/// combinations fail during deserialization or map loading instead of being silently discarded.</remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class InputBindingSettings
{
    /// <summary>Creates a key binding with its default field values.</summary>
    public InputBindingSettings() { }

    /// <summary>Gets the concrete event kind.</summary>
    /// <value>The event discriminator.</value>
    public InputBindingKind Kind { get; init; }

    /// <summary>Gets the source device, or null for the event kind's default device.</summary>
    /// <value>A device identifier, including -1 for all controller devices.</value>
    public int? Device { get; init; }

    /// <summary>Gets the logical key code for a key binding.</summary>
    /// <value>A logical key or <see cref="Key.None"/>.</value>
    public Key Keycode { get; init; }

    /// <summary>Gets the physical key code for a key binding.</summary>
    /// <value>A physical key or <see cref="Key.None"/>.</value>
    public Key PhysicalKeycode { get; init; }

    /// <summary>Gets the localized label for a key binding.</summary>
    /// <value>A label key or <see cref="Key.None"/>.</value>
    public Key KeyLabel { get; init; }

    /// <summary>Gets the key location for a physical key binding.</summary>
    /// <value>A key location or <see cref="KeyLocation.Unspecified"/>.</value>
    /// <remarks>A location alone does not identify a key; at least one key code or label is required.</remarks>
    public KeyLocation Location { get; init; }

    /// <summary>Gets Shift, Alt, Control, Meta, or portable Command-or-Control modifier bits.</summary>
    /// <value>Modifier flags for a key or mouse binding.</value>
    public KeyModifierMask Modifiers { get; init; }

    /// <summary>Gets the mouse button or wheel direction.</summary>
    /// <value>A mouse button or <see cref="MouseButton.None"/>.</value>
    public MouseButton MouseButtonIndex { get; init; }

    /// <summary>Gets the controller button.</summary>
    /// <value>A controller button.</value>
    public JoyButton JoyButtonIndex { get; init; }

    /// <summary>Gets the controller axis.</summary>
    /// <value>A controller axis.</value>
    public JoyAxis JoyAxis { get; init; }

    /// <summary>Gets the signed controller-axis direction; a nonzero value is required.</summary>
    /// <value>The signed axis direction.</value>
    public float AxisValue { get; init; }

    /// <summary>Gets the name of a synthetic action binding.</summary>
    /// <value>A nonblank ordinal action name.</value>
    public string Action { get; init; } = string.Empty;

    internal InputEvent CreateEvent()
    {
        const KeyModifierMask allowedModifiers = KeyModifierMask.Shift | KeyModifierMask.Alt |
            KeyModifierMask.Control | KeyModifierMask.Meta | KeyModifierMask.CommandOrControl;
        if (!Enum.IsDefined(Kind) || Device < InputMap.AllDevices ||
            (Modifiers & ~allowedModifiers) != 0 ||
            ((Modifiers & KeyModifierMask.CommandOrControl) != 0 &&
                (Modifiers & (KeyModifierMask.Control | KeyModifierMask.Meta)) != 0))
            throw new InvalidDataException("An input binding has an invalid kind, device, or modifier combination.");

        var keyIdentity = Keycode != Key.None || PhysicalKeycode != Key.None || KeyLabel != Key.None;
        var keyFields = keyIdentity || Location != KeyLocation.Unspecified;
        var mouseFields = MouseButtonIndex != MouseButton.None;
        var joyButtonFields = JoyButtonIndex != JoyButton.A;
        var joyMotionFields = JoyAxis != JoyAxis.LeftX || AxisValue != 0f;
        var actionFields = !string.IsNullOrEmpty(Action);
        if ((Kind != InputBindingKind.Key && keyFields) ||
            (Kind != InputBindingKind.MouseButton && mouseFields) ||
            (Kind != InputBindingKind.JoypadButton && joyButtonFields) ||
            (Kind != InputBindingKind.JoypadMotion && joyMotionFields) ||
            (Kind != InputBindingKind.Action && actionFields) ||
            (Kind is not (InputBindingKind.Key or InputBindingKind.MouseButton) && Modifiers != 0))
            throw new InvalidDataException("An input binding contains properties for another event kind.");

        InputEvent result = Kind switch
        {
            InputBindingKind.Key => new InputEventKey(),
            InputBindingKind.MouseButton => new InputEventMouseButton(),
            InputBindingKind.JoypadButton => new InputEventJoypadButton(),
            InputBindingKind.JoypadMotion => new InputEventJoypadMotion(),
            InputBindingKind.Action => new InputEventAction(),
            _ => throw new InvalidDataException("Unknown input binding kind."),
        };
        try
        {
            if (Device is { } device) result.Device = device;
            switch (result)
            {
                case InputEventKey key:
                    if (!keyIdentity || !Enum.IsDefined(Location))
                        throw new InvalidDataException("A key binding needs a key identity and a valid location.");
                    key.Keycode = Keycode;
                    key.PhysicalKeycode = PhysicalKeycode;
                    key.KeyLabel = KeyLabel;
                    key.Location = Location;
                    ApplyModifiers(key);
                    break;
                case InputEventMouseButton mouse:
                    if (!mouseFields) throw new InvalidDataException("A mouse binding needs a button.");
                    mouse.ButtonIndex = MouseButtonIndex;
                    ApplyModifiers(mouse);
                    break;
                case InputEventJoypadButton button:
                    button.ButtonIndex = JoyButtonIndex;
                    break;
                case InputEventJoypadMotion motion:
                    if (AxisValue == 0f) throw new InvalidDataException("A controller-axis binding needs a signed direction.");
                    motion.Axis = JoyAxis;
                    motion.AxisValue = AxisValue;
                    break;
                case InputEventAction action:
                    InputEvent.ValidateActionName(Action, nameof(Action));
                    action.Action = Action;
                    break;
            }
            return result;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            result.Dispose();
            throw new InvalidDataException("The input binding contains invalid event data.", error);
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private void ApplyModifiers(InputEventWithModifiers @event)
    {
        @event.ShiftPressed = (Modifiers & KeyModifierMask.Shift) != 0;
        @event.AltPressed = (Modifiers & KeyModifierMask.Alt) != 0;
        if ((Modifiers & KeyModifierMask.CommandOrControl) != 0)
            @event.CommandOrControlAutoremap = true;
        else
        {
            @event.ControlPressed = (Modifiers & KeyModifierMask.Control) != 0;
            @event.MetaPressed = (Modifiers & KeyModifierMask.Meta) != 0;
        }
    }
}
