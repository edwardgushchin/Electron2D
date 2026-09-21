# ClockDirection

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/Math/ClockDirection.cs`](../../src/Core/Math/ClockDirection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum ClockDirection`

> Identifies clockwise or counterclockwise rotation in the 2D coordinate plane.

## Description

The direction follows screen-style 2D coordinates used by images: positive X points right and positive Y points down. It is currently consumed by [`Image.Rotate90`](Image.md#rotate90) and is reusable by later 2D APIs without introducing a separate direction type.

## Examples

Partial snippet: `image` is an existing nonempty, uncompressed `Image` supplied by the caller.

```csharp
image.Rotate90(ClockDirection.Clockwise);
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`Clockwise`](#clockwise) | 0 | Rotate in the direction of clock hands. |
| [`CounterClockwise`](#counterclockwise) | 1 | Rotate opposite to clock hands. |

## Enumeration Descriptions

<a id="clockwise"></a>
### `Clockwise = 0`

For image coordinates, moves the former top edge to the right edge.

<a id="counterclockwise"></a>
### `CounterClockwise = 1`

For image coordinates, moves the former top edge to the left edge.

## Invariants and errors

The enum is immutable and thread-safe. Consumers validate values when direction affects mutation; `Image.Rotate90` rejects undefined values with `ArgumentOutOfRangeException`.

## Dependencies and interactions

`ClockDirection` has no dependencies. It is currently part of the managed-image component's public surface.

## Verification and known limitations

Tests verify both numeric identities, both rotation directions, output dimensions, and pixel placement. Only quarter-turn image rotation currently consumes it.
