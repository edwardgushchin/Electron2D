# ViewportTexture

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.ViewportTexture` · **Source:** [ViewportTexture.cs](../../src/Scene/Resources/ViewportTexture.cs).

**Inherits:** [Texture](Texture.md).

## Description

A scene-local live view of a viewport's completed native canvas image.

A scene-local weak borrowed view with stable resource RID independent of native reallocation. Viewport.GetTexture supplies a cached direct view. Authored ViewportPath resolves relative to GetLocalScene during reconstruction; separate PackedScene instances bind their own copied targets. Direct view duplication computes a scene-relative path while ordinary unattached duplication can retain its borrowed direct target. Missing required local bindings fail explicitly. Viewport disposal leaves an unresolved, live texture with zero dimensions and null image. GetImage returns a caller-owned completed RGBA8 native readback, or null when no active completed target exists; it allocates and synchronizes explicitly. Drawing/material sampling uses native images without CapturePixels or per-frame upload/readback. PixelFormat is RGBA8, HasAlpha metadata false, HasMipmaps false and MipmapCount zero. Stored RGBA can contain alpha; premultiplied blending is available through CanvasItemMaterial. Size notifications are delivered to borrowed texture consumers before public viewport observers, and exceptions cannot skip subsequent delivery. Disposing the texture detaches observers and releases its logical RID without owning the viewport.

Native targets belong to RenderingServer, which reuses the active device. Completed/write image pairs avoid writable-attachment feedback; producers update before consumers and cycles observe available completed data. Scene input is isolated by viewport, with explicit PushInput removing that viewport final transform. Rendering/mutation/query uses the attached owner thread. No new clock, vendor patch or backend dependency is introduced. [The offscreen component](../components/canvas-rendering.md#offscreen-canvas-targets) records actual behavior and exact gaps.

## Example

This snippet requires the indicated live window/view and an authored child hierarchy; [SubViewportTests](../../tests/Electron2D.Tests/SubViewportTests.cs) exercises this public workflow.

```csharp
ViewportTexture texture = view.GetTexture();
RID identity = texture.GetRID();
using Image? image = texture.GetImage(); // Explicit cold readback after a frame.
using var authored = new ViewportTexture { ViewportPath = "preview" };
// Scene-local setup resolves authored from its PackedScene root.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public ViewportTexture()` | Creates an unresolved scene-local viewport texture. |

## Constructor Descriptions

<a id="member-3497fbee2db9"></a>
### .ctor

`public ViewportTexture()`

Creates an unresolved scene-local viewport texture.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean HasAlpha { get;  }` | Gets whether the original format contains an alpha channel. |
| `public System.Boolean HasMipmaps { get;  }` | Gets whether the texture contains a mipmap chain. |
| `public System.Int32 MipmapCount { get;  }` | Gets the number of mip levels after the base image. |
| `public Electron2D.Image.Format PixelFormat { get;  }` | Gets the original image's pixel format. |
| `public System.String ViewportPath { get; set; }` | Gets or sets the viewport path relative to the resource's local scene root. |

## Property Descriptions

<a id="member-b86e95327094"></a>
### HasAlpha

`public System.Boolean HasAlpha { get;  }`

Gets whether the original format contains an alpha channel.

Value: False for formats without alpha or for an uninitialized texture.

<a id="member-142e59e17f47"></a>
### HasMipmaps

`public System.Boolean HasMipmaps { get;  }`

Gets whether the texture contains a mipmap chain.

Value: False for an uninitialized texture.

<a id="member-70d92f3df009"></a>
### MipmapCount

`public System.Int32 MipmapCount { get;  }`

Gets the number of mip levels after the base image.

Value: Zero when no mipmaps are stored.

<a id="member-13555f22f214"></a>
### PixelFormat

`public Electron2D.Image.Format PixelFormat { get;  }`

Gets the original image's pixel format.

Value: L8 for an uninitialized texture.

<a id="member-d41e7d001442"></a>
### ViewportPath

`public System.String ViewportPath { get; set; }`

Gets or sets the viewport path relative to the resource's local scene root.

Value: Empty initially.

Remarks: An equal write is ignored. A committed different path releases the old binding and resolves within the local scene when available. Missing or incompatible paths fail local setup explicitly.

System.ArgumentNullException: The path is null.

System.ObjectDisposedException: The resource is disposed.

System.InvalidOperationException: A bound scene is mutated off-owner.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `public override System.Int32 GetHeight()` | Gets the logical height used for drawing. |
| `public override Electron2D.Image GetImage()` | Copies the completed native image, or returns null before rendering or after target loss. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource lifecycle for this concrete type. |
| `public override System.Int32 GetWidth()` | Gets the logical width used for drawing. |
| `protected override System.Void OnSetupLocalToScene()` | Customizes a newly duplicated scene-local resource. |

## Method Descriptions

<a id="member-f5a31a57ac2a"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-bdb219113a0a"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-5d632b69743f"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-ba1eb094bcd2"></a>
### GetHeight

`public override System.Int32 GetHeight()`

Gets the logical height used for drawing.

Returns: Height in pixels, as defined by the concrete texture.

<a id="member-bee8dc6b91f6"></a>
### GetImage

`public override Electron2D.Image GetImage()`

Copies the completed native image, or returns null before rendering or after target loss.

Returns: A caller-owned RGBA8 image; null when no active completed target exists.

Remarks: This explicit cold readback synchronizes native execution and allocates. It is not part of ordinary drawing.

System.ObjectDisposedException: The texture is disposed.

System.InvalidOperationException: Readback is off-owner or occurs during native submission.

<a id="member-738a71ac1e6e"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-650e3ee4de2e"></a>
### GetWidth

`public override System.Int32 GetWidth()`

Gets the logical width used for drawing.

Returns: Width in pixels, as defined by the concrete texture.

<a id="member-2433fafd6968"></a>
### OnSetupLocalToScene

`protected override System.Void OnSetupLocalToScene()`

Customizes a newly duplicated scene-local resource.

Remarks: The owning scene is available through Electron2D.Resource.GetLocalScene while this callback runs.


## Verification and dependencies

SubViewportTests verifies guards/defaults/clamps, stable RID/texture identity, observer failures, scene-local copies, input/transform isolation, owner checks and nonunit AA recording invalidation. Two Linux Wayland GPU/two compatibility Engine.Run cycles check ten pixel phases; additional hosts check dependencies, external layers, feedback, stretch, hidden root and detached cleanup. Warm 64 active and 64 idle frames measure zero owner managed bytes separately from explicit readback. GPU checks uniform-only visibility/live sampling and server RID readback. Native/driver allocation totals, other platforms/browser, human acceptance, file/editor/container and multiview/layered storage remain unverified or absent. [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0008](../decisions/scene.md#adr-0008) define rendering and node roles.
