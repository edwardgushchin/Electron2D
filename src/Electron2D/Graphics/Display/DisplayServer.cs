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
/// Provides display, orientation and virtual keyboard state.
/// </summary>
///
/// <remarks>
/// <para>
/// The 0.1.0 Preview implementation stores a compact process-wide state used by
/// mobile input and export pipelines. Platform backends feed this state through
/// internal methods; public methods expose stable values without native handles.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// All methods synchronize access to process-wide display state.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="InputEventScreenTouch"/>
/// <seealso cref="InputEventScreenDrag"/>
public static class DisplayServer
{
    private static readonly object SyncRoot = new();
    private static Rect2I displaySafeArea;
    private static ScreenOrientation screenOrientation = ScreenOrientation.Landscape;
    private static VirtualKeyboardState virtualKeyboard = VirtualKeyboardState.Hidden;

    /// <summary>
    /// Identifies a requested screen orientation.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Sensor-based values are accepted and stored even when the current platform
    /// backend cannot apply them immediately.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be used from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum ScreenOrientation
    {
        /// <summary>
        /// Represents default landscape orientation.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        Landscape = 0,

        /// <summary>
        /// Represents default portrait orientation.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        Portrait = 1,

        /// <summary>
        /// Represents reverse landscape orientation.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        ReverseLandscape = 2,

        /// <summary>
        /// Represents reverse portrait orientation.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        ReversePortrait = 3,

        /// <summary>
        /// Represents automatic landscape orientation selected by sensors.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        SensorLandscape = 4,

        /// <summary>
        /// Represents automatic portrait orientation selected by sensors.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        SensorPortrait = 5,

        /// <summary>
        /// Represents automatic landscape or portrait orientation selected by sensors.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept ScreenOrientation.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="ScreenOrientation" />
        ///
        Sensor = 6
    }

    /// <summary>
    /// Identifies the requested virtual keyboard layout.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The platform backend may fall back to <see cref="Default"/> when a more
    /// specific keyboard layout is unavailable.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be used from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum VirtualKeyboardType
    {
        /// <summary>
        /// Represents a default text keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        Default = 0,

        /// <summary>
        /// Represents a multiline text keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        Multiline = 1,

        /// <summary>
        /// Represents a numeric keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        Number = 2,

        /// <summary>
        /// Represents a numeric keyboard that allows decimal separators.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        NumberDecimal = 3,

        /// <summary>
        /// Represents a phone-number keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        Phone = 4,

        /// <summary>
        /// Represents an email-address keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        EmailAddress = 5,

        /// <summary>
        /// Represents a password keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        Password = 6,

        /// <summary>
        /// Represents a URL keyboard.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept VirtualKeyboardType.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="VirtualKeyboardType" />
        ///
        Url = 7
    }

    /// <summary>
    /// Gets the current display safe area.
    /// </summary>
    ///
    /// <returns>
    /// The unobscured display area, or an empty rectangle when the platform
    /// has not reported one.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned rectangle is a snapshot. Platform updates that arrive later
    /// do not mutate previously returned values.
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
    /// <seealso cref="DisplayServer" />
    ///
    public static Rect2I GetDisplaySafeArea()
    {
        lock (SyncRoot)
        {
            return displaySafeArea;
        }
    }

    /// <summary>
    /// Gets the current screen orientation state.
    /// </summary>
    ///
    /// <param name="screen">The screen index. The 0.1.0 Preview stores one process-wide state.</param>
    /// <returns>
    /// The current screen orientation.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The <paramref name="screen"/> parameter is accepted for API compatibility
    /// with future multi-screen support and is ignored in this baseline.
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
    /// <seealso cref="ScreenSetOrientation"/>
    public static ScreenOrientation ScreenGetOrientation(int screen = -1)
    {
        _ = screen;
        lock (SyncRoot)
        {
            return screenOrientation;
        }
    }

    /// <summary>
    /// Sets the requested screen orientation state.
    /// </summary>
    ///
    /// <param name="orientation">The requested orientation.</param>
    /// <param name="screen">The screen index. The 0.1.0 Preview stores one process-wide state.</param>
    ///
    /// <remarks>
    /// <para>
    /// Invalid enum values are ignored. The request is recorded for the platform
    /// backend but does not expose any native display object through public API.
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
    /// <seealso cref="ScreenGetOrientation"/>
    public static void ScreenSetOrientation(ScreenOrientation orientation, int screen = -1)
    {
        _ = screen;
        if (!IsValidOrientation(orientation))
        {
            return;
        }

        lock (SyncRoot)
        {
            screenOrientation = orientation;
        }
    }

