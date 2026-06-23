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
/// Configures a visible Electron2D runtime loop.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="RuntimeHostOptions"/> is consumed by
/// <see cref="RuntimeHost.Run(Node, RuntimeHostOptions?)"/> and
/// <see cref="RuntimeHost.Run(SceneTree, RuntimeHostOptions?)"/>.
/// It describes the user-facing window and deterministic smoke-run limits
/// without exposing native handles or backend-specific objects.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are mutable and are not synchronized. Configure an instance before
/// passing it to the runtime host.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="RuntimeHost"/>
/// <seealso cref="RuntimeHostResult"/>
internal sealed class RuntimeHostOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostOptions"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The default options create a 960 by 540 window titled <c>Electron2D</c>
    /// and run until the user closes the window.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the
    /// options instance.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RuntimeHostOptions"/>
    public RuntimeHostOptions()
    {
    }

    /// <summary>
    /// Gets or sets the title shown by the visible runtime window.
    /// </summary>
    ///
    /// <value>
    /// The window title. The value must not be empty when the run starts.
    /// </value>
    ///
    /// <remarks>
    /// The title is copied when the window is created. Later mutations do not
    /// rename an already running window.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RuntimeHost"/>
    public string WindowTitle { get; set; } = "Electron2D";

    /// <summary>
    /// Gets or sets the requested logical window size.
    /// </summary>
    ///
    /// <value>
    /// A positive logical size in pixels.
    /// </value>
    ///
    /// <remarks>
    /// The runtime host presents the current frame using this logical size.
    /// Backends may expose a different physical pixel size on high-DPI displays;
    /// <see cref="RuntimeHostResult.PixelWidth"/> and
    /// <see cref="RuntimeHostResult.PixelHeight"/> report the observed value.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RuntimeHostResult.WindowWidth"/>
    /// <seealso cref="RuntimeHostResult.WindowHeight"/>
    public Vector2I WindowSize { get; set; } = new(960, 540);

    /// <summary>
    /// Gets or sets the maximum number of frames to present.
    /// </summary>
    ///
    /// <value>
    /// <c>0</c> to run until the window is closed, or a positive frame count for
    /// an automated smoke run.
    /// </value>
    ///
    /// <remarks>
    /// Reference-game scripted checks use a positive frame limit so the process
    /// can show a real window frame, capture it and exit without manual input.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int FrameLimit { get; set; }

    /// <summary>
    /// Gets or sets the fixed frame delta used by the preview runtime loop.
    /// </summary>
    ///
    /// <value>
    /// A positive finite duration in seconds.
    /// </value>
    ///
    /// <remarks>
    /// The same value is used for process and physics callbacks in the minimal
    /// 0.1.0 Preview host. More advanced variable-frame scheduling can be added
    /// without changing the current options contract.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SceneTree"/>
    public double FixedDelta { get; set; } = 1d / 60d;

    /// <summary>
    /// Gets or sets an optional PNG path for the final presented frame.
    /// </summary>
    ///
    /// <value>
    /// An absolute or relative file path, or <c>null</c> to skip screenshot
    /// writing.
    /// </value>
    ///
    /// <remarks>
    /// When set, the runtime host writes a PNG after the last presented frame.
    /// Relative paths are resolved by the current process in the normal .NET
    /// file-system manner.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RuntimeHostResult.ScreenshotPath"/>
    public string? ScreenshotPath { get; set; }

    /// <summary>
    /// Gets or sets whether pressing Escape should close the runtime loop.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to treat Escape as a close request; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Games that bind Escape to a gameplay action, such as pause or cancel,
    /// can set this property to <c>false</c> and rely on the window close button.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool QuitOnEscape { get; set; } = true;

    /// <summary>
    /// Gets or sets the color used to clear the preview frame before drawing.
    /// </summary>
    ///
    /// <value>
    /// The frame clear color.
    /// </value>
    ///
    /// <remarks>
    /// The preview rasterizer clears to this color before applying canvas draw
    /// commands. Scene content should still draw its own background for stable
    /// screenshots.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Configure it before the run starts.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Color ClearColor { get; set; } = new(0.04f, 0.05f, 0.06f, 1f);
}
