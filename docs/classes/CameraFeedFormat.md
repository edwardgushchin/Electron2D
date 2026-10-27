# CameraFeedFormat

Last updated: 2026-10-27

`public struct Electron2D.CameraFeedFormat`

Inherits: `System.ValueType`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

Immutable copied source-format metadata replacing backend dictionaries. Width/Height are pixels; PixelFormat names the source encoding. FPSNumerator/FPSDenominator form the rational frame rate; zero numerator means unknown. CameraFeed.Formats validates positive bounded dimensions, a nonempty format name and a positive denominator before returning its independent array. Construction itself is a plain C# value operation; record equality, deconstruction and formatting retain ordinary value semantics.

## Example

The source/format comments below identify any required caller context.

```csharp
var format = new CameraFeedFormat(640, 480, "Authored RGBA", 30000, 1001);
var rate = (double)format.FPSNumerator / format.FPSDenominator;
```

## API reference

| Declaration | Contract |
| --- | --- |
| [`public CameraFeedFormat(System.Int32 Width, System.Int32 Height, System.String PixelFormat, System.Int32 FPSNumerator, System.Int32 FPSDenominator)`](#member-67dd2084d630) | Describes one camera source format without exposing native backend identifiers. |
| [`public System.Void Deconstruct(out System.Int32 Width, out System.Int32 Height, out System.String PixelFormat, out System.Int32 FPSNumerator, out System.Int32 FPSDenominator)`](#member-e064f32450af) | Ordinary C# record value operation. |
| [`public virtual System.Boolean Equals(Electron2D.CameraFeedFormat other)`](#member-b1794c28d97d) | Ordinary C# record value operation. |
| [`public override System.Boolean Equals(System.Object obj)`](#member-72b5814feac2) | Ordinary C# record value operation. |
| [`public override System.Int32 GetHashCode()`](#member-138d52507b89) | Ordinary C# record value operation. |
| [`public override System.String ToString()`](#member-d3399db23542) | Ordinary C# record value operation. |
| [`public static System.Boolean op_Equality(Electron2D.CameraFeedFormat left, Electron2D.CameraFeedFormat right)`](#member-6ff7ebb16f2a) | Ordinary C# record value operation. |
| [`public static System.Boolean op_Inequality(Electron2D.CameraFeedFormat left, Electron2D.CameraFeedFormat right)`](#member-01e27eb4d52f) | Ordinary C# record value operation. |
| [`public System.Int32 FPSDenominator { get; set; }`](#member-da32a2355d98) | Positive rational frame-rate denominator. |
| [`public System.Int32 FPSNumerator { get; set; }`](#member-43df1e9544d8) | Nonnegative rational frame-rate numerator; zero means unknown. |
| [`public System.Int32 Height { get; set; }`](#member-dad17b2fcc03) | Positive frame height in pixels. |
| [`public System.String PixelFormat { get; set; }`](#member-43b2bfbe142f) | Source format name, distinct from the converted output image format. |
| [`public System.Int32 Width { get; set; }`](#member-933762af0589) | Positive frame width in pixels. |

## Member descriptions

<a id="member-67dd2084d630"></a>
### `public CameraFeedFormat(System.Int32 Width, System.Int32 Height, System.String PixelFormat, System.Int32 FPSNumerator, System.Int32 FPSDenominator)`

Describes one camera source format without exposing native backend identifiers.

Parameters:

- `Width`: Positive frame width in pixels.
- `Height`: Positive frame height in pixels.
- `PixelFormat`: Source format name, distinct from the converted output image format.
- `FPSNumerator`: Nonnegative rational frame-rate numerator; zero means unknown.
- `FPSDenominator`: Positive rational frame-rate denominator.

<a id="member-e064f32450af"></a>
### `public System.Void Deconstruct(out System.Int32 Width, out System.Int32 Height, out System.String PixelFormat, out System.Int32 FPSNumerator, out System.Int32 FPSDenominator)`

Ordinary C# record value operation.

<a id="member-b1794c28d97d"></a>
### `public virtual System.Boolean Equals(Electron2D.CameraFeedFormat other)`

Ordinary C# record value operation.

<a id="member-72b5814feac2"></a>
### `public override System.Boolean Equals(System.Object obj)`

Ordinary C# record value operation.

<a id="member-138d52507b89"></a>
### `public override System.Int32 GetHashCode()`

Ordinary C# record value operation.

<a id="member-d3399db23542"></a>
### `public override System.String ToString()`

Ordinary C# record value operation.

<a id="member-6ff7ebb16f2a"></a>
### `public static System.Boolean op_Equality(Electron2D.CameraFeedFormat left, Electron2D.CameraFeedFormat right)`

Ordinary C# record value operation.

<a id="member-01e27eb4d52f"></a>
### `public static System.Boolean op_Inequality(Electron2D.CameraFeedFormat left, Electron2D.CameraFeedFormat right)`

Ordinary C# record value operation.

<a id="member-da32a2355d98"></a>
### `public System.Int32 FPSDenominator { get; set; }`

Positive rational frame-rate denominator.

<a id="member-43df1e9544d8"></a>
### `public System.Int32 FPSNumerator { get; set; }`

Nonnegative rational frame-rate numerator; zero means unknown.

<a id="member-dad17b2fcc03"></a>
### `public System.Int32 Height { get; set; }`

Positive frame height in pixels.

<a id="member-43b2bfbe142f"></a>
### `public System.String PixelFormat { get; set; }`

Source format name, distinct from the converted output image format.

<a id="member-933762af0589"></a>
### `public System.Int32 Width { get; set; }`

Positive frame width in pixels.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
