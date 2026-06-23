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
/// Describes the result of a visible Electron2D runtime loop.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="RuntimeHostResult"/> is intentionally machine-readable so
/// automated checks can prove that a project opened a runtime window, presented
/// at least one frame and saved a screenshot when requested.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are immutable and may be read from any thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="RuntimeHost"/>
/// <seealso cref="RuntimeHostOptions"/>
internal sealed class RuntimeHostResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostResult"/> type.
    /// </summary>
    ///
    /// <param name="succeeded">Whether the run completed without a host-level failure.</param>
    /// <param name="windowCreated">Whether the runtime window was created.</param>
    /// <param name="windowShown">Whether the runtime window was shown.</param>
    /// <param name="framePresented">Whether at least one frame was presented to the window.</param>
    /// <param name="eventPumpObserved">Whether the event pump was executed.</param>
    /// <param name="inputEventsDispatched">The number of input events dispatched into the scene tree.</param>
    /// <param name="frameCount">The number of frames presented by the host.</param>
    /// <param name="drawCommands">The number of draw commands in the final frame.</param>
    /// <param name="windowWidth">The observed logical window width.</param>
    /// <param name="windowHeight">The observed logical window height.</param>
    /// <param name="pixelWidth">The observed physical pixel width.</param>
    /// <param name="pixelHeight">The observed physical pixel height.</param>
    /// <param name="videoDriver">The display driver name reported by the backend.</param>
    /// <param name="screenshotPath">The screenshot path requested by the caller, if any.</param>
    /// <param name="screenshotSaved">Whether a screenshot file was written.</param>
    /// <param name="diagnosticMessage">A host-level diagnostic message, or an empty string on success.</param>
    ///
    /// <remarks>
    /// The constructor stores values exactly as observed by
    /// <see cref="RuntimeHost"/>. It does not validate that a
    /// screenshot still exists on disk after the result is returned.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Use it from the thread that owns the
    /// runtime host.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    public RuntimeHostResult(
        bool succeeded,
        bool windowCreated,
        bool windowShown,
        bool framePresented,
        bool eventPumpObserved,
        int inputEventsDispatched,
        int frameCount,
        int drawCommands,
        int windowWidth,
        int windowHeight,
        int pixelWidth,
        int pixelHeight,
        string videoDriver,
        string? screenshotPath,
        bool screenshotSaved,
        string diagnosticMessage)
    {
        Succeeded = succeeded;
        WindowCreated = windowCreated;
        WindowShown = windowShown;
        FramePresented = framePresented;
        EventPumpObserved = eventPumpObserved;
        InputEventsDispatched = inputEventsDispatched;
        FrameCount = frameCount;
        DrawCommands = drawCommands;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        VideoDriver = videoDriver;
        ScreenshotPath = screenshotPath;
        ScreenshotSaved = screenshotSaved;
        DiagnosticMessage = diagnosticMessage;
    }

    /// <summary>
    /// Gets whether the run completed without a host-level failure.
    /// </summary>
    ///
    /// <value><c>true</c> on success; otherwise, <c>false</c>.</value>
    ///
    /// <remarks>
    /// User script diagnostics are reported by the scene tree and do not
    /// automatically change this value in the minimal preview host.
    /// </remarks>
    ///
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets whether the runtime window was created.
    /// </summary>
    /// <value><c>true</c> if the window was created; otherwise, <c>false</c>.</value>
    /// <remarks>This value proves that the run did not stay in a text-only path.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public bool WindowCreated { get; }

    /// <summary>
    /// Gets whether the runtime window was shown.
    /// </summary>
    /// <value><c>true</c> if the backend reported a shown window; otherwise, <c>false</c>.</value>
    /// <remarks>Some headless display drivers may create a window without showing a desktop surface.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public bool WindowShown { get; }

    /// <summary>
    /// Gets whether at least one frame was presented.
    /// </summary>
    /// <value><c>true</c> after a frame is copied to the window surface; otherwise, <c>false</c>.</value>
    /// <remarks>Reference-game acceptance checks require this value to be <c>true</c>.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public bool FramePresented { get; }

    /// <summary>
    /// Gets whether the event pump executed during the run.
    /// </summary>
    /// <value><c>true</c> when the host polled window/input events; otherwise, <c>false</c>.</value>
    /// <remarks>This is a host-level marker, not a guarantee that user input was received.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public bool EventPumpObserved { get; }

    /// <summary>
    /// Gets the number of input events dispatched into the scene tree.
    /// </summary>
    /// <value>The dispatched input event count.</value>
    /// <remarks>Automated runs may report zero when no user input was pending.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int InputEventsDispatched { get; }

    /// <summary>
    /// Gets the number of frames presented by the host.
    /// </summary>
    /// <value>The presented frame count.</value>
    /// <remarks>For a positive <see cref="RuntimeHostOptions.FrameLimit"/>, this normally equals that limit.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int FrameCount { get; }

    /// <summary>
    /// Gets the number of draw commands in the final frame.
    /// </summary>
    /// <value>The final draw command count.</value>
    /// <remarks>A value greater than zero proves that scene canvas/UI drawing reached the renderer submission path.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int DrawCommands { get; }

    /// <summary>
    /// Gets the observed logical window width.
    /// </summary>
    /// <value>The logical window width in pixels.</value>
    /// <remarks>The value is captured after the final frame.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int WindowWidth { get; }

    /// <summary>
    /// Gets the observed logical window height.
    /// </summary>
    /// <value>The logical window height in pixels.</value>
    /// <remarks>The value is captured after the final frame.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int WindowHeight { get; }

    /// <summary>
    /// Gets the observed physical pixel width.
    /// </summary>
    /// <value>The physical pixel width reported by the window backend.</value>
    /// <remarks>This can differ from <see cref="WindowWidth"/> on high-DPI displays.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int PixelWidth { get; }

    /// <summary>
    /// Gets the observed physical pixel height.
    /// </summary>
    /// <value>The physical pixel height reported by the window backend.</value>
    /// <remarks>This can differ from <see cref="WindowHeight"/> on high-DPI displays.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public int PixelHeight { get; }

    /// <summary>
    /// Gets the display driver name reported by the backend.
    /// </summary>
    /// <value>The display driver name, or <c>unknown</c> when unavailable.</value>
    /// <remarks>The value is diagnostic only and should not be used for gameplay branching.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public string VideoDriver { get; }

    /// <summary>
    /// Gets the requested screenshot path.
    /// </summary>
    /// <value>The requested path, or <c>null</c> when no screenshot was requested.</value>
    /// <remarks>Use <see cref="ScreenshotSaved"/> to check whether the file was written.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public string? ScreenshotPath { get; }

    /// <summary>
    /// Gets whether a screenshot file was written.
    /// </summary>
    /// <value><c>true</c> when <see cref="ScreenshotPath"/> was written; otherwise, <c>false</c>.</value>
    /// <remarks>Screenshot writing uses the final frame presented by the host.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public bool ScreenshotSaved { get; }

    /// <summary>
    /// Gets a host-level diagnostic message.
    /// </summary>
    /// <value>An empty string on success, or a failure summary when the host could not complete the run.</value>
    /// <remarks>The message does not include secret values or native handles.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    public string DiagnosticMessage { get; }
}
