# FramebufferCacheRD

Last updated: 2026-10-27

## Purpose

Reuses copied multipass framebuffers of the active GPU rendering session. See the [resource cache contract](../components/device-resource-caches.md) for attachment/pass keys, lifetime and verification limits.

## Type

`public sealed class FramebufferCacheRD : ElectronObject`

The constructor is private. Static operations address a retained cache owned by the current renderer device; local-device identities and caller disposal reject.

## Members

### GetCacheMultipass

`public static RID GetCacheMultipass(ReadOnlySpan<RID> textures, ReadOnlySpan<RDFramebufferPass> passes, int views)`

Returns an existing intact framebuffer with equal ordered attachment RIDs, view count and all pass fields, or creates it through the ordinary validated device path. Empty passes select the inferred single pass. Empty texture RID slots are permitted only when no pass accesses or preserves them. Views select one through 256 image layers. Keys copy caller data and retain no description objects. Explicit result FreeRID, attachment retirement and renderer shutdown evict entries. Calls require the active GPU session, its owner thread and no active command list. Invalid arguments/layouts reject; unavailable native format/sample profiles throw NotSupportedException.

## Implementation and verification

[Source](../../src/Servers/Rendering/Device/FramebufferCacheRD.cs) reuses existing multipass/MSAA/input/depth/multiview integration. [The public consumer](../../tests/DeviceResourceCaches.Consumer/DeviceResourceCacheChecks.cs) executes real graphics, resolves, layers and native canvas pixels; the component owns the exact qualification boundary.
