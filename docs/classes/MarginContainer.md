# MarginContainer

Last updated: 2026-09-30

**Inherits:** [Container](Container.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class MarginContainer : Container` · **Source:** [LayoutContainers.cs](../../src/Scene/GUI/LayoutContainers.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

MarginContainer fits each visible direct non-top-level Control into the same inner rectangle. Four inherited typed theme constants, `margin_left`, `margin_top`, `margin_right`, and `margin_bottom`, are explicitly stored as zero in the default theme. They are signed logical-pixel offsets; positive margins reserve space, while negative values extend the child allocation beyond the container edge. Locally visible children contribute their largest bound minimum plus the horizontal/vertical margin sums. Arrangement uses visibility in the tree. Theme changes invalidate the minimum and queue a new sort. Maximum propagation subtracts the margin sums before constraining child bounds.

```csharp
var margin = new MarginContainer { Size = new Vector2(180, 100) };
margin.AddThemeConstantOverride("margin_left", 12);
margin.AddThemeConstantOverride("margin_right", 12);
margin.AddChild(new Panel { Name = "Content" });
```

## API summary

| Signature | Contract |
| --- | --- |
| `public MarginContainer()` | Creates an empty container using the inherited zero-valued theme margins. |
| `public int GetMarginSize(Side side)` | Reads the resolved margin for Left, Top, Right or Bottom. |
| `protected override Vector2 OnGetMinimumSize()` | Largest eligible bound child minimum plus the signed margin sums. |
| `protected override void OnNotification(int what)` | Fits children on the deferred sort notification. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Advisory Fill and three shrink choices. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Advisory Fill and three shrink choices. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates the exact type when unpacking a scene. |

## Method descriptions

<a id="getmarginsize"></a>
`GetMarginSize` uses inherited theme resolution, including local and ancestor overrides. An undefined `Side` throws `ArgumentOutOfRangeException`; theme lookup and attached layout obey the scene owner-thread policy. Margin entries are stored through typed theme override descriptors, so PackedScene retains explicit local overrides without freezing inherited defaults.

Layout calls inherited `FitChildInRect`: Fill uses the inner allocation; shrink flags select the child minimum and alignment within it. The container snapshots eligible children before fitting, rechecks membership after callbacks, attempts later children when one fit throws, then reports collected errors. Reentrant changes queue a later sort. [LayoutContainersTests](../../tests/Electron2D.Tests/LayoutContainersTests.cs) checks margins, theme updates, visibility/top-level exclusion, packing, callbacks and warmed active frames. [LayoutContainersRenderingTests](../../tests/Electron2D.Tests/LayoutContainersRenderingTests.cs) checks pixels on Linux Wayland GPU and compatibility. Native allocations, other platforms and owner visual acceptance remain unverified; see [coverage](../coverage/classes/MarginContainer.md).
