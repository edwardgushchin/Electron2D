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
/// Provides the Electron2D base node for 2D user interface controls.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Control</c> inherits from <see cref="CanvasItem"/> and adds a rectangular
/// UI area, theme font overrides, GUI input callbacks, mouse filtering and
/// focus ownership used by Electron2D UI nodes.
/// </para>
/// <para>
/// Anchors, offsets, minimum-size clamping, focus navigation and the baseline
/// input pipeline are available in this preview. Containers and full widgets
/// are implemented by later UI tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate controls on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="CanvasItem" />
/// <seealso cref="Label" />
public class Control : CanvasItem
{

    /// <summary>
    /// Initializes a new instance of the Control type.
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
    /// <seealso cref="Control" />
    ///
    public Control()
    {
    }

    private readonly Dictionary<string, Font> fontOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> fontSizeOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> themeConstantOverrides = new(StringComparer.Ordinal);
    private float anchorLeft;
    private float anchorTop;
    private float anchorRight;
    private float anchorBottom;
    private float offsetLeft;
    private float offsetTop;
    private float offsetRight;
    private float offsetBottom;
    private Vector2 customMinimumSize;
    private float sizeFlagsStretchRatio = 1f;

    /// <summary>
    /// Gets or sets the left anchor of this control.
    /// </summary>
    ///
    /// <value>
    /// The left anchor as a fraction of the parent layout size.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The final left edge is calculated as the parent layout width multiplied
    /// by this value, plus <see cref="OffsetLeft"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="OffsetLeft"/>
    /// <seealso cref="AnchorRight"/>
    public float AnchorLeft
    {
        get => anchorLeft;
        set => anchorLeft = ValidateFinite(value, nameof(AnchorLeft));
    }

    /// <summary>
    /// Gets or sets the top anchor of this control.
    /// </summary>
    ///
    /// <value>
    /// The top anchor as a fraction of the parent layout size.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The final top edge is calculated as the parent layout height multiplied
    /// by this value, plus <see cref="OffsetTop"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="OffsetTop"/>
    /// <seealso cref="AnchorBottom"/>
    public float AnchorTop
    {
        get => anchorTop;
        set => anchorTop = ValidateFinite(value, nameof(AnchorTop));
    }

    /// <summary>
    /// Gets or sets the right anchor of this control.
    /// </summary>
    ///
    /// <value>
    /// The right anchor as a fraction of the parent layout size.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The final right edge is calculated as the parent layout width multiplied
    /// by this value, plus <see cref="OffsetRight"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="OffsetRight"/>
    /// <seealso cref="AnchorLeft"/>
    public float AnchorRight
    {
        get => anchorRight;
        set => anchorRight = ValidateFinite(value, nameof(AnchorRight));
    }

    /// <summary>
    /// Gets or sets the bottom anchor of this control.
    /// </summary>
    ///
    /// <value>
    /// The bottom anchor as a fraction of the parent layout size.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The final bottom edge is calculated as the parent layout height
    /// multiplied by this value, plus <see cref="OffsetBottom"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="OffsetBottom"/>
    /// <seealso cref="AnchorTop"/>
    public float AnchorBottom
    {
        get => anchorBottom;
        set => anchorBottom = ValidateFinite(value, nameof(AnchorBottom));
    }

    /// <summary>
    /// Gets or sets the left offset of this control.
    /// </summary>
    ///
    /// <value>
    /// The pixel offset applied to <see cref="AnchorLeft"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Offsets are expressed in the local coordinate space of the parent layout
    /// rectangle.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AnchorLeft"/>
    /// <seealso cref="OffsetRight"/>
    public float OffsetLeft
    {
        get => offsetLeft;
        set => offsetLeft = ValidateFinite(value, nameof(OffsetLeft));
    }

