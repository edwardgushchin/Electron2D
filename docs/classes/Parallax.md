# Parallax

Last updated: 2026-09-23

- Declaration: `public class Parallax : Entity`
- Source: [Parallax.cs](../../src/Scene/2D/Parallax.cs)
- Inherits: [Entity](Entity.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#parallax-scrolling)
- Coverage: [Parallax2D](../coverage/classes/Parallax2D.md)

## Description

Parallax positions its canvas subtree in response to the current camera. `ScrollScale` changes the apparent scrolling speed; `ScrollOffset` adds a manual displacement. `RepeatSize` and `RepeatTimes` draw copies of each descendant CanvasItem without copying nodes or invoking draw callbacks again. It is an ordinary spatial Entity for hierarchy and transform queries. `Position` is calculated during scroll updates and is omitted from packed scene state.

Camera updates assign `ScreenOffset` unless `IgnoreCameraScroll` is true. Attached mutations and callbacks run on the scene owner thread. The root Window is the currently supported rendering viewport. Physics interpolation is an inherited Node gap recorded in coverage.

## Example

```csharp
var background = new Parallax
{
    ScrollScale = new Vector2(0.5f, 1f),
    RepeatSize = new Vector2(256, 0),
    RepeatTimes = 2,
};
background.AddChild(new Sprite { Texture = texture }); // texture is a live caller-owned Texture.
window.AddChild(background); // window is the active root Window.
```

The Sprite and Texture retain their normal ownership. Repeating the drawing does not duplicate either object.

## API summary

| Declaration | Contract |
| --- | --- |
| `public Parallax()` | Detached node with camera following and one extra repeat copy by default. |
| `public Vector2 Autoscroll { get; set; }` | Offset velocity in canvas units per process second. |
| `public bool FollowViewport { get; set; }` | Include camera screen position in the calculated offset. |
| `public bool IgnoreCameraScroll { get; set; }` | Retain manual ScreenOffset across camera movement. |
| `public Vector2 LimitBegin { get; set; }` | Lower camera scroll limit per axis. |
| `public Vector2 LimitEnd { get; set; }` | Upper camera scroll limit per axis. |
| `public Vector2 RepeatSize { get; set; }` | Local copy spacing; zero disables an axis. |
| `public int RepeatTimes { get; set; }` | Number of additional copies along each enabled axis. |
| `public Vector2 ScreenOffset { get; set; }` | Camera-derived or manual screen origin. |
| `public Vector2 ScrollOffset { get; set; }` | Manual displacement; autoscroll changes this value. |
| `public Vector2 ScrollScale { get; set; }` | Per-axis camera scroll multiplier. |
| `protected override void OnNotification(int what)` | Advances automatic scroll on the internal process notification. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores typed settings and hides calculated Position. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Supplies the exact-type packed-scene constructor. |

Inherited spatial, canvas and tree members remain on [Entity](Entity.md), [CanvasItem](CanvasItem.md) and [Node](Node.md). No new public methods, events, constants or enum types are declared.

## Property descriptions

### Autoscroll

`Vector2.Zero` initially. A nonzero velocity enables the internal process lane only when at least one `RepeatSize` axis is nonzero. Each process frame adds velocity times the scaled process delta to `ScrollOffset`, wraps enabled axes by their local repeat size, then recalculates `Position`. With no repeat axis, the stored velocity has no running effect. Nonfinite values are rejected.

### FollowViewport

`true` initially. When false, the calculation subtracts `ScreenOffset` after applying camera scaling and manual offsets. Setting this policy alone does not recalculate `Position`; the next camera, scroll or autoscroll update does.

### IgnoreCameraScroll

`false` initially. When true, camera notifications leave `ScreenOffset` unchanged, allowing manual scrolling. Toggling the flag does not erase or immediately replace the stored offset.

### LimitBegin and LimitEnd

Initially `(-10000000, -10000000)` and `(10000000, 10000000)`. The camera-derived offset is clamped per axis to `[LimitBegin, LimitEnd - viewport size]` only when that interval is valid. These limits affect the scroll calculation, never the Camera or Viewport. Setting a limit alone takes effect on the next scroll update. Nonfinite vectors are rejected.

### RepeatSize

`Vector2.Zero` initially. Negative components clamp to zero; nonfinite components are rejected. Every enabled axis adds repeated copies separated by that local distance transformed by the Parallax canvas basis. Setting the value updates automatic processing and position immediately while attached. A zero scale axis cannot form a scroll period and uses the nonrepeating scroll formula for that axis.

### RepeatTimes

`1` initially. Values below one clamp to one. The renderer draws `RepeatTimes + 1` copies on each enabled axis and one copy on each disabled axis. For `RepeatTimes = 1`, copies start at the original and one positive offset; higher counts begin at `-floor(RepeatTimes / 2) * RepeatSize`. Setting this value changes the next submitted drawing without changing node transforms or draw callbacks. Large counts multiply work per drawable item.

### ScreenOffset

`Vector2.Zero` initially. A current Camera assigns its adjusted top-left screen position when it updates; transform pixel snapping rounds this position using viewport parity. Setting the value manually recalculates `Position` while attached. `IgnoreCameraScroll` prevents later camera overwrites. Nonfinite values are rejected.

### ScrollOffset

`Vector2.Zero` initially. Setting the manual offset recalculates `Position` while attached. Direct writes preserve the supplied value; only automatic frame advancement wraps enabled repeat axes. Nonfinite values are rejected.

### ScrollScale

`Vector2.One` initially. A scale of `1` follows the camera, a fraction scrolls more slowly, and `0` removes camera displacement on that axis. Setting it stores the finite value; the next scroll update applies it.

## Protected extension points

`OnNotification` calls the base notification path first, then advances Autoscroll for `NotificationInternalProcess` while attached. A derived override should call `base.OnNotification(what)` to retain this behavior. `GetPropertyDescriptors` extends the Entity descriptors, omitting `Position` and adding all ten Parallax settings. Derived types should include these descriptors when adding their own stored state. `CreateSceneInstanceFactory` returns a static constructor only for the exact Parallax type; a derived type needs its own noncapturing factory for `PackedScene` reconstruction.

## Lifecycle and rendering

On tree entry, Parallax binds to its containing Viewport and computes its current position. The current Camera publishes an adjusted screen origin on view updates. If a parallax transform callback throws, the Viewport attempts the remaining registered updates and then reports the aggregated failures. Auto scrolling uses Node's internal idle lane and inherited pause/process policy. Tree exit unregisters the node even when canvas callbacks fail. `PackedScene` stores the ten settings and inherited spatial orientation/scale, while the calculated `Position` and live Viewport association are not stored.

The renderer finds the nearest repeated Parallax ancestor for each retained CanvasItem, preserving canvas-layer, Z, behind-parent and Y ordering. Copies reuse recorded commands and are transformed only for submission. `TopLevel` or a neutral Node interrupts the direct CanvasItem parent chain. Material/texture backend restrictions still apply to each copy.

## Verification and limits

[ParallaxTests](../../tests/Electron2D.Tests/ParallaxTests.cs) covers defaults, clamping, invalid input, camera and manual movement, limits, follow/ignore policy, automatic wrapping, packed state, detach/reattach, callback-failure continuation and owner-thread guards. Its self-contained native pixel check verifies retained drawing, nearest nested repeat sources, scaled/Y-sorted copies, copy counts and camera motion on Linux Wayland compatibility and GPU backends and dummy/software compatibility. The full Linux Wayland rendering suite also passes. Other platforms, high repeat counts, nested/offscreen viewports, physics interpolation, editor behavior and owner visual acceptance remain unverified or unimplemented as tracked in coverage.

The public name follows [ADR 0004](../decisions/product.md#adr-0004), scene inheritance follows [ADR 0008](../decisions/scene.md#adr-0008), and rendering follows [ADR 0028](../decisions/rendering.md#adr-0028).
