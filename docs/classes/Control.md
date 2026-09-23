# Control

Last updated: 2026-09-23

**Inherits:** [CanvasItem](CanvasItem.md) → [Node](Node.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** No production type yet. The accepted GUI branch will place BaseButton and Button here.

- **Source:** [Control.cs](../../src/Scene/GUI/Control.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Control : CanvasItem`

## Description

Control is the rectangular UI branch beside [Entity](Entity.md). It inherits the scene tree and canvas rendering API. A direct Control parent supplies the area for anchors; a root Control uses its viewport's visible size. A direct non-Control canvas parent supplies a zero-size anchor area in this slice. Detached controls also use a zero-size anchor area. Offsets are local canvas units; anchors are fractions of the parent area. Changes to the parent rectangle or viewport size reflow an attached control synchronously. Pivot, rotation and scale change the canvas transform without changing its layout rectangle. Control itself emits no drawing commands.

This first executable layout slice does not provide focus, mouse routing, accessibility, themes, container minimum/maximum sizing, layout direction, clipping, or button behavior. See [Control coverage](../coverage/classes/Control.md) for individual gaps.

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
| `protected override void OnNotification(int what)` | Connects/disconnects layout sources and raises Resized after NotificationResized. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Supplies stored position, size, rotation degrees, scale, pivot, anchors and offsets. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Creates exact Control instances for PackedScene. |
| `protected override void Dispose(bool disposing)` | Disconnects parent/viewport events and clears Resized. |

## Events and constants

| Member | Contract |
| --- | --- |
| `public event Action? Resized` | Raised synchronously on the owner thread after a size change while attached. |
| `public const int NotificationResized = 40` | Delivered before Resized, after the rectangle and transform are committed. |

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

OnNotification subscribes to the direct canvas parent's geometry or root viewport size at canvas entry, disconnects at exit and emits Resized after NotificationResized. GetPropertyDescriptors stores rectangle, transform, anchors and offsets for PackedScene. The factory creates exact Control instances. Disposal removes borrowed event subscriptions and clears Resized.

### `Resized`, `NotificationResized`

The constant is 40. A size change while attached delivers this notification after ItemRectChanged, then raises Resized synchronously. Position-only changes do not raise Resized. A throwing handler sees committed geometry and stops later synchronous handlers.

## Lifecycle and invariants

Position and Size setters alter the four offsets so the currently resolved rectangle takes the assigned value; they preserve anchor fractions. Size rejects negative or nonfinite input. SetAnchor preserves the current edge by default, and can push an opposite anchor to prevent crossing. SetOffset accepts finite values; if the resolved end precedes the beginning, the visible size clamps to zero. The individual anchor and offset properties call these same methods. Invalid Side values and nonfinite inputs throw ArgumentOutOfRangeException. Rotation, Scale and PivotOffset change only the local transform and request redraw. GlobalPosition uses the inverse of a direct canvas parent's transform and rejects a singular inverse. Attached mutations enforce the scene owner thread. PackedScene stores all four anchors and offsets alongside transform and rectangle state.

When reflow changes position or size, the control commits both values, invalidates global transforms, raises inherited ItemRectChanged, and delivers NotificationResized followed by Resized if size changed. Descendant controls recalculate through the parent's ItemRectChanged event. A throwing subscriber sees the committed values; as with other synchronous canvas events, that exception stops later subscribers. Detaching disconnects the parent or viewport subscription. Reparent preserves the global transformed origin by default, while anchors and size resolve against the new parent. Inherited CanvasItem drawing, visibility, material, Z and coordinate methods remain available.

## Verification and limits

[ControlLayoutTests](../../tests/Electron2D.Tests/ControlLayoutTests.cs) check nested anchors, parent and viewport resize propagation, callback order, transform inheritance, packed anchors/offsets, pivot/global position, invalid arguments, owner thread and disposal. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) includes a child Sprite pixel check in the native renderer. Further layout policies and GUI behavior are tracked in coverage; this class is only partially implemented against the accepted reference API.

Targeted scene-hierarchy pixel checks passed on Linux Wayland compatibility/GPU and dummy/software. The full Wayland rendering suite in this run stopped earlier in CameraRenderingTests on a camera pixel assertion; its remaining stages were not verified by that run.

**Decisions:** [scene hierarchy](../decisions/scene.md#adr-0008), [typed 2D API](../decisions/product.md#adr-0004), [rendering](../decisions/rendering.md#adr-0028).
