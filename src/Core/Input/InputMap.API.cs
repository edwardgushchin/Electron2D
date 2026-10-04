namespace Electron2D;

public sealed partial class InputMap
{
    /// <summary>Occurs after a project action map has replaced the live bindings.</summary>
    /// <remarks>Raised synchronously outside the map lock. A throwing listener observes the committed map.</remarks>
    public static event Action? ProjectSettingsLoaded
    {
        add => Service.ProjectSettingsLoadedCore += value;
        remove => Service.ProjectSettingsLoadedCore -= value;
    }

    /// <summary>Replaces all actions with the typed <c>input/*</c> records in the process-wide project settings.</summary>
    /// <remarks>Active project feature overrides are applied. Validation finishes before the live map changes.
    /// Loaded bindings are borrowed by the map like manually
    /// added bindings; references obtained from <see cref="ActionGetEvents"/> remain usable after a later reload.
    /// This operation allocates and belongs in project setup, outside input dispatch. A loaded listener exception
    /// propagates after the new map has committed.</remarks>
    /// <exception cref="InvalidDataException">An input setting has the wrong type, schema version, or binding data.</exception>
    /// <exception cref="ObjectDisposedException">The project settings registry is disposed.</exception>
    public static void LoadFromProjectSettings() => Service.LoadFromProjectSettingsCore();

    /// <summary>Gets whether an action exists.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <returns><see langword="true"/> when registered.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static bool HasAction(string action) => Service.HasActionCore(action);

    /// <summary>Gets action names in registration order.</summary>
    /// <returns>An immutable snapshot.</returns>
    public static IReadOnlyList<string> GetActions() => Service.GetActionsCore();

    /// <summary>Adds an empty action.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <param name="deadzone">The analog threshold from zero through one.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="deadzone"/> is outside zero through one, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The action already exists.</exception>
    public static void AddAction(string action, float deadzone = DefaultDeadzone) => Service.AddActionCore(action, deadzone);

    /// <summary>Removes an action and all of its bindings.</summary>
    /// <param name="action">The registered action name.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static void EraseAction(string action) => Service.EraseActionCore(action);

    /// <summary>Gets an action's analog deadzone.</summary>
    /// <param name="action">The registered action name.</param>
    /// <returns>A value from zero through one.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static float ActionGetDeadzone(string action) => Service.ActionGetDeadzoneCore(action);

    /// <summary>Changes an action's analog deadzone.</summary>
    /// <param name="action">The registered action name.</param>
    /// <param name="deadzone">The new threshold from zero through one.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="deadzone"/> is outside zero through one, NaN, or infinite.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static void ActionSetDeadzone(string action, float deadzone) => Service.ActionSetDeadzoneCore(action, deadzone);

    /// <summary>Adds an event binding to an action.</summary>
    /// <param name="action">The registered action name.</param>
    /// <param name="event">A live action-compatible event describing the binding.</param>
    /// <remarks>An equal exact action binding is ignored. At most 32 bindings may belong to one action.
    /// Binding lookup uses action matching; a public <see cref="InputEvent.IsMatch"/> result may be broader for synthetic action events.</remarks>
    /// <exception cref="ArgumentException"><paramref name="action"/> is invalid or <paramref name="event"/> is not action-compatible.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The action already has 32 distinct bindings.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    public static void ActionAddEvent(string action, InputEvent @event) => Service.ActionAddEventCore(action, @event);

    /// <summary>Gets whether an action contains an exact event binding.</summary>
    /// <param name="action">The registered action name.</param>
    /// <param name="event">The binding configuration to find.</param>
    /// <returns><see langword="true"/> when an exact action binding exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    public static bool ActionHasEvent(string action, InputEvent @event) => Service.ActionHasEventCore(action, @event);

    /// <summary>Removes the first exact matching binding from an action, if present.</summary>
    /// <param name="action">The registered action name.</param>
    /// <param name="event">The binding configuration to remove.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    /// <remarks>An absent binding leaves the action unchanged. Removing a binding invalidates its cached action state.</remarks>
    public static void ActionEraseEvent(string action, InputEvent @event) => Service.ActionEraseEventCore(action, @event);

    /// <summary>Removes every binding from an action.</summary>
    /// <param name="action">The registered action name.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static void ActionEraseEvents(string action) => Service.ActionEraseEventsCore(action);

    /// <summary>Gets an action's bindings in registration order.</summary>
    /// <param name="action">The registered action name.</param>
    /// <returns>An immutable snapshot containing the original binding references.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static IReadOnlyList<InputEvent> ActionGetEvents(string action) => Service.ActionGetEventsCore(action);

    /// <summary>Tests whether an event belongs to an action.</summary>
    /// <param name="event">The live event to test.</param>
    /// <param name="action">The registered action name.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns><see langword="true"/> when the event matches the action.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> or a tested binding is disposing or disposed.</exception>
    public static bool EventIsAction(InputEvent @event, string action, bool exactMatch = false) => Service.EventIsActionCore(@event, action, exactMatch);

    /// <summary>Gets a human-readable disjunction of an action's concrete bindings.</summary>
    /// <param name="action">The registered action name.</param>
    /// <returns>The localized no-input message, or concrete binding descriptions joined by a localized separator.</returns>
    /// <remarks>Synthetic <see cref="InputEventAction"/> bindings are indirection and are omitted. Translation uses this map's inherited translation domain and enabled state; each concrete event supplies its own description.</remarks>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException">A binding is disposing or disposed.</exception>
    public static string GetActionDescription(string action) => Service.GetActionDescriptionCore(action);

}
