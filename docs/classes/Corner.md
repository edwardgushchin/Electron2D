# Corner

Last updated: 2026-09-27

**Inherits:** — · **Inherited By:** —

**Declaration:** `public enum Corner` · **Source:** [Corner.cs](../../src/Core/Math/Corner.cs) · **Component:** [Geometry values](../components/geometry-values.md)

Identifies one corner of a 2D rectangle, clockwise from the top left. Values are immutable and carry no ownership or thread affinity. The enum is not a bitmask.

## Example

```csharp
using var style = new StyleBoxFlat();
style.SetCornerRadius(Corner.TopLeft, 8);
```

## Values

| Name | Value | Rectangle corner |
| --- | --- | --- |
| `TopLeft` | `0` | Left/top intersection. |
| `TopRight` | `1` | Right/top intersection. |
| `BottomRight` | `2` | Right/bottom intersection. |
| `BottomLeft` | `3` | Left/bottom intersection. |

<a id="topleft"></a><a id="topright"></a><a id="bottomright"></a><a id="bottomleft"></a>
These numeric identities remain stable. C# permits undefined integer casts; StyleBoxFlat.GetCornerRadius and SetCornerRadius reject them with ArgumentOutOfRangeException. The enum itself performs no operation and cannot throw.

## Verification and dependencies

[StyleBoxFlatTests](../../tests/Electron2D.Tests/StyleBoxFlatTests.cs) verifies all four numeric identities and invalid-corner rejection alongside actual radius storage and geometry. The current consumer is [StyleBoxFlat](StyleBoxFlat.md); no 3D corner semantics are implied. See [global enum coverage](../coverage/classes/@GlobalScope.md) and [ADR 0082](../decisions/rendering.md#adr-0082).
