# AspectRatioContainer

Last updated: 2026-09-30

**Inherits:** [Container](Container.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class AspectRatioContainer : Container` · **Source:** [LayoutContainers.cs](../../src/Scene/GUI/LayoutContainers.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

AspectRatioContainer allocates each visible direct non-top-level child a rectangle at the configured width-to-height `Ratio`. `Fit` chooses the largest rectangle inside both available axes, `Cover` the smallest rectangle covering both axes, and the width/height policies fill their controlling axis. The allocation grows componentwise to the child's bound minimum. Horizontal alignment mirrors under RTL after calculating the leading/center/trailing offset; vertical alignment is unchanged. The child still applies its own Fill/shrink flags through inherited `FitChildInRect`. The container's intrinsic minimum is the largest eligible child bound minimum.

```csharp
var frame = new AspectRatioContainer { Ratio = 16f / 9f, Size = new Vector2(320, 240) };
frame.AddChild(new Panel { Name = "Content" });
```

## API summary

| Signature | Contract |
| --- | --- |
| `public AspectRatioContainer()` | Creates a centered, fitting ratio-one container. |
| `public enum AlignmentMode` | Begin=0, Center=1, End=2. |
| `public enum StretchMode` | WidthControlsHeight=0, HeightControlsWidth=1, Fit=2, Cover=3. |
| `public float Ratio { get; set; }` | Positive finite width divided by height; one initially. |
| `public StretchMode Stretch { get; set; }` | Fit initially. |
| `public AlignmentMode AlignmentHorizontal { get; set; }` | Center initially, mirrored under RTL. |
| `public AlignmentMode AlignmentVertical { get; set; }` | Center initially. |
| `protected override Vector2 OnGetMinimumSize()` | Largest eligible bound child minimum. |
| `protected override void OnNotification(int what)` | Computes aspect allocations on the deferred sort notification. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Advisory Fill and three shrink choices. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Advisory Fill and three shrink choices. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores ratio, stretch and both alignments. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates the exact type when unpacking a scene. |

## Property descriptions

<a id="ratio"></a>
`Ratio` is width divided by height. A nonpositive or nonfinite assignment throws `ArgumentOutOfRangeException` before mutation. A different valid value queues a deferred sort; an equal value is silent. A valid but extreme ratio that produces nonfinite layout geometry fails during sorting without applying an invalid child rectangle.

<a id="stretch"></a>
`Stretch` chooses WidthControlsHeight, HeightControlsWidth, Fit or Cover. The first two fill their controlling axis. Fit keeps the aspect rectangle inside both axes; Cover can allocate past the container bounds. Clipping remains the inherited `ClipContents` policy. Undefined modes throw before mutation.

<a id="alignmenthorizontal"></a>
`AlignmentHorizontal` places spare horizontal space at Begin, Center or End. RTL mirrors the resulting physical offset. Undefined modes throw before mutation.

<a id="alignmentvertical"></a>
`AlignmentVertical` applies the same three choices without RTL mirroring. Setting a different alignment queues a deferred sort. Typed descriptors preserve all four properties in PackedScene.

[LayoutContainersTests](../../tests/Electron2D.Tests/LayoutContainersTests.cs) checks four stretch policies, RTL, both axes, bounds, invalid values, packing, callbacks and warmed active frames. [LayoutContainersRenderingTests](../../tests/Electron2D.Tests/LayoutContainersRenderingTests.cs) checks colored panels on Linux Wayland GPU and compatibility. Native allocations, other platforms and owner visual acceptance remain unverified; see [coverage](../coverage/classes/AspectRatioContainer.md).
