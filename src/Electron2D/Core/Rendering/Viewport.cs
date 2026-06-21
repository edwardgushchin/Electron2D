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
/// Provides the Electron2D viewport node for 2D camera selection and canvas transforms.
/// </summary>
///
/// <remarks>
/// Electron2D 0.1.0 Preview uses `Viewport` as the concrete root node created
/// by <see cref="SceneTree" />. The public `SceneTree.Root` property remains
/// typed as <see cref="Node" /> for the current object-model baseline.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate viewports on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Camera2D" />
public class Viewport : Node
{
    private Camera2D? currentCamera;
    private ViewportTexture? viewportTexture;

    /// <summary>
    /// Gets or sets the viewport size in pixels.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2I Size { get; set; }

    /// <summary>
    /// Gets or sets the base transform applied to 2D canvas submission.
    /// </summary>
    ///
    /// <remarks>
    /// The active <see cref="Camera2D" /> transform is applied after this
    /// transform when internal sprite submission is built.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Transform2D CanvasTransform { get; set; } = Transform2D.Identity;

    /// <summary>
    /// Gets or sets whether submitted 2D transforms snap to full pixels.
    /// </summary>
    ///
    /// <remarks>
    /// Snapping affects internal render commands only. It does not mutate node
    /// transforms.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Snap2DTransformsToPixel { get; set; }

    /// <summary>
    /// Gets or sets whether submitted 2D destination rectangles snap to full pixels.
    /// </summary>
    ///
    /// <remarks>
    /// This is the command-level baseline for future GPU vertex snapping.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Snap2DVerticesToPixel { get; set; }

    /// <summary>
    /// Gets the current active 2D camera.
    /// </summary>
    ///
    /// <returns>The current <see cref="Camera2D" />, or <c>null</c> when no camera is current.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Camera2D? GetCamera2D()
    {
        ThrowIfFreed();
        return currentCamera;
    }

    /// <summary>
    /// Gets the visible viewport rectangle.
    /// </summary>
    ///
    /// <returns>A rectangle starting at <see cref="Vector2.Zero" /> with the current viewport size.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Rect2 GetVisibleRect()
    {
        ThrowIfFreed();
        return new Rect2(0f, 0f, Size.X, Size.Y);
    }

    /// <summary>
    /// Gets the dynamic texture that represents this viewport.
    /// </summary>
    ///
    /// <returns>
    /// A <see cref="ViewportTexture" /> bound to this viewport. The same
    /// instance is returned on later calls.
    /// </returns>
    /// <remarks>
    /// The returned texture reflects the current <see cref="Size" /> when its
    /// metadata is queried. Electron2D 0.1.0 Preview exposes the Electron2D
    /// resource object before exposing public image readback or GPU handles.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ViewportTexture" />
    public ViewportTexture GetTexture()
    {
        ThrowIfFreed();
        viewportTexture ??= new ViewportTexture(this);
        return viewportTexture;
    }

    internal void SetCurrentCamera(Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        currentCamera = camera;
    }

    internal void ClearCurrentCamera(Camera2D camera, bool enableNext)
    {
        if (!ReferenceEquals(currentCamera, camera))
        {
            return;
        }

        currentCamera = null;
        if (enableNext)
        {
            currentCamera = FindFirstEnabledCamera(this, camera);
        }
    }

    internal Transform2D GetFinalCanvasTransform()
    {
        var cameraTransform = currentCamera is null ? Transform2D.Identity : currentCamera.GetCameraTransform(Size);
        return CanvasTransform * cameraTransform;
    }

    private static Camera2D? FindFirstEnabledCamera(Node node, Camera2D exclude)
    {
        foreach (var child in node.GetChildrenSnapshot())
        {
            if (!ReferenceEquals(child, exclude) && child is Camera2D { Enabled: true } camera)
            {
                return camera;
            }

            var descendant = FindFirstEnabledCamera(child, exclude);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
