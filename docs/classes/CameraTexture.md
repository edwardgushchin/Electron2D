# CameraTexture

Last updated: 2026-10-27

`public sealed class Electron2D.CameraTexture`

Inherits: `Electron2D.Texture`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

An ordinary Texture borrowing a registered source and image plane. Its own rendering RID remains stable while source frames change. Logical dimensions describe the full frame even for a smaller chroma plane; GetImage returns a coherent independent copy preserving actual sampling dimensions. Missing source IDs yield no pixels and zero size. CameraFeedID/WhichFeed are stored process-local selectors. CameraIsActive is a runtime-only convenience switch, so loading/copying a resource never activates a camera. Worker construction/storage uses synchronized view binding. Disposal detaches observers without owning source activity.

## Example

The source/format comments below identify any required caller context.

```csharp
// source is a live registered CameraFeed.
using var view = new CameraTexture { CameraFeedID = source.GetID() };
using var snapshot = view.GetImage(); // null until a first plane exists.
```

## API reference

| Declaration | Contract |
| --- | --- |
| [`public CameraTexture()`](#member-bdc41e1ecec7) | Creates an empty view with feed ID zero and the first image plane. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-3eb8937a041d) | Copies stored selectors and binds the duplicate to the same borrowed source; it does not activate capture. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-ba8ac5963a7e) | Creates an independent view object for ordinary resource duplication. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-4aa35910518c) | Retires this object's observers/pixel/native state without disposing borrowed sources. |
| [`public override System.Int32 GetHeight()`](#member-de8b1a18f49c) | Returns the full source-frame height in pixels, or zero for an absent source. |
| [`public override Electron2D.Image GetImage()`](#member-3aeb0f8b80aa) | Returns an independent coherent copy of the selected plane, or null when absent. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-0098d917a85a) | Extends the inherited typed property metadata with this type's current configuration. |
| [`public override Electron2D.Vector2 GetSize()`](#member-8975d1122a8f) | Returns one coherent sample of the full source-frame dimensions, or zero for an absent source. |
| [`public override System.Int32 GetWidth()`](#member-53d1aa104a0e) | Returns the full source-frame width in pixels, or zero for an absent source. |
| [`public System.Int32 CameraFeedID { get; set; }`](#member-e13890f64330) | Gets or selects a process-local feed ID; zero selects no source. |
| [`public System.Boolean CameraIsActive { get; set; }`](#member-7fa36ad501fd) | Gets or changes the selected live feed's activation on its owner. |
| [`public System.Boolean HasAlpha { get;  }`](#member-407a0030a93b) | Reports false for the camera texture role. |
| [`public Electron2D.CameraServer.FeedImage WhichFeed { get; set; }`](#member-12266a6bc06d) | Gets or selects the source image plane. |

## Member descriptions

<a id="member-bdc41e1ecec7"></a>
### `public CameraTexture()`

Creates an empty view with feed ID zero and the first image plane.

<a id="member-3eb8937a041d"></a>
### `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Copies stored selectors and binds the duplicate to the same borrowed source; it does not activate capture.

<a id="member-ba8ac5963a7e"></a>
### `protected override Electron2D.Resource CreateDuplicateInstance()`

Creates an independent view object for ordinary resource duplication.

<a id="member-4aa35910518c"></a>
### `protected override System.Void Dispose(System.Boolean disposing)`

Retires this object's observers/pixel/native state without disposing borrowed sources.

<a id="member-de8b1a18f49c"></a>
### `public override System.Int32 GetHeight()`

Returns the full source-frame height in pixels, or zero for an absent source.

<a id="member-3aeb0f8b80aa"></a>
### `public override Electron2D.Image GetImage()`

Returns an independent coherent copy of the selected plane, or null when absent.

<a id="member-0098d917a85a"></a>
### `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Extends the inherited typed property metadata with this type's current configuration.

<a id="member-8975d1122a8f"></a>
### `public override Electron2D.Vector2 GetSize()`

Returns one coherent sample of the full source-frame dimensions, or zero for an absent source.

<a id="member-53d1aa104a0e"></a>
### `public override System.Int32 GetWidth()`

Returns the full source-frame width in pixels, or zero for an absent source.

<a id="member-e13890f64330"></a>
### `public System.Int32 CameraFeedID { get; set; }`

Gets or selects a process-local feed ID; zero selects no source.

Value: Zero initially. Unknown positive IDs remain empty until registered.

<a id="member-7fa36ad501fd"></a>
### `public System.Boolean CameraIsActive { get; set; }`

Gets or changes the selected live feed's activation on its owner.

Value: False when no registered feed exists. This convenience property is not stored.

<a id="member-407a0030a93b"></a>
### `public System.Boolean HasAlpha { get;  }`

Reports false for the camera texture role.

<a id="member-12266a6bc06d"></a>
### `public Electron2D.CameraServer.FeedImage WhichFeed { get; set; }`

Gets or selects the source image plane.

Value: The first RGB/Y/packed plane initially; CBCRImage selects separate chroma.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
