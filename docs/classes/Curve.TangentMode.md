# Curve.TangentMode

Last updated: 2026-09-23

- **Declaration:** `public enum Curve.TangentMode`
- **Source:** [Curve.cs](../../src/Scene/Resources/Curve.cs)
- **Component:** [Curves](../components/curves.md)

## Description

Controls each side of a [Curve](Curve.md) control point independently. Values are stable integers, not flags. A tangent is a dy/dx slope, not an angle.

| Value | Integer | Contract |
| --- | --- | --- |
| `Free` | 0 | Caller-supplied slope; default. Explicit tangent setters switch their side to Free. |
| `Linear` | 1 | Recompute slope to an adjacent point on point edits. Without a neighbor the stored slope remains unchanged. Duplicate offsets can produce a nonfinite slope. |
| `Count` | 2 | Sentinel; rejected by mode setters and AddPoint. |

## Example

```csharp
using var curve = new Curve();
curve.AddPoint(new(0, 0), rightMode: Curve.TangentMode.Linear);
curve.AddPoint(new(1, 1), leftMode: Curve.TangentMode.Linear);
```

## Verification

[CurveTests](../../tests/Electron2D.Tests/CurveTests.cs) checks interpolation, mode changes, automatic recomputation after editing/deletion, Free preservation and invalid-mode rejection. No native dependency. See [ADR 0013](../decisions/resources.md#adr-0013).
