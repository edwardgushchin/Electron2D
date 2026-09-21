# EngineVersionInfo

Last updated: 2026-09-21

## Declaration

- Source: [`EngineVersionInfo.cs`](../../src/Core/Config/EngineVersionInfo.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class EngineVersionInfo`
- Domain: [Core](../domains/core.md)
- Component: [Engine runtime](../components/engine-runtime.md)
- Owner: [`Engine`](Engine.md)

## Responsibility and ownership

`EngineVersionInfo` is the immutable typed replacement for an untyped version dictionary. `Engine` creates one process-wide value from the loaded `Electron2D.dll` metadata and retains it for the process lifetime. Consumers neither own nor dispose it.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `Version AssemblyVersion { get; }` | Numeric assembly version, falling back to `0.0` only if metadata is absent |
| `string InformationalVersion { get; }` | Assembly informational version, or the numeric version string when absent |
| `string ToString()` | Returns `InformationalVersion` |

Construction is assembly-internal so callers cannot fabricate a descriptor that appears to describe the loaded engine.

## Lifecycle, invariants, and errors

The type is immutable after construction and has no failure or disposal state. Both properties are non-null. The same instance is returned by every `Engine.VersionInfo` read.

## Threading

All state is immutable, so reads are safe from any thread.

## Dependencies and interactions

The type depends only on `System.Version`. Assembly reflection and fallback selection belong to `Engine`, not this value object.

## Verification and known limitations

Executable checks compare `AssemblyVersion` with the loaded assembly, require a nonblank informational version, and verify `ToString()`. Commit hash, build timestamp, channel, authors, donors, and license data are not invented; those require an accepted build/attribution manifest.
