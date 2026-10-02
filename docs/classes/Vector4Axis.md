# Vector4Axis

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum Vector4Axis` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Identifies one vector component. Returned by [Vector4](Vector4.md) and [Vector4i](Vector4i.md) axis queries. Only X, Y, Z and W are valid.

## Values

| Value | Meaning |
| --- | --- |
| `X = 0` | Identifies the X component. |
| `Y = 1` | Identifies the Y component. |
| `Z = 2` | Identifies the Z component. |
| `W = 3` | Identifies the W component. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
