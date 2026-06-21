/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

/// <summary>
/// Stores input actions and their input event bindings.
/// </summary>
///
/// <remarks>
/// <para>
/// An input action is a named command such as <c>jump</c> or
/// <c>move_left</c>. The map stores a deadzone and a set of keyboard, mouse or
/// direct action events for each action.
/// </para>
/// <para>
/// The map is process-wide in 0.1.0 Preview. Project-file persistence is handled
/// by an internal serializer used by the future project settings layer, not by
/// public file I/O methods on this type.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// All methods synchronize access to the process-wide action registry. Returned
/// arrays are snapshots and may be read from any thread.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Input"/>
/// <seealso cref="InputEventAction"/>
public static class InputMap
{
    private const float DefaultDeadzone = 0.5f;
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, ActionDefinition> Actions = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds an action to the input map.
    /// </summary>
    ///
    /// <param name="action">The action name to add.</param>
    /// <param name="deadzone">The action deadzone in the range <c>0.0</c> through <c>1.0</c>.</param>
    ///
    /// <remarks>
    /// Re-adding an existing action leaves its deadzone and events unchanged.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty or
    /// <paramref name="deadzone"/> is not finite or outside the supported
    /// range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static void AddAction(string action, float deadzone = DefaultDeadzone)
    {
        var actionName = ValidateActionName(action);
        ValidateDeadzone(deadzone);

        lock (SyncRoot)
        {
            Actions.TryAdd(actionName, new ActionDefinition(actionName, deadzone));
        }
    }

    /// <summary>
    /// Removes an action and its events from the input map.
    /// </summary>
    ///
    /// <param name="action">The action name to remove.</param>
    ///
    /// <remarks>
    /// Removing an unknown action is a no-op. Any runtime state tracked for the
    /// action is cleared as part of removal.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static void EraseAction(string action)
    {
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            Actions.Remove(actionName);
        }

