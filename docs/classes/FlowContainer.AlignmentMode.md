# FlowContainer.AlignmentMode

Last updated: 2026-10-06

**Source:** [`src/Scene/GUI/FlowContainer.cs`](../../src/Scene/GUI/FlowContainer.cs)
**Declaration:** `public enum AlignmentMode` nested in `Electron2D.FlowContainer`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Aligns each wrapping row or column along its primary filling axis. This enum belongs to [FlowContainer](FlowContainer.md); other container alignment enums are distinct public types. Begin/End mirror horizontally in RTL layout.

## Values

| Value | Meaning |
| --- | --- |
| `Begin = 0` | Begins at the leading edge. |
| `Center = 1` | Centers the child group. |
| `End = 2` | Ends at the trailing edge. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Values remain Begin=0, Center=1 and End=2. The declaring owner is preserved under ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
