# FlowContainer.AlignmentMode

Last updated: 2026-10-01

- Declaration: `public enum FlowContainer.AlignmentMode`
- Source: [FlowContainer.cs](../../src/Scene/GUI/FlowContainer.cs)
- Owner: [FlowContainer](FlowContainer.md)

## Description

Typed numeric alignment choices for wrapping layout. Invalid values reject before mutation. Setter timing, coordinates and eligibility are documented on [FlowContainer](FlowContainer.md).

## Values

| Value | Number | Contract |
| --- | ---: | --- |
| `Begin` | 0 | Leading edge. |
| `Center` | 1 | Centered group. |
| `End` | 2 | Trailing edge. |

## Verification

[FlowContainerTests](../../tests/Electron2D.Tests/FlowContainerTests.cs) exercises every combination and numeric boundary; native pixels exercise actual alignment. No backend or editor semantic service is added by this enum.
