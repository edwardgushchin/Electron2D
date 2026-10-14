# OpenEXR image I/O

Last updated: 2026-10-14

`Image.LoadEXRFromBuffer` and `.exr` file loading preserve flat HDR image channels and precision. `SaveEXRToBuffer(bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)` and `SaveEXR` encode the stable base level with lossless PIZ compression, independently of the renderer.

## Pixels and options

Saving accepts exactly the R/RG/RGB/RGBA byte, half and float families: twelve source formats. Byte samples normalize into HALF channels; HALF and FLOAT sources retain their precision. Other formats reject. Saving never changes source pixels, full mip chain or events. Multichannel grayscale requests reject; scalar grayscale writes `Y`, while ordinary scalar data writes `R`.

`colorImage` clamps negative stored components to zero. A nonnegative `maxLinearValue` caps all stored components, including alpha; negative values disable that cap. These options also apply to half samples before rounding. NaN bounds reject. Infinite bounds retain the corresponding clamp semantics. Unclamped raw float data preserves negative values, negative zero, nonfinite values and extrema. These are data options, without an implicit transfer-function conversion.

Loading supports scanline and tiled flat images, reads the base level and omits mipmaps. Names are case-sensitive. `R`, `RG`, `RGB` and `RGBA` project to matching Image formats; `Y` expands into RGB and optional alpha. UINT samples convert numerically to FLOAT. Presence of a HALF source channel selects half output, including mixed-precision files; otherwise output is float. A missing red/luminance channel or incomplete RGB without alpha rejects. An alpha-bearing sparse image fills absent G/B with zero. The mature codec supplies channel sampling reconstruction. Deep and multipart containers do not represent one flat Image and reject, as do unavailable compression profiles.

Metadata, materialized channel data, canonical float conversion and emitted bytes have pre-allocation limits. Encoded input, decoded storage and output are individually bounded to 64 MiB. Dimensions also follow Image's storage ceilings; texture dimensions remain owned by the renderer. Decode failure preserves state; successful commit emits one `Changed` event, whose observers run after publication. File saving encodes first and uses the existing atomic replacement writer; the parent directory must exist.

## Runtime and source ownership

The private TinyEXR.NET codec at `b57d68083c443da538a71410ac181cd232127f0a` compiles into `Electron2D.dll`. Its source archive SHA-256 is `4071d5dc05448e0c613e6a13a4fe88564fcccc017264716181da324a7359a5a8`. The selected net8-compatible path uses the BCL, without a new native library or managed backend assembly.

`src/Vendor/EXR/manifest.json` retains original and adapted source fingerprints. Namespace/type visibility and warning directives are packaging adaptations. The private namespace sits outside the public namespace so original BCL roles cannot bind to engine types. Original MIT and underlying TinyEXR/OpenEXR BSD notices ship through `licence/`.

The B44 raw-block fast path directly copies native small edge-tile fallbacks into the caller's buffer, avoiding temporary channel planes and codec work. The shared legacy block path handles the same case. This allocation/decoder correction is submitted in [TinyEXR.NET #6](https://github.com/ksgfk/TinyEXR.NET/pull/6), with 252 passing tests in each upstream target path. The writer also uses canonical raw fallbacks when B44 encoding is no smaller. Other algorithms remain unchanged.

ImageTexture and resource archives retain copied half/float Image data. The programmable GPU path samples float storage. The current Compatibility driver rejects float textures when it cannot retain their precision; callers may explicitly convert a copied image to RGBA8 to render its visible range. EXR encoding and decoding remain available for CPU authoring on either renderer. Rendered RGBA8 captures can save to half EXR and restore their visible bytes.

## Verification limits

ImageEXRTests exercises all twelve save roles, raw and monochrome data, clamp options, nonfinite values, source/mip/event preservation, limits, malformed/unsupported/disposed inputs, concurrency, files and resources. The independent native OpenEXR oracle covers 192 combinations of precision, channel count, scanline/tile storage, compression and negative data-window origin. Independent OpenEXR also decodes all twelve saved role vectors exactly. Native tests exercise GPU sampling/capture, explicit Compatibility RGBA8 conversion and actual float-precision rejection/cleanup.

These checks establish local Linux behavior. Foreign runtime execution, NativeAOT and actual HDR display output remain unverified under ADR 0021. Source integration and local package validation do not imply publication or owner acceptance.