    /// <summary>
    /// Gets or sets the top offset of this control.
    /// </summary>
    ///
    /// <value>
    /// The pixel offset applied to <see cref="AnchorTop"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Offsets are expressed in the local coordinate space of the parent layout
    /// rectangle.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AnchorTop"/>
    /// <seealso cref="OffsetBottom"/>
    public float OffsetTop
    {
        get => offsetTop;
        set => offsetTop = ValidateFinite(value, nameof(OffsetTop));
    }

    /// <summary>
    /// Gets or sets the right offset of this control.
    /// </summary>
    ///
    /// <value>
    /// The pixel offset applied to <see cref="AnchorRight"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The right offset can be negative when the right edge should stay inset
    /// from the parent layout rectangle.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AnchorRight"/>
    /// <seealso cref="OffsetLeft"/>
    public float OffsetRight
    {
        get => offsetRight;
        set => offsetRight = ValidateFinite(value, nameof(OffsetRight));
    }

    /// <summary>
    /// Gets or sets the bottom offset of this control.
    /// </summary>
    ///
    /// <value>
    /// The pixel offset applied to <see cref="AnchorBottom"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The bottom offset can be negative when the bottom edge should stay inset
    /// from the parent layout rectangle.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AnchorBottom"/>
    /// <seealso cref="OffsetTop"/>
    public float OffsetBottom
    {
        get => offsetBottom;
        set => offsetBottom = ValidateFinite(value, nameof(OffsetBottom));
    }

    /// <summary>
    /// Gets or sets the local position of this control.
    /// </summary>
    ///
    /// <value>
    /// The top-left corner of the computed local rectangle.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Setting this property updates offsets while preserving anchors and the
    /// current computed <see cref="Size"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetRect"/>
    /// <seealso cref="GetGlobalRect"/>
    public Vector2 Position
    {
        get => GetLocalRect().Position;
        set => SetPositionCore(value, Size);
    }

    /// <summary>
    /// Gets or sets the local size of this control.
    /// </summary>
    ///
    /// <value>
    /// The computed local rectangle size.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Setting this property is equivalent to calling
    /// <see cref="SetSize(Vector2)"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetSize(Vector2)"/>
    /// <seealso cref="GetCombinedMinimumSize"/>
    public Vector2 Size
    {
        get => GetLocalRect().Size;
        set => SetSize(value);
    }

    /// <summary>
    /// Gets or sets the custom minimum size of this control.
    /// </summary>
    ///
    /// <value>
    /// The user-defined minimum size. Components must be finite and
    /// non-negative.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This value is combined with <see cref="_GetMinimumSize"/> by
    /// <see cref="GetMinimumSize"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a component is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="_GetMinimumSize"/>
    /// <seealso cref="GetCombinedMinimumSize"/>
    public Vector2 CustomMinimumSize
    {
        get => customMinimumSize;
        set => customMinimumSize = ValidateMinimumSize(value, nameof(CustomMinimumSize));
    }

    /// <summary>
    /// Gets or sets the horizontal grow direction used when minimum size expands this control.
    /// </summary>
    ///
    /// <value>
    /// The horizontal <see cref="GrowDirection"/>. The default is
    /// <see cref="GrowDirection.End"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This value is used by <see cref="SetSize(Vector2)"/> and
    /// <see cref="ResetSize"/> when the requested width is smaller than the
    /// minimum width.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GrowVertical"/>
    /// <seealso cref="GrowDirection"/>
    public GrowDirection GrowHorizontal { get; set; } = GrowDirection.End;

    /// <summary>
    /// Gets or sets the vertical grow direction used when minimum size expands this control.
    /// </summary>
    ///
    /// <value>
    /// The vertical <see cref="GrowDirection"/>. The default is
    /// <see cref="GrowDirection.End"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This value is used by <see cref="SetSize(Vector2)"/> and
    /// <see cref="ResetSize"/> when the requested height is smaller than the
    /// minimum height.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GrowHorizontal"/>
    /// <seealso cref="GrowDirection"/>
    public GrowDirection GrowVertical { get; set; } = GrowDirection.End;

