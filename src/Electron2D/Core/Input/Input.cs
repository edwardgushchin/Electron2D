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

    internal static void ProcessEvent(InputEvent inputEvent)
    {
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
                    state.ActiveBindings.Add(match.BindingId);
                }
                else
                {
                    state.ActiveBindings.Remove(match.BindingId);
                }

                var newStrength = state.ActiveBindings.Count > 0 ? 1f : state.DirectStrength;
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

    private sealed class InputActionState
    {
        public HashSet<string> ActiveBindings { get; } = new(StringComparer.Ordinal);

        public float DirectStrength { get; set; }

        public float Strength { get; set; }

        public bool JustPressed { get; set; }
    }
}
