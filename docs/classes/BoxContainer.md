# BoxContainer

Last updated: 2026-09-27

**Inherits:** [Container](Container.md), Control, CanvasItem, Node, ElectronObject · **Inherited By:** [HBoxContainer](HBoxContainer.md), [VBoxContainer](VBoxContainer.md)

**Declaration:** `public class BoxContainer : Container` · **Source:** [BoxContainer.cs](../../src/Scene/GUI/BoxContainer.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Arranges visible direct non-top-level controls along one pixel-aligned axis. Minimum size sums ceil-rounded bound minima plus signed separation on the primary axis and takes the largest cross minimum. Layout ignores hidden/top-level/non-Control children. Primary Expand bits select weighted allocation; Fill independently selects filled versus minimum-sized content. Horizontal order and leading/trailing alignment follow RTL.

```csharp
var box = new BoxContainer { Vertical = true, Size = new Vector2(80, 100), Separation = 4 };
box.AddChild(new Control { Name = "Content", CustomMinimumSize = new Vector2(20, 10),
    SizeFlagsVertical = Control.SizeFlags.ExpandFill });
box.AddSpacer(false);
using var tree = new SceneTree(box);
tree.ProcessFrame(0);
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `public BoxContainer()` | Horizontal, Begin, separation four. |
| `protected BoxContainer(bool vertical)` | Creates a fixed-orientation specialization. |
| `public AlignmentMode Alignment { get; set; }` | Begin; changes arrange attached children synchronously. |
| `public bool Vertical { get; set; }` | False; generic boxes allow changes, fixed subclasses reject all assignments. |
| `public int Separation { get; set; }` | Signed local pixel gap, four; typed theme-constant projection. |
| `public Control AddSpacer(bool begin)` | Adds real primary-axis ExpandFill child. |
| `protected override Vector2 OnGetMinimumSize()` | Bound minimum aggregation. |
| `protected override void OnNotification(int what)` | Arranges during NotificationSortChildren. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Omits cross-axis Expand when vertical. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Omits cross-axis Expand when horizontal. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores generic orientation, alignment and separation. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact generic box identity. |
| `public enum AlignmentMode` | [Begin=0, Center=1, End=2](BoxContainer.AlignmentMode.md). |

## Property descriptions

<a id="alignment"></a>
**Alignment:** distributes leftover space before the child group. Begin follows leading edge, End follows trailing edge, Center uses integer division. Undefined values throw ArgumentOutOfRangeException before mutation; equal assignments are silent. A changed value invokes layout immediately when visible/attached. Reentrant alignment/orientation requests repeat using cleared reusable slots; callbacks that never settle fail after 64 passes.

<a id="vertical"></a>
**Vertical:** chooses axis for minima, stretch, spacer flags and allowed inspector choices. Generic assignments request minimum refresh and immediate layout, including equal assignments. HBox/VBox throw InvalidOperationException even for equal values; their stored descriptors omit Vertical so packing cannot trigger an illegal write.

<a id="separation"></a>
**Separation:** signed integer pixel gap. Changes update minimum and queue layout; equal assignments are silent. Negative gaps overlap allocations. This local typed property projects the existing theme constant; global Theme resources/inheritance remain a separate coverage dependency.

## Method and enumeration descriptions

<a id="addspacer"></a>
**AddSpacer:** creates a real Control using MouseFilter.Pass and primary ExpandFill (ratio one, cross-axis Fill); inserts first when begin=true, otherwise last. Selects an unused sibling name. Scene hierarchy owns the child's lifetime; it has no automatic scene Owner for packing. Insertion callback failures propagate after structural commit.

<a id="ongetminimumsize"></a><a id="onnotification"></a>
**Layout hooks:** each weighted share uses available primary pixels and the ratio sum. Iterative refit fixes children below minimum or above maximum and redistributes surplus. Fractional shares accumulate pixel error; the last still-expanding child reaches the boundary. Zero/negative weights retain source behavior; positive total enters refit. Maximum propagation passes remaining primary bound through a temporary child cache. FitChildInRect supplies final fill/shrink, zero anchors and reset visual transforms.

<a id="getallowedsizeflagshorizontal"></a><a id="getallowedsizeflagsvertical"></a><a id="getpropertydescriptors"></a><a id="createsceneinstancefactory"></a><a id="alignmentmode"></a>
**Inspector/storage hooks and enumeration:** choices are advisory; arbitrary stored bits remain allowed. Generic/fixed scene factories preserve exact types. AlignmentMode is documented on its own page.

## Lifecycle, errors, dependencies and verification

Inherited Container subscriptions/coalescing and Control owner/capture/maximum/input guards apply. Nonfinite ratios reject; overflowing integer pixel layout or nonfinite total weight throws InvalidOperationException rather than producing invalid geometry. Child callback failures attempt later captured children and aggregate. Scratch storage is reused; refit is quadratic in children and has no measured large-GUI guarantee.

[Managed tests](../../tests/Electron2D.Tests/BoxContainerTests.cs) verify fractional pixels, signed/zero weights and separation, propagated maximums, weighted min/max, RTL, alignment, shrink, visibility, spacers, exact packing, reentrant arrangement, deferred phases and zero bytes for 64 warmed resize/layout cycles. [Native tests](../../tests/Electron2D.Tests/BoxContainerRenderingTests.cs) verify retained NinePatchRect pixels, redistribution and 64 warmed resize/immediate-layout/render frames on Linux Wayland GPU/compatibility. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. Inherited Container accessibility and global themes remain separate dependencies. See [coverage](../coverage/classes/BoxContainer.md) and [ADR 0081](../decisions/rendering.md#adr-0081).
