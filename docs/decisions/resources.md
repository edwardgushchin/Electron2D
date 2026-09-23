# Electron2D resources decisions

Last updated: 2026-09-23

This bounded log owns the complete architectural records for resources. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0013](#adr-0013), [0014](#adr-0014), [0039](#adr-0039).

<a id="adr-0013"></a>
## ADR 0013: Managed typed Resource contract

Last updated: 2026-09-23

### Status

Accepted. Lifetime policy clarified by [ADR 0014](resources.md#adr-0014). Local-scene behavior and the narrow Resources↔Scene dependency are implemented and refined by [ADR 0023](scene.md#adr-0023).

### Context

The reference Resource API inherits reference-counted lifetime, discovers stored properties through Variant metadata, duplicates nested object graphs, participates in a global path cache, and integrates with packed scenes, render resource IDs, loaders/savers, and editor-only path-ID tables.

Electron2D already commits to typed C#, managed deterministic lifetime, C# events, one Electron2D-owned assembly, and no fictional API for absent domains. The first resource layer must preserve useful data semantics without introducing Variant, reflection-driven copying, a second lifetime protocol, or empty integration stubs.

### Decision

`Resource` inherits `ElectronObject` directly. The managed runtime owns object memory, while inherited `IDisposable` owns deterministic logical cleanup and future native-handle release. A public manual reference-count API is not reproduced. This does not prohibit an asset manager from using internal reference-counted leases for deterministic native-asset retention, as clarified by ADR 0014.

Resource base state is explicit and typed. Registered paths use a process-wide ordinal weak-reference cache. Ordinary assignment enforces one live owner, takeover is explicit, and `SetPathCache` deliberately stores an unregistered loader-oriented value.

Signals are typed synchronous events. `Changed` is the content-change channel. The obsolete setup event is retained because it has executable ordering semantics and a future packed-scene consumer; setup invokes the event before the virtual hook and aggregates dual failure.

Duplication is opt-in for every derived type through two protected hooks: construct a fresh exact-type target and copy all stored typed state. A graph session registers targets before recursion, preserving aliases and cycles. Deep policy controls ordinary nested resources; the hook also receives a force-duplicate delegate, while direct assignment expresses a never-duplicate field. The derived type controls its collection containers. Path and scene IDs are never copied. Every created target is disposed if duplication fails.

`CopyFromResource` requires exact runtime types, preserves target identity, resets non-stored state, shallow-copies stored state, and coalesces notifications. It is non-transactional for arbitrary derived state and therefore emits one change even after a failed attempt.

Invalid scene IDs throw without mutation. Automatic replacement by a random ID is rejected because silently discarding caller input is unsuitable for the typed C# boundary; `GenerateSceneUniqueID` remains explicit.

Renderer RID, loader/saver cache modes, editor path-ID mapping, and automatic file-serialization discovery remain deferred to their missing domains. Packed-scene local duplication, root ownership, association and setup automation are implemented under ADR 0023.

### Concrete curve resources

`Curve` is the scalar y(x) resource; `PathCurve` is the spatial resource corresponding to Godot `Curve2D`. Their inherited managed Resource contract remains unchanged. The spatial name distinguishes its role without a redundant 2D suffix; it does not merge scalar and spatial APIs. Both types preserve their applicable point, handle/tangent, sample, bake, tessellation, event and property contracts. Dynamic indexed properties project to typed descriptors and explicit point methods; private packed Variant storage is not exposed. Exact-state copy hooks avoid setter reclamping and own independent point containers.

Following [ADR 0034](core-math.md#adr-0034), this first implementation corrects demonstrated defects instead of reproducing them: CleanDupes uses positive horizontal separation (the pinned signed comparison removes distinct ascending points); structural removal refreshes surviving Linear tangents; slopes use widened dy/dx instead of normalizing first, avoiding squared-length overflow for valid large coordinates; scalar bake coordinates divide before multiplying to avoid intermediate overflow inside a valid finite domain; nonconstant closed spatial segments are subdivided even when endpoint/midpoint chords vanish; closest-point projection treats zero-length segments as points; degenerate spatial poses use identity orientation. Existing source-specific notification timing, insertion tie order, resolution-one behavior and tessellation depth semantics are retained and tested. Canonical Mathf.Epsilon applies under ADR 0034.

Finite input checks and typed exceptions replace error logging/default returns at invalid query boundaries. Scalar limits must retain a positive finite span; explicit slopes are finite but derived duplicate-offset slopes may be IEEE nonfinite. Scalar interpolation otherwise retains float arithmetic, including overshoot/overflow. Path derived nonfinite geometry or length fails before cache publication. Public tessellation depth is bounded to 0..20, cache depth to ten, preserving ordinary defaults while making recursive resource work finite. Per-resource state operations are serialized, notifications run outside the state lock after commitment, and warm cached sampling/closest queries allocate no managed storage; array exports, edits and baking may allocate. Resource copying still requires caller coordination.

CPU curve resources do not require a native backend or editor. Scene paths/followers are a next executable Entity slice; CurveTexture/CurveXYZTexture require their own curve-change-to-texture update and typed floating-channel GPU sampling integration. Those consumers are implementation gaps, not aliases or compatibility stubs; Curve3D remains excluded by strict 2D scope. Verification is recorded in the [curve component](../components/curves.md).

### Consequences

- Derived resource authors must write small explicit copy hooks, but adding a field cannot silently produce an incomplete duplicate.
- Graph duplication supports repeated references and cycles without runtime reflection.
- Base resource behavior is independently testable before concrete assets exist.
- The weak path cache does not extend object lifetime and remains safe when disposal is omitted.
- Future serialization must define stored-property discovery and call existing hooks rather than changing current copy semantics accidentally.
- Packed scenes own automatic local duplication, local-scene association, setup timing and deterministic local-resource cleanup under ADR 0023.

`Resource.GetLocalScene()` and the narrow Resources↔Scene dependency are current behavior under ADR 0023.

### Rejected alternatives

- Public manual reference counting on every resource: duplicates managed lifetime and permits contradictory ownership state. Internal asset-manager leases remain available under ADR 0014.
- Reflection over all public properties: cannot distinguish stored, computed, identity, non-stored, or ownership-sensitive state safely.
- A Variant property bag: contradicts the typed API decision.
- Constructor-only duplication with no custom copy contract: silently loses derived fields.
- Empty RID, local-scene, and editor-ID methods: would claim unavailable domains and hide missing behavior.
- Vendoring a general cloning dependency: unnecessary for the explicit resource graph contract.

### Verification

Executable checks cover path ownership and races, event timing and failures, all copy policies, aliases, cycles, invalid factories, non-transactional copy notification, cleanup rollback, and disposal. Living class/component/domain documents contain the current API matrix and deferred boundaries.

<a id="adr-0014"></a>
## ADR 0014: Managed Resource lifetime and realtime allocation

Last updated: 2026-09-20

### Status

Accepted. Clarifies ADR 0003 and amends the Resource lifetime wording in ADR 0013.

### Context

Electron2D runs on .NET and must keep gameplay frame times predictable. Three separate concerns must not be conflated:

1. The runtime reclaims managed object memory through garbage collection.
2. `IDisposable` provides deterministic logical teardown and release of native handles, but does not free managed object memory.
3. A shared native-backed asset may need deterministic retention until its final consumer releases ownership.

Copying the reference engine's public `RefCounted` API would not remove Electron2D objects from the managed heap or prevent garbage collection. It would add a second caller-visible lifetime protocol, atomic counter traffic, cycle/error risks, and unclear interaction with `IDisposable`. Conversely, relying on garbage collection to release SDL, audio, renderer, or physics handles would make scarce native-resource lifetime nondeterministic.

### Decision

`ElectronObject` and `Resource` do not expose public manual reference counting. Managed object memory remains owned by the .NET runtime.

`IDisposable` remains the deterministic contract for logical shutdown, event/cache detachment, owned child cleanup, and release of native resources. Native handles are wrapped in `SafeHandle`-derived types so forgotten disposal has a narrowly scoped fallback; ordinary engine objects do not gain finalizers.

When the first concrete loader and native-backed asset establish real shared-ownership transitions, the Resources domain may add a resource manager with internal disposable leases. Acquiring a lease retains the manager-owned asset payload; disposing a lease releases that retention. Reaching zero leases makes the native payload eligible for deterministic release under the manager's cache policy. It does not force collection of the managed `Resource` wrapper and is not exposed as `Ref()`, `Unref()`, or a public counter on `Resource`.

No lease type or resource manager is implemented before that integration boundary. Their exact API, cache eviction rules, thread affinity, reload behavior, and failure semantics must be decided from the concrete loader/native backend rather than speculative scaffolding.

Steady-state frame, fixed-step physics, future rendering, input-dispatch, and audio-mixing hot paths must avoid managed allocations. Implementations prefer preallocated storage, value types, bounded reusable buffers, and pools where measurements show repeated allocation. Resource construction, loading, scene transitions, and tooling are not automatically allocation-free.

GC latency modes and no-GC regions are host-wide performance controls, not default engine semantics. They may be enabled only after allocation and frame-time profiling establishes a measurable need, a memory budget, recovery behavior, and target-platform validation. Electron2D does not claim hard real-time guarantees.

### Consequences

- `Resource` remains a direct `ElectronObject` subclass; its existing API and binary behavior do not change.
- Pure managed resource wrappers may outlive logical disposal until garbage collection; disposed instances remain unusable.
- Native payload release can become deterministic without pretending that managed memory was freed.
- Callers cannot corrupt object lifetime through mismatched public increment/decrement calls.
- A future asset manager owns lease counts and cache policy in one place instead of distributing counters across resources.
- Realtime performance is enforced by measured allocation budgets and hot-path tests, not by assuming either GC or manual reference counting is free.

### Rejected alternatives

- Public `RefCounted` inheritance for every resource: it does not replace managed garbage collection and creates two lifetime protocols.
- Using `Dispose` as if it freed managed memory: this is false and would produce misleading performance assumptions.
- Releasing native payloads only from garbage collection/finalization: timing is nondeterministic and can retain scarce resources too long.
- Implementing a speculative resource manager or lease API now: no loader, native-backed asset, cache policy, or ownership transition exists to validate it.
- Enabling a process-wide low-latency or no-GC mode by default: it can increase heap growth or fail under an incorrect allocation budget and must be measured per host and target.

### Verification boundary

This ADR changes architecture and documentation only; no runtime behavior is added. Existing Resource tests continue to verify deterministic logical disposal, weak path caching, graph duplication, and callback failure handling. Future resource-manager work must add lease-count, concurrent acquire/release, cache eviction, native-handle lifetime, allocation-budget, and frame-time tests before claiming completion.

### References

- [.NET `IDisposable` contract](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)
- [.NET garbage-collector latency modes](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/latency)
- [.NET no-GC regions](https://learn.microsoft.com/en-us/dotnet/api/system.gc.trystartnogcregion)

<a id="adr-0039"></a>
## ADR 0039: Managed image buffers and codec boundaries

Last updated: 2026-09-22

### Status

Accepted; managed buffers implemented, native codec integration partially executable.

### Context

The first concrete asset must provide useful CPU-side image behavior before textures, rendering, importing, or an editor exist. The current official 4.7.2 image contract includes raw storage in 47 uncompressed and GPU-block-compressed formats, mipmaps, pixel access and conversion, region composition, filtering, color processing, normal-map helpers, metrics, file codecs, and editor/GPU compression hooks.

Electron2D owns `Color`, `Vector2I`, `RectI`, `Resource`, typed duplication, blocking `FileAccess`, and an SDL host. Texture and GPU integration is in progress. The user selected SDL_image through SDL3-CS for encoded images, including the `SDL3-CS.Linux.Image` native package. Importing and general resource loading/saving remain unfinished. Image has five native loaders and PNG/JPEG saving; the full codec family and semantics remain unfinished. Reimplementing PNG, JPEG, WebP, SVG, DDS, KTX, and EXR would duplicate mature codec libraries.

### Decision

`Image` is the first concrete managed `Resource`. It owns a private portable little-endian byte buffer, dimensions, one `Image.Format`, and an optional complete mip chain.

- The public format enum preserves all 47 current raw format identities. Uncompressed formats support pixel operations. GPU-block-compressed formats support exact-size construction, copying, byte export, mip offsets, duplication, and disposal; operations needing decompression reject the state explicitly.
- Dimensions are positive for populated images, bounded by `MaxWidth`, `MaxHeight`, a 268,435,456-pixel ceiling, and the managed array limit. A default instance is the only empty zero-by-zero state.
- Integer `Rgba16I` alpha uses the full `0..65535` storage range; detection and blending normalize only the alpha arithmetic rather than treating an integer value of one as fully opaque. Byte counts are checked in wide arithmetic before allocation.
- `GetData` and every incoming byte-array path copy data. No caller receives mutable engine storage.
- Per-image reads and mutations are lock-serialized. Bulk mutations compute a complete replacement before publishing it; `SetPixel` updates one pixel under the same serialization to avoid a full-buffer allocation. Every successful mutation releases image locks, then emits one synchronous `Resource.Changed` notification. Observer failure propagates after state commitment. Operations reading other images use stable snapshots and are not multi-image transactions.
- The implemented backend-independent surface covers typed state queries, pixel reads/writes, base/mipmap offsets, complete mipmap generation/clear, conversion among every uncompressed format, crop/region, flips/rotations, five resize filters, fill, blit/blend with masks, alpha/channel/used-rectangle detection, brightness/contrast/saturation, alpha-edge repair and premultiplication, sRGB conversion, bump/normal/RGBE processing, typed `ImageMetrics`, independent Resource duplication, and the complete compression/source/ASTC enum family needed by the deferred encoder boundary.
- The dynamic `data` dictionary is replaced by `GetData`, `SetData`, dimensions, `PixelFormat`, and `HasMipmaps`. Error codes are replaced by typed C# exceptions. Vector overload pairs are ordinary overloads rather than suffixed method names. Metric dictionaries are replaced by `ImageMetrics`.
- `Image` is a managed CPU payload and therefore does not introduce the internal native-asset lease mechanism reserved by ADR 0014. Its buffer is released logically on disposal and reclaimed by the managed runtime.
- Raw compressed bytes do not imply codec or renderer support. No compression, decompression, load/save, texture, RID, import, or renderer method is added as a placeholder.
- Use SDL_image through the complete vendored SDL3-CS Image module for image decoding and supported encoding. Linux uses `SDL3-CS.Linux.Image` 3.4.6.9 under [ADR 0012](product.md#adr-0012). Native surfaces are temporary implementation details: public `Image` retains copied managed buffers, and `Texture`/`ImageTexture` retain the ownership contract recorded in [shader materials](../components/shader-materials.md). This approves the dependency; each codec, file/buffer path, limit, and claimed platform still requires executable integration and verification. The current profile loads PNG/JPEG/WebP/BMP/TGA to RGBA8 base pixels and saves PNG/JPEG; input/output is capped at 64 MiB and dimensions are checked before native decoding. PNG uses the bundled SDL core decoder to avoid confirmed grayscale scaling and RGB16 byte-order defects in SDL_image 3.4.6. This workaround adds no dependency. Original channel layouts, color/metadata interpretation and further codec variants remain Partial until their semantic audit and integration are complete.

### Audit coverage inventory

| Reference category | Electron2D status |
| --- | --- |
| `Image -> Resource -> RefCounted -> Object` inheritance | `Image -> Resource -> ElectronObject`; managed lifetime deliberately replaces public reference counting under ADR 0014 |
| Construction, dimensions, format, mipmap state, byte size, compression/empty/visibility state | Implemented through the constructor, three typed factories, and ten read-only properties |
| Raw data, mip offsets, copy, pixel/vector access | Implemented with copied `byte[]`, typed overloads, exact length validation, and exceptions |
| Fill, region, crop, flip, rotation, resize, mipmaps, conversion, blit/blend/masks | Implemented for uncompressed CPU data; compressed inputs fail explicitly where decoding is required |
| Alpha/channel/used bounds, color adjustment, alpha processing, color-space, bump/normal/RGBE, metrics | Implemented; dynamic metric dictionaries are replaced by `ImageMetrics` |
| Format/interpolation/alpha/channel/compression/source/ASTC enums and size constants | Implemented as seven nested enums plus `MaxWidth`/`MaxHeight`; encoder-oriented enums are stable types but do not imply an encoder |
| Inherited change signal, naming/path identity, duplication, descriptors, disposal | Implemented through `Resource`/`ElectronObject`; image duplication owns an independent buffer |
| Dynamic `data` property and integer error codes | Permanently adapted to typed state properties, copied arrays, and exceptions |
| Codec load/save, compression/decompression, textures/importing | Five native file/buffer loaders and PNG/JPEG saving execute; source-layout/color/metadata semantics and further formats remain pending. Texture sampling is partially integrated; further work is listed below. |

### Deferred coverage and exact implementation triggers

| Deferred official counterpart | Missing prerequisite and exact trigger | Required slice |
| --- | --- | --- |
| `load`, `load_from_file`, buffer loaders, and PNG/JPEG/WebP/EXR/DDS save methods | SDL_image and its Linux native package are selected and authorized above. The five selected loaders and PNG/JPEG saves now have executable file/buffer paths, preflight size checks and native tests. Supported-format discovery, remaining formats, complete channel/color/metadata adaptation and AOT delivery remain unfinished; another dependency approval is not required for this integration. SDL_image 3.4.6 WebP saving at quality 100 failed an exact opaque-color lossless round trip; WebP saving requires a verified native correction before being exposed. | The first image-codec vertical slice must add capability discovery, bounded/untrusted-input validation, all selected codec buffer and `FileAccess` paths, malformed/cancellation/failure tests, and native/AOT verification for each claimed target. Formats SDL_image cannot supply need their own implementation decision; unsupported formats remain absent, not success stubs. |
| `compress` and `compress_from_channels` | The editor executable plus the primary SDL3 GPU renderer must expose a concrete offline texture-compression toolchain and selected BC/ETC/BPTC/ASTC encoders. | The first approved texture-compression/import slice; not the initial renderer draw slice unless that slice explicitly includes authoring/import compression. |
| `decompress` for BC/ETC/BPTC/ASTC | A selected, portable CPU decompressor or renderer readback/conversion backend must exist with format-capability reporting. | The first vertical slice that consumes compressed image pixels on CPU. Raw upload-only texture work does not trigger CPU decompression. |
| `ImageTexture` conversion and renderer upload | The backend-neutral texture API and first SDL3 GPU renderer vertical slice must exist. | That renderer slice must define copy/ownership, format capability, mip upload, device loss, and fallback behavior. |
| Resource loading/import metadata, cache leases, and scene-file image persistence | A typed resource loader/saver/import format and a native-backed texture payload must establish real cache and ownership transitions. | The first concrete resource-manager plus native-backed texture slice under ADR 0014; managed `Image` alone does not trigger leases. |

### Consequences

- Procedural images, CPU processing, atlas preparation, tests, and future texture uploads have a complete typed foundation independent of the selected codec and renderer integrations.
- Raw data remains deterministic across supported CPU endianness because multi-byte fields use canonical little-endian encoding.
- Large processing operations allocate replacement buffers by contract and are not real-time frame hot paths. Pixel reads/writes themselves allocate no managed memory after warmup.
- A codec or renderer integration can consume the existing buffer contract without changing `Image` ownership or exposing mutable arrays.

### Rejected alternatives

- Add an image-codec dependency implicitly: rejected; SDL_image is now explicitly authorized and recorded above. Platform and codec claims still require their own verification.
- Vendor or hand-write all common codecs in this slice: rejected because it creates unnecessary parser/security maintenance and duplicates mature libraries.
- Expose only RGBA8: rejected because the accepted renderer/shader direction foreseeably needs HDR, integer, compact, and precompressed texture payloads.
- Pretend compression/load/save succeeds while doing nothing: rejected because it creates dependency fiction and corrupts asset expectations.
- Expose the backing array for performance: rejected because callers could bypass validation, locking, mipmap invariants, and change notification.

### Verification

The executable harness verifies empty/invalid states, all 25 uncompressed byte sizes and all 22 compressed identities, block/mipmap sizing, copy isolation, every processing family, clipping, interpolation, alpha/channel detection, typed metrics, Resource duplication, post-commit observer failure, and disposed-state rejection. Release XML generation and the repository identity scan remain part of the full gate.

The managed image checks are Linux/.NET 8 only and do not establish native codec or GPU behavior. The rendering integration separately verifies Texture/ImageTexture sampling and the SDL_image package probe, including PNG alpha, malformed input and repeat loading, on Linux x64. Native ImageCodecTests additionally verifies the five public loaders and PNG/JPEG savers, and rendering tests verify decoded PNG texture pixels. AOT, further codec APIs, memory-pressure, visual-quality and other target-platform verification remain pending.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0002: C# events for signals](product.md#adr-0002)
- [0013: Managed typed Resource contract](resources.md#adr-0013)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0020: Typed file access](core-data-io.md#adr-0020)
- [0021: Runtime and editor target platforms](product.md#adr-0021)
- [0024: Typed color values and portable quantization](core-math.md#adr-0024)
- [0035: Foreseeable public type-family completeness](core-math.md#adr-0035)
