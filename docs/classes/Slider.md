# Slider

Last updated: 2026-09-27

**Inherits:** [Range](Range.md), [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** [HSlider](HSlider.md), [VSlider](VSlider.md)

**Declaration:** `public abstract class Slider : Range` · **Source:** [Slider.cs](../../src/Scene/GUI/Slider.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

An interactive double-valued Range with themed track, fill, grabber and ticks. The concrete horizontal/vertical types choose orientation. Pointer drag, wheel, directional actions and Home/End update the inherited Range value; shared peers retain the existing snapping, rounding, bounds and callback policy.

```csharp
using var slider = new HSlider { Name = "Volume", MinValue = 0, MaxValue = 100,
    Value = 50, Size = new Vector2(160, 24), TickCount = 5 };
slider.DragEnded += changed => Console.WriteLine(changed);
// Attach the slider to a Window/Control hierarchy to receive routed GUI input.
```

The built-in Theme supplies actual styles/icons. Override the named theme entries below to change appearance. No Font resource or text renderer is needed by this control.

## API summary

| Signature | Contract/default |
| --- | --- |
| `protected Slider(bool vertical = true)` | Fixed internal axis; Step=1 and FocusMode.All. |
| `public bool Editable { get; set; }` | True; false disables GUI changes while programmatic Value remains writable. |
| `public bool Scrollable { get; set; }` | True; enables wheel Up/Down changes. |
| `public int TickCount { get; set; }` | Signed count, zero initially. |
| `public bool TicksOnBorders { get; set; }` | False; skips first/last ticks. |
| `public TickPosition TicksPosition { get; set; }` | BottomRight initially; unknown values select no tick branch. |
| `public event Action? DragStarted` | Begins a pointer press before its corresponding source value signal. |
| `public event Action<bool>? DragEnded` | Reports the approximately compared start/end ratio change on release. |
| `public enum TickPosition` | [BottomRight=0, TopLeft=1, Both=2, Center=3](Slider.TickPosition.md). |
| `protected override Vector2 OnGetMinimumSize()` | Integer track minimum with normal grabber cross-axis extent. |
| `protected override void OnGUIInput(InputEvent inputEvent)` | Handles applicable pointer and action input. |
| `protected override void OnNotification(int what)` | Handles drawing, theme/hover/lifecycle and internal repeat processing. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores slider fields and exact inherited defaults. |
| `protected override void Dispose(bool disposing)` | Cancels interactions and clears events during deterministic cleanup. |

There is no public orientation switch or CustomStep API. The fixed concrete subclasses supply exact scene factories.

## Properties and inherited defaults

<a id="editable"></a>
**Editable:** an actual change cancels an active drag and queues redraw; an equal write is silent. Disabling also cancels controller repeat. It does not make inherited Value read-only or change FocusMode. Focus remains governed by Control, while rendering chooses the disabled grabber and ignores hover/focus highlighting.

<a id="scrollable"></a>
**Scrollable:** governs only wheel input. It does not disable dragging, keys or controller actions. Wheel Up/Down presses request Value plus/minus Step and may acquire focus when focusable; the wheel factor does not multiply that step.

<a id="tickcount"></a><a id="ticksonborders"></a><a id="ticksposition"></a>
**Ticks:** changed count/border/position values queue redraw; equal writes are silent. Signed counts are preserved; only counts greater than one enter tick drawing. Positions are uniform over grabber travel. When borders are disabled, endpoint ticks are omitted; Both draws both mirrored sides. Undefined position values draw no ticks. Drawing rejects more than 1,048,576 actual tick commands before recording any slider geometry; it does not reject a large count when that state emits no ticks.

**Inherited defaults:** Step is 1, FocusMode is All, MouseFilter remains Stop. Horizontal sliders use horizontal Fill and vertical ShrinkBegin; vertical sliders use horizontal ShrinkBegin and vertical Fill. Range's MinValue=0, MaxValue=100, Page=0 and numeric policies remain inherited. Typed descriptors preserve these actual defaults during revert and scene packing.

## Theme inputs and geometry

| Typed lookup | Name | Consumer behavior |
| --- | --- | --- |
| StyleBox | `slider` | Full track; its minimum defines track thickness. |
| StyleBox | `grabber_area` | Filled portion in ordinary/disabled state. |
| StyleBox | `grabber_area_highlight` | Filled portion when editable and hovered/focused. |
| Texture | `grabber` | Normal grabber; also supplies the cross-axis minimum. |
| Texture | `grabber_highlight` | Hover/focus grabber; motion uses its travel extent. |
| Texture | `grabber_disabled` | Grabber when Editable=false. |
| Texture | `tick` | Tick marks, including mirrored top/left drawing. |
| Constant | `center_grabber` | Nonzero ignores grabber length when computing travel. |
| Constant | `grabber_offset` | Signed cross-axis grabber offset. |
| Constant | `tick_offset` | Signed tick-side offset, reversed for top/left. |

<a id="ongetminimumsize"></a><a id="onnotification"></a>
**Minimum/drawing:** style minimum and normal grabber size are truncated to integer pixels; the primary minimum is the style minimum and the cross minimum is the larger of style/grabber. Ticks and signed offsets do not enlarge it. Draw recording occurs during NotificationDraw before the public Draw event and user OnDraw, in track → fill → ticks → grabber order. Hover/focus selects highlights only when editable.

Horizontal ratios increase left-to-right in LTR and reverse in RTL. Vertical ratios increase bottom-to-top. Placement preserves the source's integer truncation and vertical fill rounding; a NaN Range ratio is drawn as zero. Centered grabbers may extend beyond the control's primary bounds. Theme resources remain borrowed and their changes use the existing ThemeOwner invalidation path. Built-in icons are created at scale one; nonunit default-asset scaling remains a ThemeDB dependency.

## Input and event descriptions

<a id="onguiinput"></a><a id="dragstarted"></a><a id="dragended"></a>
**Pointer press/drag/release:** the press records the starting ratio and integer pointer origin and invokes DragStarted. For a changed click value, the first shared Range pass runs the source value hook without its local ValueChanged signal while peers receive their usual delivery. The drag state is then installed and a forced shared pass notifies source and peers even if the snapped value is unchanged. Thus changed-click peers can receive two deliveries. The ordinary Range helper remains responsible for required-phase continuation after callback errors. A forced nested gesture temporarily lifts the source's local signal suppression and restores it in finally, so an outer suppressed pass cannot erase the inner gesture's own ValueChanged; ordinary recursive suppression remains intact.

Motion uses displacement from the press origin and the highlighted grabber's travel, negated for vertical/RTL. Nonpositive motion travel returns without updating; the press path retains Range's NaN/Infinity behavior for zero travel. An editable left release clears dragging and emits DragEnded with approximate start/end ratio comparison even if no grab is currently active. Drag cancellation caused by hiding, detaching, disposal or an Editable change does not fabricate a release event. A lifecycle generation prevents a callback from installing an old press after cancellation or reentry.

**Actions:** directional presses/keyboard echoes change one Step on the matching axis and accept the GUI event. Horizontal direction follows RTL; opposite-axis actions remain available to normal GUI navigation. ui_home/ui_end request MinValue/MaxValue through Range, so Page/snapping still apply. Their permanent default bindings are Key.Home/Key.End and remain rebindable through InputMap.

**Joypad repeat:** only the first matching joypad press starts the internal process lane. It waits 0.5 seconds, then repeats at 0.05-second intervals using the scene delta and current held actions, with at most one repetition step per process callback rather than an unbounded catch-up loop. Release resets the delay. Hiding, leaving the tree, disabling editing or losing focus cancels repeat so an inactive control cannot keep changing its value; this bounded correction is recorded in ADR 0080.

## Storage, lifecycle and verification

<a id="getpropertydescriptors"></a><a id="dispose"></a>
Stored state includes slider properties and inherited Range/Control/theme values, not active dragging, hover, pending repeat or event subscribers. Exact HSlider/VSlider factories preserve their fixed axis. Owner-thread/capture/lifetime guards apply to state changes and input. Invalid drawing resources or computed nonfinite geometry fail through the existing retained recording contract, which clears failed partial recordings.

SliderTests verifies defaults, themed geometry/ticks, drag/shared signals including nested peer gestures, keyboard/wheel/joypad repeat, lifecycle and packing. Reused drag events and viewport-routed keyboard actions allocate zero managed bytes across 64 measured cycles after 64 warmup cycles. SliderRenderingTests passes eight pixel/input phases through queued SDL events and the real DisplayServer/SceneTree GUI path on Linux Wayland GPU and compatibility; 64 measured active frames after 64 warmup frames allocate zero managed bytes from ProcessFrameStarted through FramePostDraw. Font resources remain a separate text dependency; the first semantic accessibility service must publish the Slider role and inherited Range value/bounds/editable metadata. Physical controller hardware, native allocator counts, broad GUI performance, other platforms and owner acceptance are not implied by managed input or native pixel tests. See [coverage](../coverage/classes/Slider.md), [ADR 0080](../decisions/rendering.md#adr-0080) and [ADR 0083](../decisions/rendering.md#adr-0083).
