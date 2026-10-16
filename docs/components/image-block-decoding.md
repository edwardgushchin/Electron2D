# ETC/EAC and ASTC pixel decoding

Last updated: 2026-10-16

`Image.Decompress` executes all 22 stored block formats, retaining the complete
mip chain. The [BC decoder](image-bcn.md) supplies nine roles. The shared private
KTX bridge supplies the thirteen ETC/EAC/ASTC roles below. Encoding those new
families remains a separate authoring capability.

| Source | Decoded storage | Channel behavior |
| --- | --- | --- |
| Etc, Etc2Rgb8 | Rgba8 | RGB, opaque alpha; ETC2 individual/differential/T/H/planar modes |
| Etc2Rgba8, Etc2Rgb8A1 | Rgba8 | Full or punchthrough alpha |
| Etc2R11, Etc2Rg11 | Rgba8 | Unsigned R/RG, 11-to-8-bit truncation; other colors zero, alpha one |
| Etc2R11S, Etc2Rg11S | Rgbaf | Signed R/RG normalized to [-1,1]; other colors zero, alpha one |
| Etc2RaAsRg | Rgba8 | R and stored alpha become RG; blue zero, alpha one |
| Astc4X4, Astc8X8 | Rgba8 | LDR RGBA with the stated block footprint |
| Astc4X4Hdr, Astc8X8Hdr | Rgbah | HDR RGBA, extended range and specified error-block NaNs |

Reserved signed EAC base -128 is normalized to -127. Signed EAC is an accepted
executable extension where the pinned comparison decoder has no implementation.
Invalid ETC1 differential colors fail explicitly. ASTC error blocks use the codec's
specified color/nonfinite result rather than reporting the whole image as corrupt.

Exact input lengths, ragged block edges, dimensions and every stored mip are
validated. Source bytes and conservative RGBA-float expansion must each fit
64 MiB before decoded allocation. A failed later mip leaves the published Image
unchanged; successful publication emits Changed once outside locks. Repeating
Decompress on uncompressed pixels is silent. Other pixel processing still requires
explicit decompression.

ImageTexture, TextureArray, CompressedTextureArray and RenderingServer use the same
prepared pixel snapshots. Their source reads and archives preserve original blocks.
GPU storage for these families currently uses decoded RGBA8 or RGBA32Float;
native compressed GPU support remains limited to the existing BC capability path.
Compatibility accepts the byte roles. The exercised Linux driver rejects signed
EAC and HDR ASTC floating-point texture precision and releases the host cleanly.
KTX1/KTX2 signed EAC and punchthrough identities are recognized; KTX orientation
and channel transforms decode every mip when needed. WebP saving also consumes
the common decoded snapshot without changing source data.

## Native source

The pinned detex ISC subset is decompress-etc.c, decompress-eac.c, clamp.c and its
headers, at commit cab11584d9be140602a66fd9a88ef0b99f08a97a; archive SHA-256 is
6d5e7c61fa1e1f8c064d5cc4aa024700bf7c2d59d58cf87e4dc91e7f4faa0ff3.
Exact-preimage generated copies replace negative signed shifts with multiplication
and signed selector shifts with unsigned masks. The downloaded source is immutable.
The independent pixel corpus remains unchanged after these arithmetic corrections.

ASTC uses scalar Arm astcenc from the already selected KTX archive, with a
single-thread decompression-only context per call. KTX's existing static archive
already contains its complete ASTC closure. No additional managed DLL is needed.
The fifth private export is e2d_block_decode; the four container ownership exports
retain their contract. Its half output requires two-byte alignment, checked before
calling astcenc. Managed image buffers and mip offsets satisfy that requirement.

## Executed checks and limits

ImageBlockTests checks thirteen complete independent mip oracles, source ownership,
prepared textures/arrays, imported resources, KTX transformations, WebP, fresh-process
archives, observer errors and atomic decoder/budget failures. ETC byte oracles use
AssetRipper.TextureDecoder at 13b4f6ee4906877402ab69fc5a0b51b83c15db44;
signed EAC uses explicit numerical vectors. ASTC uses its standalone CLI and the
independent OpenEXR reader. The recipe records tool hashes and retains only the
small encoded/pixel corpus in Git.

The native corpus checks twelve decoder modes with all mips, exact buffer sizes,
unaligned borrowed input, output guards, 9216 block mutations and 1152 concurrent
calls. The same corpus passes the merged static ASan/UBSan/leak build. RA-as-RG
swizzling is exercised at the managed boundary. Linux Wayland GPU tests compare
all thirteen ordinary and imported-array pixel pairs with independent pixels,
sample array layers/mips, and read back actual decoded storage. Compatibility
checks nine byte roles and all four explicit precision rejections. Each successful
host completes 24 warmup and 64 active frames with zero owner-thread managed bytes.
Foreign execution, native allocator totals, refreshed public bundles and owner
acceptance require separate evidence.

Fresh source, ProjectReference and NuGet consumers execute all thirteen public block roles with native compilers blocked. A separate NativeAOT package consumer executes the same decoder/texture/array flow. A self-contained managed Linux publish passes the 72-ELF and 92-notice audits; AOT retains all 92 notices byte-for-byte. These local checks do not establish foreign execution or public package availability.

The generated detex header selects __forceinline for the MSVC producer and retains the original inline spelling for other compilers. Source copies include that header consistently; this mechanical adaptation does not change decoder algorithms. Windows execution still requires its own verification.
