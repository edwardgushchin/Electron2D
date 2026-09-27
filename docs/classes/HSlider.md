# HSlider

Last updated: 2026-09-27

**Inherits:** [Slider](Slider.md), [Range](Range.md), [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** —

**Declaration:** `public class HSlider : Slider` · **Source:** [Slider.cs](../../src/Scene/GUI/Slider.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

A fixed horizontal slider whose value increases left-to-right in LTR, reversed in RTL. It uses the complete inherited themed input, tick, drag and Range contract.

```csharp
using var slider = new HSlider { MinValue = 0, MaxValue = 10, Value = 5 };
```

## API summary

| Signature | Contract |
| --- | --- |
| `public HSlider()` | Step=1, FocusMode.All, horizontal Fill and vertical ShrinkBegin. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Creates exactly HSlider for typed scene packing. |

<a id="hslider"></a><a id="createsceneinstancefactory"></a>
The constructor fixes the internal orientation; no public orientation property exists. Default Theme entries are resolved under HSlider then inherited type dependencies. A custom subclass supplies its own exact factory. The caller owns a detached instance until it joins a hierarchy; styles and textures remain borrowed.

Managed SliderTests and eight native pixel/input phases pass on Linux Wayland GPU and compatibility. Reused input cycles and active redraw frames allocate zero managed bytes in their bounded warmed measurements; see [Slider](Slider.md) for scope and limits. See [Slider](Slider.md) for interaction/event details, limits and dependencies, and [coverage](../coverage/classes/HSlider.md) for its own rows.
