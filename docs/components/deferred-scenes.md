# Deferred scene instances

Last updated: 2026-10-18

## Selection

| Candidate | User API and dependent slices | Current backend feasibility |
| --- | --- | --- |
| Deferred scenes | Six reference declarations connect Node flags, placeholder inspection and explicit scene construction. Enables lazy actors and levels after linked templates. | Selected: existing typed archives, scene ownership and runtime factories supply the complete path. |
| Node duplication | Eight declarations cover live subtree copying and scripts/signals/groups policies. | Live Script identity and ownership still need the ADR 0091 contract; a stored scene snapshot cannot implement the Scripts flag. |
| Root inheritance | Derived authored scenes and base-state inspection. | Needs a distinct base/override and deletion-record contract rather than interpreting an ordinary runtime snapshot as an inherited root. |

## Authoring and inspection

`Node.SetSceneInstanceLoadPlaceholder(true)` changes future capture of an actual scene instance with a nonempty scene file path. It does not replace the live node or emit EditorStateChanged. Packing retains that path instead of a PackedScene reference. Loading creates an `InstancePlaceholder : Node` without opening the deferred file or constructing that scene's types. Skipped inherited records do not require registration of their custom node factories. This Node has no Entity transform. Ordinary Node fields apply directly to it; fields unavailable on this role remain pending.

`GetInstancePath()` returns the late lookup path. `GetStoredPropertyNames()` returns pending names in application order; `GetStoredValue<T>(name)` returns a compatible typed snapshot, with copied arrays. Node references are queried as relative string paths, including null references. Unknown scalar schemas retain their encoded payload until their compiled codec is available. Existing typed property descriptors allow editing pending values while preserving their original schema and order. There is no universal value container or dynamic method invocation.

Inherited records whose target does not exist under the placeholder are skipped, as are records under missing parents. Ordinary locally added children with a surviving parent remain ordinary placeholder children. Skipped inherited-child overrides do not become deferred replay instructions. Strongly typed authored connections must accept the real placeholder type; a deferred object cannot stand in for an unconstructed subtype.

SceneState exposes the late path and placeholder marker, an empty node type for a deferred root and typed raw field queries. Ordinary and reused linked records keep their established meanings. Empty manually authored placeholders can be captured without inventing a path.

## Explicit creation

`CreateInstance(replace: false, customScene: null)` requires an attached nonroot placeholder on its scene owner. Detached or parentless nodes return null. A supplied PackedScene is borrowed; otherwise the path uses the normal loader. The template completes its usual scene notification before the root's pending fields are restored. Node references resolve inside the new hierarchy first, then relative to the placeholder. Name and multiplayer authority follow the placeholder; ordinary children remain on it.

Creation adds a sibling at the placeholder's old position. With `replace: true`, the placeholder leaves its parent and joins the original tree's deletion queue. Loading, factory and property failures preserve the placeholder. Lifecycle callback failures after a committed structural change propagate with the completed progress retained; cleanup failures aggregate. Capture/instantiation barriers and owner checks guard reentry and unfinished hierarchies.

## Files and ownership

Archive version 6 adds late paths and bounded self-describing typed fields, including node paths and resource references. Readers retain versions 1–5. Metadata discovery consumes the extra field table without calling deferred factories. Compression and numeric payload endianness preserve each field's own scalar encoding. Late paths are ordinary lookup strings: BundleResources and dependency discovery do not turn them into eager resource edges or embed the deferred file.

Every placeholder, including one supplied by a nested bundled template, retains the acquired decoded graph independently of the parent template and runtime hierarchy. A created root acquires the required file leases and scene-local graph copies, preserving resource aliases, cycles and nulls. Default loading gives engine-created templates shared internal ownership across instances, captures and exported SceneState views; their last lease retires the template. An existing caller-owned cached template remains borrowed. The weak resource path cache remains non-owning, and no public reference-counting protocol is added.

## Verification status

Ten focused profiles pass: deferred managed/fresh-file, GPU, Compatibility, linked scenes, resource archives, authored connections, missing resources/textures, foldable scenes and RESET save. Native rendering checks the before/after pixels, replacement retirement, actual body motion/point queries and 64 prepared render frames with zero owner-thread managed bytes. The software Vulkan ICD and loaded process/descendant modules are checked; no hardware driver is observed. The public-only portability consumer passes fresh-process authoring/loading under JIT and strict NativeAOT, including typed pending resource reads and final default-template retirement. The complete Linux regression passes in 1342.87 seconds against the frozen runtime sources; Release, compiled API coverage, formatting and wiki generation/check gates pass. Evidence is retained in ignored bin/deferred-scene-validation/2026-10-18. No native-allocator, hardware, foreign-platform or human acceptance is claimed.

## Decisions

[ADR 0023](../decisions/scene.md#adr-0023) owns deferred instance semantics. [ADR 0001](../decisions/product.md#adr-0001) and [ADR 0005](../decisions/core-object-runtime.md#adr-0005) govern typed values and metadata. [ADRs 0013/0014](../decisions/resources.md) own archive graphs and internal deterministic leases.
