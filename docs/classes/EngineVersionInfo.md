# EngineVersionInfo

Last updated: 2026-11-14

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Config/EngineVersionInfo.cs`](../../src/Core/Config/EngineVersionInfo.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class EngineVersionInfo`

> Describes the version embedded in the Electron2D assembly.

## Description

Describes the version embedded in the Electron2D assembly.

`EngineVersionInfo` is the immutable typed replacement for an untyped version dictionary. `Engine` creates one process-wide value from the loaded `Electron2D.dll` metadata and retains it for the process lifetime. Consumers neither own nor dispose it.

[ADR 0096](../decisions/versioning.md#adr-0096) defines the product version policy. The shared build version is `0.2.0-alpha`. The descriptor always reflects the loaded engine and may differ between builds.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
EngineVersionInfo version = Engine.VersionInfo;
Console.WriteLine(version.InformationalVersion);
```

## Properties

| Member | Description |
| --- | --- |
| [`public Version AssemblyVersion { get; }`](#p-electron2d-engineversioninfo-assemblyversion) | Gets the numeric assembly version. |
| [`public string InformationalVersion { get; }`](#p-electron2d-engineversioninfo-informationalversion) | Gets the complete product version string. |
| `public int Major { get; }`, `public int Minor { get; }`, `public int Patch { get; }` | Numeric product version components. If product metadata is missing or its numeric part cannot be parsed, these come from the assembly version, with zero for an unspecified patch. |
| `public long Hex { get; }` | Arithmetic sum `Major * 65536 + Minor * 256 + Patch` in signed 64-bit precision. Each component contributes its full value, including at and above 256. For example, `1.256.256` yields `131328` (`0x20100`). |
| `public string Status { get; }` | The full product prerelease label, `stable` for a product version without a prerelease label, or `unknown` when usable product version metadata is absent. |
| `public string Build { get; }` | Assembly build configuration, such as `Release`, or `unknown` if missing or blank. |
| `public string SourceRevision { get; }` | SDK source revision recorded independently of informational version, or `unknown` if missing or blank. |
| `public long SourceTimestamp { get; }` | Unix seconds supplied for the source revision. Zero denotes an unknown revision or a timestamp that is absent, invalid, negative, or beyond the signed 64-bit range. |

## Methods

| Member | Description |
| --- | --- |
| [`public override string ToString()`](#m-electron2d-engineversioninfo-tostring) | Returns the complete product version string. |

## Property Descriptions

<a id="p-electron2d-engineversioninfo-assemblyversion"></a>
### `public Version AssemblyVersion { get; }`

Gets the numeric assembly version.

**Value:** The immutable version reported by the loaded Electron2D assembly.

<a id="p-electron2d-engineversioninfo-informationalversion"></a>
### `public string InformationalVersion { get; }`

Gets the complete product version string.

**Value:** The assembly informational version when one is present; otherwise the numeric
[`EngineVersionInfo.AssemblyVersion`](EngineVersionInfo.md#p-electron2d-engineversioninfo-assemblyversion) converted to a string.

## Method Descriptions

<a id="m-electron2d-engineversioninfo-tostring"></a>
### `public override string ToString()`

Returns the complete product version string.

**Returns:** [`EngineVersionInfo.InformationalVersion`](EngineVersionInfo.md#p-electron2d-engineversioninfo-informationalversion).

## Lifecycle, invariants, and errors

The type is immutable after construction. The same instance is returned by every `Engine.VersionInfo` read. Missing numeric assembly version metadata uses `0.0`. An absent informational version attribute falls back to `AssemblyVersion.ToString()`, while a present attribute is returned verbatim. Optional metadata failures use the values in the property table.

## Threading

All state is immutable, so reads are safe from any thread.

## Dependencies and interactions

The descriptor is initialized from the assembly that defines `Engine`. Runtime reads use retained values and require no filesystem, source directory or assembly file path.

## Verification and known limitations

[`EngineMetadata.Consumer`](../../tests/EngineMetadata.Consumer/Program.cs) accepts a version expectation document with independently supplied values. It checks architecture, every version property, the display string and reference identity across parallel reads. Architecture execution and package qualification on each target remain subject to [ADR 0021](../decisions/product.md#adr-0021).
