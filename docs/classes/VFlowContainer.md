# VFlowContainer

Last updated: 2026-10-01

- Declaration: `public class VFlowContainer : FlowContainer`
- Source: [FlowContainer.cs](../../src/Scene/GUI/FlowContainer.cs)
- Inherits: [FlowContainer](FlowContainer.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#flow-layout)

## Description

Fixed vertical column flow with inherited wrapping, alignment, reverse/RTL, theme gaps and deferred sorting. Vertical is always `true`; any assignment rejects and the writable orientation descriptor is omitted. PackedScene preserves this exact subtype and inherited properties. Ordinary Control/Container ownership, thread/capture/disposal guards remain in force.

## Example

```csharp
var flow = new VFlowContainer { Size = new Vector2(240, 100) };
flow.AddChild(new Button { Text = "One" });
flow.AddChild(new Button { Text = "Two" });
```

## API summary

| Declaration | Contract |
| --- | --- |
| `public VFlowContainer()` | Initializes fixed vertical flow. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Restores exact subtype. |

## Constructor and factory

The constructor fixes orientation without changing Begin/Inherit alignment, reverse false or four-pixel gaps. The factory applies this type in typed scene capture/instantiation; user subclasses still supply their own exact factories under the inherited rule.

## Verification and limits

[Managed](../../tests/Electron2D.Tests/FlowContainerTests.cs) checks fixed assignments, descriptor omission and exact packing. [Native](../../tests/Electron2D.Tests/FlowContainerRenderingTests.cs) checks common row/column behavior and warmed layout/render. Native allocations, larger GUI performance, other platforms and owner acceptance remain unverified. See [FlowContainer](FlowContainer.md) and [ADR 0081](../decisions/rendering.md#adr-0081).
