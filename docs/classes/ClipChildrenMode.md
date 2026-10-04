# ClipChildrenMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum ClipChildrenMode`.

**Source:** [ClipChildrenMode.cs](../../src/Scene/Main/ClipChildrenMode.cs).

## Description

Typed alpha-mask policy for [CanvasItem.ClipChildren](CanvasItem.md#alpha-masks). Values retain numeric identity; Max is an exclusive upper bound, not an assignable mode. The setter rejects invalid values before changing stored state. The enum owns no resources. In-memory PackedScene stores the selected mode. GPU and hardware compatibility execute the baseline; software explicitly rejects native advanced blend requirements.

## Example

Given an authored drawable with recorded mask geometry and canvas children:

```csharp
drawable.ClipChildren = ClipChildrenMode.Only;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ClipChildrenMode AndDraw = 2` | Draws the item before its descendants, then masks their combined color. |
| `public const Electron2D.ClipChildrenMode Disabled = 0` | Draws the item and descendants normally. |
| `public const Electron2D.ClipChildrenMode Max = 3` | Defines the exclusive upper bound; it is not an assignable mode. |
| `public const Electron2D.ClipChildrenMode Only = 1` | Uses the item's alpha as a mask without drawing its own color. |

## Enumeration Descriptions

<a id="member-b56ea896d531"></a>
### AndDraw

`public const Electron2D.ClipChildrenMode AndDraw = 2`

Draws the item before its descendants, then masks their combined color.

<a id="member-512bbdee2e63"></a>
### Disabled

`public const Electron2D.ClipChildrenMode Disabled = 0`

Draws the item and descendants normally.

<a id="member-1958d773e3cf"></a>
### Max

`public const Electron2D.ClipChildrenMode Max = 3`

Defines the exclusive upper bound; it is not an assignable mode.

<a id="member-17d3f7126e57"></a>
### Only

`public const Electron2D.ClipChildrenMode Only = 1`

Uses the item's alpha as a mask without drawing its own color.


## Verification and limits

[CanvasClipTests](../../tests/Electron2D.Tests/CanvasClipTests.cs) checks enum boundaries, stored reconstruction and native behavior. Nested same-Z masks/groups, writable-backbuffer reads and editor inspector integration retain explicit boundaries in [the composition component](../components/canvas-rendering.md#canvas-alpha-masks).
