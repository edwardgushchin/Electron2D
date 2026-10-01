# HSplitContainer

Last updated: 2026-10-01

**Declaration:** `public class HSplitContainer : SplitContainer` · **Source:** [SplitContainer.cs](../../src/Scene/GUI/SplitContainer.cs). **Inherits:** [SplitContainer](SplitContainer.md), [Container](Container.md), [Control](Control.md). **Inherited By:** no shipped subclass.

## Description and example

Fixed horizontal resizable panel layout. Uses the inherited geometry, offsets, internal drag areas, touch/intersection behavior, ownership, errors and tests. Constructor selects Vertical=false; every orientation assignment rejects and orientation is omitted from stored descriptors. Fixed classes select `grabber` and `touch_dragger` theme keys.

```csharp
var split = new HSplitContainer { Size = new Vector2(300, 200) };
split.AddChild(new Panel { Name = "First" });
split.AddChild(new Panel { Name = "Second" });
// Attach split to an active scene.
```

## API summary

| Signature | Contract |
| --- | --- |
| `public HSplitContainer()` | Initializes fixed horizontal layout and inherited defaults. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Reconstructs exact HSplitContainer identity; custom subclasses retain the inherited explicit factory rule. |

## Method descriptions

### Constructor

`public HSplitContainer()` creates its real first internal drag control. Detached configuration is supported. Theme resources remain borrowed.

### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()` returns the static factory for this exact concrete type. Runtime helper nodes are reconstructed and ordinary Node.Owner storage selection remains unchanged.

## Verification and limitations

[SplitContainerTests](../../tests/Electron2D.Tests/SplitContainerTests.cs) covers fixed orientation, actual geometry and packed identity. The inherited [native tests](../../tests/Electron2D.Tests/SplitContainerRenderingTests.cs) cover both orientations, real input and warmed rendering on current backends. Editor highlighting/native semantic accessibility and other inherited gaps retain their own declaring rows; native allocations, other platforms and owner acceptance remain unverified. See [the full contract](SplitContainer.md).
