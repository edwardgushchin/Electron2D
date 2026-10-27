# FramebufferCacheRD API coverage

Last updated: 2026-10-27

Godot source: [doc/classes/FramebufferCacheRD.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FramebufferCacheRD.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: [`public sealed class Electron2D.FramebufferCacheRD`](../../classes/FramebufferCacheRD.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class FramebufferCacheRD`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FramebufferCacheRD.xml) | [`public sealed class Electron2D.FramebufferCacheRD`](../../classes/FramebufferCacheRD.md) | Implemented | ADRs 0028/0095: complete copied-key resource caches execute on the active retained GPU device, with content comparison, compute/graphics/multipass/MSAA/multiview consumers, automatic dependency retirement, explicit-free eviction and independent session lifetime. Local-device, absent/Compatibility, owner/list and invalid-description guards reject. Prepared cache pairs and native frames require their recorded managed-allocation receipts; native heap, large-cache/free workloads and foreign targets remain separate gates. See docs/components/device-resource-caches.md. |
| [`method get_cache_multipass(RID[] textures, RDFramebufferPass[] passes, int views) -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FramebufferCacheRD.xml) | [`public static Electron2D.RID GetCacheMultipass(System.ReadOnlySpan<Electron2D.RID> textures, System.ReadOnlySpan<Electron2D.RDFramebufferPass> passes, System.Int32 views)`](../../classes/FramebufferCacheRD.md) | Implemented | ADRs 0028/0095: complete copied-key resource caches execute on the active retained GPU device, with content comparison, compute/graphics/multipass/MSAA/multiview consumers, automatic dependency retirement, explicit-free eviction and independent session lifetime. Local-device, absent/Compatibility, owner/list and invalid-description guards reject. Prepared cache pairs and native frames require their recorded managed-allocation receipts; native heap, large-cache/free workloads and foreign targets remain separate gates. See docs/components/device-resource-caches.md. |
