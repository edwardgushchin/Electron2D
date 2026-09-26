# Control.SizeFlags

Last updated: 2026-09-27

**Inherits:** System.Enum · **Inherited By:** —

**Declaration:** `[Flags] public enum SizeFlags` nested in [Control](Control.md) · **Source:** [Control.SizeFlags.cs](../../src/Scene/GUI/Control.SizeFlags.cs)

## Description

Independent axis bits consumed by [Container](Container.md) and [BoxContainer](BoxContainer.md). Fill controls whether a child fills its allocation; Expand controls participation in weighted primary-axis allocation. Shrink flags position a non-filled rectangle. Unknown bits remain stored and ignored. End wins over Center, and Fill disables shrink alignment. Horizontal leading/trailing edges follow RTL.

## Enumeration summary and descriptions

| Signature | Behavior |
| --- | --- |
| `ShrinkBegin = 0` | Use bound minimum at the leading edge. |
| `Fill = 1` | Fill the allocation subject to size bounds. |
| `Expand = 2` | Participate in primary-axis weighted expansion. |
| `ExpandFill = 3` | Expand and fill. |
| `ShrinkCenter = 4` | Center the non-filled rectangle, flooring its offset. |
| `ShrinkEnd = 8` | Position non-filled content at the trailing edge. |

<a id="shrinkbegin"></a><a id="fill"></a><a id="expand"></a><a id="expandfill"></a><a id="shrinkcenter"></a><a id="shrinkend"></a>
Values can combine Fill/Expand with alignment bits. Expansion does not imply filling. Cross-axis Expand has no allocation effect for a box; inspector choices omit it on that axis without rejecting stored values.

## Example, defaults and verification

```csharp
var child = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, SizeFlagsStretchRatio = 2 };
```

Both axes default to Fill and ratio to one. Range overrides vertical flags to ShrinkBegin; TextureProgressBar restores Fill. Actual changes emit SizeFlagsChanged; equal assignments are silent. Finite ratios may be zero or negative; positive total weights govern allocation. Nonfinite inputs reject before mutation. Owner/capture/lifetime guards apply. Stored descriptors preserve flags and ratio in PackedScene. [Managed tests](../../tests/Electron2D.Tests/BoxContainerTests.cs) and [native tests](../../tests/Electron2D.Tests/BoxContainerRenderingTests.cs) verify the real consumer under [ADR 0081](../decisions/rendering.md#adr-0081).
