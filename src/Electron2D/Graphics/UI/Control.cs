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
/// This type is available since Electron2D 0.1-preview.
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
    /// This API is available since Electron2D 0.1-preview.
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
    private readonly Dictionary<string, Color> colorOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> iconOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StyleBox> styleBoxOverrides = new(StringComparer.Ordinal);
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
    private Theme? theme;
    private string themeTypeVariation = string.Empty;
    private string tooltipText = string.Empty;

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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// The 0.1-preview baseline applies this flag to mouse and touch
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="FocusMode"/>
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="HasFocus"/>
    public FocusMode FocusMode { get; set; } = FocusMode.None;

    /// <summary>
    /// Gets or sets the theme resource applied to this control branch.
    /// </summary>
    ///
    /// <value>
    /// The theme resource assigned to this control, or <c>null</c> when this
    /// control should inherit a theme from its parent controls.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// A theme assigned to a control is used by that control and by descendant
    /// controls while the parent chain remains made of controls.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Theme"/>
    /// <seealso cref="GetThemeColor(string, string)"/>
    public Theme? Theme
    {
        get
        {
            ThrowIfFreed();
            return theme;
        }
        set
        {
            ThrowIfFreed();
            theme = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the theme type variation used before this control type.
    /// </summary>
    ///
    /// <value>
    /// The variation name, or an empty string when no variation is used.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// A non-empty value gives a theme a way to style one control differently
    /// from other controls of the same runtime type.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Theme"/>
    public string ThemeTypeVariation
    {
        get
        {
            ThrowIfFreed();
            return themeTypeVariation;
        }
        set
        {
            ThrowIfFreed();
            themeTypeVariation = value ?? string.Empty;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the default tooltip text for this control.
    /// </summary>
    ///
    /// <value>
    /// The tooltip text. A <c>null</c> assignment is normalized to an empty
    /// string.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Override <see cref="_GetTooltip(Vector2)"/> when the text depends on
    /// the pointer position.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTooltip(Vector2)"/>
    public string TooltipText
    {
        get
        {
            ThrowIfFreed();
            return tooltipText;
        }
        set
        {
            ThrowIfFreed();
            tooltipText = value ?? string.Empty;
        }
    }

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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This property is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AcceptEvent"/>
    /// <seealso cref="MouseFilter"/>
    /// <seealso cref="FocusMode"/>
    public virtual void _GuiInput(InputEvent inputEvent)
    {
    }

    /// <summary>
    /// Called to resolve tooltip text for a local pointer position.
    /// </summary>
    ///
    /// <param name="atPosition">
    /// The pointer position in this control's local coordinate space.
    /// </param>
    ///
    /// <returns>
    /// The tooltip text for the position, or an empty string when no tooltip
    /// should be shown.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The base implementation returns <see cref="TooltipText"/>. Derived
    /// controls can return different text for different subregions.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTooltip(Vector2)"/>
    /// <seealso cref="_MakeCustomTooltip(string)"/>
    public virtual string _GetTooltip(Vector2 atPosition)
    {
        return TooltipText;
    }

    /// <summary>
    /// Called to create a custom tooltip control for resolved text.
    /// </summary>
    ///
    /// <param name="forText">
    /// The tooltip text returned by <see cref="GetTooltip(Vector2)"/>.
    /// </param>
    ///
    /// <returns>
    /// A control used as a custom tooltip, or <c>null</c> to use the default
    /// tooltip presentation.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The base implementation returns <c>null</c>. Callers that show tooltip
    /// visuals own the returned control's lifetime.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTooltip(Vector2)"/>
    public virtual Control? _MakeCustomTooltip(string forText)
    {
        ArgumentNullException.ThrowIfNull(forText);
        return null;
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// In the 0.1-preview layout baseline this is the same value as
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// Gets tooltip text for a local pointer position.
    /// </summary>
    ///
    /// <param name="atPosition">The pointer position in this control's local coordinate space.</param>
    ///
    /// <returns>The tooltip text, or an empty string when no tooltip is available.</returns>
    ///
    /// <remarks>
    /// This method wraps <see cref="_GetTooltip(Vector2)"/> and normalizes a
    /// <c>null</c> result to an empty string.
    /// </remarks>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="TooltipText"/>
    /// <seealso cref="_MakeCustomTooltip(string)"/>
    public string GetTooltip(Vector2 atPosition)
    {
        ThrowIfFreed();
        ValidateFinite(atPosition.X, nameof(atPosition));
        ValidateFinite(atPosition.Y, nameof(atPosition));
        return _GetTooltip(atPosition) ?? string.Empty;
    }

    /// <summary>
    /// Adds or replaces a color theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme color name.</param>
    /// <param name="color">The color value to use.</param>
    ///
    /// <remarks>Local overrides have priority over assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeColor(string, string)"/>
    public void AddThemeColorOverride(string name, Color color)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        colorOverrides[name] = color;
        QueueRedraw();
    }

    /// <summary>
    /// Removes a color theme override from this control.
    /// </summary>
    ///
    /// <param name="name">The theme color name.</param>
    ///
    /// <remarks>Removing a missing override is a no-op.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="HasThemeColorOverride(string)"/>
    public void RemoveThemeColorOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (colorOverrides.Remove(name))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Reports whether this control has a local color override.
    /// </summary>
    ///
    /// <param name="name">The theme color name.</param>
    ///
    /// <returns><c>true</c> when an override exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method does not inspect assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="AddThemeColorOverride(string, Color)"/>
    public bool HasThemeColorOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return colorOverrides.ContainsKey(name);
    }

    /// <summary>
    /// Gets a color from local overrides or inherited themes.
    /// </summary>
    ///
    /// <param name="name">The theme color name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns>The resolved color, or opaque white when no value exists.</returns>
    ///
    /// <remarks>Use <see cref="HasThemeColor(string, string)"/> to distinguish a missing value from an explicit white value.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="Theme"/>
    public Color GetThemeColor(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (colorOverrides.TryGetValue(name, out var color))
        {
            return color;
        }

        return TryResolveThemeItem(themeType, (Theme resolvedTheme, string candidateType, out Color value) => resolvedTheme.TryGetColor(name, candidateType, out value), out color)
            ? color
            : new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>
    /// Reports whether a color can be resolved.
    /// </summary>
    ///
    /// <param name="name">The theme color name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns><c>true</c> when a local or theme color exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method checks local overrides and inherited themes.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeColor(string, string)"/>
    public bool HasThemeColor(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return colorOverrides.ContainsKey(name) ||
            TryResolveThemeItem<Color>(themeType, (Theme resolvedTheme, string candidateType, out Color value) => resolvedTheme.TryGetColor(name, candidateType, out value), out _);
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// Removes a font theme override from this control.
    /// </summary>
    ///
    /// <param name="name">The theme font name.</param>
    ///
    /// <remarks>Removing a missing override is a no-op.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="HasThemeFontOverride(string)"/>
    public void RemoveThemeFontOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (fontOverrides.Remove(name))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Reports whether this control has a local font override.
    /// </summary>
    ///
    /// <param name="name">The theme font name.</param>
    ///
    /// <returns><c>true</c> when an override exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method does not inspect assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="AddThemeFontOverride(string, Font)"/>
    public bool HasThemeFontOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return fontOverrides.ContainsKey(name);
    }

    /// <summary>
    /// Gets a font from local overrides, inherited themes or the default theme font.
    /// </summary>
    ///
    /// <param name="name">The theme font item name.</param>
    /// <param name="themeType">
    /// The optional theme type to query before this control's variation and
    /// runtime type chain.
    /// </param>
    ///
    /// <returns>
    /// The resolved font resource, or <c>null</c> when no matching item and no
    /// default theme font exist.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Local overrides have priority. If no local override exists, the lookup
    /// walks this control and its control parents until it finds a matching
    /// theme item or default theme font.
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddThemeFontOverride(string, Font)"/>
    /// <seealso cref="Theme.SetFont(string, string, Font)"/>
    /// <seealso cref="GetThemeDefaultFont"/>
    public Font? GetThemeFont(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (fontOverrides.TryGetValue(name, out var font))
        {
            return font;
        }

        if (TryResolveThemeItem(themeType, (Theme resolvedTheme, string candidateType, out Font? value) => resolvedTheme.TryGetFont(name, candidateType, out value), out font))
        {
            return font;
        }

        return GetThemeDefaultFont();
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// Removes a font size theme override from this control.
    /// </summary>
    ///
    /// <param name="name">The theme font size name.</param>
    ///
    /// <remarks>Removing a missing override is a no-op.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="HasThemeFontSizeOverride(string)"/>
    public void RemoveThemeFontSizeOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (fontSizeOverrides.Remove(name))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Reports whether this control has a local font size override.
    /// </summary>
    ///
    /// <param name="name">The theme font size name.</param>
    ///
    /// <returns><c>true</c> when an override exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method does not inspect assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="AddThemeFontSizeOverride(string, int)"/>
    public bool HasThemeFontSizeOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return fontSizeOverrides.ContainsKey(name);
    }

    /// <summary>
    /// Gets a scaled font size from local overrides, inherited themes or fallback values.
    /// </summary>
    ///
    /// <param name="name">The theme font size item name.</param>
    /// <param name="themeType">
    /// The optional theme type to query before this control's variation and
    /// runtime type chain.
    /// </param>
    ///
    /// <returns>
    /// The resolved font size after applying the resolved theme base scale.
    /// The fallback value is <c>16</c> before scaling.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Local overrides and theme values are stored as logical UI units.
    /// <c>GetThemeFontSize</c> multiplies the value by
    /// <see cref="GetThemeDefaultBaseScale"/> and rounds it to at least
    /// <c>1</c>.
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddThemeFontSizeOverride(string, int)"/>
    /// <seealso cref="Theme.SetFontSize(string, string, int)"/>
    /// <seealso cref="GetThemeDefaultBaseScale"/>
    public int GetThemeFontSize(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (fontSizeOverrides.TryGetValue(name, out var fontSize))
        {
            return ScaleFontSize(fontSize);
        }

        if (TryResolveThemeItem(themeType, (Theme resolvedTheme, string candidateType, out int value) => resolvedTheme.TryGetFontSize(name, candidateType, out value), out fontSize))
        {
            return ScaleFontSize(fontSize);
        }

        return GetThemeDefaultFontSize();
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
    /// The 0.1-preview UI containers use this baseline for spacing and
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// Removes a constant theme override from this control.
    /// </summary>
    ///
    /// <param name="name">The theme constant name.</param>
    ///
    /// <remarks>Removing a missing override is a no-op.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="HasThemeConstantOverride(string)"/>
    public void RemoveThemeConstantOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (themeConstantOverrides.Remove(name))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets an integer theme constant override by name.
    /// </summary>
    ///
    /// <param name="name">
    /// The theme constant name.
    /// </param>
    /// <param name="themeType">
    /// The optional theme type to query before this control's variation and
    /// runtime type chain.
    /// </param>
    ///
    /// <returns>
    /// The resolved constant after applying the resolved theme base scale, or
    /// <c>0</c> when no value exists.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Containers and controls use this method for spacing and margin values.
    /// Local overrides have priority over inherited themes. Missing values
    /// deliberately resolve to <c>0</c> so a caller can supply its own default.
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddThemeConstantOverride(string, int)"/>
    /// <seealso cref="HasThemeConstant(string, string)"/>
    /// <seealso cref="GetThemeDefaultBaseScale"/>
    public int GetThemeConstant(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (themeConstantOverrides.TryGetValue(name, out var constant))
        {
            return ScaleConstant(constant);
        }

        return TryResolveThemeItem(themeType, (Theme resolvedTheme, string candidateType, out int value) => resolvedTheme.TryGetConstant(name, candidateType, out value), out constant)
            ? ScaleConstant(constant)
            : 0;
    }

    /// <summary>
    /// Reports whether a constant can be resolved.
    /// </summary>
    ///
    /// <param name="name">The theme constant name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns><c>true</c> when a local or theme constant exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method checks local overrides and inherited themes.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeConstant(string, string)"/>
    public bool HasThemeConstant(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return themeConstantOverrides.ContainsKey(name) ||
            TryResolveThemeItem<int>(themeType, (Theme resolvedTheme, string candidateType, out int value) => resolvedTheme.TryGetConstant(name, candidateType, out value), out _);
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
    /// This method is available since Electron2D 0.1-preview.
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

    /// <summary>
    /// Adds or replaces an icon theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme icon name.</param>
    /// <param name="icon">The texture resource to use.</param>
    ///
    /// <remarks>Local overrides have priority over assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="icon"/> is <c>null</c>.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeIcon(string, string)"/>
    public void AddThemeIconOverride(string name, Texture2D icon)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(icon);
        iconOverrides[name] = icon;
        QueueRedraw();
    }

    /// <summary>
    /// Removes an icon theme override from this control.
    /// </summary>
    ///
    /// <param name="name">The theme icon name.</param>
    ///
    /// <remarks>Removing a missing override is a no-op.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="HasThemeIconOverride(string)"/>
    public void RemoveThemeIconOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (iconOverrides.Remove(name))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Reports whether this control has a local icon override.
    /// </summary>
    ///
    /// <param name="name">The theme icon name.</param>
    ///
    /// <returns><c>true</c> when an override exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method does not inspect assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="AddThemeIconOverride(string, Texture2D)"/>
    public bool HasThemeIconOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return iconOverrides.ContainsKey(name);
    }

    /// <summary>
    /// Gets an icon from local overrides or inherited themes.
    /// </summary>
    ///
    /// <param name="name">The theme icon name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns>The resolved icon, or <c>null</c> when no value exists.</returns>
    ///
    /// <remarks>Icon lookup follows the same branch theme rules as colors and fonts.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="Theme.SetIcon(string, string, Texture2D)"/>
    public Texture2D? GetThemeIcon(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (iconOverrides.TryGetValue(name, out var icon))
        {
            return icon;
        }

        return TryResolveThemeItem(themeType, (Theme resolvedTheme, string candidateType, out Texture2D? value) => resolvedTheme.TryGetIcon(name, candidateType, out value), out icon) ? icon : null;
    }

    /// <summary>
    /// Reports whether an icon can be resolved.
    /// </summary>
    ///
    /// <param name="name">The theme icon name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns><c>true</c> when a local or theme icon exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method checks local overrides and inherited themes.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeIcon(string, string)"/>
    public bool HasThemeIcon(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return iconOverrides.ContainsKey(name) ||
            TryResolveThemeItem<Texture2D?>(themeType, (Theme resolvedTheme, string candidateType, out Texture2D? value) => resolvedTheme.TryGetIcon(name, candidateType, out value), out _);
    }

    /// <summary>
    /// Adds or replaces a style box theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme style box name.</param>
    /// <param name="styleBox">The style box resource to use.</param>
    ///
    /// <remarks>Local overrides have priority over assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="styleBox"/> is <c>null</c>.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeStyleBox(string, string)"/>
    public void AddThemeStyleBoxOverride(string name, StyleBox styleBox)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(styleBox);
        styleBoxOverrides[name] = styleBox;
        QueueRedraw();
    }

    /// <summary>
    /// Removes a style box theme override from this control.
    /// </summary>
    ///
    /// <param name="name">The theme style box name.</param>
    ///
    /// <remarks>Removing a missing override is a no-op.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="HasThemeStyleBoxOverride(string)"/>
    public void RemoveThemeStyleBoxOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (styleBoxOverrides.Remove(name))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Reports whether this control has a local style box override.
    /// </summary>
    ///
    /// <param name="name">The theme style box name.</param>
    ///
    /// <returns><c>true</c> when an override exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method does not inspect assigned theme resources.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="AddThemeStyleBoxOverride(string, StyleBox)"/>
    public bool HasThemeStyleBoxOverride(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return styleBoxOverrides.ContainsKey(name);
    }

    /// <summary>
    /// Gets a style box from local overrides or inherited themes.
    /// </summary>
    ///
    /// <param name="name">The theme style box name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns>The resolved style box, or <c>null</c> when no value exists.</returns>
    ///
    /// <remarks>Style boxes are used by controls that draw themed backgrounds and borders.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="StyleBox"/>
    public StyleBox? GetThemeStyleBox(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (styleBoxOverrides.TryGetValue(name, out var styleBox))
        {
            return styleBox;
        }

        return TryResolveThemeItem(themeType, (Theme resolvedTheme, string candidateType, out StyleBox? value) => resolvedTheme.TryGetStyleBox(name, candidateType, out value), out styleBox) ? styleBox : null;
    }

    /// <summary>
    /// Reports whether a style box can be resolved.
    /// </summary>
    ///
    /// <param name="name">The theme style box name.</param>
    /// <param name="themeType">The optional theme type to query before this control's type chain.</param>
    ///
    /// <returns><c>true</c> when a local or theme style box exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>This method checks local overrides and inherited themes.</remarks>
    ///
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="GetThemeStyleBox(string, string)"/>
    public bool HasThemeStyleBox(string name, string themeType = "")
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return styleBoxOverrides.ContainsKey(name) ||
            TryResolveThemeItem<StyleBox?>(themeType, (Theme resolvedTheme, string candidateType, out StyleBox? value) => resolvedTheme.TryGetStyleBox(name, candidateType, out value), out _);
    }

    /// <summary>
    /// Gets the resolved theme base scale.
    /// </summary>
    ///
    /// <returns>The nearest valid theme base scale, or <c>1</c> when no theme supplies one.</returns>
    ///
    /// <remarks>This value is used to scale font sizes and constants resolved by this control.</remarks>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="Theme.DefaultBaseScale"/>
    public float GetThemeDefaultBaseScale()
    {
        ThrowIfFreed();
        foreach (var candidate in EnumerateThemeOwners())
        {
            if (candidate.Theme?.HasDefaultBaseScale() == true)
            {
                return candidate.Theme.DefaultBaseScale;
            }
        }

        return 1f;
    }

    /// <summary>
    /// Gets the resolved default theme font.
    /// </summary>
    ///
    /// <returns>The nearest default font, or <c>null</c> when no theme supplies one.</returns>
    ///
    /// <remarks>This value is used as fallback by <see cref="GetThemeFont(string, string)"/>.</remarks>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="Theme.DefaultFont"/>
    public Font? GetThemeDefaultFont()
    {
        ThrowIfFreed();
        foreach (var candidate in EnumerateThemeOwners())
        {
            if (candidate.Theme?.HasDefaultFont() == true)
            {
                return candidate.Theme.DefaultFont;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the resolved default theme font size.
    /// </summary>
    ///
    /// <returns>The scaled nearest default font size, or <c>16</c> when no theme supplies one.</returns>
    ///
    /// <remarks>This value is used as fallback by <see cref="GetThemeFontSize(string, string)"/>.</remarks>
    ///
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    ///
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    ///
    /// <seealso cref="Theme.DefaultFontSize"/>
    public int GetThemeDefaultFontSize()
    {
        ThrowIfFreed();
        foreach (var candidate in EnumerateThemeOwners())
        {
            if (candidate.Theme?.HasDefaultFontSize() == true)
            {
                return ScaleFontSize(candidate.Theme.DefaultFontSize);
            }
        }

        return ScaleFontSize(16);
    }

    internal int GetThemeConstantOrDefault(string name, int defaultValue)
    {
        return HasThemeConstant(name) ? GetThemeConstant(name) : ScaleConstant(defaultValue);
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
            CanReceiveFocusCore() &&
            ReferenceEquals(GetViewport(), viewport);
    }

    internal virtual bool CanReceiveFocusCore()
    {
        return true;
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

    private delegate bool ThemeItemResolver<TValue>(Theme theme, string themeType, out TValue value);

    private bool TryResolveThemeItem<TValue>(string themeType, ThemeItemResolver<TValue> resolver, out TValue value)
    {
        foreach (var owner in EnumerateThemeOwners())
        {
            var ownerTheme = owner.Theme;
            if (ownerTheme is null)
            {
                continue;
            }

            foreach (var candidateType in GetThemeTypeCandidates(themeType))
            {
                if (resolver(ownerTheme, candidateType, out value!))
                {
                    return true;
                }
            }
        }

        value = default!;
        return false;
    }

    private IEnumerable<Control> EnumerateThemeOwners()
    {
        for (Control? current = this; current is not null; current = current.GetParent() as Control)
        {
            yield return current;
        }
    }

    private IEnumerable<string> GetThemeTypeCandidates(string explicitThemeType)
    {
        if (!string.IsNullOrWhiteSpace(explicitThemeType))
        {
            yield return explicitThemeType;
        }

        if (!string.IsNullOrWhiteSpace(ThemeTypeVariation))
        {
            yield return ThemeTypeVariation;
        }

        for (var type = GetType(); type is not null && typeof(Control).IsAssignableFrom(type); type = type.BaseType)
        {
            yield return type.Name;
            if (type == typeof(Control))
            {
                yield break;
            }
        }
    }

    private int ScaleConstant(int value)
    {
        var scaled = value * GetThemeDefaultBaseScale();
        return Math.Max(0, (int)MathF.Round(scaled, MidpointRounding.AwayFromZero));
    }

    private int ScaleFontSize(int value)
    {
        var scaled = value * GetThemeDefaultBaseScale();
        return Math.Max(1, (int)MathF.Round(scaled, MidpointRounding.AwayFromZero));
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
