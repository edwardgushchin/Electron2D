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
/// Identifies how a <see cref="Node" /> participates in scene-tree callbacks
/// while its <see cref="SceneTree" /> is paused.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="ProcessMode" /> is evaluated by the scene tree before invoking
/// <see cref="Node._Process(double)" />, <see cref="Node._PhysicsProcess(double)" />
/// and <see cref="Node._Input(InputEvent)" />. Drawing still runs for visible
/// canvas items so a paused menu can be rendered.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This enum is immutable and can be read from any thread. Apply enum values to
/// nodes on the scene thread that owns those nodes.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Node.ProcessMode" />
/// <seealso cref="SceneTree.Paused" />
public enum ProcessMode
{
    /// <summary>
    /// Inherits the effective process mode from the nearest ancestor.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// A node without an ancestor that supplies a concrete mode falls back to
    /// <see cref="Pausable" />.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Inherit = 0,

    /// <summary>
    /// Processes only when the owning <see cref="SceneTree" /> is not paused.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Use this mode for normal gameplay nodes that should stop while pause
    /// menus are open.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Pausable = 1,

    /// <summary>
    /// Processes only while the owning <see cref="SceneTree" /> is paused.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Use this mode for pause-menu roots and controls that must stay
    /// interactive while gameplay is paused.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This value is available since Electron2D 0.1.0 Preview.
    /// </since>
    WhenPaused = 2,

    /// <summary>
    /// Processes regardless of the owning <see cref="SceneTree" /> pause state.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Use this mode for nodes that must keep receiving callbacks in both
    /// gameplay and paused states.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Always = 3,

    /// <summary>
    /// Does not receive process, physics-process or input callbacks.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Child nodes may still process if they use their own concrete mode such
    /// as <see cref="Always" /> or <see cref="WhenPaused" />.
    /// </para>
    /// </remarks>
    ///
    /// <since>
    /// This value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Disabled = 4
}
