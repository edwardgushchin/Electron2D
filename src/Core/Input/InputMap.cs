namespace Electron2D;

/// <summary>Owns the process-wide mapping from named game actions to typed input-event bindings.</summary>
/// <remarks>Public static operations delegate to the retained service object; state, identity and ownership remain object-scoped.
///
/// Collection operations are lock-serialized and return snapshots. Binding resources remain caller-owned and mutable;
/// callers must not mutate or dispose a binding concurrently with matching. Action names use ordinal comparison.
/// </remarks>
public sealed partial class InputMap : ElectronObject
{
    /// <summary>Allows a controller binding to match the same control on every device.</summary>
    internal const int AllDevices = -1;

    /// <summary>Defines the default analog deadzone for newly registered actions.</summary>
    internal const float DefaultDeadzone = 0.2f;

    private static readonly InputMap SharedInstance = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, ActionDefinition> _actions = new(StringComparer.Ordinal);
    private readonly List<string> _actionOrder = [];
    private readonly Dictionary<InputEvent, HashSet<string>> _bindingActions =
        new(ReferenceEqualityComparer.Instance);

    private InputMap()
    {
        foreach (var (name, definition) in BuildProjectActions(ProjectSettings.Service))
        {
            _actions.Add(name, definition);
            _actionOrder.Add(name);
            foreach (var binding in definition.Events)
                AttachBindingUnderLock(name, binding);
        }
    }

    internal static InputMap Service => SharedInstance;

    internal event Action? ProjectSettingsLoadedCore;

    internal void LoadFromProjectSettingsCore() => LoadFromProjectSettings(ProjectSettings.Service);

    internal void LoadFromProjectSettings(ProjectSettingsRegistry settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var candidates = BuildProjectActions(settings);

        string[] previousActions;
        lock (_gate)
        {
            previousActions = _actionOrder.ToArray();
            foreach (var action in _actionOrder)
                foreach (var binding in _actions[action].Events)
                    DetachBindingUnderLock(action, binding);

            _actions.Clear();
            _actionOrder.Clear();
            foreach (var (name, definition) in candidates)
            {
                _actions.Add(name, definition);
                _actionOrder.Add(name);
                foreach (var binding in definition.Events)
                    AttachBindingUnderLock(name, binding);
            }
        }

        foreach (var action in previousActions.Concat(candidates.Select(static item => item.Name)).Distinct(StringComparer.Ordinal))
            Input.Service.OnActionMapChanged(action, removed: false);
        ProjectSettingsLoadedCore?.Invoke();
    }

    private static List<(string Name, ActionDefinition Definition)> BuildProjectActions(ProjectSettingsRegistry settings)
    {
        var records = settings.GetRegisteredSettingsInGroup<InputActionSettings>("input/");
        var candidates = new List<(string Name, ActionDefinition Definition)>(records.Length);
        try
        {
            foreach (var (fullName, data) in records)
            {
                var name = fullName["input/".Length..];
                try { InputEvent.ValidateActionName(name, nameof(settings)); }
                catch (ArgumentException error)
                {
                    throw new InvalidDataException($"Project input action '{fullName}' has an invalid name.", error);
                }
                data.Validate();
                var definition = new ActionDefinition(data.Deadzone);
                candidates.Add((name, definition));
                foreach (var binding in data.Bindings)
                {
                    if (binding is null)
                        throw new InvalidDataException($"Input action '{name}' contains a null binding.");
                    var @event = binding.CreateEvent();
                    if (FindEventIndex(definition, @event, exactMatch: true) >= 0)
                        @event.Dispose();
                    else
                        definition.Events.Add(@event);
                }
            }
            return candidates;
        }
        catch
        {
            foreach (var (_, definition) in candidates)
                foreach (var @event in definition.Events)
                    @event.Dispose();
            throw;
        }
    }

