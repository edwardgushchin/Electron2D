# ProcessPhase

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum ProcessPhase` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Selects the physics or idle scene-tree update phase. Used by [Timer](Timer.md), [Tween](Tween.md), and [Camera](Camera.md) to choose physics or idle scene-tree updates.

## Values

| Value | Meaning |
| --- | --- |
| `Physics = 0` | Advances during fixed-step physics-process frames. |
| `Idle = 1` | Advances during variable-step process frames. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. The owner-specific behavior and platform limits are documented by the consuming APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
