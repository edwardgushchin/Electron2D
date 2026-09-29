# CenterContainer

Last updated: 2026-09-30

**Inherits:** [Container](Container.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class CenterContainer : Container` · **Source:** [LayoutContainers.cs](../../src/Scene/GUI/LayoutContainers.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

CenterContainer fits every visible direct non-top-level child at its bound minimum size. In ordinary mode it floors the half-space remaining on each axis, placing each child's rectangle at the center of the container. `UseTopLeft` instead centers the child on local origin `(0,0)`, potentially giving negative positions, and suppresses the container's intrinsic minimum and desired size. Child visibility, maximum bounds, lifecycle and deferred sort phases follow [Container](Container.md).

```csharp
var center = new CenterContainer { Size = new Vector2(160, 80) };
center.AddChild(new Panel { Name = "Badge", CustomMinimumSize = new Vector2(24, 12) });
```

## API summary

| Signature | Contract |
| --- | --- |
| `public CenterContainer()` | Creates an ordinary centering container. |
| `public bool UseTopLeft { get; set; }` | False initially; true places each child around local origin. |
| `protected override Vector2 OnGetMinimumSize()` | Largest locally visible child bound minimum, or zero in top-left mode. |
| `protected override void OnNotification(int what)` | Fits children on the deferred sort notification. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Empty advisory choice list. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Empty advisory choice list. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the typed mode. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates the exact type when unpacking a scene. |

## Property descriptions

<a id="usetopleft"></a>
Changing `UseTopLeft` queues minimum-size and arrangement updates; an equal assignment is silent. The stored value survives PackedScene. After a child minimum changes, its own deferred minimum notification queues the container's subsequent sort, so the final rectangle may require two captured deferred batches. Geometry uses local logical pixels and inherited `FitChildInRect`. A failing child callback does not prevent later eligible children from fitting.

[LayoutContainersTests](../../tests/Electron2D.Tests/LayoutContainersTests.cs) checks both centers, odd-pixel flooring, deferred minimum changes, packing, callbacks and warmed active frames. [LayoutContainersRenderingTests](../../tests/Electron2D.Tests/LayoutContainersRenderingTests.cs) checks colored panels on Linux Wayland GPU and compatibility. Native allocations, other platforms and owner visual acceptance remain unverified; see [coverage](../coverage/classes/CenterContainer.md).