    /// <summary>
    /// Gets or sets the horizontal size flags used by a parent <see cref="Container"/>.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="SizeFlags"/> value that describes how this control should
    /// use horizontal space allocated by a container.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This property is read by layout containers only. It does not change this
    /// control's rectangle until a parent container performs layout.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SizeFlagsVertical"/>
    /// <seealso cref="SizeFlagsStretchRatio"/>
    /// <seealso cref="Container"/>
    public SizeFlags SizeFlagsHorizontal { get; set; } = SizeFlags.Fill;

    /// <summary>
    /// Gets or sets the vertical size flags used by a parent <see cref="Container"/>.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="SizeFlags"/> value that describes how this control should
    /// use vertical space allocated by a container.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This property is read by layout containers only. It does not change this
    /// control's rectangle until a parent container performs layout.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SizeFlagsHorizontal"/>
    /// <seealso cref="SizeFlagsStretchRatio"/>
    /// <seealso cref="Container"/>
    public SizeFlags SizeFlagsVertical { get; set; } = SizeFlags.Fill;

    /// <summary>
    /// Gets or sets the expansion weight used by a parent <see cref="Container"/>.
    /// </summary>
    ///
    /// <value>
    /// The positive finite ratio used when a container distributes free space
    /// between expanding children.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is considered only when the relevant size flag contains
    /// <see cref="SizeFlags.Expand"/> or <see cref="SizeFlags.ExpandFill"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is not finite or is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SizeFlagsHorizontal"/>
    /// <seealso cref="SizeFlagsVertical"/>
    public float SizeFlagsStretchRatio
    {
        get => sizeFlagsStretchRatio;
        set
        {
            if (!Mathf.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Stretch ratio must be finite and greater than zero.");
            }

            sizeFlagsStretchRatio = value;
        }
    }

    /// <summary>
    /// Gets or sets whether this control clips GUI hit-testing for its descendants.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to prevent descendants from receiving pointer input outside
    /// this control's global rectangle; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview baseline applies this flag to mouse and touch
    /// dispatch. Renderer scissor integration is handled by later rendering
    /// backend work.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetGlobalRect"/>
    /// <seealso cref="MouseFilter"/>
    public bool ClipContents { get; set; }

    /// <summary>
    /// Gets or sets how this control receives and consumes mouse input.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="MouseFilter"/> value. The default is
    /// <see cref="MouseFilter.Stop"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The root <see cref="Viewport"/> reads this property while routing
    /// <see cref="InputEventMouse"/> events to <see cref="_GuiInput(InputEvent)"/>.
    /// </para>
    /// <para>
    /// <see cref="MouseFilter.Stop"/> handles the event after this control
    /// receives it. <see cref="MouseFilter.Pass"/> lets unhandled events bubble
    /// to the parent control. <see cref="MouseFilter.Ignore"/> skips this
    /// control for mouse hit-testing.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="MouseFilter"/>
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public MouseFilter MouseFilter { get; set; } = MouseFilter.Stop;

    /// <summary>
    /// Gets or sets how this control can receive focus.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="Electron2D.FocusMode"/> value. The default is
    /// <see cref="Electron2D.FocusMode.None"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Focus is owned by the nearest <see cref="Viewport"/>. Only one visible
    /// control inside that viewport can report <see cref="HasFocus"/> at a
    /// time.
    /// </para>
    /// <para>
    /// <see cref="Electron2D.FocusMode.Click"/> and
    /// <see cref="Electron2D.FocusMode.All"/> allow mouse press events to focus
    /// this control before <see cref="_GuiInput(InputEvent)"/> is called.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusMode"/>
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="HasFocus"/>
    public FocusMode FocusMode { get; set; } = FocusMode.None;

    /// <summary>
    /// Gets or sets the explicit focus target used when navigating to the next control.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="NodePath"/> resolved from this control. The default value is
    /// an empty path.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="FindNextValidFocus"/> uses this path before falling back to
    /// viewport tree order.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusPrevious"/>
    /// <seealso cref="FindNextValidFocus"/>
    public NodePath FocusNext { get; set; }

