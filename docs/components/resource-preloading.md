# Named scene resources

Last updated: 2026-10-14

## Responsibility and structure

[ResourcePreloader](../classes/ResourcePreloader.md) supplies the named scene asset table in `src/Scene/Resources/ResourcePreloader.cs`. It uses the .NET ordered dictionary for insertion order/lookup and existing Unicode scalar comparison for canonical capture. It depends on Node mutation/lifetime, Resource identity/lifetime, typed descriptors and existing PackedScene/resource archive graph machinery. It adds no package, universal value store, loading task or public backend handle.

## Runtime and persistence

Duplicate Add calls retain the first object and assign the new entry the first unoccupied numeric suffix from 2. Rename logically frees the old name before that search, preserves the resource identity and moves the entry to the end. Get/Remove/Rename missing names use typed errors; Has is observational. Clearing/removing the holder does not dispose borrowed assets.

Each stored entry is an exact Resource descriptor with a reserved encoded label. Canonical UTF8 hexadecimal protects empty names, NUL and separators from field-name transport; invalid Unicode/codecs/schema labels reject. Capture emits Unicode scalar name order, matching canonical storage; in-memory/file reconstruction therefore may sort public list order. No dynamic object values or reflected member lookup occur.

PackedScene reconstruction and the archive writer/reader recover only this owner/family/schema. The built-in ResourceFileTypes factory permits new-process loading without game-specific registration. Existing graph scopes preserve shared aliases, cycles, scene-local resource copies/association and loaded-file ownership leases. Instances can outlive the loaded PackedScene; the final owner releases decoded assets. Node/capture mutation guards remain active and user callbacks use ordinary public lookup.

## Verification scope

ResourcePreloaderTests exercises suffix gaps/collisions, rename/self/excluded-source ordering, case, independent name snapshots, empty/NUL/Unicode fields, unknown/null/disposed input, borrowed disposal, 128 prepared Get/Has cycles, scene-local/nonlocal aliases, canonical schema validation, capture rejection and empty/ordinary/compressed/big-endian files with loaded graph retirement.

ResourcePreloaderNativeTests authors and saves a Window scene, destroys its source, loads a new hierarchy, obtains a texture from the restored named holder in Ready and checks actual blue pixels from the window render target. GPU and compatibility profiles have separate host gates. The Linux public-package/fresh-process checkpoint below records exercised authoring and rendering. Other platforms and owner acceptance retain separate gates. Inherited Node gaps remain recorded on Node's coverage page.

## Public-package Linux checkpoint (2026-10-14)

PreloaderConsumer uses only public Electron2D APIs and an independent managed package/private restore cache. One process authors a compressed big-endian Window scene with texture and local Gradient aliases; another fresh process loads and inspects it after the producer exits. It confirms empty/NUL-capable typed labels, shared/local identity, PackedScene disposal independence and final graph retirement. Ordinary and NativeAOT publications both execute fresh inspection and actual GPU/compatibility rendering from that file with LD_LIBRARY_PATH unset; blue-pixel snapshots are captured through the public viewport texture. JSON reflection is disabled. Evidence is retained in bin/preloader-validation/2026-10-14. This is Linux x64 current-backend execution, separate from other platforms, native allocator, general editor and human/owner acceptance.
