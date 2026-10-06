# FlowContainer

Last updated: 2026-10-06

- Declaration: `public class FlowContainer : Container`
- Source: [FlowContainer.cs](../../src/Scene/GUI/FlowContainer.cs)
- Inherits: [Container](Container.md), [Control](Control.md)
- Inherited by: [HFlowContainer](HFlowContainer.md), [VFlowContainer](VFlowContainer.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#flow-layout)

## Description

Arranges direct visible non-top-level Control children in wrapping rows or columns. Children are visited in scene order. Bound minimum/maximum sizes are truncated to integer pixels; a wrap starts before the next child would exceed the primary extent. Empty visible arrangement caches one implicit line; before the first arrangement GetLineCount is zero. Hidden/detached state retains the last successful cache. Local-visible children determine minimum size; runtime arrangement uses visible-in-tree controls.

Primary Expand allocates extra space by positive SizeFlagsStretchRatio, capped at maximum minus minimum. Each share truncates independently; unused remainder follows alignment. Cross Fill/Center/End receive the line's largest cross minimum, then Container fits each child using min/max and shrink/RTL rules. Cross Expand alone adds no space. Anchors, rotation and scale reset through the inherited fit path. Parent maximum bounds propagate through existing Control state; changing bounds releases obsolete caches.

The minimum primary extent is the largest eligible child minimum; the cross extent is cached by the last arrangement. The internal desired-size projection uses child bound desired sizes with the same cached cross extent. Properties and GetLineCount require the scene owner thread while attached. Mutation rejects scene capture/disposal. Configuration is stored with typed descriptors and exact factories.

## Example

```csharp
var flow = new HFlowContainer { Size = new Vector2(240, 100), HSeparation = 8 };
flow.AddChild(new Button { Text = "Start" });
flow.AddChild(new Button { Text = "Options" });
flow.AddChild(new Button { Text = "Exit" });
// Add flow to an active scene; resize its width to change wrapping.
```

## API summary

| Declaration | Contract |
| --- | --- |
| `public FlowContainer()` | Horizontal Begin/Inherit, reverse false, gaps four. |
| `protected FlowContainer(bool vertical)` | Initializes a fixed-orientation subclass. |
| [`public bool Vertical { get; set; }`](#vertical) | False on generic FlowContainer. |
| [`public bool ReverseFill { get; set; }`](#reversefill) | False initially. |
| [`public FlowContainer.AlignmentMode Alignment { get; set; }`](#alignment) | Begin initially. |
| [`public LastWrapAlignmentMode LastWrapAlignment { get; set; }`](#lastwrapalignment) | Inherit initially. |
| [`public int HSeparation { get; set; }`](#hseparation) | Horizontal gap, built-in default four. |
| [`public int VSeparation { get; set; }`](#vseparation) | Vertical equivalent using v_separation, initially four. |
| [`public int GetLineCount()`](#getlinecount) | Cached latest visible line/column count. |
| `protected override Vector2 OnGetMinimumSize()` | Largest eligible primary minimum and cached cross extent. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Advisory Fill/Expand/shrink choices; Expand omitted on cross axis. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Orientation-transposed advisory choices. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Typed properties, omitting fixed orientation. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact generic scene identity. |
| `protected override void OnNotification(int what)` | Deferred sort/minimum and translation handling. |

## Enumerations

[AlignmentMode](FlowContainer.AlignmentMode.md): Begin=0, Center=1, End=2.
[LastWrapAlignmentMode](FlowContainer.LastWrapAlignmentMode.md): Inherit=0, Begin=1, Center=2, End=3.

## Property descriptions

### Vertical

`public bool Vertical { get; set; }`

False on generic FlowContainer. Changing orientation is synchronous while attached and requests minimum refresh. Fixed HFlow/VFlow reject every assignment and omit this storage descriptor.

### ReverseFill

`public bool ReverseFill { get; set; }`

False initially. Horizontal flows fill rows from the bottom when true; vertical column reversal combines with RTL as exclusive-or. It does not reverse primary child order.

### Alignment

`public FlowContainer.AlignmentMode Alignment { get; set; }`

Begin initially. Uses leading/center/trailing residual primary space after capped expansion. Changing it arranges synchronously; equal values are silent. Undefined enum values reject before mutation.

### LastWrapAlignment

`public LastWrapAlignmentMode LastWrapAlignment { get; set; }`

Inherit initially. A non-first final line is unfilled when another copy of its last child’s minimum primary extent would still fit (the classification omits a gap). Begin/Center/End align that line relative to the previous occupied group, after expansion/capping. Inherit uses the full container. Single or filled lines retain ordinary Alignment.

### HSeparation

`public int HSeparation { get; set; }`

Horizontal gap, built-in default four. Signed values are preserved through the local typed h_separation theme override. Restoring the inherited value removes that override. Theme invalidation runs before deferred sorting; an overflowing layout fails before child fitting.

### VSeparation

`public int VSeparation { get; set; }`

Vertical equivalent using v_separation, initially four. Negative gaps can overlap rows/columns; intrinsic cached extent can be signed, while combined Control minimum remains clamped to zero.

## Method descriptions

### GetLineCount

`public int GetLineCount()`

Returns the count cached by the latest successful visible layout, without forcing a layout. It is zero initially and one after an empty visible sort. Rows count for horizontal mode, columns for vertical mode. Queued sorting, hidden/detached state and failed geometry preflight leave the prior value. Off-owner access and disposed objects reject.

### Layout hooks

OnGetMinimumSize/GetDesiredSize scan eligible local-visible children; allowed-size hooks return caller-owned advisory arrays. OnNotification consumes the inherited deferred sort stage, refreshes minimum size and queues translation changes. Descriptor/factory hooks preserve exact types, defaults and inherited state. No editor application or semantic accessibility service is implied.

## Lifecycle, errors and geometry limits

Child minimum/maximum/flags/order/visibility and own resize/theme/direction/translation changes use Container's coalesced sort. Alignment/last-wrap/reverse/orientation changes run attached arrangement immediately. Reentrant immediate requests repeat from a fresh snapshot, bounded at 64 passes; nonsettling callbacks fail explicitly. Child errors are collected after attempting later surviving placements; removed/disposed/reparented children are skipped and membership changes invalidate old queued work. Geometry/range/weight preflight completes before fitting any child, preserving prior rectangles on failure; configuration state remains committed when callbacks or deferred layout fail.

Two pre-release corrections preserve geometry bounds: an oversized first child does not insert an artificial leading empty wrap, and nonpositive stored stretch ratios receive no expansion rather than a negative allocation or over-allocation of positive peers. Source values remain accepted. Signed gaps and independent integer truncation remain intact. Checked integer arithmetic rejects overflow; finite sum overflow rejects before placement. Large-line capped refit is quadratic and uses reused lists, pending a measured need for another allocator.

[TextureRect](TextureRect.md) FitWidth/FitWidthProportional/FitHeight/FitHeightProportional now retain current child sizes before ordinary fitting when there are multiple wraps, preventing wrap feedback. Positions still apply alignment/RTL/reverse rules; single-wrap layout uses normal fitting. TextureRectTests checks all four modes over both orientations and direction combinations. Inherited accessibility and broader viewport/editor limits stay in their own coverage rows.

## Verification

[FlowContainerTests](../../tests/Electron2D.Tests/FlowContainerTests.cs) checks defaults/enums, rows/columns, all twelve relative-alignment combinations, visibility/order/top-level changes, capped weights/zero/signed ratios, truncation, RTL/reverse, fixed identities, packing, owner/disposal guards, callbacks/reentry, maximum changes, overflow rollback and membership. Sixty-four active wrap/merge cycles after warmup and 64 idle frames allocate zero managed bytes on Linux/.NET 10.

[FlowContainerRenderingTests](../../tests/Electron2D.Tests/FlowContainerRenderingTests.cs) checks six real pixel states and 64 active plus 64 idle warmed resize/layout/record/render frames from ProcessFrameStarted to FramePostDraw on Linux Wayland GPU/compatibility and dummy/software. Native allocations, large-GUI frame-time, other platforms and owner acceptance remain unverified. [ADR 0081](../decisions/rendering.md#adr-0081) owns layout and [ADR 0083](../decisions/rendering.md#adr-0083) owns theme lookup.

## Internal records

Private Slot stores a borrowed child, integer main/cross minima, primary maximum/extra, ratio/active flag, cross flags and planned Rect2. Private Line stores a slot range, cross extent, used/residual primary pixels and filled classification. Reused lists retain only current-pass child references and clear in finally; they own no nodes or native data.
