# BoxContainer.AlignmentMode

Last updated: 2026-09-27

**Inherits:** System.Enum · **Inherited By:** —

**Declaration:** `public enum AlignmentMode` nested in [BoxContainer](BoxContainer.md) · **Source:** [BoxContainer.cs](../../src/Scene/GUI/BoxContainer.cs)

## Description, summary and enumeration descriptions

Places primary-axis unused space around the child group. Horizontal leading/trailing follows RTL; vertical leading is top.

| Signature | Contract |
| --- | --- |
| `Begin = 0` | Default; begin at the leading edge. |
| `Center = 1` | Integer-centered group. |
| `End = 2` | End at the trailing edge. |

<a id="begin"></a><a id="center"></a><a id="end"></a>
All values are consumed by actual arrangement; undefined values reject before mutation. Example: `box.Alignment = BoxContainer.AlignmentMode.Center;`. Assignment uses the owning box's owner/capture/lifetime guards and immediately arranges visible attached children. [BoxContainerTests](../../tests/Electron2D.Tests/BoxContainerTests.cs) verifies ordering and placement under [ADR 0081](../decisions/rendering.md#adr-0081).
