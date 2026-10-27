# Retained device resource caches

Last updated: 2026-10-27

[ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0095](../decisions/singleton-services.md#adr-0095) own this executing cache profile. [UniformSetCacheRD](../classes/UniformSetCacheRD.md) and [FramebufferCacheRD](../classes/FramebufferCacheRD.md) provide static access to private retained objects of the active GPU [RenderingDevice](../classes/RenderingDevice.md). They do not open a renderer or select a local device. Calls without an active GPU session, including Compatibility rendering, throw InvalidOperationException. Owner-thread, native submission and readback-access guards remain the device's existing rules.

## Keys and use

`UniformSetCacheRD.GetCache(shader, set, uniforms)` reuses a complete set keyed by shader, set number and ordered binding contents: binding number, uniform category and ordered resource RIDs. New description objects with the same contents reuse the RID. Descriptions are validated and copied on a miss; their later mutation or disposal does not modify cached keys or the executing set. Invalid/null/incomplete descriptions, wrong-kind, foreign-device and retired identities reject through the ordinary device validation. A hit is usable during compute recording, consistently with UniformSetCreate.

`FramebufferCacheRD.GetCacheMultipass(textures, passes, views)` keys ordered texture RIDs, view count and every pass field: color, depth, resolve, input and preservation attachments. Empty passes request the ordinary inferred single pass. Empty texture RID slots are retained in keys and permitted only when no pass accesses or preserves them. Views select one through 256 ordered image layers. Cache hits and misses require no active command list. Existing format, sample, dimension, attachment, pass, resolve and input-feedback rules remain intact. Multipass, sparse outputs, depth/stencil, MSAA resolve and multiview reuse their executing device paths.

Hash buckets perform complete key comparisons, so a hash match never substitutes another description. Caller arrays and descriptions are not retained. First use and each new key allocate copied managed metadata and ordinary device resources; prepared hits allocate no managed memory. Equivalent framebuffer layouts share a device format ID, so free/recreate cycles do not retain another identical layout each time. Format IDs remain valid independently of individual framebuffer identities and never cross devices.

## Lifetime

Cache results are cache-managed borrowed RIDs. Explicit FreeRID is allowed and evicts the key. Freeing a shader or bound resource removes dependent cached uniform sets; freeing an attachment removes dependent cached framebuffers. Unrelated results remain alive. Ordinary noncached borrowers keep their existing invalidation and explicit-free contract.

Renderer texture and instance-buffer retirement sweeps remove expired cached borrowers at their existing safe device boundary. No native allocation is released through a cache while queued work still uses it. Cache eviction removes only the managed set/framebuffer entry; underlying resources retain their original owner. Retirement scans run only after an actual dependency retirement, rather than on every unchanged frame. Explicit cold frees may scan live cache metadata.

The device owns both retained cache objects and closes them during renderer shutdown. Reopening creates independent cache state; old shader, set, texture and framebuffer RIDs reject. Cache objects have private constructors, no public Instance accessor and reject caller disposal. Resource caches are runtime acceleration services and are not authored resources or persistent scene state.

## Verification

The public-only [DeviceResourceCaches.Consumer](../../tests/DeviceResourceCaches.Consumer/DeviceResourceCacheChecks.cs) executes compute writes, multipass graphics, native MSAA resolve, multiview layers and TextureRD canvas pixels. It checks copied descriptions, edits/restoration, explicit/dependency frees, shader and session retirement, foreign devices, invalid inputs, owner-thread and active-list guards. Repeated framebuffer eviction/recreation checks stable format identity. Prepared measurements cover 512 cache pairs and 64 complete native frames.

Actual CLR, strict NativeAOT, regression and allocation claims require the corresponding recorded run receipts. Native allocator totals, large cache/free workloads, foreign platforms and owner acceptance remain separate verification gates. Compatibility has an explicit unavailable-GPU-service contract; it does not execute these GPU caches.