    /// <summary>
    /// Gets the current virtual keyboard height.
    /// </summary>
    ///
    /// <returns>
    /// The last reported virtual keyboard height in pixels, or <c>0</c> when the
    /// virtual keyboard is hidden or unsupported.
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
    /// <seealso cref="VirtualKeyboardShow"/>
    /// <seealso cref="VirtualKeyboardHide"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public static int VirtualKeyboardGetHeight()
    {
        lock (SyncRoot)
        {
            return virtualKeyboard.Visible ? virtualKeyboard.Height : 0;
        }
    }

    /// <summary>
    /// Hides the virtual keyboard.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This call clears the stored keyboard request and resets the reported
    /// height to zero. Unsupported platforms treat it as a no-op.
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
    /// <seealso cref="VirtualKeyboardShow"/>
    public static void VirtualKeyboardHide()
    {
        lock (SyncRoot)
        {
            virtualKeyboard = VirtualKeyboardState.Hidden;
        }
    }

    /// <summary>
    /// Shows the virtual keyboard.
    /// </summary>
    ///
    /// <param name="existingText">The text already present in the edited control.</param>
    /// <param name="position">The edited control rectangle in window coordinates.</param>
    /// <param name="type">The requested virtual keyboard layout.</param>
    /// <param name="maxLength">The maximum accepted text length, or <c>-1</c> for unlimited input.</param>
    /// <param name="cursorStart">The start of the current selection, or <c>-1</c> when unknown.</param>
    /// <param name="cursorEnd">The end of the current selection, or <c>-1</c> when unknown.</param>
    ///
    /// <remarks>
    /// <para>
    /// This method records the request for the platform backend. Passing
    /// <c>null</c> for <paramref name="existingText"/> stores an empty string.
    /// Invalid keyboard types fall back to <see cref="VirtualKeyboardType.Default"/>.
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
    /// <seealso cref="VirtualKeyboardHide"/>
    /// <seealso cref="VirtualKeyboardGetHeight"/>
    public static void VirtualKeyboardShow(
        string existingText = "",
        Rect2 position = default,
        VirtualKeyboardType type = VirtualKeyboardType.Default,
        int maxLength = -1,
        int cursorStart = -1,
        int cursorEnd = -1)
    {
        lock (SyncRoot)
        {
            virtualKeyboard = new VirtualKeyboardState(
                Visible: true,
                Height: Math.Max(0, virtualKeyboard.Height),
                existingText ?? string.Empty,
                position,
                IsValidKeyboardType(type) ? type : VirtualKeyboardType.Default,
                Math.Max(-1, maxLength),
                Math.Max(-1, cursorStart),
                Math.Max(-1, cursorEnd));
        }
    }

    internal static void ResetForTests()
    {
        lock (SyncRoot)
        {
            displaySafeArea = default;
            screenOrientation = ScreenOrientation.Landscape;
            virtualKeyboard = VirtualKeyboardState.Hidden;
        }
    }

    internal static void SetDisplaySafeArea(Rect2I safeArea)
    {
        lock (SyncRoot)
        {
            displaySafeArea = safeArea.Abs();
        }
    }

    internal static void SetDisplaySafeAreaForTests(Rect2I safeArea)
    {
        SetDisplaySafeArea(safeArea);
    }

    internal static void SetScreenOrientationFromPlatform(ScreenOrientation orientation)
    {
        ScreenSetOrientation(orientation);
    }

    internal static void SetVirtualKeyboardHeight(int height)
    {
        lock (SyncRoot)
        {
            virtualKeyboard = virtualKeyboard with
            {
                Visible = height > 0 || virtualKeyboard.Visible,
                Height = Math.Max(0, height)
            };
        }
    }

    internal static void SetVirtualKeyboardHeightForTests(int height)
    {
        SetVirtualKeyboardHeight(height);
    }

    private static bool IsValidOrientation(ScreenOrientation orientation)
    {
        return orientation is >= ScreenOrientation.Landscape and <= ScreenOrientation.Sensor;
    }

    private static bool IsValidKeyboardType(VirtualKeyboardType type)
    {
        return type is >= VirtualKeyboardType.Default and <= VirtualKeyboardType.Url;
    }

    private readonly record struct VirtualKeyboardState(
        bool Visible,
        int Height,
        string ExistingText,
        Rect2 Position,
        VirtualKeyboardType Type,
        int MaxLength,
        int CursorStart,
        int CursorEnd)
    {
        public static VirtualKeyboardState Hidden { get; } = new(
            Visible: false,
            Height: 0,
            ExistingText: string.Empty,
            Position: default,
            Type: VirtualKeyboardType.Default,
            MaxLength: -1,
            CursorStart: -1,
            CursorEnd: -1);
    }
}
