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

    /// <summary>
    /// Initializes a new instance of the Viewport type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Viewport" />
    ///
    public Viewport()
    {
    }

    private Camera2D? currentCamera;
    private ViewportTexture? viewportTexture;
    private Control? focusedControl;
    private Control? hoveredControl;
    private bool inputDispatchActive;
    private bool inputHandled;

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
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current size value.
    /// </value>
    ///
    /// <seealso cref="Viewport" />
    ///
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
    /// <value>
    /// The current canvas transform value.
    /// </value>
    ///
    /// <seealso cref="Viewport" />
    ///
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
    /// <value>
    /// The current snap2 dtransforms to pixel value.
    /// </value>
    ///
    /// <seealso cref="Viewport" />
    ///
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
    /// <value>
    /// The current snap2 dvertices to pixel value.
    /// </value>
    ///
    /// <seealso cref="Viewport" />
    ///
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
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Viewport" />
    ///
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
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Viewport" />
    ///
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

    /// <summary>
    /// Marks the input event currently being dispatched by this viewport as handled.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This method affects only the event that is currently moving through
    /// <see cref="SceneTree.DispatchInput(InputEvent)"/>. The process-wide
    /// <see cref="Input"/> state has already been updated before user callbacks
    /// can call this method, so action state is not rolled back.
    /// </para>
    /// <para>
    /// Calling this method when the viewport is not dispatching an input event
    /// has no effect.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it from the main scene thread
    /// while processing input for this viewport.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Control.AcceptEvent"/>
    /// <seealso cref="Node.GetViewport"/>
    public void SetInputAsHandled()
    {
        ThrowIfFreed();
        if (inputDispatchActive)
        {
            inputHandled = true;
        }
    }

    /// <summary>
    /// Gets the control currently under the GUI pointer.
    /// </summary>
    ///
    /// <returns>
    /// The last visible control hit by mouse or touch input, or <c>null</c>
    /// when no control is currently hovered.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is updated during <see cref="SceneTree.DispatchInput(InputEvent)"/>
    /// when the dispatched event carries a pointer position.
    /// </para>
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
    /// <seealso cref="Control.GetTooltip(Vector2)"/>
    public Control? GuiGetHoveredControl()
    {
        ThrowIfFreed();
        return hoveredControl is not null &&
            Object.IsInstanceValid(hoveredControl) &&
            hoveredControl.IsVisibleInTree()
            ? hoveredControl
            : null;
    }

    internal bool IsInputHandled => inputHandled;

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

    internal void BeginInputDispatch()
    {
        inputHandled = false;
        inputDispatchActive = true;
    }

    internal void EndInputDispatch()
    {
        inputDispatchActive = false;
    }

    internal void DispatchGuiInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);

        if (inputEvent is InputEventMouse mouseEvent)
        {
            var target = FindMouseTarget(this, mouseEvent.Position);
            hoveredControl = target;
            if (target is not null)
            {
                DispatchMouseGuiInput(target, inputEvent, mouseEvent);
            }

            return;
        }

        if (inputEvent is InputEventScreenTouch screenTouch)
        {
            var target = FindMouseTarget(this, screenTouch.Position);
            hoveredControl = target;
            if (target is not null)
            {
                DispatchMouseGuiInput(target, inputEvent, screenTouch);
            }

            return;
        }

        if (inputEvent is InputEventScreenDrag screenDrag)
        {
            var target = FindMouseTarget(this, screenDrag.Position);
            hoveredControl = target;
            if (target is not null)
            {
                DispatchMouseGuiInput(target, inputEvent, pointerPressEvent: null);
            }

            return;
        }

        var focusOwner = GetValidFocusOwner();
        if (focusOwner is not null)
        {
            if (TryNavigateFocus(focusOwner, inputEvent))
            {
                return;
            }

            focusOwner.DispatchGuiInput(inputEvent);
        }
    }

    internal void GrabFocus(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (control.CanReceiveFocus(this))
        {
            focusedControl = control;
        }
    }

    internal void ReleaseFocus(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (ReferenceEquals(focusedControl, control))
        {
            focusedControl = null;
        }
    }

    internal bool HasFocus(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return ReferenceEquals(GetValidFocusOwner(), control);
    }

    private void DispatchMouseGuiInput(Control target, InputEvent inputEvent, InputEvent? pointerPressEvent)
    {
        var current = target;
        while (current is not null && !inputHandled)
        {
            if (current.MouseFilter == MouseFilter.Ignore)
            {
                current = current.GetParent() as Control;
                continue;
            }

            if (IsPressEvent(pointerPressEvent) &&
                current.FocusMode is FocusMode.Click or FocusMode.All)
            {
                GrabFocus(current);
            }

            current.DispatchGuiInput(inputEvent);

            if (inputHandled)
            {
                return;
            }

            if (current.MouseFilter == MouseFilter.Stop)
            {
                inputHandled = true;
                return;
            }

            current = current.GetParent() as Control;
        }
    }

    private Control? GetValidFocusOwner()
    {
        if (focusedControl is null)
        {
            return null;
        }

        if (!Object.IsInstanceValid(focusedControl) ||
            !focusedControl.CanReceiveFocus(this))
        {
            focusedControl = null;
            return null;
        }

        return focusedControl;
    }

    private bool TryNavigateFocus(Control focusOwner, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true } keyEvent)
        {
            return false;
        }

        var target = keyEvent.Keycode switch
        {
            Key.Tab when keyEvent.ShiftPressed => focusOwner.FindPrevValidFocus(),
            Key.Tab => focusOwner.FindNextValidFocus(),
            Key.Backtab => focusOwner.FindPrevValidFocus(),
            Key.Left or Key.Up or Key.Right or Key.Down => focusOwner.FindFocusNeighborForKey(keyEvent.Keycode),
            _ => null
        };

        if (target is null || ReferenceEquals(target, focusOwner))
        {
            return false;
        }

        GrabFocus(target);
        inputHandled = true;
        return true;
    }

    private static bool IsPressEvent(InputEvent? inputEvent)
    {
        return inputEvent switch
        {
            InputEventMouseButton { Pressed: true } => true,
            InputEventScreenTouch { Pressed: true, Canceled: false } => true,
            _ => false
        };
    }

    private static Control? FindMouseTarget(Node node, Vector2 position)
    {
        if (node is CanvasItem canvasItem && !canvasItem.IsVisibleInTree())
        {
            return null;
        }

        if (node is Control { ClipContents: true } clippingControl &&
            !clippingControl.GetGlobalRect().HasPoint(position))
        {
            return null;
        }

        var children = node.GetChildrenSnapshot();
        for (var index = children.Length - 1; index >= 0; index--)
        {
            var childTarget = FindMouseTarget(children[index], position);
            if (childTarget is not null)
            {
                return childTarget;
            }
        }

        return node is Control control && control.CanReceiveMouseInput(position)
            ? control
            : null;
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
