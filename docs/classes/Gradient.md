# Gradient

Last updated: 2026-09-23

- Declaration: `public sealed class Gradient : Resource`
- Source: [Gradient.cs](../../src/Scene/Resources/Gradient.cs)
- Inherits: [Resource](Resource.md)
- Component: [Gradients](../components/gradients.md)

## Description

A reusable color transition with finite, unclamped offsets and RGBA values. The default points are opaque black at zero and white at one. Sampling returns nonlinear SRGB colors; alpha is interpolated independently. Linear, Constant and Cubic modes support SRGB, LinearSRGB and OKLAB spaces. Cubic sampling may overshoot. Exact points and outer endpoint holds return the stored colors without conversion. Empty gradients sample opaque black; a single point is constant everywhere.

Points sort lazily before indexed color/offset operations, Reverse and Sample. Bulk arrays expose the current storage order, and RemovePoint removes from that order without sorting. Equal-offset ordering is unspecified. Array input/output is copied; the gradient owns its points. Resource duplication owns independent containers and preserves unsorted storage, modes and values exactly.

A resource lock serializes individual operations and complete texture bakes. Events execute synchronously on the editing thread, outside the lock after commitment. A throwing subscriber propagates its error and prevents later subscribers from running, following the inherited C# event contract; after correcting the subscriber, EmitChanged can notify dependent textures again. Coordinate multi-call edits, copying and disposal across threads. Warm Sample calls allocate no memory; edits, sorting setup and array exports may allocate.

## Example

```csharp
using var gradient = new Gradient();
gradient.SetColor(0, Colors.Red);
gradient.SetColor(1, Colors.Blue);
Color middle = gradient.Sample(0.5f);
using var ramp = new GradientRampTexture { Gradient = gradient, Width = 256 };
using var image = ramp.GetImage()!;
```

Construction, sampling and image access require no active window or native backend.

## API summary

| Declaration | Contract |
| --- | --- |
| `Gradient()` | Black-to-white, Linear, SRGB. |
| `float[] Offsets { get; set; }` | Copied positions in current storage order; resizes points. |
| `Color[] Colors { get; set; }` | Copied colors in current storage order; resizes points. |
| `InterpolationMode InterpolationMode { get; set; }` | Linear (0), Constant (1), Cubic (2). |
| `ColorSpace InterpolationColorSpace { get; set; }` | SRGB (0), LinearSRGB (1), OKLAB (2). |
| `void AddPoint(float offset, Color color)` | Append, invalidate sort, notify. |
| `void RemovePoint(int point)` | Remove current storage index; cannot remove the final point. |
| `int GetPointCount()` | Count, including zero; no sorting. |
| `Color GetColor(int point)` | Sorted indexed color query. |
| `float GetOffset(int point)` | Sorted indexed position query. |
| `void SetColor(int point, Color color)` | Sort, replace color, notify. |
| `void SetOffset(int point, float offset)` | Sort, replace offset, invalidate sort, notify. |
| `void Reverse()` | Mirror offsets about 0.5, sort, notify. |
| `Color Sample(float offset)` | Interpolate between actual stored positions, holding outer colors. |

## Property descriptions

### Offsets

Initially [0, 1]. Setter resizes to the supplied length, retains existing colors by storage index and initializes added colors to opaque black. Empty input clears all points. It marks sorting dirty and emits Changed even for equal values. Null throws ArgumentNullException; nonfinite values throw ArgumentException before mutation. Offsets outside 0..1 remain valid.

### Colors

Initially opaque black and white. Setter resizes points, preserves existing offsets and initializes added offsets to zero. Growth invalidates sorting; shrinking preserves the previous sort state. Every assignment emits Changed, even when equal. Empty input clears points. Null and nonfinite channels fail before mutation. Finite signed/HDR channels are accepted without clamping.

### InterpolationMode

Linear initially. Actual changes emit Changed followed by PropertyListChanged; equal assignments are silent. If a Changed callback disposes the resource or throws, the subsequent property-list event does not execute. Constant returns the preceding color until the exact next point and ignores the selected color space. Undefined values throw ArgumentOutOfRangeException. Runtime descriptors retain the color-space property; an editor inspector hiding it in Constant mode is not implemented.

### InterpolationColorSpace

SRGB initially. LinearSRGB converts the RGB channels before interpolation and converts back afterward. OKLAB uses the existing internal perceptual-color conversion through linear RGB. Alpha is never color-space converted. Actual changes emit Changed; equal assignments are silent. Undefined values throw ArgumentOutOfRangeException.

## Method descriptions

### AddPoint

Appends the finite offset/color pair without clamping or immediate sorting and emits Changed. Nonfinite inputs throw ArgumentException before mutation.

### RemovePoint

Uses the current storage index, which can differ from sorted order. Invalid indices throw ArgumentOutOfRangeException; removing the final remaining point throws InvalidOperationException. Use empty Colors or Offsets to clear a gradient. Successful removal emits Changed.

### GetPointCount

Returns the current count without sorting or emitting events.

### GetColor

Sorts by offset and returns the stored color at the supplied sorted index. Invalid indices throw ArgumentOutOfRangeException. No event is emitted by sorting.

### GetOffset

Sorts by offset and returns the finite stored position. Invalid indices throw ArgumentOutOfRangeException. No event is emitted.

### SetColor

Sorts, validates and replaces the specified point's color. The index must exist and channels must be finite. Successful assignment always emits Changed, including equality.

### SetOffset

Sorts, validates and replaces one offset, then marks storage unsorted. The index must exist and offset must be finite. Successful assignment always emits Changed, including equality.

### Reverse

Replaces each offset with 1-offset and sorts. Emits Changed even for empty or symmetric data. It does not clamp the domain.

### Sample

Validates a finite position, sorts once and binary-searches neighboring points. Positions outside the actual first/last offsets hold those endpoint colors; there is no initial clamp to 0..1. Linear interpolates adjacent colors; Cubic uses Catmull–Rom with duplicated outer neighbors. Weight arithmetic widens coordinate subtraction to avoid overflow for finite extreme offsets. Color arithmetic retains derived IEEE overflow and cubic overshoot; GPU appearance of nonfinite derived colors is not guaranteed. Nonfinite explicit positions throw ArgumentException.

## Enumeration descriptions

See [InterpolationMode](InterpolationMode.md) and [Gradient.ColorSpace](Gradient.ColorSpace.md).

## Protected hooks and lifecycle

GetPropertyDescriptors supplies typed Offsets, Colors, InterpolationMode and InterpolationColorSpace metadata plus Resource metadata. CreateDuplicateInstance returns Gradient. CopyCustomStateTo clones point storage and copies the sort flag and modes without setter resizing or sorting. Dispose(bool) releases points before Resource cleanup. State operations throw ObjectDisposedException after disposal. Resource paths, local-scene graph ownership and copy notifications retain their inherited contracts.

## Verification and limits

[GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) checks numeric color-space/interpolation samples, storage order, events, extreme coordinates, empty/degenerate data, copy/scene ownership, concurrency, failure and warm allocations. [Native checks](../../tests/Electron2D.Tests/RenderingGradientTests.cs) verify the texture consumers. Editor authoring, disk serialization, other platforms, owner visual acceptance and fresh AOT/self-contained publication remain unverified or absent. See [ADR 0013](../decisions/resources.md#adr-0013).
