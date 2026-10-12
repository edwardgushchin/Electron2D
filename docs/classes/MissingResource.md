# MissingResource

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Source:** [MissingResource.cs](../../src/Core/IO/MissingResource.cs).

## Description

Preserves an unavailable compiled resource factory and its bounded portable fields. Scalars keep copied encoded bytes and original byte order; resource identities remain borrowed and are relocated through the archive table. New fields require RecordingProperties; exact-schema replacements are allowed in either mode. OriginalClass selects the saved factory identity.

SetRecordedProperty/GetRecordedProperty use closed typed codecs/schemas, with no CLR property dispatch. Scalar descriptors expose copied encoded bytes; resource/array descriptors expose typed base-resource identities. A missing concrete subtype cannot satisfy an incompatible requested type. Duplication and scene-local construction preserve aliases/cycles through the existing Resource session. Disposal clears this producer without disposing manually borrowed children; file ownership follows Resource.

Version-2 strict/tolerant loading is described in [resource files](../components/resource-files.md#missing-resource-recovery). Version-1 known graphs load; unknown legacy fields and absent Node/signal schemas retain explicit prerequisites.

## Member summary

| Declaration | Contract |
| --- | --- |
| `public sealed class Electron2D.MissingResource` | Preserves bounded typed file fields when an original compiled resource factory is unavailable. |
| `public MissingResource()` | Creates an empty placeholder with recording disabled. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `public TValue GetRecordedProperty<TValue>(System.String name)` | Reads a copied typed scalar/array or borrowed resource from one original field. |
| `protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `public System.Void SetRecordedProperty<TValue>(System.String name, TValue value)` | Copies a typed value into an original asset field. |
| `public System.String OriginalClass { get; set; }` | Gets or sets the stable factory identity written when this resource is saved. |
| `public System.Boolean RecordingProperties { get; set; }` | Gets or selects whether previously absent recorded fields may be added. |

## Member descriptions

### MissingResource

`public sealed class Electron2D.MissingResource`

Preserves bounded typed file fields when an original compiled resource factory is unavailable.

Details : Fields are asset data, never CLR member dispatch. Scalar descriptors expose copied encoded bytes; typed access validates the original codec/schema. Materials and other nested resources remain borrowed.

### .ctor

`public MissingResource()`

Creates an empty placeholder with recording disabled.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### GetRecordedProperty

`public TValue GetRecordedProperty<TValue>(System.String name)`

Reads a copied typed scalar/array or borrowed resource from one original field.

Type TValue: Exact recorded schema with an available codec.

Parameter name: Existing original field name.

Returns : The decoded copied value or borrowed resource identity.

### RemapResourceReferences

`protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### SetRecordedProperty

`public System.Void SetRecordedProperty<TValue>(System.String name, TValue value)`

Copies a typed value into an original asset field.

Type TValue: A registered scalar codec, Resource or registered typed resource array.

Parameter name: Nonblank original file field name; no CLR property is invoked.

Parameter value: Value to copy; nested resource identities are borrowed.

Details : Adding requires RecordingProperties; replacement requires the same schema. Failure leaves prior fields intact.

### OriginalClass

`public System.String OriginalClass { get; set; }`

Gets or sets the stable factory identity written when this resource is saved.

Value : An ordinal ID; empty uses the ordinary MissingResource registration.

### RecordingProperties

`public System.Boolean RecordingProperties { get; set; }`

Gets or selects whether previously absent recorded fields may be added.

Value : False initially. Existing fields retain writable exact-schema storage in either mode.

## Verification

[MissingResourceTests](../../tests/Electron2D.Tests/MissingResourceTests.cs) exercises typed authoring, identity/copy/alias bounds, malformed input, Replace, external dependencies, threaded policy, version-1 known files and separate-process preservation/restoration. [MissingResourceNativeTests](../../tests/Electron2D.Tests/MissingResourceNativeTests.cs) checks fresh unknown/restored custom mesh pixels on GPU and compatibility, with 64 active and 64 idle complete frames at zero measured managed bytes. Native allocation totals, foreign targets and human acceptance remain separate.
