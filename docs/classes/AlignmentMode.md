# AlignmentMode

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum AlignmentMode` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Aligns content at the leading edge, center, or trailing edge. Used by [BoxContainer](BoxContainer.md), [AspectRatioContainer](AspectRatioContainer.md), and [FlowContainer](FlowContainer.md). Each owner applies the same leading/center/trailing choice to its own layout geometry.

## Values

| Value | Meaning |
| --- | --- |
| `Begin = 0` | Begins at the leading edge. |
| `Center = 1` | Centers the child group. |
| `End = 2` | Ends at the trailing edge. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
