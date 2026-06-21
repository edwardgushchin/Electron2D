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
/// Provides the Electron2D 2D camera node for selecting the visible canvas region.
/// </summary>
///
/// <remarks>
/// `Camera2D` can become current on the nearest ancestor <see cref="Viewport" />.
/// Electron2D 0.1.0 Preview implements target position, offset, zoom and
/// rotation behavior; camera limits, drag margins and smoothing are intentionally
/// not implemented yet.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate cameras on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Viewport" />
public class Camera2D : Node2D
{
    private bool enabled = true;
    private Vector2 zoom = Vector2.One;

    /// <summary>
    /// Gets or sets whether this camera can become current.
    /// </summary>
    ///
    /// <remarks>
    /// When the current camera is disabled, its viewport clears the current
    /// camera and may select another enabled camera when requested by
    /// <see cref="ClearCurrent" />.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Enabled
    {
        get
        {
            ThrowIfFreed();
            return enabled;
        }
        set
        {
            ThrowIfFreed();
            if (enabled == value)
            {
                return;
            }

            enabled = value;
            var viewport = FindViewport();
            if (viewport is null)
            {
                return;
            }

            if (!enabled)
            {
                viewport.ClearCurrentCamera(this, enableNext: true);
            }
            else if (viewport.GetCamera2D() is null)
            {
                viewport.SetCurrentCamera(this);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the camera ignores its node rotation.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool IgnoreRotation { get; set; } = true;

    /// <summary>
    /// Gets or sets the camera target offset.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the camera zoom.
    /// </summary>
    ///
    /// <remarks>
    /// Higher values zoom in. Each component must be finite and non-zero.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Zoom
    {
        get
        {
            ThrowIfFreed();
            return zoom;
        }
        set
        {
            ThrowIfFreed();
            if (!value.IsFinite() || Mathf.IsZeroApprox(value.X) || Mathf.IsZeroApprox(value.Y))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Camera zoom components must be finite and non-zero.");
            }

            zoom = value;
        }
    }

    /// <summary>
    /// Aligns the camera to the tracked position.
    /// </summary>
    ///
    /// <remarks>
    /// This is a no-op in Electron2D 0.1.0 Preview because camera smoothing and
    /// drag margins are not implemented yet.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void Align()
    {
        ThrowIfFreed();
    }

    /// <summary>
    /// Clears this camera as the current camera on its viewport.
    /// </summary>
    ///
    /// <param name="enableNext">
    /// If <c>true</c>, the viewport selects the first enabled camera in tree
    /// order after this camera is cleared.
    /// </param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void ClearCurrent(bool enableNext = true)
    {
        ThrowIfFreed();
        FindViewport()?.ClearCurrentCamera(this, enableNext);
    }

    /// <summary>
    /// Forces the camera scroll state to update immediately.
    /// </summary>
    ///
    /// <remarks>
    /// This is a no-op in Electron2D 0.1.0 Preview because smoothing state is
    /// not implemented yet.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void ForceUpdateScroll()
    {
        ThrowIfFreed();
    }

    /// <summary>
    /// Gets the screen center position in global coordinates.
    /// </summary>
    ///
    /// <returns>The camera target position for the 0.1.0 Preview baseline.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTargetPosition" />
    public Vector2 GetScreenCenterPosition()
    {
        ThrowIfFreed();
        return GetTargetPosition();
    }

    /// <summary>
    /// Gets the camera screen rotation in radians.
    /// </summary>
    ///
    /// <returns><c>0</c> when <see cref="IgnoreRotation" /> is <c>true</c>; otherwise, <see cref="Node2D.GlobalRotation" />.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float GetScreenRotation()
    {
        ThrowIfFreed();
        return IgnoreRotation ? 0f : GlobalRotation;
    }

    /// <summary>
    /// Gets the camera target position in global coordinates.
    /// </summary>
    ///
    /// <returns><see cref="Node2D.GlobalPosition" /> plus <see cref="Offset" />.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 GetTargetPosition()
    {
        ThrowIfFreed();
        return GlobalPosition + Offset;
    }

    /// <summary>
    /// Checks whether this camera is current on its viewport.
    /// </summary>
    ///
    /// <returns><c>true</c> if this camera is the active 2D camera; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool IsCurrent()
    {
        ThrowIfFreed();
        return ReferenceEquals(FindViewport()?.GetCamera2D(), this);
    }

    /// <summary>
    /// Makes this camera current on the nearest ancestor viewport.
    /// </summary>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when the camera is not a descendant of a <see cref="Viewport" />.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void MakeCurrent()
    {
        ThrowIfFreed();
        var viewport = FindViewport();
        if (viewport is null)
        {
            throw new InvalidOperationException("Camera2D must be a descendant of a Viewport before it can become current.");
        }

        viewport.SetCurrentCamera(this);
    }

    /// <summary>
    /// Resets camera smoothing state.
    /// </summary>
    ///
    /// <remarks>
    /// This is a no-op in Electron2D 0.1.0 Preview because smoothing state is
    /// not implemented yet.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void ResetSmoothing()
    {
        ThrowIfFreed();
    }

    /// <inheritdoc />
    public override void _EnterTree()
    {
        if (Enabled && FindViewport() is { } viewport && viewport.GetCamera2D() is null)
        {
            viewport.SetCurrentCamera(this);
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        FindViewport()?.ClearCurrentCamera(this, enableNext: true);
    }

    internal Transform2D GetCameraTransform(Vector2I viewportSize)
    {
        var center = new Vector2(viewportSize.X * 0.5f, viewportSize.Y * 0.5f);
        var toTarget = Transform2D.Identity.Translated(-GetTargetPosition());
        var scale = new Transform2D(new Vector2(Zoom.X, 0f), new Vector2(0f, Zoom.Y), Vector2.Zero);
        var rotation = new Transform2D(-GetScreenRotation(), Vector2.Zero);
        var toCenter = Transform2D.Identity.Translated(center);
        return toCenter * rotation * scale * toTarget;
    }

    private Viewport? FindViewport()
    {
        Node? current = this;
        while (current is not null)
        {
            if (current is Viewport viewport)
            {
                return viewport;
            }

            current = current.GetParent();
        }

        return null;
    }
}
