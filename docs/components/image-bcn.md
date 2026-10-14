# BC and DDS image integration

Last updated: 2026-10-14

Image.Compress/CompressFromChannels, Decompress, LoadDDSFromBuffer, SaveDDS and SaveDDSToBuffer provide an executable CPU-to-texture slice. S3TC selects opaque BC1, alpha BC3, red BC4 or RG BC5; BPTC selects BC7 or signed/unsigned BC6 for finite HDR RGB. All stored mips retain their dimensions and order. Compression is lossy and runs during authoring, never inside retained drawing.

DDS retains exact supported raw/BC bytes. The container is bounded to 64 MiB; dimensions, complete mip count, block linear size and exact source payload are validated before the mature reader or engine layout adapter runs. Raw layouts are tightly packed. Legacy channel masks, packed colors, luminance/alpha, numeric floating/unorm FourCC and applicable DX10 raw/HDR/BC layouts are adapted to portable Image storage. Arrays/cubes select their first image and volumes select the first slice of every mip, after validating the whole payload. This Image operation does not construct a TextureArray/Cubemap/TextureVolume resource from a multidimensional file. Saving uses the existing atomic FileAccess replacement and never recompresses blocks. Unsupported DDS storage roles fail explicitly.

Source bytes and conservative expanded RGBA-float codec pixels are each limited to 64 MiB before codec allocation. Failed encoding/decoding/container validation preserves the image. Successful publication emits one Changed outside image locks; observer failure retains the committed source. Decompress on uncompressed data is silent.

TexturePixels preserves original compressed source snapshots and prepares decoded pixels for CPU opacity and Compatibility. GPUTexture selects native BC storage through actual device capability queries, retaining compressed transfer/VRAM storage and full mip/layer offsets; unsupported native BC roles use decoded upload. RA-as-RG uses decoded upload to preserve its semantic channels. RenderingDevice exports report the actual native format and lease the existing allocation. ImageTexture/TextureArray Update and ordinary source lifetime rules are unchanged.

Compatibility draws BC1–BC5/BC7 from prepared RGBA8. BC6 requires an HDR-capable Compatibility driver; the current Linux driver rejects it and releases the host. GPU supports both BC6 roles on the exercised device. Integer sampling and ETC/EAC/ASTC remain separate dependencies. Foreign platforms and native allocation acceptance are unverified.

## Private source dependency

The runtime compiles BCnEncoder.NET 2.3.0 at `51d3199b3bc374e0bdda3947d9f347cb6235a9a0` and the required CommunityToolkit.HighPerformance 8.4.0 closure at `657c6971a8d42655c648336b781639ed96c2c49f` into Electron2D.dll. They add no managed runtime dependency DLL or public backend type.

`tools/image-bcn/update_sources.py --bcn <pinned-checkout> --toolkit <pinned-checkout>` reproduces the import; `--check` compares every delivered byte. Namespace/type visibility, target symbols, nullable/diagnostic policy, the Half identifier and two explicit writable Span casts are mechanical compilation adaptations. Codec algorithms are unchanged. Source manifests retain original and delivered SHA-256 hashes, pins and notices; `python3 -B tools/image-bcn/check_sources.py` verifies the shipped closure. The buffer-tool memory/enumerable/extension/buffer/stream closure compiles the selected upstream codec API without replacing its data structures.

## Slice choice

Before implementation, three candidates were compared. BC/DDS provides procedural authoring, asset loading, CPU decoding, resource archives and native texture storage, and unlocks KTX integration using the same selected source dependency. GPU timestamps need a real GPU timestamp producer in the pinned SDL GPU C API/binding; CPU timings cannot satisfy those counters. Window mouse pass-through needs native input-region integration for the active Wayland/X11 window system; SDL hit testing alone does not deliver pass-through. BC/DDS was the achievable connected slice with the largest executable benefit.

## Verification

ImageBCnTests covers S3TC/BPTC channel roles, signed/unsigned HDR, 22 exact raw DDS roles, complete ragged mips, 16 independently specified legacy mask fixtures, packed DX10 and numeric FourCC fixtures, array/cube/volume selection, ownership, source/output budgets, corruption and observer failure, atomic save failures and fresh-process compressed texture/array archives. The unmodified BCnEncoder.Net 2.3.0 package independently decodes 24 mip payloads for all eight BC formats; this verifies the engine integration against the original package, not an independent algorithm implementation.

Native Linux Wayland tests compare compressed/decoded canvas and region pixels, update, Sprite/Atlas consumers and allocation-free retained frames. GPU tests additionally read exact compressed bytes for all mips and both array layers, verify the exported native format and disposal of borrowed array identities. The complete results and assembly/source digests are retained in ignored `bin/image-bcn-validation/2026-10-14/` on the primary checkout. The test evidence establishes the exercised desktop backend, not other target platforms or visual-quality/native allocator acceptance.

The broader default runtime run and isolated ELECTRON2D_TEST_AREA_CONTRACT=gpu repeat stopped in the existing PhysicsAreaFieldTests GPU point-gravity failure assertion (Expected AggregateException at PhysicsAreaFieldTests.cs:203), outside the BC/DDS slice. The complete runtime suite is therefore not a passed gate. Its source test is unchanged from main c30b97ee; the image contract profile separately invokes the complete existing VerifyImages checks. Investigation of the Area failure remains in the physics scope; it is not evidence of BC/DDS or full-engine acceptance.