    /// <summary>
    /// Gets or sets the explicit focus target used when navigating to the previous control.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="NodePath"/> resolved from this control. The default value is
    /// an empty path.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="FindPrevValidFocus"/> uses this path before falling back to
    /// viewport tree order.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusNext"/>
    /// <seealso cref="FindPrevValidFocus"/>
    public NodePath FocusPrevious { get; set; }

    /// <summary>
    /// Gets or sets the explicit focus target used for left-direction navigation.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="NodePath"/> resolved from this control. The default value is
    /// an empty path.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Directional focus paths are used by the viewport keyboard navigation
    /// baseline when arrow-key navigation is requested.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusNeighborRight"/>
    public NodePath FocusNeighborLeft { get; set; }

    /// <summary>
    /// Gets or sets the explicit focus target used for top-direction navigation.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="NodePath"/> resolved from this control. The default value is
    /// an empty path.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Directional focus paths are used by the viewport keyboard navigation
    /// baseline when arrow-key navigation is requested.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusNeighborBottom"/>
    public NodePath FocusNeighborTop { get; set; }

    /// <summary>
    /// Gets or sets the explicit focus target used for right-direction navigation.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="NodePath"/> resolved from this control. The default value is
    /// an empty path.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Directional focus paths are used by the viewport keyboard navigation
    /// baseline when arrow-key navigation is requested.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusNeighborLeft"/>
    public NodePath FocusNeighborRight { get; set; }

    /// <summary>
    /// Gets or sets the explicit focus target used for bottom-direction navigation.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="NodePath"/> resolved from this control. The default value is
    /// an empty path.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Directional focus paths are used by the viewport keyboard navigation
    /// baseline when arrow-key navigation is requested.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusNeighborTop"/>
    public NodePath FocusNeighborBottom { get; set; }

    /// <summary>
    /// Called when a GUI input event is delivered to this control.
    /// </summary>
    ///
    /// <param name="inputEvent">
    /// The input event delivered by the containing <see cref="Viewport"/>.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// Mouse events reach this method when the event position falls inside the
    /// control rectangle and <see cref="MouseFilter"/> is not
    /// <see cref="MouseFilter.Ignore"/>. Non-mouse events reach only the
    /// currently focused control.
    /// </para>
    /// <para>
    /// Call <see cref="AcceptEvent"/> to stop further propagation of the
    /// current event.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called on the main scene thread
    /// during input dispatch.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AcceptEvent"/>
    /// <seealso cref="MouseFilter"/>
    /// <seealso cref="FocusMode"/>
    public virtual void _GuiInput(InputEvent inputEvent)
    {
    }

    /// <summary>
    /// Called to compute the control-specific minimum size.
    /// </summary>
    ///
    /// <returns>
    /// The minimum size requested by this control implementation.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Derived controls can override this method to reserve space for text,
    /// textures or other content. Negative or non-finite components returned
    /// by an override are treated as <c>0</c>.
    /// </para>
    /// <para>
    /// User code should normally call <see cref="GetMinimumSize"/> or
    /// <see cref="GetCombinedMinimumSize"/> instead of calling this method
    /// directly.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called on the main scene thread
    /// during layout calculations.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CustomMinimumSize"/>
    /// <seealso cref="GetMinimumSize"/>
    public virtual Vector2 _GetMinimumSize()
    {
        return Vector2.Zero;
    }

    /// <summary>
    /// Gets the local rectangle occupied by this control.
    /// </summary>
    ///
    /// <returns>
    /// A <see cref="Rect2"/> containing <see cref="Position"/> and
    /// <see cref="Size"/>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The rectangle is computed from anchors and offsets against the current
    /// parent layout size.
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
    /// <seealso cref="GetGlobalRect"/>
    /// <seealso cref="AnchorLeft"/>
    public Rect2 GetRect()
    {
        ThrowIfFreed();
        return GetLocalRect();
    }