    internal bool HasActionCore(string action)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        lock (_gate)
            return _actions.ContainsKey(action);
    }

    internal IReadOnlyList<string> GetActionsCore()
    {
        lock (_gate)
            return Array.AsReadOnly(_actionOrder.ToArray());
    }

    internal void AddActionCore(string action, float deadzone = DefaultDeadzone)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        ValidateDeadzone(deadzone, nameof(deadzone));

        lock (_gate)
        {
            if (!_actions.TryAdd(action, new ActionDefinition(deadzone)))
                throw new InvalidOperationException($"Input action '{action}' already exists.");
            _actionOrder.Add(action);
        }

        Input.Service.OnActionMapChanged(action, removed: false);
    }

    internal void EraseActionCore(string action)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        lock (_gate)
        {
            if (!_actions.Remove(action, out var definition))
                throw new KeyNotFoundException($"Input action '{action}' is not registered.");
            foreach (var binding in definition.Events)
                DetachBindingUnderLock(action, binding);
            _actionOrder.Remove(action);
        }

        Input.Service.OnActionMapChanged(action, removed: true);
    }

    internal float ActionGetDeadzoneCore(string action)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        lock (_gate)
            return GetActionUnderLock(action).Deadzone;
    }

    internal void ActionSetDeadzoneCore(string action, float deadzone)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        ValidateDeadzone(deadzone, nameof(deadzone));
        lock (_gate)
            GetActionUnderLock(action).Deadzone = deadzone;

        Input.Service.OnActionMapChanged(action, removed: false);
    }

    internal void ActionAddEventCore(string action, InputEvent @event)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        if (!@event.IsActionType())
            throw new ArgumentException("Only action-compatible input events can be bound to an action.", nameof(@event));

        lock (_gate)
        {
            var definition = GetActionUnderLock(action);
            if (FindEventIndex(definition, @event, exactMatch: true) >= 0)
                return;
            if (definition.Events.Count >= Input.MaxEventsPerAction)
                throw new InvalidOperationException("An input action cannot contain more than 32 bindings.");
            definition.Events.Add(@event);
            AttachBindingUnderLock(action, @event);
        }

        Input.Service.OnActionMapChanged(action, removed: false);
    }

    internal bool ActionHasEventCore(string action, InputEvent @event)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        lock (_gate)
            return FindEventIndex(GetActionUnderLock(action), @event, exactMatch: true) >= 0;
    }

    internal void ActionEraseEventCore(string action, InputEvent @event)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        var removed = false;
        lock (_gate)
        {
            var definition = GetActionUnderLock(action);
            var index = FindEventIndex(definition, @event, exactMatch: true);
            if (index >= 0)
            {
                var binding = definition.Events[index];
                definition.Events.RemoveAt(index);
                DetachBindingUnderLock(action, binding);
                removed = true;
            }
        }

        if (removed)
            Input.Service.OnActionMapChanged(action, removed: false);
    }

    internal void ActionEraseEventsCore(string action)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        lock (_gate)
        {
            var definition = GetActionUnderLock(action);
            foreach (var binding in definition.Events)
                DetachBindingUnderLock(action, binding);
            definition.Events.Clear();
        }
        Input.Service.OnActionMapChanged(action, removed: false);
    }

    internal IReadOnlyList<InputEvent> ActionGetEventsCore(string action)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        lock (_gate)
            return Array.AsReadOnly(GetActionUnderLock(action).Events.ToArray());
    }

    internal bool EventIsActionCore(InputEvent @event, string action, bool exactMatch = false) =>
    TryGetActionStatus(@event, action, exactMatch, out _);

    internal string GetActionDescriptionCore(string action)
    {
        InputEvent.ValidateActionName(action, nameof(action));
        lock (_gate)
        {
            var events = GetActionUnderLock(action).Events;
            var descriptions = events
                .Where(static @event => @event is not InputEventAction)
                .Select(static @event => @event.AsText())
                .ToArray();
            if (descriptions.Length == 0)
                return Tr("Action has no bound inputs");
            return string.Join(Tr(" or "), descriptions);
        }
    }

    internal bool TryGetActionStatus(InputEvent @event, string action, bool exactMatch, out InputActionMatch status)
    {
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        InputEvent.ValidateActionName(action, nameof(action));

        lock (_gate)
        {
            var definition = GetActionUnderLock(action);
            if (@event is InputEventAction direct)
            {
                if (!string.Equals(direct.Action, action, StringComparison.Ordinal))
                {
                    status = default;
                    return false;
                }

                var pressed = direct.IsPressed();
                var strength = pressed ? direct.Strength : 0f;
                status = new InputActionMatch(pressed, strength, strength);
                return true;
            }

            for (var index = 0; index < definition.Events.Count; index++)
            {
                var binding = definition.Events[index];
                binding.EnsureUsable();
                if ((binding.Device == AllDevices || binding.Device == @event.Device) &&
                    binding.TryMatchAction(@event, exactMatch, definition.Deadzone, out status))
                    return true;
            }
        }

        status = default;
        return false;
    }

    internal void CollectMatches(InputEvent @event, List<MappedInputAction> matches)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(matches);
        @event.EnsureUsable();
        matches.Clear();

        lock (_gate)
        {
            if (@event is InputEventAction direct)
            {
                if (!_actions.TryGetValue(direct.Action, out var action))
                    throw new KeyNotFoundException($"Input action '{direct.Action}' is not registered.");

                var pressed = direct.IsPressed();
                var strength = pressed ? direct.Strength : 0f;
                var sourceIndex = direct.EventIndex >= 0 ? direct.EventIndex : action.Events.Count;
                if (sourceIndex >= Input.MaxEventsPerAction)
                    throw new InvalidOperationException("A direct action event without an explicit index cannot address an action that already has 32 bindings.");
                matches.Add(new MappedInputAction(
                    direct.Action,
                    sourceIndex,
                    new InputActionMatch(pressed, strength, strength),
                    Exact: true));
                return;
            }

            foreach (var actionName in _actionOrder)
            {
                var action = _actions[actionName];
                for (var index = 0; index < action.Events.Count; index++)
                {
                    var binding = action.Events[index];
                    binding.EnsureUsable();
                    if ((binding.Device == AllDevices || binding.Device == @event.Device) &&
                        binding.TryMatchAction(@event, exactMatch: false, action.Deadzone, out var status))
                    {
                        var exact = binding.TryMatchAction(@event, exactMatch: true, action.Deadzone, out _);
                        matches.Add(new MappedInputAction(actionName, index, status, exact));
                    }
                }
            }
        }
    }

    internal bool TryGetFirstEventText(string action, out string text)
    {
        lock (_gate)
        {
            if (_actions.TryGetValue(action, out var definition))
            {
                foreach (var @event in definition.Events)
                {
                    if (@event is InputEventAction)
                        continue;

                    text = @event.AsText();
                    return true;
                }
            }
        }

        text = string.Empty;
        return false;
    }

    /// <inheritdoc />
    /// <remarks>The process-wide action map cannot be disposed.</remarks>
    /// <exception cref="InvalidOperationException">Always thrown because the singleton has process lifetime.</exception>
    protected override void ValidateDisposal() =>
        throw new InvalidOperationException("The process-wide InputMap instance cannot be disposed.");

    private static int FindEventIndex(ActionDefinition definition, InputEvent @event, bool exactMatch)
    {
        for (var index = 0; index < definition.Events.Count; index++)
        {
            var candidate = definition.Events[index];
            candidate.EnsureUsable();
            if ((candidate.Device == AllDevices || candidate.Device == @event.Device) &&
                candidate.TryMatchAction(@event, exactMatch, definition.Deadzone, out _))
                return index;
        }

        return -1;
    }

    private ActionDefinition GetActionUnderLock(string action) =>
        _actions.TryGetValue(action, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Input action '{action}' is not registered.");

    private void AttachBindingUnderLock(string action, InputEvent binding)
    {
        if (!_bindingActions.TryGetValue(binding, out var actions))
        {
            actions = new HashSet<string>(StringComparer.Ordinal);
            _bindingActions.Add(binding, actions);
            binding.BindingChanged += OnBindingChanged;
            binding.BindingDisposed += OnBindingDisposed;
        }

        actions.Add(action);
    }

    private void DetachBindingUnderLock(string action, InputEvent binding)
    {
        if (!_bindingActions.TryGetValue(binding, out var actions))
            return;

        actions.Remove(action);
        if (actions.Count != 0)
            return;

        binding.BindingChanged -= OnBindingChanged;
        binding.BindingDisposed -= OnBindingDisposed;
        _bindingActions.Remove(binding);
    }

    private void OnBindingChanged(InputEvent resource)
    {
        string[] actions;
        lock (_gate)
        {
            if (!_bindingActions.TryGetValue((InputEvent)resource, out var affected))
                return;
            actions = affected.ToArray();
        }

        foreach (var action in actions)
            Input.Service.OnActionMapChanged(action, removed: false);
    }

    private void OnBindingDisposed(InputEvent binding)
    {
        HashSet<string> actions;
        lock (_gate)
        {
            if (!_bindingActions.Remove(binding, out actions!))
                return;

            foreach (var action in actions)
            {
                if (_actions.TryGetValue(action, out var definition))
                {
                    for (var index = definition.Events.Count - 1; index >= 0; index--)
                    {
                        if (ReferenceEquals(definition.Events[index], binding))
                            definition.Events.RemoveAt(index);
                    }
                }
            }

            binding.BindingChanged -= OnBindingChanged;
            binding.BindingDisposed -= OnBindingDisposed;
        }

        foreach (var action in actions)
            Input.Service.OnActionMapChanged(action, removed: false);
    }

    private static void ValidateDeadzone(float deadzone, string parameterName)
    {
        if (!float.IsFinite(deadzone) || deadzone < 0f || deadzone > 1f)
            throw new ArgumentOutOfRangeException(parameterName, deadzone, "An action deadzone must be finite and between 0 and 1.");
    }

    private sealed class ActionDefinition(float deadzone)
    {
        public float Deadzone = deadzone;
        public readonly List<InputEvent> Events = [];
    }
}

internal readonly record struct MappedInputAction(
    string Action,
    int SourceIndex,
    InputActionMatch Status,
    bool Exact);
