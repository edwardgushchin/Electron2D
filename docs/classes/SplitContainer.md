# SplitContainer

Last updated: 2026-10-01

**Declaration:** `public partial class SplitContainer : Container` · **Source:** [SplitContainer.cs](../../src/Scene/GUI/SplitContainer.cs), [layout](../../src/Scene/GUI/SplitContainer.Layout.cs), [input](../../src/Scene/GUI/SplitContainer.Input.cs).

**Inherits:** [Container](Container.md), [Control](Control.md), [CanvasItem](CanvasItem.md), [Node](Node.md), [ElectronObject](ElectronObject.md). **Inherited By:** [HSplitContainer](HSplitContainer.md), [VSplitContainer](VSplitContainer.md). **Component:** [Canvas rendering](../components/canvas-rendering.md#split-panels).

## Description

Arranges ordinary, locally visible, non-top-level Control children as horizontal or vertical panels. Every boundary has a real internal Control for input, drawing and optional custom children. At least one drag area exists from construction, even without panels. Internal children remain absent from ordinary GetChildCount/GetChildren and packed-scene traversal, while rendering/input/process include them. Custom controls added below drag areas execute at runtime but do not persist through omitted internal branches.

SplitOffsets stores relative integer offsets from weighted default positions. Default positions consider primary Expand flags, positive stretch ratios and truncated bound minima/maxima. The two-expanded-panel profile retains its ratio-based default before range clamping. With more panels, capped weights refit and accumulate fractional default pixels. Bound ranges consider minima/maxima on both sides. Ordinary layout gives the first split priority over overlapping later splits; ClampSplitOffset can select another priority, propagate neighbour constraints and rewrite all offsets to the visual values. Horizontal RTL mirrors physical allocations and pointer delta; vertical orientation retains top-to-bottom primary order.

Container fitting applies fill/shrink, custom bounds, anchors and transform reset. Intrinsic minimum sums bound primary minima plus gaps and takes the largest cross minimum, truncated to pixels. Internal desired-size forwarding uses the same child projection. Collapsed layout uses default positions without discarding stored offsets and hides drag targets. The dragger visibility mode controls the ordinary icon and gap, independently of DraggingEnabled; touch images can remain visible in a zero-gap mode.

Structural reconciliation preserves the per-panel desired extents when more than two existing panels are reordered or removed, and after the first removal when panels are added back. A new panel's current primary size has priority; positive expanded siblings grow/shrink, then larger non-expanding panels shrink as needed. Enabled maximum propagation can resize the split itself to the bounded desired total. Pending multi-offset arrays are retained while the child count is incomplete. Initial scene construction does not rewrite pending authored offsets. Locally hidden and top-level panels leave allocation; hidden ancestors retain the eligible local composition.

Attached API access requires the scene owner thread. Mutation rejects capture and logical disposal. Layout snapshots/preflight precede panel fitting; later live panel placements are attempted after callback failures. Immediate orientation/configuration reentry repeats from a fresh pass, bounded at 64. Checked pixel arithmetic and invalid allocations fail explicitly, preserving previous panel rectangles when preflight fails. Property state remains committed after later callback/deferred failures. Resource ownership stays borrowed.

## Example

Partial snippet; the host attaches `split` to a scene:

```csharp
var split = new HSplitContainer { Size = new Vector2(600, 300) };
split.AddChild(new Panel
{
    Name = "Tools", CustomMinimumSize = new Vector2(100, 100),
    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
});
split.AddChild(new Panel
{
    Name = "Content", CustomMinimumSize = new Vector2(200, 100),
    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    SizeFlagsStretchRatio = 2
});
split.SplitOffset = 20;
split.Dragged += offset => Console.WriteLine(offset);
```

## API summary

| Full signature | Default/contract |
| --- | --- |
| `public SplitContainer()` | Horizontal, [0] offsets, draggable, noncollapsed, Visible mode. |
| `protected SplitContainer(bool vertical)` | Initializes a fixed-orientation specialization. |
| [`public bool Vertical { get; set; }`](#vertical) | False on generic type. |
| [`public bool Collapsed { get; set; }`](#collapsed) | False. |
| [`public bool DraggingEnabled { get; set; }`](#draggingenabled) | True. |
| [`public bool TouchDraggerEnabled { get; set; }`](#touchdraggerenabled) | False. |
| [`public bool DragNestedIntersections { get; set; }`](#dragnestedintersections) | False. |
| [`public int DragAreaMarginBegin { get; set; }`](#dragareamarginbegin) | Zero, signed. |
| [`public int DragAreaMarginEnd { get; set; }`](#dragareamarginend) | Zero, signed. |
| [`public int DragAreaOffset { get; set; }`](#dragareaoffset) | Zero, signed. |
| [`public DraggerVisibility DraggerVisibilityMode { get; set; }`](#draggervisibilitymode) | Visible. |
| [`public int SplitOffset { get; set; }`](#splitoffset) | Scalar first offset, zero initially. |
| [`public int[] SplitOffsets { get; set; }`](#splitoffsets) | Copied arrays, [0] initially. |
| [`public void ClampSplitOffset(int priorityIndex = 0)`](#clampsplitoffset) | Clamp/rewrite with active-split priority. |
| [`public Control GetDragAreaControl()`](#getdragareacontrol) | Borrowed first internal area. |
| [`public Control[] GetDragAreaControls()`](#getdragareacontrols) | Caller-owned snapshot of borrowed areas. |
| [`public event Action? DragStarted`](#dragstarted) | Started pointer drag. |
| [`public event Action<int>? Dragged`](#dragged) | Relative offset of the moved dragger. |
| [`public event Action? DragEnded`](#dragended) | Completed/cancelled drag. |
| `protected override Vector2 OnGetMinimumSize()` | Eligible child geometry and separation. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Expand omitted on cross axis; advisory array. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Transposed advisory array. |
| `protected override void OnNotification(int what)` | Membership, structure, sort, resources, theme/direction/visibility. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stored configuration, excluding fixed orientation. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact concrete factory. |
| `protected override void Dispose(bool disposing)` | Owned node teardown, borrowed subscriptions/residency cleanup. |

## Enumeration descriptions

[DraggerVisibility](SplitContainer.DraggerVisibility.md) contains Visible=0, Hidden=1, HiddenCollapsed=2. There are no own public constants outside this enum.

## Property descriptions

### Vertical

`public bool Vertical { get; set; }`

False initially. Changes primary direction, updates touch icons/minimum and immediately arranges while attached. HSplitContainer/VSplitContainer reject every assignment with InvalidOperationException, including equal values, and omit orientation storage.

### Collapsed

`public bool Collapsed { get; set; }`

False initially. Uses zero visual offsets and hides all drag areas after queued layout, retaining authored offsets. It does not mean child content disappears.

### DraggingEnabled

`public bool DraggingEnabled { get; set; }`

True initially. Disabling ends active drags, makes ordinary areas Ignore and hides touch images. Required updates are attempted after end-callback errors; they do not roll back the committed value. Query cursors fall back to the area's ordinary arrow when unavailable.

### TouchDraggerEnabled

`public bool TouchDraggerEnabled { get; set; }`

False initially. Adds one internal TextureRect beneath each dragger, with natural centered geometry, directional icon/cursor and theme tint. It overlaps panels without enlarging separation to the touch icon's size. Pointer GUI events pass through to the dragger. Normal grabber icons are suppressed. Disabling removes the image nodes. Geometry updates retain hover/press modulation; theme changes refresh it. This is a larger pointer target; native touchscreen emulation remains an input-domain capability.

### DragNestedIntersections

`public bool DragNestedIntersections { get; set; }`

False initially. Both ancestor and descendant must opt in, be orthogonal, visible, enabled, noncollapsed and have at least two panels. The descendant must adjoin the corresponding ordinary ancestor panel edge within minimum_grab_thickness. Internal pass-through targets at the intersection allow both axes to receive one gesture. Top-level and selected-edited-scene boundaries stop participation. Unrelated non-Control siblings are skipped without suppressing later candidates. Reused targets follow both bars; disable/exit/disposal removes stale targets on reconciliation. The helper is runtime-owned and not a new public widget type.

### DragAreaMarginBegin

`public int DragAreaMarginBegin { get; set; }`

Signed cross-axis inset at the beginning, zero initially. Changes the hit target and bar background, not panel allocation. Vertical RTL swaps which physical edge is beginning. Excess margins can collapse the target to zero size.

### DragAreaMarginEnd

`public int DragAreaMarginEnd { get; set; }`

Equivalent inset at the opposite cross edge, zero initially.

### DragAreaOffset

`public int DragAreaOffset { get; set; }`

Signed hit-target displacement along the primary axis, zero initially. The background/icon retain the visual bar location. Horizontal RTL reverses the physical displacement.

### DraggerVisibilityMode

`public DraggerVisibility DraggerVisibilityMode { get; set; }`

Visible initially. Hidden suppresses the ordinary icon, preserving its separation contribution. HiddenCollapsed uses zero gap while minimum_grab_thickness retains a usable hit target. DraggingEnabled and touch-image visibility are independent. Undefined enum values throw before mutation. The property name distinguishes the nested enum from the C# property.

### SplitOffset

`public int SplitOffset { get; set; }`

Scalar projection of SplitOffsets[0], initially zero. An explicitly empty array throws ArgumentOutOfRangeException until layout grows it. Writes queue layout and do not emit pointer events. Reading returns authored values even when visual layout clamps them.

### SplitOffsets

`public int[] SplitOffsets { get; set; }`

Copies incoming/outgoing arrays; equal contents are silent and null rejects before mutation. Layout grows empty/short arrays to at least one value and every actual boundary. Longer pending arrays remain available until matching panels exist; structural desired-size reconciliation rewrites the active array when applicable. Checked overflow during layout does not silently saturate relative offsets. The typed `int[]` scene profile snapshots and restores independent values, with element-based revert equality.

## Method descriptions

### ClampSplitOffset

`public void ClampSplitOffset(int priorityIndex = 0)`

Validates a stored index, then returns without layout work if fewer than two panels exist. With panels, it validates an actual boundary and clamps every offset to the live visual positions while prioritizing the selected boundary. Neighbour min/max constraints propagate left/right. Requests sorting and emits no drag signal. Invalid indices reject; owner/capture/disposal rules apply.

### GetDragAreaControl

`public Control GetDragAreaControl()`

Returns the first required borrowed internal area. At least one is retained. Add custom children through ordinary Node API; MouseFilter.Ignore avoids blocking the grabber. Removing/disposal of required areas is invalid and later queries/layout fail explicitly instead of dereferencing stale native state. The caller does not own the area.

### GetDragAreaControls

`public Control[] GetDragAreaControls()`

Returns a newly allocated Control[] snapshot in boundary order. Replacing entries changes only that array. The controls remain borrowed and runtime-owned. Structural changes can replace/remove later areas; do not retain a removed area as a live split boundary. Owner-thread/disposal validation applies.

### Extension hooks

Minimum and internal desired size use eligible child bounds. The two allowed-size hooks return caller-owned advisory arrays. Notifications attempt base and owned processing, report callback failures, refresh membership and coalesce sorting. Stored descriptors retain modes, offsets, flags and margins; runtime drag state and helper nodes are reconstructed rather than captured. The factory keeps exact generic/fixed identities. Disposal releases residency, owned helper nodes and events while preserving borrowed theme/image resources.

## Event descriptions

### DragStarted

`public event Action? DragStarted`

Pointer press first clamps offsets to visual positions, commits active state, then emits. Capture start values follow callback changes; a throwing observer does not leave an uninitialized drag anchor. Keyboard stepping emits no drag event.

### Dragged

`public event Action<int>? Dragged`

After pointer motion, relative offset writes and active-priority clamping commit before emission. The argument identifies the moved value, not its index. Nested intersection input bubbles to the ancestor, so each participating container emits its own event. Callback errors propagate after committed state.

### DragEnded

`public event Action? DragEnded`

Once per active drag on release/cancellation. Focus loss, hidden/exiting area and disabled dragging cancel. State clears before callbacks; child intersection cancellation and required configuration cleanup are attempted after errors. All events run on the scene owner thread.

## Themes and dependencies

Uses named constants separation=12, minimum_grab_thickness=6 and autohide=1; split_bar_background; h_grabber/v_grabber and fixed-class grabber; h_touch_dragger/v_touch_dragger and fixed-class touch_dragger. Touch colors default white with alpha .3/.6/1 for idle/hover/press. The default theme embeds four pinned directional SVGs under the existing runtime adaptation notice. Ordinary icon minimum separation is max(constant, primary icon extent); touch mode uses the constant directly. Empty/hidden bar drawing retains StyleBox behavior.

Resource revision polling covers icons, nested atlases and textured styles, refreshes layout/drawing, and retains current texture renderer residency while attached. Exit/replacement releases residency without resource ownership. First preparation, structural edits and copied array queries can allocate; warmed steady-state is checked separately.

## Verification and limitations

[SplitContainerTests](../../tests/Electron2D.Tests/SplitContainerTests.cs) verifies numeric defaults/weights/caps/overlap priority, RTL/vertical geometry, margins, top-level/visibility/order changes, borrowed custom children, copied offsets, typed int-array packing/revert, owner/index/enum guards, pointer and keyboard behavior, callback failure/reentry/preflight recovery, nested target construction after unrelated siblings and 64 active plus 64 idle zero-managed-allocation intervals after warmup.

[SplitContainerRenderingTests](../../tests/Electron2D.Tests/SplitContainerRenderingTests.cs) verifies 15 real pixel/input phases on Linux Wayland GPU/compatibility plus six nested intersection phases where one native gesture changes both axes and release ends each once. Twenty warmup frames precede 64 active offset/resize and 64 idle measured ProcessFrameStarted→FramePostDraw frames with zero managed bytes. Dummy/software separately verifies six programmatic pixel/layout/touch-image phases and the same warmed allocation intervals; dummy has no native system cursors, so pointer-input acceptance is established on Wayland. Readback figures are inspected separately from owner acceptance.

Editor drag-area highlighting remains Blocked until the self-hosted editor context/overlay slice under ADR 0027. No inert property is exposed. Internal draggers retain FocusMode.Accessibility by default; native semantic splitter publication and actions require the inherited Control accessibility service. A host can set the returned area's FocusMode.All and use keyboard stepping now; the native test exercises this explicit choice. Other inherited viewport/editor gaps remain on their declaring rows. Native allocator counts, other platforms, physical touch devices, dense/deep-panel frame time and owner acceptance remain unverified. [ADR 0081](../decisions/rendering.md#adr-0081) owns layout; [ADR 0023](../decisions/scene.md#adr-0023) owns storage.

## Internal records

Slot stores one borrowed panel, bounds, ratio, final size and a preflight rectangle. Dragger is a private Control used for ordinary and pass-through intersection targets; it owns only temporary touch-child nodes. SourceState records borrowed Resource identity/revision/disposal. Reusable child/position/desired/intersection/resource lists keep prepared capacity; structural reconciliation can scan quadratically. No helper exposes backend types or transfers texture ownership.
