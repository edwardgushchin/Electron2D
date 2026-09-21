# ImageMetrics

Last updated: 2026-09-21

**Inherits:** `System.ValueType`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public readonly record struct ImageMetrics`

> Contains scalar error measurements returned by [`Image.ComputeImageMetrics`](Image.md#computeimagemetrics).

## Description

`ImageMetrics` is an immutable value. All errors are measured in 8-bit component units even when source images use another readable format. A luminance comparison contributes one error per overlapping pixel; an RGBA comparison contributes four.

The type is a typed replacement for an unstructured metric dictionary. Record-struct value equality compares all five stored doubles.

## Examples

Partial snippet: `actual` and `expected` are existing nonempty, uncompressed images supplied by the caller.

```csharp
ImageMetrics metrics = actual.ComputeImageMetrics(expected, useLuma: false);
if (metrics.Maximum > 1)
    throw new InvalidOperationException("Image mismatch exceeds tolerance.");
```

## Constructors

| Member | Description |
| --- | --- |
| [`public ImageMetrics(double maximum, double mean, double meanSquared, double rootMeanSquared, double peakSignalToNoiseRatio)`](#ctor) | Initializes all measurements. |

## Properties

| Member | Description |
| --- | --- |
| [`public double Maximum { get; }`](#maximum) | Largest absolute component error. |
| [`public double Mean { get; }`](#mean) | Mean absolute component error. |
| [`public double MeanSquared { get; }`](#meansquared) | Mean squared component error. |
| [`public double RootMeanSquared { get; }`](#rootmeansquared) | Root-mean-squared component error. |
| [`public double PeakSignalToNoiseRatio { get; }`](#peaksignaltonoiseratio) | Peak signal-to-noise ratio in decibels. |

## Methods and operators

| Member | Description |
| --- | --- |
| [`public bool Equals(ImageMetrics other)`](#equals-typed) | Generated value comparison across all five fields. |
| [`public override bool Equals(object? obj)`](#equals-object) | Generated boxed value comparison. |
| [`public override int GetHashCode()`](#gethashcode) | Generated hash of the five fields. |
| [`public override string ToString()`](#tostring) | Generated diagnostic record representation. |
| [`public static bool operator ==(ImageMetrics left, ImageMetrics right)`](#op-equality) | Generated value equality. |
| [`public static bool operator !=(ImageMetrics left, ImageMetrics right)`](#op-inequality) | Generated value inequality. |

## Constructor Descriptions

<a id="ctor"></a>
### `public ImageMetrics(double maximum, double mean, double meanSquared, double rootMeanSquared, double peakSignalToNoiseRatio)`

Stores all supplied values without recomputation or validation. Normal engine-produced values follow the ranges below; direct callers can construct other values.

## Property Descriptions

<a id="maximum"></a>
### `public double Maximum { get; }`

The largest observed absolute 8-bit error, normally from 0 through 255.

<a id="mean"></a>
### `public double Mean { get; }`

The arithmetic mean of absolute 8-bit errors, normally from 0 through 255.

<a id="meansquared"></a>
### `public double MeanSquared { get; }`

The arithmetic mean of squared 8-bit errors, normally from 0 through 65,025.

<a id="rootmeansquared"></a>
### `public double RootMeanSquared { get; }`

The square root of `MeanSquared`, normally from 0 through 255.

<a id="peaksignaltonoiseratio"></a>
### `public double PeakSignalToNoiseRatio { get; }`

The decibel value `20 × log10(255 / RootMeanSquared)`, clamped to `0..500`. Identical compared regions report 500.

## Method and Operator Descriptions

<a id="equals-typed"></a>
### `public bool Equals(ImageMetrics other)`

Returns true when all five stored `double` values compare equal under generated record-struct equality.

<a id="equals-object"></a>
### `public override bool Equals(object? obj)`

Returns true only for another equal `ImageMetrics` value.

<a id="gethashcode"></a>
### `public override int GetHashCode()`

Combines all five stored values for hash-based collections. The hash is not a persistent serialization identity.

<a id="tostring"></a>
### `public override string ToString()`

Returns the compiler-generated diagnostic record representation; it is not a stable persistence format.

<a id="op-equality"></a>
### `public static bool operator ==(ImageMetrics left, ImageMetrics right)`

Returns `left.Equals(right)`.

<a id="op-inequality"></a>
### `public static bool operator !=(ImageMetrics left, ImageMetrics right)`

Returns the negation of value equality.

## Lifecycle, invariants, and threading

The value owns no resources, has no mutable state, needs no disposal, and is thread-safe by immutability. Default construction yields five zero values. Its constructor deliberately does not enforce relationships among fields; `Image.ComputeImageMetrics` is the authoritative producer.

## Dependencies and interactions

The type contains only `double` values and is returned by `Image`. It has no resource, renderer, or native dependency.

## Verification and known limitations

Tests verify identical images, single-component differences, RGBA aggregation, luminance mode, and rejection of out-of-range source components. Metrics compare only the common top-left base-level area and do not apply perceptual color management. See [ADR 0039](../decisions/resources.md#adr-0039).
