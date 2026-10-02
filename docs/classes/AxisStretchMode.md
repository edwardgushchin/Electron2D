# AxisStretchMode

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum AxisStretchMode` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Controls how a nine-patch center region fills one axis in controls and styles. Used by [NinePatchRect](NinePatchRect.md) and [StyleBoxTexture](StyleBoxTexture.md) for independent horizontal and vertical center mapping.

## Values

| Value | Meaning |
| --- | --- |
| `Stretch = 0` | Stretches one source center across the destination center. |
| `Tile = 1` | Repeats at natural pixel size, clipping the last partial tile. |
| `TileFit = 2` | Rounds the repeat count and scales complete tiles to fit. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
