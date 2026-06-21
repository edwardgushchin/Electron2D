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
/// Provides process-wide action input state.
/// </summary>
///
/// <remarks>
/// <para>
/// The input state is fed by <see cref="SceneTree.DispatchInput(InputEvent)"/>
/// through the action bindings registered in <see cref="InputMap"/>. It stores
/// frame-local transition state such as <see cref="IsActionJustPressed"/>.
/// </para>
/// <para>
/// 0.1.0 Preview supports keyboard bindings, mouse button bindings and direct
/// <see cref="InputEventAction"/> events. Gamepad, touch and mobile-specific
/// input are separate backlog items.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// All methods synchronize access to the process-wide input state.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="InputMap"/>
public static class Input
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, InputActionState> ActionStates = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, JoypadState> Joypads = new();

    /// <summary>
    /// Checks whether an action is currently pressed.
    /// </summary>
    ///
    /// <param name="action">The action name to query.</param>
    /// <param name="exactMatch">Reserved for exact modifier/device matching in future input work.</param>
    /// <returns>
    /// <c>true</c> when the action currently has a positive strength;
    /// otherwise, <c>false</c>.
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
    /// <seealso cref="GetActionStrength(string, bool)"/>
    public static bool IsActionPressed(string action, bool exactMatch = false)
    {
        _ = exactMatch;
        return GetActionStrength(action) > 0f;
    }

    /// <summary>
    /// Checks whether an action became pressed during the current frame.
    /// </summary>
    ///
    /// <param name="action">The action name to query.</param>
    /// <param name="exactMatch">Reserved for exact modifier/device matching in future input work.</param>
    /// <returns>
    /// <c>true</c> when the action transitioned from released to pressed since
    /// the previous transition flush; otherwise, <c>false</c>.
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
    /// <seealso cref="IsActionPressed(string, bool)"/>
    public static bool IsActionJustPressed(string action, bool exactMatch = false)
    {
        _ = exactMatch;
        var actionName = InputMap.ValidateActionName(action);

        lock (SyncRoot)
        {
            return ActionStates.TryGetValue(actionName, out var state) && state.JustPressed;
        }
    }

    /// <summary>
    /// Gets the current strength of an action.
    /// </summary>
    ///
    /// <param name="action">The action name to query.</param>
    /// <param name="exactMatch">Reserved for exact modifier/device matching in future input work.</param>
    /// <returns>
    /// The action strength in the range <c>0.0</c> through <c>1.0</c>, or
    /// <c>0.0</c> for unknown or released actions.
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
    /// <seealso cref="InputMap.ActionGetDeadzone"/>
    public static float GetActionStrength(string action, bool exactMatch = false)
    {
        _ = exactMatch;
        var actionName = InputMap.ValidateActionName(action);

        lock (SyncRoot)
        {
            return ActionStates.TryGetValue(actionName, out var state) ? state.Strength : 0f;
        }
    }

    /// <summary>
    /// Gets a 2D vector from four directional actions.
    /// </summary>
    ///
    /// <param name="negativeX">The action used for negative X movement.</param>
    /// <param name="positiveX">The action used for positive X movement.</param>
    /// <param name="negativeY">The action used for negative Y movement.</param>
    /// <param name="positiveY">The action used for positive Y movement.</param>
    /// <param name="deadzone">
    /// The vector deadzone. Use a negative value to average the four action
    /// deadzones from <see cref="InputMap"/>.
    /// </param>
    /// <returns>
    /// A vector whose components are built from action strengths and whose
    /// length is clamped to <c>1.0</c>.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when any action name is empty or <paramref name="deadzone"/> is
    /// not finite.
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
    /// <seealso cref="GetActionStrength(string, bool)"/>
    public static Vector2 GetVector(
        string negativeX,
        string positiveX,
        string negativeY,
        string positiveY,
        float deadzone = -1f)
    {
        if (!float.IsFinite(deadzone))
        {
            throw new ArgumentException("Vector deadzone must be finite.", nameof(deadzone));
        }

        var vector = new Vector2(
            GetActionStrength(positiveX) - GetActionStrength(negativeX),
            GetActionStrength(positiveY) - GetActionStrength(negativeY));

        var length = vector.Length();
        if (length > 1f)
        {
            vector /= length;
            length = 1f;
        }

        var effectiveDeadzone = deadzone >= 0f
            ? deadzone
            : (InputMap.ActionGetDeadzone(negativeX) +
                InputMap.ActionGetDeadzone(positiveX) +
                InputMap.ActionGetDeadzone(negativeY) +
                InputMap.ActionGetDeadzone(positiveY)) / 4f;

        return effectiveDeadzone > 0f && length <= effectiveDeadzone ? Vector2.Zero : vector;
    }

    /// <summary>
    /// Gets the connected gamepad device ids.
    /// </summary>
    ///
    /// <returns>
    /// A stable ascending snapshot of connected gamepad device ids.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Device ids are runtime-local identifiers reported by the platform input
    /// layer. They are not stable across process restarts and should not be
    /// persisted in project files.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetJoyName(int)"/>
    /// <seealso cref="IsJoyKnown(int)"/>
    public static int[] GetConnectedJoypads()
    {
        lock (SyncRoot)
        {
            return Joypads.Keys.Order().ToArray();
        }
    }

    /// <summary>
    /// Gets the last known value for a gamepad axis.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <param name="axis">The axis to query.</param>
    /// <returns>
    /// The normalized axis value in the range <c>-1.0</c> through <c>1.0</c>,
    /// or <c>0.0</c> when the device or axis is unknown.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is updated when an <see cref="InputEventJoypadMotion"/> is
    /// dispatched through <see cref="SceneTree"/>. Unknown devices and invalid
    /// axes fail closed and return zero.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="InputEventJoypadMotion"/>
    public static float GetJoyAxis(int device, JoyAxis axis)
    {
        if (!IsValidJoyAxis(axis))
        {
            return 0f;
        }

        lock (SyncRoot)
        {
            return Joypads.TryGetValue(device, out var state) &&
                state.Axes.TryGetValue(axis, out var value)
                    ? value
                    : 0f;
        }
    }

    /// <summary>
    /// Gets the display name for a connected gamepad.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <returns>
    /// The device name, or an empty string when the device is unknown or has no
    /// reported name.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetConnectedJoypads"/>
    public static string GetJoyName(int device)
    {
        lock (SyncRoot)
        {
            return Joypads.TryGetValue(device, out var state) ? state.Name : string.Empty;
        }
    }

    /// <summary>
    /// Gets the active vibration duration for a gamepad.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <returns>
    /// The requested vibration duration in seconds, or <c>0.0</c> when no
    /// vibration is active or the device is unknown.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// A duration of <c>0.0</c> can also represent an indefinite active effect.
    /// Use <see cref="GetJoyVibrationStrength(int)"/> to distinguish an
    /// indefinite active effect from an inactive device.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="StartJoyVibration"/>
    /// <seealso cref="StopJoyVibration"/>
    public static float GetJoyVibrationDuration(int device)
    {
        lock (SyncRoot)
        {
            return Joypads.TryGetValue(device, out var state) ? state.VibrationDuration : 0f;
        }
    }

    /// <summary>
    /// Gets the active vibration strength for a gamepad.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <returns>
    /// A vector where <see cref="Vector2.X"/> is the weak motor strength and
    /// <see cref="Vector2.Y"/> is the strong motor strength. Both components are
    /// in the range <c>0.0</c> through <c>1.0</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Unknown or unsupported devices return <see cref="Vector2.Zero"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="StartJoyVibration"/>
    /// <seealso cref="StopJoyVibration"/>
    public static Vector2 GetJoyVibrationStrength(int device)
    {
        lock (SyncRoot)
        {
            return Joypads.TryGetValue(device, out var state) ? state.VibrationStrength : Vector2.Zero;
        }
    }

    /// <summary>
    /// Checks whether a gamepad button is currently pressed.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <param name="button">The button to query.</param>
    /// <returns>
    /// <c>true</c> when the button is pressed on the connected device;
    /// otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The state is updated by <see cref="InputEventJoypadButton"/> dispatch.
    /// Invalid buttons and unknown devices return <c>false</c>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="InputEventJoypadButton"/>
    public static bool IsJoyButtonPressed(int device, JoyButton button)
    {
        if (!IsValidJoyButton(button))
        {
            return false;
        }

        lock (SyncRoot)
        {
            return Joypads.TryGetValue(device, out var state) && state.PressedButtons.Contains(button);
        }
    }

    /// <summary>
    /// Checks whether a connected gamepad uses a known mapping.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <returns>
    /// <c>true</c> when the device is connected and its mapping is known;
    /// otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Devices created from button or axis events without metadata are treated
    /// as unknown until the platform input layer supplies mapping information.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static bool IsJoyKnown(int device)
    {
        lock (SyncRoot)
        {
            return Joypads.TryGetValue(device, out var state) && state.IsKnown;
        }
    }

    /// <summary>
    /// Starts gamepad vibration when the device supports it.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    /// <param name="weakMagnitude">The weak motor strength.</param>
    /// <param name="strongMagnitude">The strong motor strength.</param>
    /// <param name="duration">
    /// The duration in seconds. Values less than or equal to <c>0.0</c> request
    /// an indefinite effect.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// Magnitudes are clamped to <c>0.0</c> through <c>1.0</c>. Unknown devices
    /// and devices without vibration support are ignored without throwing.
    /// </para>
    /// <para>
    /// This baseline records the requested state for the platform backend. It
    /// does not expose native haptic handles through the public API.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="weakMagnitude"/>,
    /// <paramref name="strongMagnitude"/> or <paramref name="duration"/> is not
    /// finite.
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
    /// <seealso cref="StopJoyVibration"/>
    /// <seealso cref="GetJoyVibrationStrength(int)"/>
    public static void StartJoyVibration(
        int device,
        float weakMagnitude,
        float strongMagnitude,
        float duration = 0f)
    {
        if (!float.IsFinite(weakMagnitude))
        {
            throw new ArgumentException("Weak vibration magnitude must be finite.", nameof(weakMagnitude));
        }

        if (!float.IsFinite(strongMagnitude))
        {
            throw new ArgumentException("Strong vibration magnitude must be finite.", nameof(strongMagnitude));
        }

        if (!float.IsFinite(duration))
        {
            throw new ArgumentException("Vibration duration must be finite.", nameof(duration));
        }

        lock (SyncRoot)
        {
            if (!Joypads.TryGetValue(device, out var state) || !state.VibrationSupported)
            {
                return;
            }

            state.VibrationStrength = new Vector2(
                Mathf.Clamp(weakMagnitude, 0f, 1f),
                Mathf.Clamp(strongMagnitude, 0f, 1f));
            state.VibrationDuration = MathF.Max(0f, duration);
        }
    }

    /// <summary>
    /// Stops gamepad vibration.
    /// </summary>
    ///
    /// <param name="device">The runtime-local gamepad device id.</param>
    ///
    /// <remarks>
    /// <para>
    /// Unknown devices and devices without vibration support are ignored without
    /// throwing.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="StartJoyVibration"/>
    public static void StopJoyVibration(int device)
    {
        lock (SyncRoot)
        {
            if (Joypads.TryGetValue(device, out var state))
            {
                state.VibrationStrength = Vector2.Zero;
                state.VibrationDuration = 0f;
            }
        }
    }

    internal static void ProcessEvent(InputEvent inputEvent)
    {
        ProcessJoypadEvent(inputEvent);

        foreach (var match in InputMap.MatchEvent(inputEvent))
        {
            lock (SyncRoot)
            {
                var state = GetOrCreateState(match.Action);
                var wasPressed = state.Strength > 0f;

                if (match.BindingId.StartsWith("action:", StringComparison.Ordinal))
                {
                    state.DirectStrength = match.Pressed ? match.Strength : 0f;
                }
                else if (match.Pressed)
                {
                    state.BindingStrengths[match.BindingId] = match.Strength;
                }
                else
                {
                    state.BindingStrengths.Remove(match.BindingId);
                }

                var bindingStrength = state.BindingStrengths.Count == 0 ? 0f : state.BindingStrengths.Values.Max();
                var newStrength = MathF.Max(bindingStrength, state.DirectStrength);
                state.Strength = Mathf.Clamp(newStrength, 0f, 1f);
                if (!wasPressed && state.Strength > 0f)
                {
                    state.JustPressed = true;
                }
            }
        }
    }

    internal static void FlushFrameTransitions()
    {
        lock (SyncRoot)
        {
            foreach (var state in ActionStates.Values)
            {
                state.JustPressed = false;
            }
        }
    }

    internal static void ConnectJoypad(
        int device,
        string? name = null,
        bool isKnown = false,
        bool vibrationSupported = false)
    {
        lock (SyncRoot)
        {
            Joypads[device] = new JoypadState
            {
                Name = name ?? string.Empty,
                IsKnown = isKnown,
                VibrationSupported = vibrationSupported
            };
        }
    }

    internal static void DisconnectJoypad(int device)
    {
        lock (SyncRoot)
        {
            Joypads.Remove(device);
        }
    }

    internal static void SetJoypadAxis(int device, JoyAxis axis, float value)
    {
        if (!IsValidJoyAxis(axis))
        {
            return;
        }

        lock (SyncRoot)
        {
            var state = GetOrCreateJoypadState(device);
            state.Axes[axis] = Mathf.Clamp(value, -1f, 1f);
        }
    }

    internal static void SetJoypadButton(int device, JoyButton button, bool pressed)
    {
        if (!IsValidJoyButton(button))
        {
            return;
        }

        lock (SyncRoot)
        {
            var state = GetOrCreateJoypadState(device);
            if (pressed)
            {
                state.PressedButtons.Add(button);
            }
            else
            {
                state.PressedButtons.Remove(button);
            }
        }
    }

    internal static void ClearActionState(string action)
    {
        lock (SyncRoot)
        {
            ActionStates.Remove(action);
        }
    }

    internal static void ClearAllActionState()
    {
        lock (SyncRoot)
        {
            ActionStates.Clear();
        }
    }

    internal static void ResetForTests()
    {
        ClearAllActionState();
        lock (SyncRoot)
        {
            Joypads.Clear();
        }
    }

    private static InputActionState GetOrCreateState(string action)
    {
        if (!ActionStates.TryGetValue(action, out var state))
        {
            state = new InputActionState();
            ActionStates.Add(action, state);
        }

        return state;
    }

    private static JoypadState GetOrCreateJoypadState(int device)
    {
        if (!Joypads.TryGetValue(device, out var state))
        {
            state = new JoypadState();
            Joypads.Add(device, state);
        }

        return state;
    }

    private static void ProcessJoypadEvent(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventJoypadButton buttonEvent:
                SetJoypadButton(buttonEvent.Device, buttonEvent.ButtonIndex, buttonEvent.Pressed);
                break;
            case InputEventJoypadMotion motionEvent:
                SetJoypadAxis(motionEvent.Device, motionEvent.Axis, motionEvent.AxisValue);
                break;
        }
    }

    private static bool IsValidJoyAxis(JoyAxis axis)
    {
        return axis is >= JoyAxis.LeftX and <= JoyAxis.TriggerRight;
    }

    private static bool IsValidJoyButton(JoyButton button)
    {
        return button is >= JoyButton.A and <= JoyButton.Touchpad;
    }

    private sealed class InputActionState
    {
        public Dictionary<string, float> BindingStrengths { get; } = new(StringComparer.Ordinal);

        public float DirectStrength { get; set; }

        public float Strength { get; set; }

        public bool JustPressed { get; set; }
    }

    private sealed class JoypadState
    {
        public string Name { get; init; } = string.Empty;

        public bool IsKnown { get; init; }

        public bool VibrationSupported { get; init; }

        public Dictionary<JoyAxis, float> Axes { get; } = new();

        public HashSet<JoyButton> PressedButtons { get; } = [];

        public Vector2 VibrationStrength { get; set; }

        public float VibrationDuration { get; set; }
    }
}
