# TextureStretchMode

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum TextureStretchMode` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Controls how a texture is placed inside a rectangular control. Used by [TextureRect](TextureRect.md) and [TextureButton](TextureButton.md) to place an image inside their control rectangle.

## Values

| Value | Meaning |
| --- | --- |
| `Scale = 0` | Stretches to the full control rectangle. |
| `Tile = 1` | Repeats at natural logical pixel size. |
| `Keep = 2` | Keeps natural size at the leading top-left position. |
| `KeepCentered = 3` | Centers natural size. |
| `KeepAspect = 4` | Fits aspect with integer-truncated dimensions. |
| `KeepAspectCentered = 5` | Centers an aspect-preserving integer fit. |
| `KeepAspectCovered = 6` | Covers the rectangle using a centered source crop. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
