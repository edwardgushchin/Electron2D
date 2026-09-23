# Control

Last updated: 2026-09-23

**Inherits:** [CanvasItem](CanvasItem.md) → [Node](Node.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** No production type yet. The accepted GUI branch will place BaseButton and Button here.

- **Source:** [Control.cs](../../src/Scene/GUI/Control.cs), [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs), [Control.Focus.cs](../../src/Scene/GUI/Control.Focus.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Control : CanvasItem`

## Description

Control is the rectangular UI branch beside [Entity](Entity.md). It inherits the scene tree and canvas rendering API. A direct Control parent supplies the area for anchors; a root Control uses its viewport's visible size. A direct non-Control canvas parent supplies a zero-size anchor area in this slice. Detached controls also use a zero-size anchor area. Offsets are local canvas units; anchors are fractions of the parent area. Changes to the parent rectangle or viewport size reflow an attached control synchronously. Pivot, rotation and scale change the canvas transform without changing its layout rectangle. Control itself emits no drawing commands.

The root viewport routes pointer events by the transformed rectangle and sends keyboard input to the focused control between `OnInput` and unhandled input. `MouseFilter` controls target selection, bubbling and hover. Hover transitions notify controls and select native cursor shapes. Tab and arrow navigation use InputMap actions and focus paths. Full GUI behavior remains partial: content clipping, stationary-pointer geometry changes, exact directional ranking and scroll clipping, touch routing, exact renderer draw ordering, nested viewports, accessibility, themes, container sizing, layout direction, and button behavior are absent. See [Control coverage](../coverage/classes/Control.md) for individual gaps.

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

| Member | Contract |
| --- | --- |
| `public Vector2 Position { get; set; }` | Upper-left layout position before pivot and scale. |
| `public Vector2 Size { get; set; }` | Local nonnegative layout size. |
| `public float Rotation { get; set; }` | Radians around PivotOffset. |
| `public float RotationDegrees { get; set; }` | Degrees around PivotOffset. |
| `public Vector2 Scale { get; set; }` | Local scale; zero components become a small positive epsilon. |
| `public Vector2 PivotOffset { get; set; }` | Local pivot for rotation and scale. |
| `public Vector2 GlobalPosition { get; set; }` | Transformed origin in global canvas coordinates. |
| `public float AnchorLeft { get; set; }` | Left anchor fraction. |
| `public float AnchorTop { get; set; }` | Top anchor fraction. |
| `public float AnchorRight { get; set; }` | Right anchor fraction. |
| `public float AnchorBottom { get; set; }` | Bottom anchor fraction. |
| `public float OffsetLeft { get; set; }` | Left local offset. |
| `public float OffsetTop { get; set; }` | Top local offset. |
| `public float OffsetRight { get; set; }` | Right local offset. |
| `public float OffsetBottom { get; set; }` | Bottom local offset. |
| `public ControlMouseFilter MouseFilter { get; set; }` | Stop by default; Pass bubbles; Ignore does not receive or block pointer events. |
| `public CursorShape MouseDefaultCursorShape { get; set; }` | Arrow by default; a hovered control refreshes the native cursor after a change. |
| `public bool MouseForcePassScrollEvents { get; set; }` | True by default; permits wheel bubbling through Stop. |
| `public ControlFocusMode FocusMode { get; set; }` | None by default; Click permits pointer or explicit focus; All also permits action navigation. |
| `public string FocusNext { get; set; }` / `FocusPrevious` | Relative paths for forward and backward focus traversal; empty by default. |
| `public string FocusNeighborLeft { get; set; }` / `FocusNeighborTop` / `FocusNeighborRight` / `FocusNeighborBottom` | Relative paths for directional navigation; empty by default. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| `public float GetAnchor(Side side)` | Reads one anchor fraction. |
| `public void SetAnchor(Side side, float anchor, bool keepOffset = false, bool pushOppositeAnchor = true)` | Updates one anchor and optionally the opposite anchor. |
| `public float GetOffset(Side side)` | Reads one local offset. |
| `public void SetOffset(Side side, float offset)` | Updates one offset and resolves the rectangle. |
| `public Control? GetParentControl()` | Returns only a direct Control parent. |
| `public Vector2 GetParentAreaSize()` | Returns the active anchor area size or zero while detached. |
| `public Rect GetRect()` | Returns transformed local origin and scale times layout size; not an axis-aligned rotated bound. |
| `public Rect GetGlobalRect()` | Returns transformed global origin and scale times layout size; not an axis-aligned rotated bound. |
| `public override Transform GetTransform()` | Returns translation composed with pivot, rotation and scale. |
| `public override void Reparent(Node newParent, bool keepGlobalTransform = true)` | Moves in the neutral tree; preserves global origin by default, validating a canvas inverse before mutation. |
| `public void AcceptEvent()` | Marks current scene input handled. |
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
| `public event Action<InputEvent>? GUIInput` | Raised after OnGUIInput with the same borrowed event. |
| `public event Action? FocusEntered` | Raised after NotificationFocusEnter when this control gains keyboard focus. |
| `public event Action? FocusExited` | Raised after NotificationFocusExit when focus is released or transferred. |
| `public event Action? MouseEntered` | Raised when the pointer enters the control or a reachable child. |
| `public event Action? MouseExited` | Raised when the pointer leaves the control and reachable children. |
| `public const int NotificationResized = 40` | Delivered before Resized, after the rectangle and transform are committed. |
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

### `MouseDefaultCursorShape`, `GetCursorShape`, `OnGetCursorShape`

The stored property defaults to Arrow, rejects unknown enum values, and is captured by PackedScene. `GetCursorShape` validates finite local coordinates and the override result. The virtual hook returns the stored property by default. While hovering, the scene queries the direct target first, then eligible Control ancestors until a non-Arrow shape or Stop filter is found. A property change refreshes the native cursor on the owner thread.

### `MouseEntered`, `MouseExited`, mouse notifications

Root-viewport pointer motion updates the hover chain even if a Node handled the input. Entry is ancestor first and exit is descendant first; direct-target self notifications are distinct from chain notifications. Each chain notification precedes its event. Hidden or detached controls release their hover chain, and native window exit clears it. Callback failures are collected while later eligible callbacks continue. Clipping, stationary-pointer geometry changes and nested viewport semantics remain incomplete.

### `FocusMode`, `GrabFocus`, `HasFocus`, `ReleaseFocus`

`FocusMode` defaults to None, rejects unsupported enum values, and releases active focus when set back to None. Click permits focus by left pointer press or explicit `GrabFocus`; All additionally permits automatic action navigation. `hideFocus` records hidden visual focus, reported as absent by `HasFocus(ignoreHiddenFocus: true)`. `GrabFocus` requires an attached control and has no effect while hidden, in None mode or outside a root Viewport. `ReleaseFocus` does nothing when this control is not focused. Hiding or detaching the control releases focus. A focus transfer commits before the previous control's NotificationFocusExit and FocusExited; the root Viewport then raises GUIFocusChanged, followed by the new control's NotificationFocusEnter and FocusEntered. Explicit release sends only the exit notification and event. Changing `hideFocus` on the current owner redraws it without another notification. Focus changes request redraw. Callback failures are collected while later focus callbacks continue, then propagate as AggregateException; focus state remains committed. Reentrant release suppresses a pending enter notification or event after focus is cleared.

### `FocusNext`, `FocusPrevious`, `FocusNeighborLeft`, `FocusNeighborTop`, `FocusNeighborRight`, `FocusNeighborBottom`

Each path is an empty string by default, is relative to this Control, and is stored by PackedScene. A nonempty sequential path is consulted before automatic traversal; a missing or non-Control path returns no target. Directional paths can chain through ineligible Controls. Setters reject null; attached mutations require the scene owner thread.

### `GetFocusNeighbor(Side side)`, `SetFocusNeighbor(Side side, string neighbor)`

These access the same four directional paths as the properties. Invalid sides throw `ArgumentOutOfRangeException`; a null new path throws `ArgumentNullException`.

### `FindNextValidFocus()`, `FindPrevValidFocus()`, `FindValidFocusNeighbor(Side side)`

Queries return a borrowed Control or null when detached or no eligible target exists. Sequential automatic traversal wraps through visible `All` Controls in the root viewport. Directional automatic search compares global axis-aligned rectangles; explicit paths can select `Click` Controls. An invalid direction throws `ArgumentOutOfRangeException`. Exact upstream spatial ordering, scroll clipping and nested viewport ownership remain partial.

### `HasPoint`, `OnGUIInput`, `GUIInput`, `AcceptEvent`

The virtual `HasPoint` tests a control-local point against `[0, Size.X) × [0, Size.Y)` by default and can define a custom hit shape. Pointer events are copied into each receiving Control's local coordinates; `OnGUIInput` runs before the `GUIInput` event. The local event is borrowed only during the callback. `AcceptEvent` forwards to the active scene handled flag and throws outside input delivery. Callback failures are collected while eligible parent and later Node callbacks continue.

### `GetAnchor(Side side)`, `SetAnchor(Side side, float anchor, bool keepOffset = false, bool pushOppositeAnchor = true)`

Side chooses left, top, right or bottom. GetAnchor returns its current fraction. SetAnchor accepts any finite fraction; keepOffset retains the current offset instead of the current edge, and pushOppositeAnchor moves the opposite anchor when sides cross. Invalid sides and nonfinite fractions throw ArgumentOutOfRangeException.

### `GetOffset(Side side)`, `SetOffset(Side side, float offset)`

GetOffset returns the selected local distance. SetOffset commits a finite distance and immediately reflows the rectangle. Invalid sides and nonfinite values throw ArgumentOutOfRangeException.

### `GetParentControl()`, `GetParentAreaSize()`

GetParentControl returns the direct parent only when it is a Control. GetParentAreaSize returns that parent's size, or the visible viewport size for a canvas root. A detached control or a direct non-Control canvas parent currently yields zero.

### `GetRect()`, `GetGlobalRect()`, `GetTransform()`

GetTransform composes layout position with pivot, rotation and scale. GetRect and GetGlobalRect use its local or global transformed origin and signed scale times Size; these rectangles are not rotated axis-aligned bounds. An attached global query requires the scene owner thread.

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

[ControlLayoutTests](../../tests/Electron2D.Tests/ControlLayoutTests.cs) check nested anchors, parent and viewport resize propagation, callback order, transform inheritance, packed anchors/offsets, pivot/global position, invalid arguments, owner thread and disposal. [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs) check root viewport GUI input order, local coordinates, filter/bubbling, wheel pass, focus notification and event order, explicit viewport release, failure continuation, pointer release capture and temporary event ownership. [ControlFocusNavigationTests](../../tests/Electron2D.Tests/ControlFocusNavigationTests.cs) checks managed Tab/arrow routing, paths, wrapping, hidden targets and packed state. [ControlHoverTests](../../tests/Electron2D.Tests/ControlHoverTests.cs) checks managed hover ordering, filters, notifications and cursor state; [ControlHoverNativeTests](../../tests/Electron2D.Tests/ControlHoverNativeTests.cs) checks native cursor precedence on Linux Wayland. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) includes a child Sprite pixel check in the native renderer. Full GUI interaction and reference parity remain unverified.

### GUI input behavior

The top hit Control in a root viewport receives a temporary local pointer event. The root GUI picker orders by canvas layer, effective Z and reverse scene traversal; it does not yet match every renderer ordering rule. `Ignore` is skipped; `Pass` continues through direct Control parents until handled or a `Stop` control; `Stop` handles the event automatically. Wheel events pass a `Stop` control when `MouseForcePassScrollEvents` is true. A left-button press retains its target for the corresponding release and held-pointer motion. A left press focuses an eligible control, with hidden visual focus. Explicit `GrabFocus` takes focus without hiding it; hiding, detaching or setting `FocusMode` to None releases it. Keyboard, controller and action events reach the focused control without bubbling. `AcceptEvent` stops later GUI and unhandled stages. Unhandled `ui_*` actions traverse focus after GUI delivery. Failures are aggregated after other eligible scene callbacks run; positional GUI copies are disposed after synchronous delivery. Hover uses the same root picker. Clipping, multiple-button capture and nested viewport routes remain incomplete.

`FocusNext` and `FocusPrevious` take precedence over automatic traversal; an invalid path returns null. Directional paths can chain through ineligible controls, with cycle protection, then fall back to spatial search. Automatic traversal accepts visible `All` controls in scene order within the root viewport; explicit paths may select visible `Click` controls. Arrow search uses global axis-aligned rectangles, not the full reference ranking or scroll clipping. Navigation happens after focused GUI callbacks if they leave the event unhandled. No native keyboard navigation has been verified for this slice.

Targeted scene-hierarchy pixel checks passed on Linux Wayland compatibility/GPU and dummy/software in an earlier slice; they do not verify this navigation change.

**Decisions:** [scene hierarchy](../decisions/scene.md#adr-0008), [typed 2D API](../decisions/product.md#adr-0004), [rendering](../decisions/rendering.md#adr-0028).
