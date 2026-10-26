# Control

Last updated: 2026-10-26

**Inherits:** [CanvasItem](CanvasItem.md) → [Node](Node.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** [Label](Label.md), [Panel](Panel.md), [Container](Container.md), [Range](Range.md), [NinePatchRect](NinePatchRect.md), [ItemList](ItemList.md), [TextureRect](TextureRect.md). BaseButton, buttons, scroll bars and ScrollContainer are current consumers.

- **Source:** [Control.cs](../../src/Scene/GUI/Control.cs), [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs), [Control.TextInput.cs](../../src/Scene/GUI/Control.TextInput.cs), [Control.Drag.cs](../../src/Scene/GUI/Control.Drag.cs), [Control.Focus.cs](../../src/Scene/GUI/Control.Focus.cs), [Control.SizeFlags.cs](../../src/Scene/GUI/Control.SizeFlags.cs), [Control.Translation.cs](../../src/Scene/GUI/Control.Translation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class Control : CanvasItem`

## Description

Control is the rectangular UI branch beside [Entity](Entity.md). It inherits the scene tree and canvas rendering API. A direct Control parent supplies the area for anchors; a root Control uses its viewport's visible size. A direct non-Control canvas parent supplies a zero-size anchor area in this slice. Detached controls also use a zero-size anchor area. Offsets are local canvas units; anchors are fractions of the parent area. Changes to the parent rectangle or viewport size reflow an attached control synchronously. Pivot, rotation and scale change the canvas transform without changing its layout rectangle. Control itself emits no drawing commands. Attached rendering also consumes its viewport's separate GUISnapControlsToPixels policy; logical geometry stays fractional.

Control starts with inherited `PhysicsInterpolationMode.Off`, so UI layout and pointer hit regions normally use the current pose. Assign `On` explicitly to interpolate a Control and its inheriting canvas children while the tree enables physics interpolation. The displayed transform may then differ from current logical layout coordinates until the next physics tick; `ResetPhysicsInterpolation` removes that history.

`SetAnchorsPreset` applies one of the sixteen [`LayoutPreset`](LayoutPreset.md) arrangements in left, top, right, bottom order. By default it changes offsets to keep the current rectangle in place; `keepOffsets: true` retains local offsets so the rectangle follows the new anchors immediately. The method works while detached or attached, and an invalid preset leaves all four anchors unchanged.

`SetOffsetsPreset` positions all four edges in one reflow without changing anchors. [`LayoutPresetMode`](LayoutPresetMode.md) selects intrinsic minimum or current width/height; wide presets stretch their selected axis regardless of that choice. Signed margins move edge placements, while center placements ignore margin. `SetAnchorsAndOffsetsPreset` applies the anchor step before the offset step; an invalid offset mode leaves the completed anchor step in place. RTL uses the pinned negative horizontal span and then mirrors the resolved rectangle. A root Control also uses the visible viewport rectangle's origin. `SetAnchorAndOffset` sets one anchor before its explicit offset, with opposite-anchor pushing disabled by default.

`CustomMinimumSize` combines componentwise with an intrinsic minimum supplied by `OnGetMinimumSize` and zero. `CustomMaximumSize` combines enabled bounds from the intrinsic hook, the caller and a direct parent that propagates its maximum; a negative component means unbounded. Reflow applies the minimum first and then the maximum, so the maximum wins on a conflicting axis. [`GrowDirection`](GrowDirection.md) independently chooses the fixed horizontal and vertical edges for growth or shrinkage. Attached visible controls coalesce size changes through `SceneTree.Defer`, caching one minimum and one maximum callback per control after first use; `Resized` occurs before the corresponding size-change event. Both bounds are queryable immediately. `UpdateMaximumSize` clears each direct non-top-level child's container allocation cache before recursively refreshing its bounds, so increasing a parent maximum cannot preserve an obsolete smaller allocation. Indexed child traversal and cached deferred callbacks avoid per-update enumerator/delegate allocations. [Container](Container.md), [BoxContainer](BoxContainer.md) and [GridContainer](GridContainer.md) now consume sizing flags and propagated allocation bounds. Changing TopLevel on a direct child of a Container clears its allocation cache and requests parent minimum refresh/sort. Desired-size resource consumers and wrapping windows remain separate dependencies.

[`LayoutDirection`](LayoutDirection.md) adds explicit LTR/RTL mirroring after horizontal minimum/maximum resolution. Inherited controls follow the nearest Window or Control in the same translation domain; `ApplicationLocale` and `SystemLocale` use the corresponding managed culture only when a matching catalog or configured fallback permits RTL. Direction changes notify the subtree before callers observe the resolved rectangles. `Position` and `Size` writes remain physical in RTL. Root and forced project direction settings, Window inheritance, exact locale aliases and automatic scene refresh after a process-wide culture change remain Partial.

`SetPosition`, `SetSize` and `SetGlobalPosition` share the property setters' geometry path. By default they update stored offsets. With `keepOffsets: true`, they calculate anchors from the requested physical rectangle and existing offsets; both parent-area axes must be nonzero or the call fails before mutation. `SetSize` clamps finite requests through the effective minimum and then maximum, including negative requests; `ResetSize` requests zero through that same path. `GetBegin`/`SetBegin` and `GetEnd`/`SetEnd` read or update the corresponding offset pairs with one reflow per write. A global position edit first inverse-transforms through the direct canvas parent and retains the control's pivot/rotation/scale origin.

`PivotOffsetRatio` adds a size-relative component to the ordinary pivot; `GetCombinedPivotOffset()` reports the sum. The additional offset transform combines absolute and size-relative position and pivot with rotation and scale, then composes after the ordinary pivot transform. It is disabled by default and retains configured values while disabled. When enabled, `OffsetTransformVisualOnly` defaults to true: drawing and visual child transforms move, while logical `GetTransform`, global queries and GUI hit testing stay in place. Turning visual-only off applies it to both logical and rendering transforms. The renderer uses this distinction in ordinary and Y-sorted canvas traversal.

A zero additional scale is accepted. When visual-only is false it makes the logical transform singular, so coordinate queries requiring an inverse fail under the ordinary CanvasItem contract.

Each viewport routes pointer events by the transformed rectangle and sends keyboard input to the focused control between `OnInput` and unhandled input. `MouseFilter` controls target selection, bubbling and hover. Hover transitions notify controls and select native cursor shapes. Tab and arrow navigation use InputMap actions and focus paths. Full GUI behavior remains partial: stationary-pointer geometry changes, exact directional ranking and scroll clipping, touch routing, exact renderer draw ordering, nested viewports, richer accessibility protocols, project Theme loading, additional container types, full locale direction policy, and button behavior are absent. See [Control coverage](../coverage/classes/Control.md) for individual gaps.

[MarginContainer](MarginContainer.md), [CenterContainer](CenterContainer.md) and [AspectRatioContainer](AspectRatioContainer.md) now consume the same bound minimum, fill/shrink flags and deferred Container fitting; the margin and aspect variants also use maximum propagation and RTL alignment respectively.

## Example

```csharp
var panel = new Control { Position = new Vector2(12, 8), Size = new Vector2(160, 80) };
panel.SetAnchor(Side.Right, 1);
panel.SetOffset(Side.Right, -12);
window.AddChild(panel); // window is an existing Electron2D Window
```

When the window changes size, the panel's right edge stays 12 units from the window's right edge. The example assumes the window is attached to a running scene.

## Constructors

| Member | Contract |
| --- | --- |
| `public Control()` | Creates a detached zero-size control with zero anchors, identity scale and transform. |

## Properties

`FocusBehaviorRecursive` and `MouseBehaviorRecursive` default to inheritance from a direct Control parent, or enabled at the root. Disabled masks the corresponding stored mode/filter; an explicitly enabled child restores its own mode/filter. A non-Control parent ends inheritance. Policy changes refresh hover and release newly ineligible focus immediately. Mouse capture is rejected on the next pointer event if its target becomes ineligible. [ControlRecursiveBehaviorTests](../../tests/Electron2D.Tests/ControlRecursiveBehaviorTests.cs) checks these root viewport behaviors and packed state.

| Member | Contract |
| --- | --- |
| [`public string AccessibilityName { get; set; }`](#accessibilityname) | Stored translatable semantic name; empty uses the prepared caption. |
| `public string AccessibilityDescription { get; set; }` | Stored translatable description, empty by default. |
| [`public string[] AccessibilityControlsNodes { get; set; }`](#accessibilitycontrolsnodes) | Copied, stored paths for native semantic controllers. |
| [`public string[] AccessibilityDescribedByNodes { get; set; }`](#accessibilitydescribedbynodes) | Copied, stored paths for native semantic descriptions. |
| [`public string[] AccessibilityLabeledByNodes { get; set; }`](#accessibilitylabeledbynodes) | Copied, stored paths for native semantic labels. |
| [`public string[] AccessibilityFlowToNodes { get; set; }`](#accessibilityflowtonodes) | Copied, stored paths for native semantic reading flow. |
| `public string TranslationContext { get; set; }` | Local default automatic translation context, empty initially; stored and revertible. |
| `public Vector2 Position { get; set; }` | Upper-left layout position before pivot and scale. |
| `public Vector2 Size { get; set; }` | Finite requested size, clamped to the effective minimum and maximum. |
| `public bool ClipContents { get; set; }` | False by default; clips direct canvas descendants and their viewport-local pointer targeting to this rectangle. |
| `public Vector2 CustomMinimumSize { get; set; }` | Finite caller-supplied minimum; combines with intrinsic size and zero. |
| `public Vector2 CustomMaximumSize { get; set; }` | Finite caller-supplied maximum; negative components normalize to unbounded `-1`. |
| `public bool PropagateMaximumSize { get; set; }` | Passes enabled maximum bounds to direct child controls unless they are top-level. |
| `public LayoutDirection LayoutDirection { get; set; }` | Inherited by default; explicit, application-locale and system-locale policies are available. |
| `public GrowDirection GrowHorizontal { get; set; }` / `GrowVertical` | Edge policy for minimum growth and maximum shrinkage. |
| `public float Rotation { get; set; }` | Radians around PivotOffset. |
| `public float RotationDegrees { get; set; }` | Degrees around PivotOffset. |
| `public Vector2 Scale { get; set; }` | Local scale; zero components become a small positive epsilon. |
| `public Vector2 PivotOffset { get; set; }` | Local pivot for rotation and scale. |
| `public Vector2 PivotOffsetRatio { get; set; }` | Current-size component added to the ordinary pivot. |
| `public bool OffsetTransformEnabled { get; set; }` | Enables or disables the retained additional transform. |
| `public Vector2 OffsetTransformPosition { get; set; }` / `OffsetTransformPositionRatio` | Absolute and size-relative additional translation. |
| `public Vector2 OffsetTransformPivot { get; set; }` / `OffsetTransformPivotRatio` | Absolute and size-relative additional pivot; relative default is `(0.5, 0.5)`. |
| `public Vector2 OffsetTransformScale { get; set; }` / `float OffsetTransformRotation` | Additional scale and rotation in radians. |
| `public bool OffsetTransformVisualOnly { get; set; }` | True by default; excludes the additional matrix from logical/input transforms. |
| `public Vector2 GlobalPosition { get; set; }` | Transformed origin in global canvas coordinates. |
| `public float AnchorLeft { get; set; }` | Left anchor fraction. |
| `public float AnchorTop { get; set; }` | Top anchor fraction. |
| `public float AnchorRight { get; set; }` | Right anchor fraction. |
| `public float AnchorBottom { get; set; }` | Bottom anchor fraction. |
| `public float OffsetLeft { get; set; }` | Left local offset. |
| `public float OffsetTop { get; set; }` | Top local offset. |
| `public float OffsetRight { get; set; }` | Right local offset. |
| `public float OffsetBottom { get; set; }` | Bottom local offset. |
| `public MouseFilter MouseFilter { get; set; }` | Stop by default; Pass bubbles; Ignore does not receive or block pointer events. |
| `public RecursiveBehavior MouseBehaviorRecursive { get; set; }` | Inherited by default; disables or restores pointer input in a direct Control subtree. |
| `public CursorShape MouseDefaultCursorShape { get; set; }` | Arrow by default; a hovered control refreshes the native cursor after a change. |
| `public bool MouseForcePassScrollEvents { get; set; }` | True by default; permits wheel bubbling through Stop. |
| `public FocusMode FocusMode { get; set; }` | None by default; Click permits pointer or explicit focus; All also permits action navigation. |
| `public RecursiveBehavior FocusBehaviorRecursive { get; set; }` | Inherited by default; disables or restores focus eligibility in a direct Control subtree. |
| `public string FocusNext { get; set; }` / `FocusPrevious` | Relative paths for forward and backward focus traversal; empty by default. |
| `public string FocusNeighborLeft { get; set; }` / `FocusNeighborTop` / `FocusNeighborRight` / `FocusNeighborBottom` | Relative paths for directional navigation; empty by default. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| `protected override string[] OnGetAccessibilityConfigurationWarnings()` | Adds missing action/landmark name warnings to the inherited hook. |
| `protected virtual string OnAccessibilityGetContextualInfo()` | Native contextual HelpText. |
| `protected virtual string OnGetAccessibilityContainerName(Node node)` | Immediate-child position, forwarding this container to a parent Control by default. |
| `public float GetAnchor(Side side)` | Reads one anchor fraction. |
| `public void SetAnchor(Side side, float anchor, bool keepOffset = false, bool pushOppositeAnchor = true)` | Updates one anchor and optionally the opposite anchor. |
| `public void SetAnchorsPreset(LayoutPreset preset, bool keepOffsets = false)` | Applies all four anchor fractions; preserves the current rectangle unless offsets are kept. |
| `public void SetOffsetsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = MinSize, int margin = 0)` | Places all edges without changing anchors; wide presets span the parent axis. |
| `public void SetAnchorsAndOffsetsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = MinSize, int margin = 0)` | Applies anchors, then matching offsets. |
| `public void SetAnchorAndOffset(Side side, float anchor, float offset, bool pushOppositeAnchor = false)` | Sets one anchor and explicit offset in sequence. |
| `public void SetPosition(Vector2 position, bool keepOffsets = false)` / `SetSize(Vector2 size, bool keepOffsets = false)` | Writes offsets by default, or recalculates anchors under a nonzero parent area. |
| `public void SetGlobalPosition(Vector2 position, bool keepOffsets = false)` | Converts through the direct canvas parent before local placement. |
| `public void ResetSize()` | Resolves the effective minimum after all current bounds. |
| `public Vector2 GetCombinedPivotOffset()` | Returns absolute pivot plus the size-relative part. |
| `public Vector2 GetBegin()` / `GetEnd()` | Reads the leading or trailing stored offset pair. |
| `public void SetBegin(Vector2 position)` / `SetEnd(Vector2 position)` | Writes one offset pair and reflows once. |
| `public bool IsLayoutRTL()` | Reports the currently resolved horizontal layout direction. |
| `public Vector2 GetMinimumSize()` / `GetCombinedMinimumSize()` | Returns intrinsic and effective componentwise minima. |
| `public void UpdateMinimumSize()` | Coalesces a changed intrinsic minimum for deferred attached-tree reflow. |
| `protected virtual Vector2 OnGetMinimumSize()` | Supplies a derived control's intrinsic minimum; base returns zero. |
| `public Vector2 GetMaximumSize()` / `GetCombinedMaximumSize()` | Returns intrinsic and effective componentwise maxima, with `-1` for an unbounded axis. |
| `public Vector2 GetBoundMinimumSize()` | Caps the combined minimum by each enabled maximum component. |
| `public void UpdateMaximumSize()` | Clears direct non-top-level child allocation caches, recursively refreshes bounds and coalesces the visible attached update. |
| `protected virtual Vector2 OnGetMaximumSize()` | Supplies a derived control's intrinsic maximum; base returns `(-1, -1)`. |
| `public float GetOffset(Side side)` | Reads one local offset. |
| `public void SetOffset(Side side, float offset)` | Updates one offset and resolves the rectangle. |
| `public Control? GetParentControl()` | Returns only a direct Control parent. |
| `public Vector2 GetParentAreaSize()` | Returns the active anchor area size or zero while detached. |
| `public Rect2 GetRect()` | Returns transformed local origin and scale times layout size; not an axis-aligned rotated bound. |
| `public Rect2 GetGlobalRect()` | Returns transformed global origin and scale times layout size; not an axis-aligned rotated bound. |
| `public override Transform GetTransform()` | Returns translation composed with pivot, rotation and scale. |
| `public override void Reparent(Node newParent, bool keepGlobalTransform = true)` | Moves in the neutral tree; preserves global origin by default, validating a canvas inverse before mutation. |
| `public void AcceptEvent()` | Marks current scene input handled. |
| `public MouseFilter GetMouseFilterWithOverride()` | Returns Ignore when recursive pointer input is disabled, else the stored filter. |
| `public FocusMode GetFocusModeWithOverride()` | Returns None when recursive focus is disabled, else the stored mode. |
| `public void GrabFocus(bool hideFocus = false)` | Requests focus while attached and visible. |
| `public bool HasFocus(bool ignoreHiddenFocus = false)` | Queries whether this is the current focused control. |
| `public void ReleaseFocus()` | Releases focus if held. |
| `public string GetFocusNeighbor(Side side)` / `void SetFocusNeighbor(Side side, string neighbor)` | Reads or writes a directional focus path. |
| `public Control? FindNextValidFocus()` / `FindPrevValidFocus()` | Finds the next eligible Control in viewport traversal order, with wrapping. |
| `public Control? FindValidFocusNeighbor(Side side)` | Finds an eligible Control through an explicit path chain or spatial search. |
| `public CursorShape GetCursorShape(Vector2 atPosition = default)` | Queries the typed cursor override with control-local coordinates. |
| `protected virtual CursorShape OnGetCursorShape(Vector2 atPosition)` | Returns MouseDefaultCursorShape unless overridden. |
| `protected virtual bool HasPoint(Vector2 point)` | Tests a local point against the half-open rectangle; override for a custom hit shape. |
| `protected virtual void OnGUIInput(InputEvent inputEvent)` | Receives routed pointer or focused keyboard input. |
| `protected virtual void OnTextInput(string text)` | Receives one complete native committed-text string before subscribers. |
| `protected virtual void OnIMECompositionChanged(string text, Vector2i selection)` | Receives the current uncommitted preedit and codepoint selection. |
| `public DragPayload? GetDragData(Vector2 atPosition)` / `protected virtual DragPayload? OnGetDragData(Vector2 atPosition)` | Produces typed data for a drag from a finite local origin. |
| `public bool CanDropData(Vector2 atPosition, DragPayload payload)` / `protected virtual bool OnCanDropData(Vector2 atPosition, DragPayload payload)` | Tests a candidate target at finite local coordinates. |
| `public void DropData(Vector2 atPosition, DragPayload payload)` / `protected virtual void OnDropData(Vector2 atPosition, DragPayload payload)` | Delivers an accepted payload. |
| `public void SetDragForwarding(Func<Vector2, DragPayload?>? getData, Func<Vector2, DragPayload, bool>? canDrop, Action<Vector2, DragPayload>? drop)` | Replaces supplied virtual hooks with typed delegates. |
| `public void ForceDrag(DragPayload payload, Control? preview = null)` | Starts an attached connected-section drag immediately. |
| `public void SetDragPreview(Control preview)` | Transfers a detached parentless preview to the active drag. |
| `public bool IsDragSuccessful()` | Reports the last completed connected-section drag result. |
| `protected override void OnNotification(int what)` | Connects/disconnects layout sources, raises Resized after NotificationResized, and receives focus notifications. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Supplies stored layout, transform and GUI input policy. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Creates exact Control instances for PackedScene. |
| `protected override void Dispose(bool disposing)` | Disconnects parent/viewport events and clears Resized. |

## Events and constants

| Member | Contract |
| --- | --- |
| `public event Action? Resized` | Raised synchronously on the owner thread after a size change while attached. |
| `public event Action? MinimumSizeChanged` | Raised after deferred reflow when the combined minimum actually changes while visible and attached. |
| `public event Action? MaximumSizeChanged` | Raised after deferred reflow when the combined maximum actually changes while visible and attached. |
| `public event Action<InputEvent>? GUIInput` | Raised after OnGUIInput with the same borrowed event. |
| `public event Action<string>? TextInput` | Raised after OnTextInput for one complete native commit. |
| `public event Action<string, Vector2i>? IMECompositionChanged` | Raised after OnIMECompositionChanged for the current preedit. |
| `public event Action? FocusEntered` | Raised after NotificationFocusEnter when this control gains keyboard focus. |
| `public event Action? FocusExited` | Raised after NotificationFocusExit when focus is released or transferred. |
| `public event Action? MouseEntered` | Raised when the pointer enters the control or a reachable child. |
| `public event Action? MouseExited` | Raised when the pointer leaves the control and reachable children. |
| `public const int NotificationResized = 40` | Delivered before Resized, after the rectangle and transform are committed. |
| `public const int NotificationLayoutDirectionChanged = 49` | Propagated parent-first when a Control's direction policy changes. |
| `public const int NotificationMouseEnter = 41` / `NotificationMouseExit = 42` | Delivered before the corresponding hover event. |
| `public const int NotificationFocusEnter = 43` / `NotificationFocusExit = 44` | Delivered before the corresponding focus event. |
| `public const int NotificationScrollBegin = 47` / `NotificationScrollEnd = 48` | Propagated through touch-scrolling descendants on gesture begin/end. |
| `public const int NotificationMouseEnterSelf = 60` / `NotificationMouseExitSelf = 61` | Delivered when the direct hover target changes. |

## Shared enums

| Enum | Values |
| --- | --- |
| [CursorShape](CursorShape.md) | Seventeen system cursor identities from Arrow = 0 through Help = 16. |
| [RecursiveBehavior](RecursiveBehavior.md) | Inherited, Disabled and Enabled policies used by both focus and pointer properties. |

## Member behavior

### `Control()`

Creates a detached control. Anchors, offsets, position, size, rotation and pivot start at zero; scale starts at one. No viewport is required until attachment.

### `Position`, `Size`

Position is the upper-left layout point before transform. Size is the local rectangle extent. Both setters preserve anchors by adjusting offsets; Size rejects negative components. Parent or viewport resize can subsequently change either resolved value.

### `Rotation`, `RotationDegrees`, `Scale`, `PivotOffset`

These form the local affine transform around PivotOffset. Rotation uses radians; RotationDegrees converts degrees. A zero scale component becomes a small positive epsilon. Assignments request redraw and invalidate descendant transforms without changing the anchor rectangle.

### `GlobalPosition`

The transformed origin in global canvas coordinates. Setting it uses the direct canvas parent's affine inverse, so a singular parent rejects the assignment before modifying Position.

### `AnchorLeft`, `AnchorTop`, `AnchorRight`, `AnchorBottom`

Each property reads or sets one fraction of the parent area through GetAnchor and SetAnchor. The default is zero. Setting a side preserves its current edge by default and pushes the opposite anchor if needed to avoid crossing.

### `OffsetLeft`, `OffsetTop`, `OffsetRight`, `OffsetBottom`

Each property reads or sets one local offset through GetOffset and SetOffset. The default is zero. The resolved edge is offset plus anchor times the corresponding parent-area dimension.

### `MouseFilter`, `MouseForcePassScrollEvents`

`MouseFilter` defaults to Stop and rejects undefined enum values. It controls hit selection, hover ancestry and whether an unhandled pointer event bubbles through direct Control parents. Changing it refreshes current hover. `MouseForcePassScrollEvents` defaults to true and allows wheel input through a Stop control; setting it false makes Stop consume a wheel event. Both values are stored in a packed scene.

### `ClipContents`

False by default and stored by PackedScene. When enabled, a Control clips direct canvas descendants to the axis-aligned bounds of its transformed rectangle; nested clips intersect. Its own drawing remains unclipped while that clip has visible area; an empty clip culls the entire item. Its own input remains independent of descendant clipping. A top-level or non-canvas boundary starts an independent canvas branch. Changing the value refreshes root-viewport hover and takes effect in the next frame without rerecording retained child commands. Rendered clips use framebuffer pixel rounding; input tests the unrounded local rectangle. [ControlClipTests](../../tests/Electron2D.Tests/ControlClipTests.cs) checks pointer delivery and packing; [ControlClipRenderingTests](../../tests/Electron2D.Tests/ControlClipRenderingTests.cs) checks pixels on dummy compatibility and Linux Wayland compatibility/GPU, including nested, rotated, repeated and empty controls.

### `MouseDefaultCursorShape`, `GetCursorShape`, `OnGetCursorShape`

The stored property defaults to Arrow, rejects unknown enum values, and is captured by PackedScene. `GetCursorShape` validates finite local coordinates and the override result. The virtual hook returns the stored property by default. While hovering, the scene queries the direct target first, then eligible Control ancestors until a non-Arrow shape or Stop filter is found. A property change refreshes the native cursor on the owner thread.

### `MouseEntered`, `MouseExited`, mouse notifications

Root-viewport pointer motion updates the hover chain even if a Node handled the input. Entry is ancestor first and exit is descendant first; direct-target self notifications are distinct from chain notifications. Each chain notification precedes its event. Hidden or detached controls release their hover chain, and native window exit clears it. Callback failures are collected while later eligible callbacks continue. Stationary-pointer geometry changes and nested viewport semantics remain incomplete.

### `FocusMode`, `GrabFocus`, `HasFocus`, `ReleaseFocus`

`FocusMode` defaults to None, rejects unsupported enum values, and releases active focus when set back to None. Click permits focus by left pointer press or explicit `GrabFocus`; All additionally permits automatic action navigation. Accessibility retains numeric value 3 and becomes eligible while native accessibility output is active. `hideFocus` records hidden visual focus, reported as absent by `HasFocus(ignoreHiddenFocus: true)`. `GrabFocus` requires an attached control and has no effect while hidden, in None mode or outside a root Viewport. `ReleaseFocus` does nothing when this control is not focused. Hiding or detaching the control releases focus. A focus transfer commits before the previous control's NotificationFocusExit and FocusExited; the root Viewport then raises GUIFocusChanged, followed by the new control's NotificationFocusEnter and FocusEntered. Explicit release sends only the exit notification and event. Changing `hideFocus` on the current owner redraws it without another notification. Focus changes request redraw. Callback failures are collected while later focus callbacks continue, then propagate as AggregateException; focus state remains committed. Reentrant release suppresses a pending enter notification or event after focus is cleared.

### `FocusNext`, `FocusPrevious`, `FocusNeighborLeft`, `FocusNeighborTop`, `FocusNeighborRight`, `FocusNeighborBottom`

Each path is an empty string by default, is relative to this Control, and is stored by PackedScene. A nonempty sequential path is consulted before automatic traversal; a missing or non-Control path returns no target. Directional paths can chain through ineligible Controls. Setters reject null; attached mutations require the scene owner thread.

### `GetFocusNeighbor(Side side)`, `SetFocusNeighbor(Side side, string neighbor)`

These access the same four directional paths as the properties. Invalid sides throw `ArgumentOutOfRangeException`; a null new path throws `ArgumentNullException`.

### `FindNextValidFocus()`, `FindPrevValidFocus()`, `FindValidFocusNeighbor(Side side)`

Queries return a borrowed Control or null when detached or no eligible target exists. Sequential automatic traversal wraps through visible `All` Controls in the root viewport. Directional automatic search compares global axis-aligned rectangles; explicit paths can select `Click` Controls. An invalid direction throws `ArgumentOutOfRangeException`. Exact upstream spatial ordering, scroll clipping and nested viewport ownership remain partial.

### `HasPoint`, `OnGUIInput`, `GUIInput`, `AcceptEvent`

The virtual `HasPoint` tests a control-local point against `[0, Size.X) × [0, Size.Y)` by default and can define a custom hit shape. Pointer events are copied into each receiving Control's local coordinates; their `GlobalPosition` is in the receiving CanvasLayer's coordinates. `OnGUIInput` runs before the `GUIInput` event. The local event is borrowed only during the callback. `AcceptEvent` forwards to the active scene handled flag and throws outside input delivery. Callback failures are collected while eligible parent and later Node callbacks continue.

### `GetAnchor(Side side)`, `SetAnchor(Side side, float anchor, bool keepOffset = false, bool pushOppositeAnchor = true)`

Side chooses left, top, right or bottom. GetAnchor returns its current fraction. SetAnchor accepts any finite fraction; keepOffset retains the current offset instead of the current edge, and pushOppositeAnchor moves the opposite anchor when sides cross. Invalid sides and nonfinite fractions throw ArgumentOutOfRangeException.

### `GetOffset(Side side)`, `SetOffset(Side side, float offset)`

GetOffset returns the selected local distance. SetOffset commits a finite distance and immediately reflows the rectangle. Invalid sides and nonfinite values throw ArgumentOutOfRangeException.

### `GetParentControl()`, `GetParentAreaSize()`

GetParentControl returns the direct parent only when it is a Control. GetParentAreaSize returns that parent's size, or the visible viewport size for a canvas root. A detached control or a direct non-Control canvas parent currently yields zero.

### `GetRect()`, `GetGlobalRect()`, `GetTransform()`

GetTransform composes layout position with pivot, rotation and scale. GetRect and GetGlobalRect use its local or global transformed origin and signed scale times Size; these rectangles are not rotated axis-aligned bounds. Attached local and global transform queries require the scene owner thread.

### `Reparent(Node newParent, bool keepGlobalTransform = true)`

Moves to a new direct parent. With the default option, it validates the destination canvas inverse first, then restores the transformed global origin after the new layout area resolves. Anchors and size remain relative to the new area. Callback failures can occur after the move has begun, consistent with Node.Reparent.

### `OnNotification(int what)`, `GetPropertyDescriptors()`, `CreateSceneInstanceFactory()`, `Dispose(bool disposing)`

OnNotification subscribes to the direct canvas parent's geometry or containing viewport size at canvas entry, disconnects at exit, releases focus when hidden or detached, and emits Resized after NotificationResized. The focus notifications 43/44 pass through this protected hook before the corresponding event. GetPropertyDescriptors stores rectangle, transform, anchors, offsets, mouse filter, wheel policy, focus mode and focus paths for PackedScene. The factory creates exact Control instances. Disposal removes borrowed event subscriptions and clears the control events.

### `Resized`, `NotificationResized`

The constant is 40. A size change while attached delivers this notification after ItemRectChanged, then raises Resized synchronously. Position-only changes do not raise Resized. A throwing handler sees committed geometry and stops later synchronous handlers.

## Lifecycle and invariants

Position and Size setters alter the four offsets so the currently resolved rectangle takes the assigned value; they preserve anchor fractions. Size rejects negative or nonfinite input. SetAnchor preserves the current edge by default, and can push an opposite anchor to prevent crossing. SetOffset accepts finite values; if the resolved end precedes the beginning, the visible size clamps to zero. The individual anchor and offset properties call these same methods. Invalid Side values and nonfinite inputs throw ArgumentOutOfRangeException. Rotation, Scale and PivotOffset change only the local transform and request redraw. GlobalPosition uses the inverse of a direct canvas parent's transform and rejects a singular inverse. Attached mutations enforce the scene owner thread. PackedScene stores all four anchors and offsets alongside transform and rectangle state.

When reflow changes position or size, the control commits both values, invalidates global transforms, raises inherited ItemRectChanged, and delivers NotificationResized followed by Resized if size changed. Descendant controls recalculate through the parent's ItemRectChanged event. A throwing subscriber sees the committed values; as with other synchronous canvas events, that exception stops later subscribers. Detaching disconnects the parent or viewport subscription. Reparent preserves the global transformed origin by default, while anchors and size resolve against the new parent. Inherited CanvasItem drawing, visibility, material, Z and coordinate methods remain available.

## Verification and limits

[ControlLayoutTests](../../tests/Electron2D.Tests/ControlLayoutTests.cs) check nested anchors, parent and viewport resize propagation, callback order, transform inheritance, packed anchors/offsets, pivot/global position, invalid arguments, owner thread and disposal. [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs) check root viewport GUI input order, local coordinates, filter/bubbling, wheel pass, focus notification and event order, explicit viewport release, failure continuation, pointer release capture and temporary event ownership. [ControlFocusNavigationTests](../../tests/Electron2D.Tests/ControlFocusNavigationTests.cs) checks managed Tab/arrow/D-pad/left-stick routing, paths, wrapping, hidden targets and packed state. [ControlHoverTests](../../tests/Electron2D.Tests/ControlHoverTests.cs) checks managed hover ordering, filters, notifications and cursor state; [ControlHoverNativeTests](../../tests/Electron2D.Tests/ControlHoverNativeTests.cs) checks native cursor precedence on Linux Wayland. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) includes a child Sprite pixel check in the native renderer. Full GUI interaction and reference parity remain unverified.

[PhysicsInterpolationTests](../../tests/Electron2D.Tests/PhysicsInterpolationTests.cs) verifies the inherited Off default, explicit descendant On policy and visual versus logical position of an opt-in moving Control. Native interpolation pixels are checked for canvas items and cameras on dummy compatibility and Linux Wayland compatibility/GPU; opt-in moving-Control GUI hit-test acceptance remains separate.

The geometry-edit checks additionally cover offset-pair commits, anchor-preserving defaults, keep-offset anchor recomputation, later parent resize, transformed global placement, minimum/maximum clamping, negative finite size, RTL viewport origins, zero-area and nonfinite rollback.

Offset-transform checks verify defaults, disabled-value retention, resize-dependent pivot and translation, matrix order, logical versus visual-only hit testing, enabled logical input, finite validation and PackedScene restoration. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) checks moving descendant pixels in dummy/software and Linux Wayland compatibility/GPU, including Y-sort traversal. Other platforms and owner visual acceptance remain separate.

### GUI input behavior

The top hit Control in a viewport receives a temporary local pointer event. The viewport GUI picker orders by canvas layer, effective Z and reverse scene traversal; it does not yet match every renderer ordering rule. `Ignore` is skipped; `Pass` continues through canvas ancestors, delivering to eligible Control parents until handled or a `Stop` control; `Stop` handles the event automatically. Wheel events pass a `Stop` control when `MouseForcePassScrollEvents` is true. Mouse-button presses retain their target through a pressed-button mask; additional buttons share the capture until the final release. Wheel presses are momentary and never claim mouse-button capture. Touch-style left-button input can pass a stopping descendant directly to its ScrollContainer ancestor. Touch contacts capture independently by index; drag and release reach the capture outside its rectangle, and uncaptured drag and gestures use hit testing. A left press focuses an eligible control, with hidden visual focus. Explicit `GrabFocus` takes focus without hiding it; hiding, detaching or setting `FocusMode` to None releases it. Keyboard, controller and action events reach the focused control without bubbling. `AcceptEvent` stops later GUI and unhandled stages. Unhandled `ui_*` actions traverse focus after GUI delivery. Failures are aggregated after other eligible scene callbacks run; positional GUI copies are disposed after synchronous delivery. Hover uses the same root picker and respects clipping ancestors. Nested viewport routes remain incomplete. Processing gates apply to hit/captured and focused GUI targets.

`FocusNext` and `FocusPrevious` take precedence over automatic traversal; an invalid path returns null. Directional paths can chain through ineligible controls, with cycle protection, then fall back to spatial search. Automatic traversal accepts visible `All` controls in scene order within the root viewport; explicit paths may select visible `Click` controls. Directional search uses global axis-aligned rectangles, not the full reference ranking or scroll clipping. Navigation happens after focused GUI callbacks if they leave the event unhandled. Analog navigation acts on a new press transition rather than every held motion event. No native keyboard or controller navigation has been verified for this slice.

Targeted scene-hierarchy pixel checks passed on Linux Wayland compatibility/GPU and dummy/software in an earlier slice; they do not verify this navigation change.

**Decisions:** [scene hierarchy](../decisions/scene.md#adr-0008), [typed 2D API](../decisions/product.md#adr-0004), [rendering](../decisions/rendering.md#adr-0028).

[NinePatchRect](../classes/NinePatchRect.md) now records one retained panel command with fixed borders, independent Stretch/Tile/TileFit axes and optional center. Live base/atlas dimensions resolve before splitting; ordinary atlas region drawing reuses that resolver. Signed margins drive Control intrinsic minimum size and inherited pointer filtering defaults to Ignore. All nine native axis combinations, center/flip/atlas/constant UV and 64 warmed resized frames are checked by [NinePatchRenderingTests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs), under [ADR 0079](../decisions/rendering.md#adr-0079). Dense CPU geometry limits, native allocator counts, other platforms and owner acceptance remain explicit.

[Range](../classes/Range.md) and [TextureProgressBar](../classes/TextureProgressBar.md) now execute shared double value policy and textured linear/centered/radial fills. Nine-patch partial progress reuses the real retained geometry/tint path. Their inherited vertical size flags now execute through [Container](../classes/Container.md) and [BoxContainer](../classes/BoxContainer.md), with Range ShrinkBegin and progress Fill defaults under [ADR 0081](../decisions/rendering.md#adr-0081). [RangeProgressTests](../../tests/Electron2D.Tests/RangeProgressTests.cs) and [native tests](../../tests/Electron2D.Tests/TextureProgressRenderingTests.cs) verify the current scope and allocation/platform limits under [ADR 0080](../decisions/rendering.md#adr-0080).

## Native committed text and IME composition

<a id="ontextinput"></a><a id="event-textinput"></a>
The active main-window SDL text commit is delivered to the root viewport's currently focused, visible and processing Control as one complete string. A commit may contain multiple Unicode scalars or one IME result. `OnTextInput` runs before `TextInput` subscribers; failure in one phase does not suppress the other, and the native event pump reports collected failures after later queued events. No `InputEventKey` or key-to-text association is synthesized. A derived text control enables native input with `Window.SetIMEActive(true)` while focused and manages its own caret/undo policy.

<a id="onimecompositionchanged"></a><a id="event-imecompositionchanged"></a>
For a native preedit update, DisplayServer first commits `IMEGetText` and the Unicode-codepoint start/length from `IMEGetSelection`. The scene-wide `Node.NotificationOsImeUpdate` then runs, followed by the focused Control's `OnIMECompositionChanged` and `IMECompositionChanged` event. An empty string clears preedit. Notification failure does not suppress the focused typed callback; hidden, detached and paused/non-processing focus targets are skipped. The native host and derived text control remain responsible for enabling/disabling IME and clearing consumer-specific preedit on focus loss. [TextDeliveryTests](../../tests/Electron2D.Tests/TextDeliveryTests.cs) checks focus, phases and failure continuation; [TextDeliveryNativeTests](../../tests/Electron2D.Tests/TextDeliveryNativeTests.cs) checks actual SDL text/editing events on Linux Wayland GPU and compatibility. Real input methods, visible candidate UI, other platforms and owner acceptance remain unverified. See [ADR 0038](../decisions/input.md#adr-0038) and [coverage](../coverage/classes/Control.md).

## Typed GUI drag and drop

<a id="getdragdata"></a><a id="ongetdragdata"></a>
`GetDragData` rejects nonfinite local coordinates and invokes a supplied forwarding delegate or `OnGetDragData`, which returns null by default. The left-held pointer path accumulates movement and asks the captured Control, then eligible ancestors until `MouseFilter.Stop`, for one [DragPayload](DragPayload.md). The origin is the original left-press position in each candidate's local coordinates. Returning null rejects that candidate; a preview set before a null result is destroyed.

<a id="candropdata"></a><a id="oncandropdata"></a><a id="dropdata"></a><a id="ondropdata"></a>
`CanDropData` rejects a null payload or nonfinite local point, then invokes its forwarding delegate or virtual hook; the default rejects. Hit-tested target and ancestors are tried until the first accepting Control or a stopping pointer filter. `DropData` invokes the supplied delegate or virtual hook only for an accepting target. A throwing drop callback leaves success false, but cleanup and drag-end notifications still run. Payload contents remain caller-owned.

<a id="setdragforwarding"></a><a id="forcedrag"></a><a id="setdragpreview"></a><a id="isdragsuccessful"></a>
`SetDragForwarding` stores nullable typed delegates for the three hooks; each supplied delegate replaces its virtual hook, even if it returns null or false. Delegates are disconnected on disposal. `ForceDrag` starts immediately with a nonnull payload and an optional preview. A preview must be live, detached, parentless and not queued for deletion; it becomes tree-owned and is disposed after a completed or cancelled drag. `SetDragPreview` replaces and destroys any previous preview while dragging or preparing a source callback. The active preview ignores pointer hit testing and renders above regular canvas layers. `IsDragSuccessful` reports the viewport's retained most-recent result; detached controls report false. Source removal or hiding cancels the drag.

Pointer and accessible operations share the existing connected-section transport. Cross-window transaction transfer and further nested-route qualification retain their own gates. [GUIDragTests](../../tests/Electron2D.Tests/GUIDragTests.cs) covers typed identity, threshold, forwarded and ancestor targets, previews, cancellation, failure and activation rollback; [GUIDragRenderingTests](../../tests/Electron2D.Tests/GUIDragRenderingTests.cs) verifies preview pixels and CanDrop cursor on Linux Wayland GPU and compatibility. Native allocations, other platforms and owner acceptance remain unverified; see [ADR 0038](../decisions/input.md#adr-0038) and [coverage](../coverage/classes/Control.md).

## Container size flags

| Signature | Contract/default |
| --- | --- |
| `public SizeFlags SizeFlagsHorizontal { get; set; }` | Fill; independent allocation/alignment bits. |
| `public SizeFlags SizeFlagsVertical { get; set; }` | Fill; Range ShrinkBegin override, progress Fill. |
| `public float SizeFlagsStretchRatio { get; set; }` | Finite one; zero/negative retained. |
| `public event Action? SizeFlagsChanged` | Actual flags/ratio change after state commit. |
| `public enum SizeFlags` | [Flagged six-value set](Control.SizeFlags.md). |

<a id="sizeflagshorizontal"></a><a id="sizeflagsvertical"></a><a id="sizeflagsstretchratio"></a><a id="sizeflagschanged"></a><a id="sizeflags"></a>
The direct Container consumes each axis for fill/shrink and the box primary axis for weighted expansion. Unknown bits are preserved, equal assignments silent, and callbacks observe committed state. Nonfinite ratio throws ArgumentOutOfRangeException. Attached reads/mutations use owner affinity, capture rejects mutation, disposed use rejects. Typed descriptors serialize all three properties. Container fit resets anchors, rectangle and visual transform. [Box tests](../../tests/Electron2D.Tests/BoxContainerTests.cs) verify flags/defaults/packing and actual layout; [ADR 0081](../decisions/rendering.md#adr-0081) records the semantic service/theme/platform limits.

## Typed theme API

Source: [Control.Theme.cs](../../src/Scene/GUI/Control.Theme.cs). All six data categories share [theme owner lookup](../components/themes.md), including borrowed Font resources and inherited defaults.

| Signature | Contract |
| --- | --- |
| `public Theme? Theme { get; set; }` | Borrowed branch resource, null initially. |
| `public string ThemeTypeVariation { get; set; }` | Empty initially. |
| `public const int NotificationThemeChanged = 45` | Refresh notification before cache clearing and dependent work. |
| `public event Action? ThemeChanged` | Raised during refresh before the previous lookup cache is cleared. |
| `public Color GetThemeColor(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeColor(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeColorOverride(string name)` | Local override slot only. |
| `public void AddThemeColorOverride(string name, Color color)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeColorOverride(string name)` | Refreshes even when no local slot existed. |
| `public int GetThemeConstant(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeConstant(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeConstantOverride(string name)` | Local override slot only. |
| `public void AddThemeConstantOverride(string name, int constant)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeConstantOverride(string name)` | Refreshes even when no local slot existed. |
| `public int GetThemeFontSize(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeFontSize(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeFontSizeOverride(string name)` | Local override slot only. |
| `public void AddThemeFontSizeOverride(string name, int fontSize)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeFontSizeOverride(string name)` | Refreshes even when no local slot existed. |
| `public Font? GetThemeFont(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeFont(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeFontOverride(string name)` | Local override slot only. |
| `public void AddThemeFontOverride(string name, Font font)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeFontOverride(string name)` | Refreshes even when no local slot existed. |
| `public Texture? GetThemeIcon(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeIcon(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeIconOverride(string name)` | Local override slot only. |
| `public void AddThemeIconOverride(string name, Texture texture)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeIconOverride(string name)` | Refreshes even when no local slot existed. |
| `public StyleBox? GetThemeStyleBox(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeStyleBox(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeStyleBoxOverride(string name)` | Local override slot only. |
| `public void AddThemeStyleBoxOverride(string name, StyleBox styleBox)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeStyleBoxOverride(string name)` | Refreshes even when no local slot existed. |
| `public float GetThemeDefaultBaseScale()` | Nearest positive theme default, then built-in default, then universal fallback. |
| `public Font? GetThemeDefaultFont()` | Nearest nonnull theme default font, built-in default, then nullable universal fallback. |
| `public int GetThemeDefaultFontSize()` | Same priority for positive font-size defaults. |
| `public void BeginBulkThemeOverride()` | Enables boolean override batching; repeated Begin is idempotent. |
| `public void EndBulkThemeOverride()` | Ends a batch and publishes one refresh; no active batch throws. |

<a id="theme"></a><a id="themetypevariation"></a>
**Assignment and inheritance:** Theme replacement rejects disposed input, suppresses equal references and refreshes this node and live consecutive Control/Window descendants. ThemeTypeVariation suppresses equal values and refreshes this node. A neutral Node between theme owners ends inheritance; root Window themes therefore reach only consecutive theme-capable branches. Stored Theme references are borrowed. An externally disposed assigned Theme remains readable through the property, but owner lookup skips it after invalidation.

<a id="getthemecolor"></a><a id="hasthemecolor"></a><a id="hasthemecoloroverride"></a><a id="addthemecoloroverride"></a><a id="removethemecoloroverride"></a><a id="getthemeconstant"></a><a id="hasthemeconstant"></a><a id="hasthemeconstantoverride"></a><a id="addthemeconstantoverride"></a><a id="removethemeconstantoverride"></a><a id="getthemefont"></a><a id="hasthemefont"></a><a id="getthemefontsize"></a><a id="hasthemefontsize"></a><a id="hasthemefontoverride"></a><a id="addthemefontoverride"></a><a id="removethemefontoverride"></a><a id="hasthemefontsizeoverride"></a><a id="addthemefontsizeoverride"></a><a id="removethemefontsizeoverride"></a><a id="getthemeicon"></a><a id="hasthemeicon"></a><a id="hasthemeiconoverride"></a><a id="addthemeiconoverride"></a><a id="removethemeiconoverride"></a><a id="getthemestylebox"></a><a id="hasthemestylebox"></a><a id="hasthemestyleboxoverride"></a><a id="addthemestyleboxoverride"></a><a id="removethemestyleboxoverride"></a><a id="getthemedefaultbasescale"></a><a id="getthemedefaultfont"></a><a id="getthemedefaultfontsize"></a><a id="beginbulkthemeoverride"></a><a id="endbulkthemeoverride"></a><a id="notificationthemechanged"></a><a id="themechanged"></a>
**Get/Has resolution:** detached queries bypass resolved-value caches, so ancestor/global resource edits are immediately visible without notifications. Attached queries use the deferred invalidation contract below. Empty themeType means the current type and variation. A local override applies only to this implicit query, the node's own class name or its selected variation. An explicit different type bypasses local overrides. The nearest theme defining the variation supplies its dependency chain; native ancestry follows. Search each nearest-to-outer owner theme across that type order, then the built-in Theme. Get uses the universal category fallback when no match exists. Has excludes that universal fallback, but a nonnull Theme.DefaultFont or positive Theme.DefaultFontSize is itself a valid Font or FontSize match. GetThemeDefaultFont searches defaults directly and ignores item-specific overrides. A cycle in the active variation chain throws InvalidOperationException. Direct resource queries on Theme remain exact-type queries and do not perform this branch search.

**Keys and errors:** node-side names/type strings may be empty and otherwise contain arbitrary text except a null character. Null strings throw ArgumentNullException, null characters throw ArgumentException. Assigned colors must be finite. Font/icon/style overrides require non-null live resources; unlike a Theme's null placeholder slots, a local resource override cannot be null. Integer overrides retain signed values. Attached queries require the owner thread; mutation also observes capture/lifetime guards. A disposed stored override retains identity until removed/replaced, so its consumer can reject invalid use.

**Overrides and batching:** Add commits the local value; Remove clears any local slot. Both refresh even for equal/missing values. Resource overrides share one content-change/disposal subscription per distinct resource. Owner-thread override changes refresh synchronously when attached, while off-thread resource changes defer to the owner. Begin is a boolean suppression flag rather than a nesting counter; End without an active batch throws InvalidOperationException, and a valid End refreshes once even if the batch made no edits. Detached changes invalidate lookup state without emitting an attached theme notification.

**Timing and callbacks:** Theme resource content changes and global context/fallback replacement are deferred through SceneTree. NotificationThemeChanged invokes ThemeChanged before clearing the previous lookup cache, so an event handler may still see an already cached inherited/resource result; direct local overrides are checked before that cache. Required invalidation, redraw and dependent geometry work are attempted after event failures. Propagation continues to later live descendants and reports aggregated failures. Reentrant branch propagation repeats from a fresh captured set and rejects a nonsettling chain after 64 passes. Tree membership generations suppress stale deferred work. Virtual getters that replace theme state cannot refill an invalidated cache; a subsequent lookup recomputes. Deferred override-resource delivery rechecks the current bulk flag, preserving suppression when a batch begins after a worker callback was queued. Same-owner notification reentry also settles iteratively within the 64-pass bound.

**Stored state and cleanup:** Theme/variation are typed stored properties. Actual local overrides use typed descriptors; a null descriptor value means removal, not a null resource override. Fresh Control/Window instances reconstruct missing override descriptors only through the six reserved prefixes and exact captured value types; other unknown/mismatched schemas remain rejected. Exact scene packing follows existing resource graph/local-to-scene rules. Cleanup removes theme/global/resource subscriptions and cached references; externally owned resources are never disposed by node cleanup. First-use query keys and capacity growth may allocate; warmed behavior is measured separately.

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) verifies font defaults/overrides, deferred updates, failures, packing and 64 warmed cycles with zero managed bytes. Project Theme loading and remaining built-in GUI defaults remain separate under [ADR 0083](../decisions/rendering.md#adr-0083).

## Structured text parser hook

`protected virtual TextBIDIRange[] OnStructuredTextParser(IReadOnlyList<string> options, string text)` supplies typed contexts when a text control selects `StructuredTextParser.Custom`. The default returns an empty array, retaining ordinary paragraph resolution. The options view is read-only. Returned [TextBIDIRange](TextBIDIRange.md) values use Unicode scalar offsets and supported [TextDirection](TextDirection.md) values; invalid bounds or direction throw before the consumer publishes a layout. Ranges may repeat or reorder contexts.

[Label](Label.md) also consumes built-in URI, file, email and list parsers. URI/path delimiters have independent LTR contexts; components use automatic direction. Email parsing separates the local part and dotted domain. List mode accepts exactly one typed string delimiter and preserves its complete scalar length. These are actual shaping inputs, not metadata-only properties. The separate script-language-specific parser is not exposed. [LabelTests](../../tests/Electron2D.Tests/LabelTests.cs) verifies custom layout and invalid-output recovery.

## Shortcuts and tooltips

Source: [Control.Tooltip.cs](../../src/Scene/GUI/Control.Tooltip.cs). All access follows the scene owner-thread and mutation/capture boundaries. Disposed controls reject access.

| Signature | Default / contract |
| --- | --- |
| `public Node? ShortcutContext { get; set; }` | Null: global shortcuts. A live weak context permits focus on itself or descendants. |
| `public string TooltipText { get; set; }` | Empty untranslated text; null is rejected. |
| `public NodeAutoTranslateMode TooltipAutoTranslateMode { get; set; }` | Inherit; retained enum policy for the default label. |
| `public string GetTooltip(Vector2 atPosition = default)` | Queries untranslated text at a finite local point. |
| `protected virtual string OnGetTooltip(Vector2 atPosition)` | Returns TooltipText by default; empty permits eligible parent lookup. |
| `protected virtual NodeAutoTranslateMode OnGetTooltipAutoTranslateModeAt(Vector2 atPosition)` | Returns TooltipAutoTranslateMode by default. |
| `protected virtual Control? OnMakeCustomTooltip(string forText)` | Returns null by default, or transfers a fresh detached control to the presenter. |

### ShortcutContext

An expired weak reference reads as null but remains inactive; assigning null explicitly restores global scope. Packed scenes store a relative node path and resolve it after construction of each independent hierarchy. The context is never a strong owner of a node.

### TooltipText and TooltipAutoTranslateMode

Text and modes are stored and packed. Text retains whitespace; presentation strips only outer characters U+0000 through U+0020, preserving Unicode nonbreaking spaces. Tooltip mode can be overridden per point. Inherit resolves through the owner hierarchy, Always enables translation and Disabled suppresses it. A default label validates its selected mode when created.

### GetTooltip and OnGetTooltip

Positions use local control coordinates and must be finite. The public method calls the hook and returns a non-null string. The presenter walks eligible parent controls while text is empty, stopping at Stop filtering or a top-level boundary. A nonempty whitespace-only string stops parent lookup and is subsequently trimmed.

### OnGetTooltipAutoTranslateModeAt

Receives the tooltip owner's local pointer position. Its result affects the default Label; a custom tooltip controls its own content and translation.

### OnMakeCustomTooltip

Receives trimmed untranslated text, including an empty string. Return null for the default Label, a new detached visible Control for custom contents, or a new hidden Control to suppress display. The presenter owns an accepted result and queues its disposal at the safe deletion point on cancellation. Tree shutdown delegates remaining owned presentation to normal hierarchy teardown. Returning an existing parented, attached, disposed or ancestor control is rejected without taking ownership. Default and custom tooltip contents cannot capture pointer input or keyboard focus.

The current root-viewport host displays tooltips through an internal CanvasLayer and PanelContainer, with TooltipPanel/TooltipLabel theme variations. Mouse motion schedules an unscaled delay; movement beyond five pixels or a new target resets it before display. Text changes, pointer presses, gestures, ui_cancel, departure and target visibility/lifetime changes cancel old content. Native independent popup windows remain a separate capability. [ControlTooltipTests](../../tests/Electron2D.Tests/ControlTooltipTests.cs) covers the concrete host and ownership contract.

[TooltipRenderingTests](../../tests/Electron2D.Tests/TooltipRenderingTests.cs) verifies real glyphs and custom contents above a maximum-index game layer, edge placement, native pointer transparency and 64 warmed active modulation frames with zero managed allocation on Linux Wayland GPU/compatibility. The dummy compatibility probe uses Nearest sampling, respecting its documented linear-filter limitation. Other platforms, native allocator counts and owner acceptance remain unverified.

Controls in a SubViewport now use its independent GUI context. GrabFocus also focuses containing SubViewportContainer controls so native keyboard/committed text reaches the embedded focus. Cursor/drop targeting follows the connected section and MouseTarget policy. Public PushInput reentry remains rejected; temporary positional event copies retain ADR 0038 ownership.

Inherited layout direction now resolves a same-domain containing Window before falling back to the application culture. PopupTests covers RTL window-to-control propagation. Other locale and project-policy gaps retain their coverage states.

LocalizeNumeralSystem now defaults true in the typed stored schema and refreshes numeric producer presentation when changed. It does not rewrite arbitrary caller-authored text. SpinBox is the current generated-numeric consumer; CodeEdit/list-index/ProgressBar producers retain their own prerequisites. See [numeric input](../components/numeric-input.md) for the exercised workflow and limits.

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean LocalizeNumeralSystem { get; set; }` | Generated numeric producer policy; true initially, stored, live translation refresh on changed writes. |

## Project layout direction

Window and Control use one domain-aware resolver. An inherited node follows the nearest Window or Control only while ancestor translation domains match. With no such ancestor, `ProjectSettings.RootNodeLayoutDirection` selects 0 application locale, 1 LTR, 2 RTL or 3 system UI locale. `ForceRightToLeftLayoutDirection` forces inherited and application/system locale modes to RTL; explicit LTR/RTL remain authoritative. Each SceneTree samples both settings and feature overrides before entry. Detached nodes use current settings. Authored per-node directions survive scene packing; project defaults do not overwrite them.

Layout-direction and translation notifications reflow geometry and invalidate text; global locale/catalog edits require an explicit translation notification under ADR 0007. Existing locale direction still requires a matching registered catalog or fallback language. [ProjectLayoutDirectionTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionTests.cs) covers resolution, lifetime and reflow; [ProjectLayoutDirectionNativeTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionNativeTests.cs) checks GUI/text pixels on Linux Wayland GPU/compatibility. Foreign targets and owner visual acceptance remain unverified.

## Pointer capture transfer and coordinates

Control.GrabClickFocus requests the last target in its viewport at the deferred boundary. Without a current mouse capture it does nothing. Held left/right/middle buttons produce temporary internal releases on the old control and internal presses on the new control in the next deferred batch. Both batches drain within the same flush; subsequent real releases follow the new capture. These events reach the target directly, preserve global Input button state and keyboard focus, and do not bubble. Real motion/releases then follow the new capture until all held buttons release.

Pending targets are weak and cancel after removal/disposal or viewport teardown. All releases are attempted after callback failure; a successful capture handoff still schedules its presses before reporting errors. Newer capture or physical release cancels stale press delivery. Runtime capture requests are not scene properties. Actual synthetic event delivery allocates temporary owned input objects; coalesced requests reuse their cached queue actions. Singular transforms reject transfer before releasing the previous capture.

GetScreenPosition returns GetScreenTransform().Origin in the popup host's coordinate space. Logical projection permits the owner or active processing groups with caller coordination; required native desktop lookup retains its separate owner guard. Embedding viewports stop the chain before their own final transform; only a chain reaching a native Window needs desktop placement. Embedded and standalone offscreen queries therefore work on Wayland when no desktop lookup is required. WarpMouse accepts finite local coordinates and composes current canvas/viewport transforms into the containing native window. Embedded viewports include their container transform and stretch scale. Invalid native ranges and missing/unsupported host capabilities fail explicitly.

ControlPointerTests checks coalescing, two deferred batches, primary buttons, transformed coordinates, keyboard-focus isolation, disposal/viewport removal, singular transforms, exceptions, real releases after the completed transfer and 64 warmed same-target requests with zero managed bytes. Native Wayland GPU/compatibility tests verify actual capture-driven pixels and explicit unavailable warp/desktop-position boundaries. Dummy compatibility uses nearest sampling for executable embedded warp coordinates. Other platform/hardware verification remains separate.

## Automatic translation context

`Control.TranslationContext` is a stored local string, empty by default. `Atr`/`AtrN` use it when the call's context is null or empty; a nonempty call argument overrides it. `Tr`/`TrN` retain their explicit context. The property is not inherited from a parent. Set it before assigning text: changing the context alone sends no notification and preserves already prepared captions. `Notify(Node.NotificationTranslationChanged)` refreshes that control; `PropagateNotification` can refresh its subtree. Existing automatic mode, object enablement and inherited domain rules still apply.

```csharp
var caption = new Label { TranslationContext = "action", Text = "Open" };
caption.TranslationContext = "status";
caption.Notify(Node.NotificationTranslationChanged);
```

Null assignment throws `ArgumentNullException`; disposed access throws `ObjectDisposedException`. Attached property access requires the scene owner thread and writes are rejected during scene capture. Equal writes are silent. Context values copy independently through PackedScene and file loading.

`GUITranslationContextTests` exercises singular/plural lookup through base references, explicit overrides, policy disabling, descriptors, cached Label/RichTextLabel captions, Button/OptionButton consumers, owner/disposal checks, fresh file processes and zero warmed managed bytes. Its native profile compares actual initial/refreshed glyph pixels with independently authored expected labels on Linux Wayland GPU and compatibility. Other platforms and native allocations remain unverified.

[VirtualJoystick](VirtualJoystick.md) consumes the inherited local GUI touch lane, ordinary themed StyleBoxes and owner/capture/lifetime rules for analog game controls.

Reparent preserves the inherited [membership/owner and failure contract](../components/scene-hierarchy.md#membership-and-authoring-ownership), including same-parent lifecycle/capture/thread checks. It restores only global position after possible insertion, even when earlier callbacks failed.

The [complete processing metadata contract](../components/scene-tree.md#complete-processing-metadata-and-callbacks) permits coordinated stored-value reads and current-group writes for all callback switches and priorities. All six public callback switches are stored; internal switches are runtime-only. Actual input remains owner-delivered, and this metadata grants no physical/render/hierarchy mutation.

Logical transform observation permits the scene owner or any active processing group, including shared ancestors and nodes in another group. This covers Entity local/global position, transform, rotation/degrees, scale/skew and pure point, angle and relative-chain queries; concrete Control transforms, combined pivots and derived rectangles follow the same logical chain. Coordinate concurrent writes: separate reads do not form an atomic scene snapshot. Ordinary external or pool jobs receive no group permission. Mutations and numerical/renderer operations retain their separate guards and remaining Partial coverage.

A dirty global matrix is computed through virtual getters outside the retained canvas-item gate. One publisher commits the matrix and clears a volatile invalidation flag under that gate. This preserves later transform notification queueing without concurrent matrix writes or a new per-node lock allocation. [Public concurrent physical consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.GroupTransforms.cs) exercise ordinary and pool-owned trees, mixed ancestry, errors, stable identities and complete moving physics/query intervals with zero warmed managed bytes. Evidence: `bin/physics-group-transforms/2026-10-18/validation.json`. See the [logical observation contract](../components/scene-hierarchy.md#logical-transform-observation-from-processing-groups); full worker authoring and hardware performance remain open.

## Viewport GUI pixel snapping

GUISnapControlsToPixels is an independent stored property on every Window and SubViewport, true initially. For an attached Control whose primary Rotation satisfies `abs(sin(Rotation * 4)) < 0.00001`, rendering rounds the complete local transform origin with `floor(origin + 0.5)`. The basis and size stay unchanged. A logical offset transform participates before rounding; a visual-only offset composes afterwards. Parent Entity and CanvasLayer matrices, canvas scaling and final vertex snapping retain their own stages. Ineligible rotations and detached controls retain fractional local origins. This is visual geometry: public Position/GetTransform/GetGlobalTransform/rectangles, layout, input, focus and physics coordinates remain fractional.

Visual submissions sample the current flag without rerecording Control commands; normal mutation invalidates low-processor rendering. No logical transform/rectangle notification is emitted by the flag alone. Explicit RenderingServer transform overrides retain precedence until a subsequent actual authored transform change. Interpolation retains its existing visual-pose rules. GUI snapping is separate from SnapTransformsToPixel and SnapVerticesToPixel and has no project-setting or server-only viewport mirror. Getters allow coordinated owner/active-group reads; attached mutation requires the owner and rejects capture, including equal assignments. Disposed access throws.

See [verification and limits](../components/canvas-rendering.md#gui-control-pixel-snapping).

## Native accessible metadata

<a id="accessibilityname"></a>
`public string AccessibilityName { get; set; }` stores an authored name, empty by default. A nonempty translated value overrides the prepared displayed caption; Button/LinkButton/Label otherwise publish their ordinary prepared text. An icon-only action needs an authored name. `public string AccessibilityDescription { get; set; }` stores supplementary text, also empty by default. Both reject null/disposed/off-owner mutations, use the Control translation context and refresh on translation notifications. Equal authored/resolved values coalesce; packed/fresh-file scenes and duplication retain source strings.

Automatic output publishes role, transformed window-client bounds, visibility, clipping, button enabled/check state and registered focus/activation actions. `FocusMode.Accessibility` is eligible while real automatic output or manually published elements activate the native runtime. Native requests reuse ordinary GUI focus and BaseButton activation; hidden, disabled, detached, queued and disposed sources reject activation. Composite semantic focus and Node notifications are described on [Node](Node.md#native-accessibility). Rich text/value/table/relationship behavior remains in coverage, as do foreign-platform and human reader acceptance.

Protected `override string[] OnGetAccessibilityConfigurationWarnings()` adds a warning for an unnamed BaseButton action or Container landmark. It preserves base warnings; authored names or displayed button captions resolve the warning. This is a cold authoring query.

Name, landmark and displayed-caption changes refresh the existing edited-scene configuration-warning notification; equal writes remain silent.

<a id="accessibilitycontrolsnodes"></a>
<a id="accessibilitydescribedbynodes"></a>
<a id="accessibilitylabeledbynodes"></a>
<a id="accessibilityflowtonodes"></a>
### AccessibilityControlsNodes, AccessibilityDescribedByNodes, AccessibilityLabeledByNodes, AccessibilityFlowToNodes

Each string array starts empty and is copied on assignment and retrieval. Null arrays throw ArgumentNullException, null items and malformed node-path syntax throw ArgumentException; access requires a live owner-thread control and mutation respects capture guards. Empty and unresolved paths are ignored; normal relative/absolute Node path rules apply. Authored paths survive duplication and scene archives; process-local RIDs do not. Topology, native-identity and property revisions resolve current targets again. Automatic publication ignores targets in a different native window and deduplicates repeated targets. Native forward targets retain current authored array order, including pure reorders, before additional manual targets. Clearing or retargeting an array removes only its automatic links; explicit AccessibilityServer links survive until their own lifetime ends.

ControlsNodes supplies ControllerFor/ControlledBy, DescribedByNodes supplies DescribedBy/DescriptionFor, LabeledByNodes supplies LabelledBy/LabelFor, and FlowToNodes supplies FlowsTo/FlowsFrom. An otherwise unnamed action with a resolved nonempty same-window label does not need a duplicate name; invalid/missing labels leave its warning. ButtonGroup toggle peers publish current directed MemberOf relations; toggling mode, changing membership, leaving the tree or changing native windows updates the graph. See [the service reference](AccessibilityServer.md#semantic-relationships) and [ADR 0114](../decisions/accessibility.md#adr-0114).

## Live region policy

AccessibilityLive is a stored [AccessibilityLiveMode](AccessibilityServer.AccessibilityLiveMode.md) property, default Off. Assignment requires a live owner-thread control, respects capture guards and rejects unknown values. Duplication and scene archives retain the authored policy. Source text/captions and ordinary GUI tooltip/menu behavior feed [native live and popup output](AccessibilityServer.md#native-live-and-popups); explicit service overrides last until the corresponding source field changes.

<a id="accessibilitydrag"></a><a id="accessibilitydrop"></a>
## Accessible drag operations

| Declaration | Contract |
| --- | --- |
| `public void AccessibilityDrag()` | Prepares one source transaction, invoking its virtual/forwarded producer with positive-infinite coordinates. |
| `public void AccessibilityDrop()` | Tries the current payload on this control/ancestors without hit testing, then finalizes. |

Both require an attached owner-thread control and respect capture/lifetime guards. Explicit targets may use MouseFilter.Ignore. The public finite-position convenience methods retain their existing validation; their underlying hooks can receive the accessibility sentinel. Full ownership, failure, keyboard, text/capacity and native consumers are defined in [the service contract](AccessibilityServer.md#custom-actions-and-accessible-drag).

## Contextual accessible navigation

The two context hooks connect custom container position and keyboard help to actual native names/HelpText. They refresh on queued source or ancestor updates, topology and locale changes. Non-Control parents end forwarding. Explicit name overrides remain until the source name/context changes. [The service contract](AccessibilityServer.md#contextual-navigation) owns composition, callback lifetime, native shortcut sources and measured limits.