    /// <summary>
    /// Gets the rectangle occupied by this control in root viewport coordinates.
    /// </summary>
    ///
    /// <returns>
    /// A <see cref="Rect2"/> containing the global position and computed
    /// <see cref="Size"/>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Parent <see cref="Control"/> positions are accumulated. A
    /// <see cref="Node2D"/> parent applies its global transform to the local
    /// position baseline.
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
    /// <seealso cref="GetRect"/>
    /// <seealso cref="Position"/>
    public Rect2 GetGlobalRect()
    {
        ThrowIfFreed();
        return new Rect2(GlobalPosition, Size);
    }

    /// <summary>
    /// Gets the minimum size requested by this control.
    /// </summary>
    ///
    /// <returns>
    /// The component-wise maximum of <see cref="CustomMinimumSize"/> and
    /// <see cref="_GetMinimumSize"/>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// This method is the minimum-size baseline used before container and theme
    /// contributions are introduced.
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
    /// <seealso cref="_GetMinimumSize"/>
    /// <seealso cref="GetCombinedMinimumSize"/>
    public Vector2 GetMinimumSize()
    {
        ThrowIfFreed();
        return CustomMinimumSize.Max(SanitizeMinimumSize(_GetMinimumSize()));
    }

    /// <summary>
    /// Gets the minimum size used by layout calculations.
    /// </summary>
    ///
    /// <returns>
    /// The combined minimum size for this control.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// In the 0.1.0 Preview layout baseline this is the same value as
    /// <see cref="GetMinimumSize"/>. Future container and theme work can add
    /// style contributions without changing the public call site.
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
    /// <seealso cref="GetMinimumSize"/>
    /// <seealso cref="SetSize(Vector2)"/>
    public Vector2 GetCombinedMinimumSize()
    {
        ThrowIfFreed();
        return GetMinimumSize();
    }

    /// <summary>
    /// Sets the computed size of this control.
    /// </summary>
    ///
    /// <param name="size">
    /// The requested size in local coordinates.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// If <paramref name="size"/> is smaller than
    /// <see cref="GetCombinedMinimumSize"/>, this method grows the final
    /// rectangle according to <see cref="GrowHorizontal"/> and
    /// <see cref="GrowVertical"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a component of <paramref name="size"/> is negative or not
    /// finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Size"/>
    /// <seealso cref="ResetSize"/>
    public void SetSize(Vector2 size)
    {
        ThrowIfFreed();
        var requestedSize = ValidateMinimumSize(size, nameof(size));
        var currentPosition = Position;
        var minimumSize = GetCombinedMinimumSize();
        var finalSize = requestedSize.Max(minimumSize);
        var adjustedPosition = new Vector2(
            AdjustPositionForGrowth(currentPosition.X, requestedSize.X, finalSize.X, GrowHorizontal),
            AdjustPositionForGrowth(currentPosition.Y, requestedSize.Y, finalSize.Y, GrowVertical));

        SetPositionCore(adjustedPosition, finalSize);
    }

    /// <summary>
    /// Resets this control size to its combined minimum size.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The current <see cref="GrowHorizontal"/> and <see cref="GrowVertical"/>
    /// values decide whether the beginning side, ending side or both sides move
    /// while the rectangle changes.
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
    /// <seealso cref="SetSize(Vector2)"/>
    /// <seealso cref="GetCombinedMinimumSize"/>
    public void ResetSize()
    {
        ThrowIfFreed();
        SetSize(GetCombinedMinimumSize());
    }

    /// <summary>
    /// Finds the next focusable control in this viewport.
    /// </summary>
    ///
    /// <returns>
    /// The explicit <see cref="FocusNext"/> target when it is valid; otherwise,
    /// the next focusable control in viewport tree order, or <c>null</c> when no
    /// valid control exists.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Controls are valid focus targets only when they are inside the same
    /// <see cref="Viewport"/>, visible in tree and have <see cref="FocusMode"/>
    /// set to a value other than <see cref="Electron2D.FocusMode.None"/>.
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
    /// <seealso cref="FindPrevValidFocus"/>
    /// <seealso cref="FocusNext"/>
    public Control? FindNextValidFocus()
    {
        ThrowIfFreed();
        return FindExplicitFocus(FocusNext) ?? FindFocusByTreeOrder(forward: true);
    }

