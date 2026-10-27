# UniformSetCacheRD

Last updated: 2026-10-27

## Purpose

Reuses complete shader descriptor sets of the active GPU rendering session. See the [resource cache contract](../components/device-resource-caches.md) for copied keys, lifetime, validation and measurement limits.

## Type

`public sealed class UniformSetCacheRD : ElectronObject`

The constructor is private. Static operations address a retained cache owned by the current renderer device. Callers cannot dispose that retained object or use local-device identities.

## Members

### GetCache

`public static RID GetCache(RID shader, uint set, ReadOnlySpan<RDUniform> uniforms)`

Returns an existing intact set with the same shader, set number and ordered binding contents, or validates and creates one. Descriptions are copied on a miss. Later edits/disposal do not rewrite previous sets. Explicit result FreeRID, dependency retirement and renderer shutdown evict entries. Calls require the active GPU session and its owner thread. Invalid layouts or identities throw ArgumentException; absent/inaccessible sessions throw InvalidOperationException.

## Implementation and verification

[Source](../../src/Servers/Rendering/Device/UniformSetCacheRD.cs) uses complete hash-bucket key comparisons and the ordinary device uniform-set creation/validation path. [The public consumer](../../tests/DeviceResourceCaches.Consumer/DeviceResourceCacheChecks.cs) executes actual compute, reuse and cleanup; the component owns the exact qualification boundary.
