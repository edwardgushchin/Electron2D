# KTX images

Last updated: 2026-10-16

This component loads flat KTX1 and KTX2 assets through `Image.LoadKTXFromBuffer`, `Image.Load`, `Image.LoadFromFile` and synchronous or threaded `ResourceLoader` image-texture loading. It follows [ADR 0039](../decisions/resources.md#adr-0039) and the private native ownership rules of [ADR 0012](../decisions/product.md#adr-0012).

## Behavior and ownership

Input is copied before decoding. A private native owner validates and materializes the complete image, then copies every mip into managed Image storage. Its source memory may retire before level copies. The owner is released on success, decoding failure, transformation failure and disposal. Only the final managed state is published; failure preserves pixels, metadata and change notifications. Subscriber exceptions occur after publication.

A flat two-dimensional image may have a base level or a complete mip chain. Cube, array and volume containers are rejected by this Image role. Raw byte, packed, unsigned 16-bit, half, float and supported block layouts preserve their channel/mip bytes. KTX1 row padding is removed at every level, including ragged RGB mips. Opposite-endian KTX1 samples become canonical little-endian Image bytes. Legacy luminance and luminance-alpha header roles are normalized for the reader while preserving Image L8/La8 semantics.

KTX2 Basis ETC1S, UASTC LDR, UASTC HDR 4x4 and HDR 6x6 intermediate payloads transcode to portable RGBA8 or RGB half pixels without an active rendering device. This keeps loaded assets usable through the existing texture pipeline on each backend. RGB8 sRGB pixels convert to linear bytes, including their mips; alpha remains unchanged. Orientation and channel swizzles apply to every level before publication. BC data is decoded when a transformation requires pixels; an unavailable ETC/EAC/ASTC transformation fails explicitly. Raw storage for those families does not claim an executable block decoder or renderer path.

Source, managed output, native materialization and conservative RGBA-float expansion are each bounded to 64 MiB. Key/value metadata is bounded to 1 MiB and 4096 entries. Header roles, complete levels, metadata/DFD ranges and sample bit fields, non-overlapping KTX2 sections, Basis global data and slice ranges are checked before native decoding. Invalid sources use `InvalidDataException`; unsupported roles/formats use `NotSupportedException`. Decoder allocation failures use `IOException`.

## Native source and delivery

The private three-export ABI is `e2d_ktx_create`, `e2d_ktx_level` and `e2d_ktx_destroy`. KTX-Software 5.0.0-rc2 is pinned to commit `16059fa93d834242a0aaed9bb54952d76b143fac` and archive SHA-256 `bce6794bca57d86de2f00683ba8eaa6b3e8875d5abe45ac1136e206eb68aa476`. Stable 4.4.2 predates the required HDR transcoder. The read-only build disables platform uploads, tools and optional SIMD requirements; the optional Ericsson ETC software decoder is disabled. Selected Basis, DFD, miniz, ASTC and Zstandard sources remain private.

The pinned reader ignored failed Basis palette/table decoding and could dereference empty tables on corrupt content. `tools/ktx-native/decoder-guards.cmake` applies an exact-preimage error-propagation correction to generated build source, preserving the fetched archive. Regression fixtures exercise malformed codebooks and invalid DFD sample lengths. The Zstandard amalgamation and declarations receive a mechanical private symbol namespace so the static closure can coexist with the font codecs. Neither adaptation changes compression/transcoding algorithms. Original notices are retained in `licence/`.

Desktop, Windows, Android, Apple and Web producers require the private bridge. Static profiles consume one archive with its codec closure; codec/merge changes force that archive to refresh. An independent link uses only the merged archive, and packaging rejects global codec symbols that could collide with the font closure. Ordinary engine/game builds restore native assets without invoking a native compiler. The compiled managed API contains no codec type, pointer or backend handle.

## Verification and limits

`ImageKTXTests` checks nine independent standalone-tool KTX2 pixel oracles, 25 KTX1 raw formats, 17 opaque block roles, all stored mips, row padding, 16/32-bit opposite endian, orientation, swizzles, sRGB, atomic failure, concurrent decoder initialization, threaded resource loading and cache identity. Native checks retire borrowed source memory before level copies, reject every oracle truncation, mutate 2304 headers and 2304 complete containers, and exercise malformed codebook/DFD regressions. ASan, UBSan and leak checks use the same corpus. Clang's function-type sanitizer is disabled only for the upstream C vtable target because that ABI erases derived object pointer types; address and other undefined-behavior instrumentation remain enabled.

Linux Wayland GPU and Compatibility hosts compare rendered pixels with independent decoded Image textures for all nine oracles plus BC source data. GPU retains half precision; Compatibility explicitly converts HDR to RGBA8. These checks do not establish ETC/EAC/ASTC sampling, foreign-target execution, native package publication or owner acceptance. Fresh Linux source/project/NuGet consumers and self-contained publishes execute public Basis LDR/HDR loading with native compilers blocked. A separate NativeAOT application executes the same public codec consumers. The managed self-contained publish passes the 72-ELF and 91-notice audits. Compressed array resources remain a separate consumer slice: GSTL parsing, legacy .basis channel roles, resource lifecycle/cache/archive integration and actual array layer/mip sampling are still required.
