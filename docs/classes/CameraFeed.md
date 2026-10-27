# CameraFeed

Last updated: 2026-10-27

`public class Electron2D.CameraFeed`

Inherits: `Electron2D.ElectronObject`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

A creating-thread-owned source with copied pixel planes. Image submission may run on producer threads; CameraServer.Process delivers coalesced format then frame events on the owner. Registered custom sources remain caller-owned and must be removed before disposal. Activation rejection keeps the source inactive; failed deactivation still commits inactivity. Source hooks reject recursive operations; events may query formats and change/stop the source, while disposal and registry mutation remain guarded. Supported texture images retain their source format and mipmaps within the shared 64 MiB source/upload budget. Two-plane submission is transactional. An invalid image, format, index, enum, disposed source or wrong owner fails explicitly before replacement. See the component for native/custom ownership and external-image limits.

## Example

The source/format comments below identify any required caller context.

```csharp
using var feed = new CameraFeed();
CameraServer.AddFeed(feed);
try
{
    feed.FeedIsActive = true;
    using var frame = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
    frame.Fill(Colors.Red);
    feed.SetRGBImage(frame);
    CameraServer.Process();
}
finally { CameraServer.RemoveFeed(feed); }
```

## API reference

| Declaration | Contract |
| --- | --- |
| [`public CameraFeed()`](#member-98d64122b633) | Creates a caller-owned inactive source with no pixels. |
| [`public event System.Action FormatChanged`](#member-03c117565dd9) | Runs on the creating thread when new image geometry or plane layout has been published. |
| [`public event System.Action FrameChanged`](#member-40caf7df3b86) | Runs on the creating thread when one or more new frames have been published. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-81e4cf9c35ab) | Retires this object's observers/pixel/native state without disposing borrowed sources. |
| [`public Electron2D.CameraFeed.FeedDataType GetDatatype()`](#member-f4042d7c5266) | Returns the current pixel arrangement. |
| [`public System.Int32 GetID()`](#member-63ba0f35c49d) | Returns the process-unique positive source identity. |
| [`public System.String GetName()`](#member-fa449284a045) | Returns the source's display name. |
| [`public Electron2D.CameraFeed.FeedPosition GetPosition()`](#member-d2b26dd2f01b) | Returns the physical camera position. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-793dc3eb8838) | Extends the inherited typed property metadata with this type's current configuration. |
| [`protected virtual System.Boolean OnActivateFeed()`](#member-fcaa9f4645af) | Starts this source; the plain authored-image source accepts activation. |
| [`protected virtual System.Void OnDeactivateFeed()`](#member-f4de7103a801) | Stops this source and releases its active native state. |
| [`protected virtual Electron2D.CameraFeedFormat[] OnGetFormats()`](#member-257909b18ccd) | Supplies supported source-format metadata. |
| [`protected virtual System.Boolean OnSetFormat(System.Int32 index, Electron2D.CameraFeedOutput output)`](#member-1d4625563b8a) | Selects one supported source format and conversion. |
| [`public System.Boolean SetFormat(System.Int32 index, Electron2D.CameraFeedOutput output = RGB)`](#member-ada0ec171e0c) | Selects a supported source format while inactive. |
| [`public System.Void SetName(System.String name)`](#member-4be5ca0a89fc) | Replaces the source's display name. |
| [`public System.Void SetPosition(Electron2D.CameraFeed.FeedPosition position)`](#member-d4c1a7f2c1c4) | Replaces the physical camera position. |
| [`public System.Void SetRGBImage(Electron2D.Image image)`](#member-b67db5ae4a8a) | Submits copied color pixels; an inactive source ignores a valid image. |
| [`public System.Void SetYCBCRImage(Electron2D.Image image)`](#member-112ad3709da3) | Submits copied packed YCbCr pixels for consumer-side conversion. |
| [`public System.Void SetYCBCRImages(Electron2D.Image yImage, Electron2D.Image cbcrImage)`](#member-dccd18165c19) | Atomically submits copied luminance and interleaved CbCr planes. |
| [`protected override System.Void ValidateDisposal()`](#member-0f16a671744e) | Enforces the documented ownership, callback and permanent-service lifetime rules. |
| [`protected override System.Void ValidateMutation()`](#member-37850bd1b7f3) | Enforces the owning service/source mutation contract before inherited mutation. |
| [`public System.Boolean FeedIsActive { get; set; }`](#member-23dc075b8073) | Gets or changes source activation through the typed hooks. |
| [`public Electron2D.Transform FeedTransform { get; set; }`](#member-8ac2ce237104) | Gets or sets finite normalized image-coordinate metadata. |
| [`public Electron2D.CameraFeedFormat[] Formats { get;  }`](#member-56c928ec468a) | Gets a copied list of supported formats from the source hook. |

## Member descriptions

<a id="member-98d64122b633"></a>
### `public CameraFeed()`

Creates a caller-owned inactive source with no pixels.

<a id="member-03c117565dd9"></a>
### `public event System.Action FormatChanged`

Runs on the creating thread when new image geometry or plane layout has been published.

<a id="member-40caf7df3b86"></a>
### `public event System.Action FrameChanged`

Runs on the creating thread when one or more new frames have been published.

<a id="member-81e4cf9c35ab"></a>
### `protected override System.Void Dispose(System.Boolean disposing)`

Retires this object's observers/pixel/native state without disposing borrowed sources.

<a id="member-f4042d7c5266"></a>
### `public Electron2D.CameraFeed.FeedDataType GetDatatype()`

Returns the current pixel arrangement.

Returns: RGB initially, even before a first image.

<a id="member-63ba0f35c49d"></a>
### `public System.Int32 GetID()`

Returns the process-unique positive source identity.

Returns: The same identity throughout this feed's lifetime.

<a id="member-fa449284a045"></a>
### `public System.String GetName()`

Returns the source's display name.

Returns: Initially three question marks.

<a id="member-d2b26dd2f01b"></a>
### `public Electron2D.CameraFeed.FeedPosition GetPosition()`

Returns the physical camera position.

Returns: Unspecified initially.

<a id="member-793dc3eb8838"></a>
### `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Extends the inherited typed property metadata with this type's current configuration.

<a id="member-fcaa9f4645af"></a>
### `protected virtual System.Boolean OnActivateFeed()`

Starts this source; the plain authored-image source accepts activation.

Returns: True if activation succeeds.

<a id="member-f4de7103a801"></a>
### `protected virtual System.Void OnDeactivateFeed()`

Stops this source and releases its active native state.

<a id="member-257909b18ccd"></a>
### `protected virtual Electron2D.CameraFeedFormat[] OnGetFormats()`

Supplies supported source-format metadata.

Returns: Source-owned metadata, copied by Formats.

<a id="member-1d4625563b8a"></a>
### `protected virtual System.Boolean OnSetFormat(System.Int32 index, Electron2D.CameraFeedOutput output)`

Selects one supported source format and conversion.

Returns: False when unsupported.

Parameters:

- `index`: Validated format index.
- `output`: Validated conversion output.

<a id="member-ada0ec171e0c"></a>
### `public System.Boolean SetFormat(System.Int32 index, Electron2D.CameraFeedOutput output = RGB)`

Selects a supported source format while inactive.

Returns: Whether the source accepts the format and conversion.

Parameters:

- `index`: Index in Formats.
- `output`: Defined conversion output; RGB initially.

Errors:

- `T:System.InvalidOperationException`: The source is active or a hook is reentered.

<a id="member-4be5ca0a89fc"></a>
### `public System.Void SetName(System.String name)`

Replaces the source's display name.

Parameters:

- `name`: A nonnull display label.

<a id="member-d4c1a7f2c1c4"></a>
### `public System.Void SetPosition(Electron2D.CameraFeed.FeedPosition position)`

Replaces the physical camera position.

Parameters:

- `position`: A defined position.

<a id="member-b67db5ae4a8a"></a>
### `public System.Void SetRGBImage(Electron2D.Image image)`

Submits copied color pixels; an inactive source ignores a valid image.

Parameters:

- `image`: Nonempty supported texture image, bounded to 64 MiB including upload storage.

<a id="member-112ad3709da3"></a>
### `public System.Void SetYCBCRImage(Electron2D.Image image)`

Submits copied packed YCbCr pixels for consumer-side conversion.

Parameters:

- `image`: Nonempty supported packed image; its channel interpretation remains with the producer and consumer.

<a id="member-dccd18165c19"></a>
### `public System.Void SetYCBCRImages(Electron2D.Image yImage, Electron2D.Image cbcrImage)`

Atomically submits copied luminance and interleaved CbCr planes.

Parameters:

- `yImage`: Nonempty supported luminance image.
- `cbcrImage`: Nonempty supported chroma image; sampling dimensions may differ.

<a id="member-0f16a671744e"></a>
### `protected override System.Void ValidateDisposal()`

Enforces the documented ownership, callback and permanent-service lifetime rules.

<a id="member-37850bd1b7f3"></a>
### `protected override System.Void ValidateMutation()`

Enforces the owning service/source mutation contract before inherited mutation.

<a id="member-23dc075b8073"></a>
### `public System.Boolean FeedIsActive { get; set; }`

Gets or changes source activation through the typed hooks.

Value: False initially. A rejected activation remains false. Deactivation commits false even if cleanup throws.

<a id="member-8ac2ce237104"></a>
### `public Electron2D.Transform FeedTransform { get; set; }`

Gets or sets finite normalized image-coordinate metadata.

Value: Initially flips the vertical coordinate and translates it by one. Ordinary Texture drawing does not apply it implicitly.

<a id="member-56c928ec468a"></a>
### `public Electron2D.CameraFeedFormat[] Formats { get;  }`

Gets a copied list of supported formats from the source hook.

Value: Empty for a plain authored-image source.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