    /// <summary>
    /// Finds the previous focusable control in this viewport.
    /// </summary>
    ///
    /// <returns>
    /// The explicit <see cref="FocusPrevious"/> target when it is valid;
    /// otherwise, the previous focusable control in viewport tree order, or
    /// <c>null</c> when no valid control exists.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Controls are valid focus targets only when they are inside the same
    /// <see cref="Viewport"/>, visible in tree and have <see cref="FocusMode"/>
    /// set to a value other than <see cref="Electron2D.FocusMode.None"/>.
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
    /// <seealso cref="FindNextValidFocus"/>
    /// <seealso cref="FocusPrevious"/>
    public Control? FindPrevValidFocus()
    {
        ThrowIfFreed();
        return FindExplicitFocus(FocusPrevious) ?? FindFocusByTreeOrder(forward: false);
    }

    /// <summary>
    /// Marks the current GUI input event as handled.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This method forwards to <see cref="Viewport.SetInputAsHandled"/> on the
    /// containing viewport. It has no effect when this control is outside a
    /// scene tree or when no input event is currently being dispatched.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread while
    /// handling <see cref="_GuiInput(InputEvent)"/>.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="_GuiInput(InputEvent)"/>
    /// <seealso cref="Viewport.SetInputAsHandled"/>
    public void AcceptEvent()
    {
        ThrowIfFreed();
        GetViewport()?.SetInputAsHandled();
    }

    /// <summary>
    /// Gives keyboard and gamepad focus to this control.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The control must be inside a scene tree, visible in that tree and have
    /// <see cref="FocusMode"/> set to a value other than
    /// <see cref="Electron2D.FocusMode.None"/>. If any of those conditions is
    /// not met, the call has no effect.
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
    /// <seealso cref="ReleaseFocus"/>
    /// <seealso cref="HasFocus"/>
    /// <seealso cref="FocusMode"/>
    public void GrabFocus()
    {
        ThrowIfFreed();
        GetViewport()?.GrabFocus(this);
    }

    /// <summary>
    /// Releases focus from this control when it currently owns it.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Calling this method on a control that does not currently own focus is a
    /// no-op.
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
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="HasFocus"/>
    public void ReleaseFocus()
    {
        ThrowIfFreed();
        GetViewport()?.ReleaseFocus(this);
    }

    /// <summary>
    /// Checks whether this control currently owns focus in its viewport.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when this control is the focused visible control in its
    /// viewport; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Hidden controls and controls outside a scene tree do not report focus,
    /// even if they were the last control selected before becoming invalid for
    /// focus dispatch.
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
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="ReleaseFocus"/>
    public bool HasFocus()
    {
        ThrowIfFreed();
        return GetViewport()?.HasFocus(this) == true;
    }

    /// <summary>
    /// Adds or replaces a font theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme font name, for example <c>font</c>.</param>
    /// <param name="font">The font resource to use.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font" /> is <c>null</c>.
    /// </exception>
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
    /// <seealso cref="Control" />
    ///
    public void AddThemeFontOverride(string name, Font font)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(font);

