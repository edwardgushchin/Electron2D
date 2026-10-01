# FlowContainer.LastWrapAlignmentMode

Last updated: 2026-10-01

- Declaration: `public enum FlowContainer.LastWrapAlignmentMode`
- Source: [FlowContainer.cs](../../src/Scene/GUI/FlowContainer.cs)
- Owner: [FlowContainer](FlowContainer.md)

## Description

Typed numeric alignment choices for wrapping layout. Invalid values reject before mutation. Setter timing, coordinates and eligibility are documented on [FlowContainer](FlowContainer.md).

## Values

| Value | Number | Contract |
| --- | ---: | --- |
| `Inherit` | 0 | Ordinary full-container alignment. |
| `Begin` | 1 | Previous group leading edge. |
| `Center` | 2 | Previous group center. |
| `End` | 3 | Previous group trailing edge. |

## Verification

[FlowContainerTests](../../tests/Electron2D.Tests/FlowContainerTests.cs) exercises every combination and numeric boundary; native pixels exercise actual alignment. No backend or editor semantic service is added by this enum.
