# VBoxContainer

Last updated: 2026-09-27

**Inherits:** [BoxContainer](BoxContainer.md), Container, Control, CanvasItem, Node, ElectronObject · **Inherited By:** —

**Declaration:** `public class VBoxContainer : BoxContainer` · **Source:** [BoxContainer.cs](../../src/Scene/GUI/BoxContainer.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

A fixed vertical box. Inherits weighted allocation, bounds, RTL policy, alignment, separation, actual spacers, deferred phase events and Control input/geometry.

```csharp
var box = new VBoxContainer { Size = new Vector2(80, 100) };
box.AddSpacer(false);
using var tree = new SceneTree(box);
tree.ProcessFrame(0);
```

## API summary and method descriptions

| Signature | Contract |
| --- | --- |
| `public VBoxContainer()` | Fixed Vertical=true, Begin, separation four. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Preserves exact VBoxContainer identity. |

<a id="createsceneinstancefactory"></a>
The inherited typed descriptor set omits Vertical: it is fixed by constructor, and all public assignments throw InvalidOperationException, including equal assignments. Packing restores other inherited flags/layout values normally. Custom subclasses follow Node's explicit factory contract.

## Lifecycle, dependencies and verification

Inherited [Container](Container.md) and [BoxContainer](BoxContainer.md) guard/order/ownership/error contracts apply. [BoxContainerTests](../../tests/Electron2D.Tests/BoxContainerTests.cs) verifies real rectangles, fixed orientation and packing; [native tests](../../tests/Electron2D.Tests/BoxContainerRenderingTests.cs) render both axes through retained NinePatchRect children. Allocation/platform/accessibility/theme limits are the inherited limits. See [coverage](../coverage/classes/VBoxContainer.md) and [ADR 0081](../decisions/rendering.md#adr-0081).