        Input.ClearActionState(actionName);
    }

    /// <summary>
    /// Checks whether an action exists.
    /// </summary>
    ///
    /// <param name="action">The action name to check.</param>
    /// <returns>
    /// <c>true</c> if the action exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static bool HasAction(string action)
    {
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            return Actions.ContainsKey(actionName);
        }
    }

    /// <summary>
    /// Gets the registered action names.
    /// </summary>
    ///
    /// <returns>
    /// A stable, ordinally sorted array of action names.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static string[] GetActions()
    {
        lock (SyncRoot)
        {
            return Actions.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }
    }

    /// <summary>
    /// Sets the deadzone for an action.
    /// </summary>
    ///
    /// <param name="action">The action name to update.</param>
    /// <param name="deadzone">The new deadzone in the range <c>0.0</c> through <c>1.0</c>.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty, the action does not
    /// exist, or <paramref name="deadzone"/> is not finite or outside the
    /// supported range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ActionGetDeadzone"/>
    public static void ActionSetDeadzone(string action, float deadzone)
    {
        ValidateDeadzone(deadzone);
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            if (!Actions.TryGetValue(actionName, out var definition))
            {
                throw new ArgumentException($"Input action '{actionName}' does not exist.", nameof(action));
            }

            definition.Deadzone = deadzone;
        }
    }

    /// <summary>
    /// Gets the deadzone for an action.
    /// </summary>
    ///
    /// <param name="action">The action name to query.</param>
    /// <returns>
    /// The action deadzone, or <c>0.0</c> when the action does not exist.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ActionSetDeadzone"/>
    public static float ActionGetDeadzone(string action)
    {
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            return Actions.TryGetValue(actionName, out var definition) ? definition.Deadzone : 0f;
        }
    }

    /// <summary>
    /// Adds an input event binding to an action.
    /// </summary>
    ///
    /// <param name="action">The action name to update.</param>
    /// <param name="inputEvent">The input event binding to add.</param>
    ///
    /// <remarks>
    /// Duplicate bindings are ignored. The stored binding is copied, so later
    /// mutations to <paramref name="inputEvent"/> do not affect the map.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty, the action does not
    /// exist, or the event type cannot be used as an action binding.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputEvent"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ActionEraseEvent"/>
    /// <seealso cref="ActionGetEvents"/>
    public static void ActionAddEvent(string action, InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        var signature = InputEventSignature.From(inputEvent);
        var eventCopy = CloneEvent(inputEvent);
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            if (!Actions.TryGetValue(actionName, out var definition))
            {
                throw new ArgumentException($"Input action '{actionName}' does not exist.", nameof(action));
            }

            if (definition.Events.Any(binding => InputEventSignature.From(binding) == signature))
            {
                return;
            }

            definition.Events.Add(eventCopy);
            definition.SortEvents();
        }
    }

    /// <summary>
    /// Removes one input event binding from an action.
    /// </summary>
    ///
    /// <param name="action">The action name to update.</param>
    /// <param name="inputEvent">The input event binding to remove.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty, the action does not
    /// exist, or the event type cannot be used as an action binding.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputEvent"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ActionAddEvent"/>
    public static void ActionEraseEvent(string action, InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        var signature = InputEventSignature.From(inputEvent);
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            if (!Actions.TryGetValue(actionName, out var definition))
            {
                throw new ArgumentException($"Input action '{actionName}' does not exist.", nameof(action));
            }

            definition.Events.RemoveAll(binding => InputEventSignature.From(binding) == signature);
        }
    }

    /// <summary>
    /// Removes all input event bindings from an action.
    /// </summary>
    ///
    /// <param name="action">The action name to update.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty or the action does not
    /// exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static void ActionEraseEvents(string action)
    {
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            if (!Actions.TryGetValue(actionName, out var definition))
            {
                throw new ArgumentException($"Input action '{actionName}' does not exist.", nameof(action));
            }

            definition.Events.Clear();
        }
    }

    /// <summary>
    /// Gets the input event bindings for an action.
    /// </summary>
    ///
    /// <param name="action">The action name to query.</param>
    /// <returns>
    /// A stable snapshot of copied input event bindings, or an empty array when
    /// the action does not exist.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static InputEvent[] ActionGetEvents(string action)
    {
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            return Actions.TryGetValue(actionName, out var definition)
                ? definition.Events.Select(CloneEvent).ToArray()
                : Array.Empty<InputEvent>();
        }
    }

    /// <summary>
    /// Checks whether an input event matches an action.
    /// </summary>
    ///
    /// <param name="inputEvent">The input event to test.</param>
    /// <param name="action">The action name to test against.</param>
    /// <param name="exactMatch">Reserved for exact modifier/device matching in future input work.</param>
    /// <returns>
    /// <c>true</c> when the event matches the action and passes the action
    /// deadzone; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="action"/> is empty.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputEvent"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Input.IsActionPressed(string, bool)"/>
    public static bool EventIsAction(InputEvent inputEvent, string action, bool exactMatch = false)
    {
        _ = exactMatch;
        ArgumentNullException.ThrowIfNull(inputEvent);
        var actionName = ValidateActionName(action);

        lock (SyncRoot)
        {
            return Actions.TryGetValue(actionName, out var definition) &&
                EventMatchesAction(inputEvent, definition, out var strength) &&
                strength > 0f;
        }
    }

    internal static InputActionMatch[] MatchEvent(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);

        lock (SyncRoot)
        {
            return Actions.Values
                .Where(definition => EventMatchesAction(inputEvent, definition, out _))
                .Select(definition =>
                {
                    EventMatchesAction(inputEvent, definition, out var strength);
                    return new InputActionMatch(
                        definition.Name,
                        IsPressedEvent(inputEvent) && strength > 0f,
                        strength,
                        InputEventSignature.StateKeyFrom(inputEvent));
                })
                .ToArray();
        }
    }

    internal static InputMapActionSnapshot[] CaptureActionSettings()
    {
        lock (SyncRoot)
        {
            return Actions.Values
                .OrderBy(action => action.Name, StringComparer.Ordinal)
                .Select(action => new InputMapActionSnapshot(
                    action.Name,
                    action.Deadzone,
                    action.Events.Select(CloneEvent).ToArray()))
                .ToArray();
        }
    }

    internal static void ReplaceActionSettings(IEnumerable<InputMapActionSnapshot> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var replacement = new Dictionary<string, ActionDefinition>(StringComparer.Ordinal);
        foreach (var snapshot in actions)
        {
            var name = ValidateActionName(snapshot.Name);
            ValidateDeadzone(snapshot.Deadzone);
            if (!replacement.TryAdd(name, new ActionDefinition(name, snapshot.Deadzone)))
            {
                throw new FormatException($"Input action '{name}' is duplicated.");
            }

            foreach (var inputEvent in snapshot.Events)
            {
                ArgumentNullException.ThrowIfNull(inputEvent);
                var signature = InputEventSignature.From(inputEvent);
                var definition = replacement[name];
                if (definition.Events.Any(binding => InputEventSignature.From(binding) == signature))
                {
                    continue;
                }

                definition.Events.Add(CloneEvent(inputEvent));
                definition.SortEvents();
            }
        }

        lock (SyncRoot)
        {
            Actions.Clear();
            foreach (var pair in replacement)
            {
                Actions.Add(pair.Key, pair.Value);
            }
        }

        Input.ClearAllActionState();
    }

    internal static void ClearForTests()
    {
        lock (SyncRoot)
        {
            Actions.Clear();
        }

        Input.ClearAllActionState();
    }

    internal static string ValidateActionName(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return action;
    }

    internal static void ValidateDeadzone(float deadzone)
    {
        if (!float.IsFinite(deadzone) || deadzone < 0f || deadzone > 1f)
        {
            throw new ArgumentException("Action deadzone must be a finite value between 0.0 and 1.0.", nameof(deadzone));
        }
    }

    internal static InputEvent CloneEvent(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        return inputEvent switch
        {
            InputEventAction actionEvent => new InputEventAction
            {
                Device = actionEvent.Device,
                Action = actionEvent.Action,
                Pressed = actionEvent.Pressed,
                Strength = actionEvent.Strength
            },
            InputEventKey keyEvent => new InputEventKey
            {
                Device = keyEvent.Device,
                WindowId = keyEvent.WindowId,
                ShiftPressed = keyEvent.ShiftPressed,
                AltPressed = keyEvent.AltPressed,
                CtrlPressed = keyEvent.CtrlPressed,
                MetaPressed = keyEvent.MetaPressed,
                Pressed = keyEvent.Pressed,
                Echo = keyEvent.Echo,
                Keycode = keyEvent.Keycode,
                PhysicalKeycode = keyEvent.PhysicalKeycode,
                KeyLabel = keyEvent.KeyLabel,
                Location = keyEvent.Location,
                Unicode = keyEvent.Unicode
            },
            InputEventMouseButton mouseButton => new InputEventMouseButton
            {
                Device = mouseButton.Device,
                WindowId = mouseButton.WindowId,
                ShiftPressed = mouseButton.ShiftPressed,
                AltPressed = mouseButton.AltPressed,
                CtrlPressed = mouseButton.CtrlPressed,
                MetaPressed = mouseButton.MetaPressed,
                ButtonMask = mouseButton.ButtonMask,
                Position = mouseButton.Position,
                GlobalPosition = mouseButton.GlobalPosition,
                ButtonIndex = mouseButton.ButtonIndex,
                Pressed = mouseButton.Pressed,
                Canceled = mouseButton.Canceled,
                DoubleClick = mouseButton.DoubleClick,
                Factor = mouseButton.Factor
            },
            InputEventJoypadButton joypadButton => new InputEventJoypadButton
            {
                Device = joypadButton.Device,
                ButtonIndex = joypadButton.ButtonIndex,
                Pressed = joypadButton.Pressed,
                Pressure = joypadButton.Pressure
            },
            InputEventJoypadMotion joypadMotion => new InputEventJoypadMotion
            {
                Device = joypadMotion.Device,
                Axis = joypadMotion.Axis,
                AxisValue = joypadMotion.AxisValue
            },
            _ => throw new ArgumentException("Input event type cannot be used as an action binding.", nameof(inputEvent))
        };
    }

    private static bool EventMatchesAction(InputEvent inputEvent, ActionDefinition definition, out float strength)
    {
        strength = 0f;
        if (inputEvent is InputEventAction actionEvent)
        {
            if (!string.Equals(actionEvent.Action, definition.Name, StringComparison.Ordinal))
            {
                return false;
            }

            strength = actionEvent.Pressed && actionEvent.Strength > definition.Deadzone ? actionEvent.Strength : 0f;
            return true;
        }

        foreach (var binding in definition.Events)
        {
            if (BindingMatches(binding, inputEvent, definition.Deadzone, out strength))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BindingMatches(InputEvent binding, InputEvent inputEvent, float deadzone, out float strength)
    {
        strength = 0f;
        var matches = (binding, inputEvent) switch
        {
            (InputEventKey boundKey, InputEventKey keyEvent) =>
                (boundKey.Keycode != Key.None && boundKey.Keycode == keyEvent.Keycode) ||
                (boundKey.PhysicalKeycode != Key.None && boundKey.PhysicalKeycode == keyEvent.PhysicalKeycode),
            (InputEventMouseButton boundMouse, InputEventMouseButton mouseEvent) =>
                boundMouse.ButtonIndex != MouseButton.None && boundMouse.ButtonIndex == mouseEvent.ButtonIndex,
            (InputEventAction boundAction, InputEventAction actionEvent) =>
                string.Equals(boundAction.Action, actionEvent.Action, StringComparison.Ordinal),
            (InputEventJoypadButton boundButton, InputEventJoypadButton buttonEvent) =>
                JoypadButtonMatches(boundButton, buttonEvent, out strength),
            (InputEventJoypadMotion boundMotion, InputEventJoypadMotion motionEvent) =>
                JoypadMotionMatches(boundMotion, motionEvent, deadzone, out strength),
            _ => false
        };

        if (matches && binding is not InputEventJoypadButton and not InputEventJoypadMotion)
        {
            strength = 1f;
        }

        return matches;
    }

    private static bool JoypadButtonMatches(
        InputEventJoypadButton binding,
        InputEventJoypadButton inputEvent,
        out float strength)
    {
        strength = 0f;
        if (binding.ButtonIndex == JoyButton.Invalid || binding.ButtonIndex != inputEvent.ButtonIndex)
        {
            return false;
        }

        strength = inputEvent.Pressed && inputEvent.Pressure > 0f ? inputEvent.Pressure : 1f;
        return true;
    }

    private static bool JoypadMotionMatches(
        InputEventJoypadMotion binding,
        InputEventJoypadMotion inputEvent,
        float deadzone,
        out float strength)
    {
        strength = 0f;
        if (binding.Axis == JoyAxis.Invalid || binding.Axis != inputEvent.Axis)
        {
            return false;
        }

        var value = inputEvent.AxisValue;
        var bindingSign = MathF.Sign(binding.AxisValue);
        if (bindingSign == 0)
        {
            bindingSign = 1;
        }

        var signMatches = MathF.Sign(value) == bindingSign;
        var absoluteValue = MathF.Abs(value);
        strength = signMatches && absoluteValue > deadzone ? absoluteValue : 0f;
        return true;
    }

    private static bool IsPressedEvent(InputEvent inputEvent)
    {
        return inputEvent switch
        {
            InputEventAction actionEvent => actionEvent.Pressed,
            InputEventKey keyEvent => keyEvent.Pressed,
            InputEventMouseButton mouseButton => mouseButton.Pressed,
            InputEventJoypadButton joypadButton => joypadButton.Pressed,
            InputEventJoypadMotion => true,
            _ => false
        };
    }

    private sealed class ActionDefinition(string name, float deadzone)
    {
        public string Name { get; } = name;

        public float Deadzone { get; set; } = deadzone;

        public List<InputEvent> Events { get; } = [];

        public void SortEvents()
        {
            Events.Sort(static (left, right) =>
                string.Compare(
                    InputEventSignature.From(left).SortKey,
                    InputEventSignature.From(right).SortKey,
                    StringComparison.Ordinal));
        }
    }
}

internal sealed record InputMapActionSnapshot(string Name, float Deadzone, InputEvent[] Events);

internal sealed record InputActionMatch(string Action, bool Pressed, float Strength, string BindingId);

internal readonly record struct InputEventSignature(string Kind, string Primary, string Secondary)
{
    public string SortKey => $"{Kind}:{Primary}:{Secondary}";

    public string StateKey => $"{Kind}:{Primary}:{Secondary}";

    public static string StateKeyFrom(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        return $"{From(inputEvent).StateKey}:device:{inputEvent.Device}";
    }

    public static InputEventSignature From(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        return inputEvent switch
        {
            InputEventAction actionEvent => new InputEventSignature("action", actionEvent.Action, string.Empty),
            InputEventKey keyEvent => new InputEventSignature("key", keyEvent.Keycode.ToString(), keyEvent.PhysicalKeycode.ToString()),
            InputEventMouseButton mouseButton => new InputEventSignature("mouse_button", mouseButton.ButtonIndex.ToString(), string.Empty),
            InputEventJoypadButton joypadButton => new InputEventSignature("joy_button", joypadButton.ButtonIndex.ToString(), string.Empty),
            InputEventJoypadMotion joypadMotion => new InputEventSignature(
                "joy_motion",
                joypadMotion.Axis.ToString(),
                joypadMotion.AxisValue < 0f ? "negative" : "positive"),
            _ => throw new ArgumentException("Input event type cannot be used as an action binding.", nameof(inputEvent))
        };
    }
}
