# VisibleOnScreenNotifier

Last updated: 2026-09-26

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [VisibleOnScreenEnabler](VisibleOnScreenEnabler.md)

**Declaration:** `public class VisibleOnScreenNotifier : Entity` · **Source:** [VisibleOnScreenNotifier.cs](../../src/Scene/2D/VisibleOnScreenNotifier.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

A local rectangular region with no runtime drawing. Actual retained render frames determine whether its conservative transformed bounds participate in the current viewport. Detection uses viewport/layer transforms, interpolation, masks, inherited visibility and alpha, clipping and repeated copies; it does not test visual occlusion by other drawing. Local bounds include the node origin and any retained drawing emitted by this item and intersect inclusively, so a border, line or point can enter. Finite negative extents normalize for culling. SelfModulate alpha does not gate detection; inherited Modulate alpha below 0.007 does. Independent offscreen viewport rendering is not integrated.

State is false until the first submitted frame. A hidden window or disabled RenderLoopEnabled retains the last sample. All notifier states commit before events, delivered before RenderingServer.FramePostDraw. Callback mutation takes effect on the next frame; actual removal/reentry invalidates stale pending delivery. Tree departure silently clears state without ScreenExited. Failed callbacks do not replay committed transitions; later queued notifiers run before an aggregate error reaches the host.

## Example

Partial snippet; attach to a rendered scene:

```csharp
var region = new VisibleOnScreenNotifier { Rect = new Rect2(-20, -20, 40, 40) };
region.ScreenEntered += () => Console.WriteLine("Entered viewport");
region.ScreenExited += () => Console.WriteLine("Left viewport");
```

## API summary

| Signature | Contract |
| --- | --- |
| `public VisibleOnScreenNotifier()` | Initially off-screen, centered 20 by 20 rectangle. |
| `public Rect2 Rect { get; set; }` | Local bounds, default (-10,-10,20,20). |
| `public bool IsOnScreen()` | Last submitted-frame state, false while detached. |
| `public event Action? ScreenEntered` | Off-screen to on-screen transition. |
| `public event Action? ScreenExited` | On-screen to off-screen rendered transition. |

## Property and method descriptions

<a id="rect"></a>
**Rect:** finite local coordinates in canvas units; configuration edits affect the next render sample. Reject nonfinite values/endpoints before assignment. Zero/negative extents follow conservative bounds rather than occlusion tests.

<a id="isonscreen"></a>
**IsOnScreen:** reads committed state on the scene owner thread. Does not force a render or re-evaluate after a transform/property edit.

<a id="screenentered"></a>
<a id="screenexited"></a>
**ScreenEntered / ScreenExited:** parameterless owner-thread callbacks after the whole frame snapshot commits. Subscriber errors are aggregated after later queued nodes are attempted; ordinary C# multicast failure semantics apply within one event. Sample state is already updated when a handler runs.

## Lifecycle, errors and verification

Entity transform/visibility/owner/capture behavior is inherited. Attached queries/mutation enforce owner thread; disposal rejects later use and releases subscriptions. PackedScene retains exact type and rectangle, while sampled state is reset. [ScreenVisibilityTests](../../tests/Electron2D.Tests/ScreenVisibilityTests.cs) and [ScreenVisibilityRenderingTests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs) verify managed behavior, both Linux Wayland native backends, masks/layers/borders/alpha/physics integration, failure/reentry and zero managed bytes over 64 warmed active transitions. Native allocation, other platforms and owner visual acceptance remain unverified. Editor ShowRect is still Blocked in [coverage](../coverage/classes/VisibleOnScreenNotifier2D.md), with its exact editor gizmo trigger in [ADR 0078](../decisions/rendering.md#adr-0078).
