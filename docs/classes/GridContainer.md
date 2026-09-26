# GridContainer

Last updated: 2026-09-27

**Inherits:** [Container](Container.md), [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** —

**Declaration:** `public class GridContainer : Container` · **Source:** [GridContainer.cs](../../src/Scene/GUI/GridContainer.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

Arranges visible direct non-top-level controls in row-major order with a fixed column count. Hidden, top-level and non-Control children consume no cell. Minimum size considers local visibility, including a grid under a hidden ancestor; attached arrangement uses visibility in the tree. All positions and separations use local pixels. Ordinary Control input, clipping, bounds, scene ownership and Container deferred phase behavior remain inherited.

Each occupied column uses its largest child width minimum and each row its largest height minimum, truncated to integer pixels. The grid sums these sizes and the gaps between occupied columns/rows. An Expand flag on any child selects that column or row for uniform sharing on its corresponding axis. Child stretch ratios do not weight a grid. Fill/shrink determines how an individual child fits within its allocated cell, independently of Expand.

## Example

```csharp
var grid = new GridContainer { Columns = 2, Size = new Vector2(100, 40) };
grid.AddChild(new Control { Name = "First", CustomMinimumSize = new Vector2(20, 10),
    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
grid.AddChild(new Control { Name = "Second", CustomMinimumSize = new Vector2(20, 10),
    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
using var tree = new SceneTree(grid);
tree.ProcessFrame(0);
```

The two cells share the available width after the four-pixel horizontal gap. The hierarchy owns the child lifetimes. Set a child's `Owner` when it should be included in a packed scene.

## API summary

| Signature | Contract/default |
| --- | --- |
| `public GridContainer()` | One column; both separations four pixels. |
| `public int Columns { get; set; }` | Positive column count; one initially. |
| `public int HSeparation { get; set; }` | Signed horizontal pixel gap; four initially. |
| `public int VSeparation { get; set; }` | Signed vertical pixel gap; four initially. |
| `protected override Vector2 OnGetMinimumSize()` | Aggregates occupied column/row bound minima. |
| `protected override void OnNotification(int what)` | Arranges during NotificationSortChildren. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores columns and both separations with exact defaults. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Preserves exact GridContainer scene identity. |

## Constructor description

<a id="gridcontainer"></a>
**GridContainer():** creates an empty grid. Container defaults remain MouseFilter.Pass and PropagateMaximumSize=true; inherited horizontal and vertical flags are Fill. Construction does not attach a scene tree or perform native rendering.

## Property descriptions

<a id="columns"></a>
**Columns:** reads or sets the positive row width. Values below one throw ArgumentOutOfRangeException before mutation. An equal assignment is silent; a change updates minimum size and requests deferred layout. Increasing Columns can create empty expanding columns: they share available width even though they have no child and contribute no extra separation. Storage is bounded by the direct-child count, not the declared column count.

<a id="hseparation"></a><a id="vseparation"></a>
**HSeparation and VSeparation:** signed integer pixel gaps between occupied columns and rows. Equal assignments are silent; changes update minimum size and request deferred layout. Negative gaps permit overlap. These local typed properties project the existing theme constants; global Theme resources, inherited theme lookup and skin authoring remain separate capabilities.

## Method descriptions

<a id="ongetminimumsize"></a>
**OnGetMinimumSize():** collects eligible children's bound minimum sizes and truncates each dimension toward zero before taking per-axis maxima. It sums occupied widths/heights and signed gaps; an empty grid contributes zero. It does not allocate storage proportional to Columns. The inherited Control minimum/bounds contract determines the final combined size.

<a id="onnotification"></a>
**OnNotification(int what):** retains Container notifications and consumes NotificationSortChildren (51) before the inherited SortChildren event. It collects live cell constraints, computes both axes, fits eligible children through FitChildInRect and refreshes minimum size. Nonnegative effective maximums aggregate by their largest value within each column/row, clamped up to that axis's minimum. An unbounded child uses the grid's effective maximum, or its current integer size if the grid is unbounded.

Minima exceeding an equal share fix the largest-minimum axis first; maxima below an equal share cap expanding axes and redistribute the remainder. The first expanding occupied columns/rows receive leftover integer pixels. Empty columns participate only in width division. RTL mirrors column placement from the grid's right edge; inherited fitting also applies RTL to non-fill alignment. Maximum propagation first supplies combined grid bounds while collecting constraints, then independently reduces each axis by its accumulated cell offset before the final fit. Removing a child clears its allocation cache before another parent supplies bounds. Raising the grid maximum clears stale child bounds through Control.UpdateMaximumSize and queues sorting through the container maximum-change event; no manual resize or QueueSort is needed.

A capped row advances the next row by its final allocated height plus separation. This prevents an overlap when a maximum-constrained row is taller than its minimum; with zero gap and total height 100, an expanding first row with minimum 10/maximum 20 is followed at Y=20 by an 80-pixel second row. The bounded correction is recorded in [ADR 0081](../decisions/rendering.md#adr-0081).

<a id="getpropertydescriptors"></a><a id="createsceneinstancefactory"></a>
**Storage and factory overrides:** typed descriptors store Columns, HSeparation and VSeparation with defaults 1, 4 and 4. The scene factory constructs exactly GridContainer; inherited descriptors retain the actual Container/Control defaults. Custom subclasses provide their own exact packing factory under the Node contract.

## Lifecycle, errors and dependencies

Container subscriptions request sorting after child membership/order/visibility/flags/bounds and own size/direction/entry changes. Changing a direct Control child's TopLevel state clears its prior parent allocation cache and requests parent minimum refresh/sort, so it leaves or rejoins normal grid layout automatically. Requests coalesce through the existing scene deferred queue; requests during a running pass schedule one follow-up batch, so reentrant changes are not lost. Detached requests do nothing; stale callbacks from prior tree memberships cannot sort the current membership. Child fit resets anchors and visual rotation/scale under the inherited contract. Layout does not own external resources or publish accessibility semantics.

Owner-thread and lifetime guards apply to reads and changes; scene capture rejects mutation. Disposed access throws ObjectDisposedException, owner/capture violations throw InvalidOperationException. Nonfinite or out-of-range pixel values throw InvalidOperationException; checked integer arithmetic overflow throws OverflowException instead of emitting invalid geometry. Geometry callback failures can follow committed child state and are aggregated after required work. Reusable occupied-axis buffers avoid work or storage proportional to an arbitrarily large Columns value; iterative min/max refit remains quadratic in occupied axis count, without a measured large-GUI guarantee.

## Verification and limits

[GridContainerTests](../../tests/Electron2D.Tests/GridContainerTests.cs) verifies defaults, integer minima, eligibility, row-major/RTL placement, independent expansion, min/max bounds, sparse columns, typed packing, guards, callback continuation/reentrancy, two-axis maximum propagation, cache clearing after removal, increasing maximum from 20 to 80, and overflow rejection before any child rectangle changes. It measures zero managed bytes for 64 warmed layout cycles and 64 alternating maximum-update/refit cycles with four deferred frames per cycle. [GridContainerRenderingTests](../../tests/Electron2D.Tests/GridContainerRenderingTests.cs) verifies seven geometry/pixel phases covering remainder placement, RTL, visibility, reordering, resize, column changes and capped rows on Linux Wayland GPU and compatibility. Each backend also passes 64 warmed resize/layout/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, large-GUI performance, other platforms and owner acceptance remain unverified. Inherited semantic accessibility and global Theme lookup retain separate dependencies; other platform integration follows [ADR 0021](../decisions/product.md#adr-0021). See [coverage](../coverage/classes/GridContainer.md) and [ADR 0081](../decisions/rendering.md#adr-0081).
