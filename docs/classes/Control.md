# Control

Last updated: 2026-09-26

**Inherits:** [CanvasItem](CanvasItem.md) → [Node](Node.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** No production type yet. The accepted GUI branch will place BaseButton and Button here.

- **Source:** [Control.cs](../../src/Scene/GUI/Control.cs), [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs), [Control.Focus.cs](../../src/Scene/GUI/Control.Focus.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Control : CanvasItem`

## Description

Control is the rectangular UI branch beside [Entity](Entity.md). It inherits the scene tree and canvas rendering API. A direct Control parent supplies the area for anchors; a root Control uses its viewport's visible size. A direct non-Control canvas parent supplies a zero-size anchor area in this slice. Detached controls also use a zero-size anchor area. Offsets are local canvas units; anchors are fractions of the parent area. Changes to the parent rectangle or viewport size reflow an attached control synchronously. Pivot, rotation and scale change the canvas transform without changing its layout rectangle. Control itself emits no drawing commands.

Control starts with inherited `PhysicsInterpolationMode.Off`, so UI layout and pointer hit regions normally use the current pose. Assign `On` explicitly to interpolate a Control and its inheriting canvas children while the tree enables physics interpolation. The displayed transform may then differ from current logical layout coordinates until the next physics tick; `ResetPhysicsInterpolation` removes that history.

`SetAnchorsPreset` applies one of the sixteen [`LayoutPreset`](LayoutPreset.md) arrangements in left, top, right, bottom order. By default it changes offsets to keep the current rectangle in place; `keepOffsets: true` retains local offsets so the rectangle follows the new anchors immediately. The method works while detached or attached, and an invalid preset leaves all four anchors unchanged.

`SetOffsetsPreset` positions all four edges in one reflow without changing anchors. [`LayoutPresetMode`](LayoutPresetMode.md) selects intrinsic minimum or current width/height; wide presets stretch their selected axis regardless of that choice. Signed margins move edge placements, while center placements ignore margin. `SetAnchorsAndOffsetsPreset` applies the anchor step before the offset step; an invalid offset mode leaves the completed anchor step in place. RTL uses the pinned negative horizontal span and then mirrors the resolved rectangle. A root Control also uses the visible viewport rectangle's origin. `SetAnchorAndOffset` sets one anchor before its explicit offset, with opposite-anchor pushing disabled by default.

`CustomMinimumSize` combines componentwise with an intrinsic minimum supplied by `OnGetMinimumSize` and zero. `CustomMaximumSize` combines enabled bounds from the intrinsic hook, the caller and a direct parent that propagates its maximum; a negative component means unbounded. Reflow applies the minimum first and then the maximum, so the maximum wins on a conflicting axis. [`GrowDirection`](GrowDirection.md) independently chooses the fixed horizontal and vertical edges for growth or shrinkage. Attached visible controls coalesce size changes through `SceneTree.Defer`; `Resized` occurs before the corresponding size-change event. Both bounds are queryable immediately. Container relayout, desired-size cache and wrapping windows remain open.

[`LayoutDirection`](LayoutDirection.md) adds explicit LTR/RTL mirroring after horizontal minimum/maximum resolution. Inherited controls follow the nearest Control in the same translation domain; `ApplicationLocale` and `SystemLocale` use the corresponding managed culture only when a matching catalog or configured fallback permits RTL. Direction changes notify the subtree before callers observe the resolved rectangles. `Position` and `Size` writes remain physical in RTL. Root and forced project direction settings, Window inheritance, exact locale aliases and automatic scene refresh after a process-wide culture change remain Partial.

`SetPosition`, `SetSize` and `SetGlobalPosition` share the property setters' geometry path. By default they update stored offsets. With `keepOffsets: true`, they calculate anchors from the requested physical rectangle and existing offsets; both parent-area axes must be nonzero or the call fails before mutation. `SetSize` clamps finite requests through the effective minimum and then maximum, including negative requests; `ResetSize` requests zero through that same path. `GetBegin`/`SetBegin` and `GetEnd`/`SetEnd` read or update the corresponding offset pairs with one reflow per write. A global position edit first inverse-transforms through the direct canvas parent and retains the control's pivot/rotation/scale origin.

`PivotOffsetRatio` adds a size-relative component to the ordinary pivot; `GetCombinedPivotOffset()` reports the sum. The additional offset transform combines absolute and size-relative position and pivot with rotation and scale, then composes after the ordinary pivot transform. It is disabled by default and retains configured values while disabled. When enabled, `OffsetTransformVisualOnly` defaults to true: drawing and visual child transforms move, while logical `GetTransform`, global queries and GUI hit testing stay in place. Turning visual-only off applies it to both logical and rendering transforms. The renderer uses this distinction in ordinary and Y-sorted canvas traversal.

A zero additional scale is accepted. When visual-only is false it makes the logical transform singular, so coordinate queries requiring an inverse fail under the ordinary CanvasItem contract.

The root viewport routes pointer events by the transformed rectangle and sends keyboard input to the focused control between `OnInput` and unhandled input. `MouseFilter` controls target selection, bubbling and hover. Hover transitions notify controls and select native cursor shapes. Tab and arrow navigation use InputMap actions and focus paths. Full GUI behavior remains partial: stationary-pointer geometry changes, exact directional ranking and scroll clipping, touch routing, exact renderer draw ordering, nested viewports, accessibility, themes, container sizing, full locale direction policy, and button behavior are absent. See [Control coverage](../coverage/classes/Control.md) for individual gaps.

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
| `public Vector2 Position { get; set; }` | Upper-left layout position before pivot and scale. |
| `public Vector2 Size { get; set; }` | Finite requested size, clamped to the effective minimum and maximum. |
| `public bool ClipContents { get; set; }` | False by default; clips direct canvas descendants and their root-viewport pointer targeting to this rectangle. |
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
| `public MouseBehaviorRecursive MouseBehaviorRecursive { get; set; }` | Inherited by default; disables or restores pointer input in a direct Control subtree. |
| `public CursorShape MouseDefaultCursorShape { get; set; }` | Arrow by default; a hovered control refreshes the native cursor after a change. |
| `public bool MouseForcePassScrollEvents { get; set; }` | True by default; permits wheel bubbling through Stop. |
| `public FocusMode FocusMode { get; set; }` | None by default; Click permits pointer or explicit focus; All also permits action navigation. |
| `public FocusBehaviorRecursive FocusBehaviorRecursive { get; set; }` | Inherited by default; disables or restores focus eligibility in a direct Control subtree. |
| `public string FocusNext { get; set; }` / `FocusPrevious` | Relative paths for forward and backward focus traversal; empty by default. |
| `public string FocusNeighborLeft { get; set; }` / `FocusNeighborTop` / `FocusNeighborRight` / `FocusNeighborBottom` | Relative paths for directional navigation; empty by default. |

## Methods and extension points

| Member | Contract |
| --- | --- |
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
| `public void UpdateMaximumSize()` | Coalesces maximum updates and refreshes direct child control bounds. |
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
| `public event Action? FocusEntered` | Raised after NotificationFocusEnter when this control gains keyboard focus. |
| `public event Action? FocusExited` | Raised after NotificationFocusExit when focus is released or transferred. |
| `public event Action? MouseEntered` | Raised when the pointer enters the control or a reachable child. |
| `public event Action? MouseExited` | Raised when the pointer leaves the control and reachable children. |
| `public const int NotificationResized = 40` | Delivered before Resized, after the rectangle and transform are committed. |
| `public const int NotificationLayoutDirectionChanged = 49` | Propagated parent-first when a Control's direction policy changes. |
| `public const int NotificationMouseEnter = 41` / `NotificationMouseExit = 42` | Delivered before the corresponding hover event. |
| `public const int NotificationFocusEnter = 43` / `NotificationFocusExit = 44` | Delivered before the corresponding focus event. |
| `public const int NotificationMouseEnterSelf = 60` / `NotificationMouseExitSelf = 61` | Delivered when the direct hover target changes. |

## Nested enums

| Enum | Values |
| --- | --- |
| [CursorShape](Control.CursorShape.md) | Seventeen system cursor identities from Arrow = 0 through Help = 16. |

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

`FocusMode` defaults to None, rejects unsupported enum values, and releases active focus when set back to None. Click permits focus by left pointer press or explicit `GrabFocus`; All additionally permits automatic action navigation. `hideFocus` records hidden visual focus, reported as absent by `HasFocus(ignoreHiddenFocus: true)`. `GrabFocus` requires an attached control and has no effect while hidden, in None mode or outside a root Viewport. `ReleaseFocus` does nothing when this control is not focused. Hiding or detaching the control releases focus. A focus transfer commits before the previous control's NotificationFocusExit and FocusExited; the root Viewport then raises GUIFocusChanged, followed by the new control's NotificationFocusEnter and FocusEntered. Explicit release sends only the exit notification and event. Changing `hideFocus` on the current owner redraws it without another notification. Focus changes request redraw. Callback failures are collected while later focus callbacks continue, then propagate as AggregateException; focus state remains committed. Reentrant release suppresses a pending enter notification or event after focus is cleared.

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

OnNotification subscribes to the direct canvas parent's geometry or root viewport size at canvas entry, disconnects at exit, releases focus when hidden or detached, and emits Resized after NotificationResized. The focus notifications 43/44 pass through this protected hook before the corresponding event. GetPropertyDescriptors stores rectangle, transform, anchors, offsets, mouse filter, wheel policy, focus mode and focus paths for PackedScene. The factory creates exact Control instances. Disposal removes borrowed event subscriptions and clears the control events.

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

The top hit Control in a root viewport receives a temporary local pointer event. The root GUI picker orders by canvas layer, effective Z and reverse scene traversal; it does not yet match every renderer ordering rule. `Ignore` is skipped; `Pass` continues through direct Control parents until handled or a `Stop` control; `Stop` handles the event automatically. Wheel events pass a `Stop` control when `MouseForcePassScrollEvents` is true. A left-button press retains its target for the corresponding release and held-pointer motion. A left press focuses an eligible control, with hidden visual focus. Explicit `GrabFocus` takes focus without hiding it; hiding, detaching or setting `FocusMode` to None releases it. Keyboard, controller and action events reach the focused control without bubbling. `AcceptEvent` stops later GUI and unhandled stages. Unhandled `ui_*` actions traverse focus after GUI delivery. Failures are aggregated after other eligible scene callbacks run; positional GUI copies are disposed after synchronous delivery. Hover uses the same root picker and respects clipping ancestors. Multiple-button capture and nested viewport routes remain incomplete.

`FocusNext` and `FocusPrevious` take precedence over automatic traversal; an invalid path returns null. Directional paths can chain through ineligible controls, with cycle protection, then fall back to spatial search. Automatic traversal accepts visible `All` controls in scene order within the root viewport; explicit paths may select visible `Click` controls. Directional search uses global axis-aligned rectangles, not the full reference ranking or scroll clipping. Navigation happens after focused GUI callbacks if they leave the event unhandled. Analog navigation acts on a new press transition rather than every held motion event. No native keyboard or controller navigation has been verified for this slice.

Targeted scene-hierarchy pixel checks passed on Linux Wayland compatibility/GPU and dummy/software in an earlier slice; they do not verify this navigation change.

**Decisions:** [scene hierarchy](../decisions/scene.md#adr-0008), [typed 2D API](../decisions/product.md#adr-0004), [rendering](../decisions/rendering.md#adr-0028).

[NinePatchRect](../classes/NinePatchRect.md) now records one retained panel command with fixed borders, independent Stretch/Tile/TileFit axes and optional center. Live base/atlas dimensions resolve before splitting; ordinary atlas region drawing reuses that resolver. Signed margins drive Control intrinsic minimum size and inherited pointer filtering defaults to Ignore. All nine native axis combinations, center/flip/atlas/constant UV and 64 warmed resized frames are checked by [NinePatchRenderingTests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs), under [ADR 0079](../decisions/rendering.md#adr-0079). Dense CPU geometry limits, native allocator counts, other platforms and owner acceptance remain explicit.
