# RecursiveBehavior

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum RecursiveBehavior` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Inherits, disables, or restores a capability within a Control subtree. Used by [Control](Control.md) for both pointer-input and keyboard-focus inheritance. The property identifies which capability the policy governs.

## Values

| Value | Meaning |
| --- | --- |
| `Inherited = 0` | Follow the direct parent control, or allow the capability when there is none. |
| `Disabled = 1` | Disable the capability unless a descendant explicitly enables it. |
| `Enabled = 2` | Allow the capability regardless of the parent control's policy. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
