# EngineVersionInfo

Last updated: 2026-10-04

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Config/EngineVersionInfo.cs`](../../src/Core/Config/EngineVersionInfo.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class EngineVersionInfo`

> Describes the version embedded in the Electron2D assembly.

## Description

Describes the version embedded in the Electron2D assembly.

`EngineVersionInfo` is the immutable typed replacement for an untyped version dictionary. `Engine` creates one process-wide value from the loaded `Electron2D.dll` metadata and retains it for the process lifetime. Consumers neither own nor dispose it.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
EngineVersionInfo version = Engine.VersionInfo;
Console.WriteLine(version.String);
```

## Properties

| Member | Description |
| --- | --- |
| [`public Version AssemblyVersion { get; }`](#p-electron2d-engineversioninfo-assemblyversion) | Gets the numeric assembly version. |
| [`public string InformationalVersion { get; }`](#p-electron2d-engineversioninfo-informationalversion) | Gets the complete product version string. |

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

The type is immutable after construction and has no failure or disposal state. Both properties are non-null. The same instance is returned by every `Engine.VersionInfo` read.

## Threading

All state is immutable, so reads are safe from any thread.

## Dependencies and interactions

The type depends only on `System.Version`. Assembly reflection and fallback selection belong to `Engine`, not this value object.

## Verification and known limitations

Executable checks compare `AssemblyVersion` with the loaded assembly, require a nonblank informational version, and verify `ToString()`. Commit hash, build timestamp, channel, authors, donors, and license data are not invented; those require an accepted build/attribution manifest.