        fontOverrides[name] = font;
        QueueRedraw();
    }

    /// <summary>
    /// Gets a font theme override by name.
    /// </summary>
    ///
    /// <param name="name">The theme font name.</param>
    /// <returns>The overridden font, or <c>null</c> when no override exists.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
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
    /// <seealso cref="Control" />
    ///
    public Font? GetThemeFont(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return fontOverrides.TryGetValue(name, out var font) ? font : null;
    }

    /// <summary>
    /// Adds or replaces a font size theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme font size name, for example <c>font_size</c>.</param>
    /// <param name="fontSize">The font size in pixels. It must be greater than zero.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fontSize" /> is less than or equal to zero.
    /// </exception>
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
    /// <seealso cref="Control" />
    ///
    public void AddThemeFontSizeOverride(string name, int fontSize)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Font size must be greater than zero.");
        }

        fontSizeOverrides[name] = fontSize;
        QueueRedraw();
    }

    /// <summary>
    /// Gets a font size theme override by name.
    /// </summary>
    ///
    /// <param name="name">The theme font size name.</param>
    /// <returns>The overridden font size in pixels, or <c>16</c> when no override exists.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
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
    /// <seealso cref="Control" />
    ///
    public int GetThemeFontSize(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return fontSizeOverrides.TryGetValue(name, out var fontSize) ? fontSize : 16;
    }

    /// <summary>
    /// Adds or replaces an integer theme constant override for this control.
    /// </summary>
    ///
    /// <param name="name">
    /// The theme constant name, for example <c>separation</c> or
    /// <c>margin_left</c>.
    /// </param>
    ///
    /// <param name="constant">
    /// The non-negative constant value in pixels.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview UI containers use this baseline for spacing and
    /// margins before full theme resources are introduced.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="constant"/> is less than zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetThemeConstant"/>
    /// <seealso cref="HasThemeConstantOverride"/>
    /// <seealso cref="Container"/>
    public void AddThemeConstantOverride(string name, int constant)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (constant < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(constant), constant, "Theme constant must be non-negative.");
        }

        themeConstantOverrides[name] = constant;
        QueueRedraw();
    }

    /// <summary>
    /// Gets an integer theme constant override by name.
    /// </summary>
    ///
    /// <param name="name">
    /// The theme constant name.
    /// </param>
    ///
    /// <returns>
    /// The overridden constant value, or <c>0</c> when no override exists.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Containers use this method for spacing and margin values. Missing values
    /// deliberately resolve to zero so a caller can supply its own default.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddThemeConstantOverride"/>
    /// <seealso cref="HasThemeConstantOverride"/>
    public int GetThemeConstant(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return themeConstantOverrides.TryGetValue(name, out var constant) ? constant : 0;
    }

    /// <summary>
    /// Reports whether this control has a theme constant override by name.
    /// </summary>
    ///
    /// <param name="name">
    /// The theme constant name.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when an override exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// This method lets container code distinguish a missing value from an
    /// explicit zero value.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddThemeConstantOverride"/>
    /// <seealso cref="GetThemeConstant"/>
    public bool HasThemeConstantOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return themeConstantOverrides.ContainsKey(name);
    }

    internal int GetThemeConstantOrDefault(string name, int defaultValue)
    {
        return themeConstantOverrides.TryGetValue(name, out var constant) ? constant : defaultValue;
    }

    internal Vector2 GlobalPosition
    {
        get
        {
            ThrowIfFreed();
            return GetParent() switch
            {
                Control parentControl => parentControl.GlobalPosition + Position,
                Node2D parentNode2D => parentNode2D.GlobalTransform.Xform(Position),
                _ => Position
            };
        }
    }

    internal Transform2D GlobalTransform => new(Vector2.Right, Vector2.Down, GlobalPosition);

    internal bool CanReceiveMouseInput(Vector2 globalPosition)
    {
        ThrowIfFreed();
        return MouseFilter != MouseFilter.Ignore &&
            Size.X > 0f &&
            Size.Y > 0f &&
            IsVisibleInTree() &&
            GetGlobalRect().HasPoint(globalPosition);
    }

    internal bool CanReceiveFocus(Viewport viewport)
    {
        ThrowIfFreed();
        return FocusMode != FocusMode.None &&
            IsInsideTree() &&
            IsVisibleInTree() &&
            ReferenceEquals(GetViewport(), viewport);
    }

    internal void DispatchGuiInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        GetTree()?.InvokeUserCallback(this, nameof(_GuiInput), () => _GuiInput(inputEvent));
    }

    internal Control? FindFocusNeighborForKey(Key key)
    {
        return key switch
        {
            Key.Left => FindExplicitFocus(FocusNeighborLeft),
            Key.Up => FindExplicitFocus(FocusNeighborTop),
            Key.Right => FindExplicitFocus(FocusNeighborRight),
            Key.Down => FindExplicitFocus(FocusNeighborBottom),
            _ => null
        };
    }

    private Rect2 GetLocalRect()
    {
        var parentSize = GetParentLayoutSize();
        var left = (parentSize.X * anchorLeft) + offsetLeft;
        var top = (parentSize.Y * anchorTop) + offsetTop;
        var right = (parentSize.X * anchorRight) + offsetRight;
        var bottom = (parentSize.Y * anchorBottom) + offsetBottom;
        return new Rect2(new Vector2(left, top), new Vector2(right - left, bottom - top));
    }

    private Vector2 GetParentLayoutSize()
    {
        return GetParent() switch
        {
            Control control => control.Size,
            Viewport viewport => new Vector2(viewport.Size.X, viewport.Size.Y),
            _ => GetViewport() is { } viewport ? new Vector2(viewport.Size.X, viewport.Size.Y) : Vector2.Zero
        };
    }

    private void SetPositionCore(Vector2 position, Vector2 size)
    {
        ValidateFinite(position.X, nameof(position));
        ValidateFinite(position.Y, nameof(position));
        ValidateMinimumSize(size, nameof(size));

        var parentSize = GetParentLayoutSize();
        offsetLeft = position.X - (parentSize.X * anchorLeft);
        offsetTop = position.Y - (parentSize.Y * anchorTop);
        offsetRight = position.X + size.X - (parentSize.X * anchorRight);
        offsetBottom = position.Y + size.Y - (parentSize.Y * anchorBottom);
    }

    private Control? FindExplicitFocus(NodePath path)
    {
        if (path.IsEmpty())
        {
            return null;
        }

        var viewport = GetViewport();
        if (viewport is null)
        {
            return null;
        }

        return GetNodeOrNull(path) is Control control && control.CanReceiveFocus(viewport)
            ? control
            : null;
    }

    private Control? FindFocusByTreeOrder(bool forward)
    {
        var viewport = GetViewport();
        if (viewport is null)
        {
            return null;
        }

        var controls = new List<Control>();
        CollectFocusableControls(viewport, viewport, controls);
        if (controls.Count == 0)
        {
            return null;
        }

        var index = controls.FindIndex(control => ReferenceEquals(control, this));
        if (index < 0)
        {
            return controls[0];
        }

        var nextIndex = forward
            ? (index + 1) % controls.Count
            : (index - 1 + controls.Count) % controls.Count;
        return controls[nextIndex];
    }

    private static void CollectFocusableControls(Node node, Viewport viewport, List<Control> controls)
    {
        if (node is Control control && control.CanReceiveFocus(viewport))
        {
            controls.Add(control);
        }

        foreach (var child in node.GetChildrenSnapshot())
        {
            CollectFocusableControls(child, viewport, controls);
        }
    }

    private static Vector2 SanitizeMinimumSize(Vector2 size)
    {
        return new Vector2(SanitizeMinimumComponent(size.X), SanitizeMinimumComponent(size.Y));
    }

    private static Vector2 ValidateMinimumSize(Vector2 size, string parameterName)
    {
        if (!Mathf.IsFinite(size.X) || size.X < 0f ||
            !Mathf.IsFinite(size.Y) || size.Y < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Size components must be finite and non-negative.");
        }

        return size;
    }

    private static float SanitizeMinimumComponent(float value)
    {
        return Mathf.IsFinite(value) && value > 0f ? value : 0f;
    }

    private static float AdjustPositionForGrowth(
        float position,
        float requestedSize,
        float finalSize,
        GrowDirection growDirection)
    {
        var extraSize = finalSize - requestedSize;
        return growDirection switch
        {
            GrowDirection.Begin => position - extraSize,
            GrowDirection.Both => position - (extraSize / 2f),
            _ => position
        };
    }

    private static float ValidateFinite(float value, string parameterName)
    {
        if (!Mathf.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
        }

        return value;
    }
}
